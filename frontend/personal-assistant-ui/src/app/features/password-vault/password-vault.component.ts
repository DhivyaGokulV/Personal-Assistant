import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { PasswordVaultApi } from './password-vault.api';
import { EncryptedField, PasswordEntry, PasswordGroup, VaultStatus } from './password-vault.models';

const ITERATIONS = 310000;
const RECOVERY_ITERATIONS = 210000;
const VERIFIER = 'personal-assistant-vault-verifier';

function today(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

@Component({
  selector: 'app-password-vault',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="container py-4 module-passwords">
      <a routerLink="/home" class="text-muted-soft small">Back home</a>
      <h1 class="page-title mt-2">Passwords</h1>
      <div class="security-note surface p-3 mb-3">
        Credentials are encrypted in this browser before they are sent to the API. The API never stores your master password; if you forget it, use your recovery PIN to set a new one.
      </div>

      @if (!status()?.isInitialized) {
        <form class="surface p-3 vault-form" [formGroup]="setupForm" (ngSubmit)="initialize()">
          <h2 class="section-title">Create vault</h2>
          <input type="password" class="form-control" placeholder="Master password" formControlName="password" />
          @if (setupForm.controls.password.touched && setupForm.controls.password.invalid) { <div class="invalid-hint">Master password is required and must be at least 8 characters.</div> }
          <input type="password" class="form-control" placeholder="Recovery PIN" formControlName="pin" />
          @if (setupForm.controls.pin.touched && setupForm.controls.pin.invalid) { <div class="invalid-hint">Recovery PIN is required and must be at least 6 characters.</div> }
          <input type="password" class="form-control" placeholder="Confirm recovery PIN" formControlName="confirmPin" />
          <button class="btn-neon">Create</button>
          @if (vaultError()) { <div class="alert alert-danger py-1 px-2 small mb-0">{{ vaultError() }}</div> }
        </form>
      } @else if (!unlocked()) {
        <form class="surface p-3 vault-form" [formGroup]="unlockForm" (ngSubmit)="unlock()">
          <h2 class="section-title">Unlock vault</h2>
          <input type="password" class="form-control" placeholder="Master password" formControlName="password" />
          <button class="btn-neon">Unlock</button>
          @if (canRecover()) { <button class="btn-link-soft text-start" type="button" (click)="toggleReset()">Forgot master password?</button> }
          @if (vaultError()) { <div class="alert alert-danger py-1 px-2 small mb-0">{{ vaultError() }}</div> }
        </form>
        @if (showReset()) {
          <form class="surface p-3 vault-form mt-3" [formGroup]="resetForm" (ngSubmit)="resetMasterPassword()">
            <h2 class="section-title">Reset master password</h2>
            <input type="password" class="form-control" placeholder="Recovery PIN" formControlName="pin" />
            <input type="password" class="form-control" placeholder="New master password" formControlName="password" />
            <input type="password" class="form-control" placeholder="Confirm new master password" formControlName="confirmPassword" />
            <button class="btn-neon">Reset and unlock</button>
            <div class="text-muted-soft small">Your old master password cannot be shown or recovered. The PIN only lets this browser re-wrap your vault key with a new master password.</div>
          </form>
        }
      } @else {
        <div class="d-flex gap-2 align-items-center mb-3 flex-wrap">
          <button class="btn-neon btn-sm" (click)="openGroup()">+ Group</button>
          <button class="btn-neon btn-sm" (click)="openEntry()">+ Entry</button>
          <select class="form-select form-select-sm slim" [(ngModel)]="groupFilter" (change)="loadEntries()"><option value="">All groups</option>@for (g of groups(); track g.id) { <option [value]="g.id">{{ g.name }}</option> }</select>
          <input class="form-control form-control-sm slim" [(ngModel)]="search" (keyup.enter)="loadEntries()" placeholder="Search entries" />
          <button class="btn-link-soft btn-sm" (click)="loadEntries()">Search</button>
          <button class="btn-link-soft btn-sm ms-auto" (click)="lock()">Lock</button>
        </div>

        @if (groupOpen()) {
          <form class="surface p-3 mb-3 form-row" [formGroup]="groupForm" (ngSubmit)="saveGroup()">
            <input class="form-control form-control-sm" placeholder="Group name *" formControlName="name" />
            <input class="form-control form-control-sm" placeholder="Description" formControlName="description" />
            <button class="btn-neon btn-sm">Save</button><button class="btn-link-soft btn-sm" type="button" (click)="groupOpen.set(false)">Cancel</button>
            @if (groupError()) { <div class="alert alert-danger py-1 px-2 small mb-0 w-100">{{ groupError() }}</div> }
          </form>
        }

        @if (entryOpen()) {
          <form class="surface p-3 mb-3 grid-form" [formGroup]="entryForm" (ngSubmit)="saveEntry()">
            <select class="form-select form-select-sm" formControlName="groupId">@for (g of groups(); track g.id) { <option [value]="g.id">{{ g.name }}</option> }</select>
            <input class="form-control form-control-sm" placeholder="Entry name *" formControlName="name" />
            <input class="form-control form-control-sm" placeholder="Username" formControlName="username" />
            <input class="form-control form-control-sm" placeholder="Email" formControlName="email" />
            <input type="password" class="form-control form-control-sm" placeholder="Password" formControlName="password" />
            <input type="date" class="form-control form-control-sm" formControlName="createdDate" />
            <input type="date" class="form-control form-control-sm" formControlName="updatedDate" />
            <button class="btn-neon btn-sm">Save</button><button class="btn-link-soft btn-sm" type="button" (click)="entryOpen.set(false)">Cancel</button>
            @if (entryError()) { <div class="alert alert-danger py-1 px-2 small mb-0 wide">{{ entryError() }}</div> }
          </form>
        }

        <div class="row g-3">
          <div class="col-md-3">
            <div class="surface p-3">
              <h2 class="section-title">Groups</h2>
              @for (g of groups(); track g.id) { <div class="group-line"><span>{{ g.name }} <em>{{ g.entryCount }}</em></span><span><button class="icon-btn" (click)="editGroup(g)">Edit</button><button class="icon-btn danger" (click)="deleteGroup(g)">Delete</button></span></div> }
            </div>
          </div>
          <div class="col-md-9">
            <div class="table-wrap surface">
              <table><thead><tr><th>Name</th><th>Group</th><th>Username</th><th>Email</th><th>Password</th><th>Dates</th><th></th></tr></thead><tbody>
                @for (e of decryptedEntries(); track e.id) {
                  <tr><td>{{ e.name }}</td><td>{{ e.groupName }}</td><td>{{ e.username || '-' }}</td><td>{{ e.email || '-' }}</td><td><span>{{ revealed()[e.id] ? e.password : masked(e.password) }}</span></td><td>{{ e.createdDate }}<br><span class="small text-muted-soft">{{ e.updatedDate || '-' }}</span></td><td><button class="icon-btn" (click)="toggleReveal(e.id)">Reveal</button><button class="icon-btn" (click)="copy(e.password)">Copy</button><button class="icon-btn" (click)="editEntry(e.raw, e)">Edit</button><button class="icon-btn danger" (click)="deleteEntry(e.raw)">Delete</button></td></tr>
                  @if (e.history.length) { <tr><td colspan="7" class="history">History: @for (h of e.history; track h.id) { <span>{{ h.changeDate }}: {{ masked(h.previousValue) }}</span> }</td></tr> }
                }
              </tbody></table>
            </div>
          </div>
        </div>
      }
    </section>
  `,
  styles: [`
    .page-title { font-size: 1.5rem; font-weight: 600; }
    .security-note { border-color: var(--neon-cyan); color: var(--fg-muted); }
    .section-title { font-size: .95rem; font-weight: 600; }
    .vault-form { max-width: 420px; display: grid; gap: .75rem; }
    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: .3rem .7rem; border-radius: var(--radius-sm); }
    .invalid-hint { color: var(--danger); font-size: .78rem; margin-top: -.45rem; }
    .form-row, .grid-form { display: flex; gap: .5rem; flex-wrap: wrap; align-items: center; }
    .grid-form > * { min-width: 160px; flex: 1; }
    .wide { width: 100%; }
    .slim { max-width: 220px; }
    .table-wrap { overflow: auto; }
    table { width: 100%; border-collapse: collapse; font-size: .86rem; }
    th, td { padding: .5rem .7rem; border-bottom: 1px solid var(--border); vertical-align: top; }
    .icon-btn { border: 0; background: transparent; color: var(--neon-cyan); }
    .icon-btn.danger { color: var(--danger); }
    .group-line { display: flex; justify-content: space-between; border-bottom: 1px solid var(--border); padding: .45rem 0; }
    .group-line em { color: var(--fg-muted); font-style: normal; }
    .history { color: var(--fg-muted); display: flex; gap: 1rem; flex-wrap: wrap; }
  `]
})
export class PasswordVaultComponent {
  private readonly api = inject(PasswordVaultApi);
  private readonly fb = inject(FormBuilder);

  readonly status = signal<VaultStatus | null>(null);
  readonly unlocked = signal(false);
  readonly groups = signal<PasswordGroup[]>([]);
  readonly entries = signal<PasswordEntry[]>([]);
  readonly decryptedEntries = signal<any[]>([]);
  readonly revealed = signal<Record<string, boolean>>({});
  readonly vaultError = signal<string | null>(null);
  readonly groupError = signal<string | null>(null);
  readonly entryError = signal<string | null>(null);
  readonly groupOpen = signal(false);
  readonly entryOpen = signal(false);
  readonly showReset = signal(false);
  readonly canRecover = computed(() => {
    const s = this.status();
    return !!(s?.recoverySalt && s.recoveryVerifierCipherText && s.recoveryVerifierIv && s.recoveryWrappedKeyCipherText && s.recoveryWrappedKeyIv && s.recoveryKdfIterations);
  });

  key: CryptoKey | null = null;
  groupFilter = '';
  search = '';
  editingGroup: string | null = null;
  editingEntry: string | null = null;
  previousPassword = '';

  unlockForm = this.fb.group({ password: ['', Validators.required] });
  setupForm = this.fb.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    pin: ['', [Validators.required, Validators.minLength(6)]],
    confirmPin: ['', Validators.required]
  });
  resetForm = this.fb.group({
    pin: ['', [Validators.required, Validators.minLength(6)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  });
  groupForm = this.fb.group({ name: ['', Validators.required], description: [''] });
  entryForm = this.fb.group({ groupId: ['', Validators.required], name: ['', Validators.required], username: [''], email: [''], password: [''], createdDate: [today(), Validators.required], updatedDate: [''] });

  constructor() { this.loadStatus(); }

  async loadStatus() { this.status.set(await firstValueFrom(this.api.status())); }

  async initialize() {
    this.vaultError.set(null);
    try {
      this.setupForm.markAllAsTouched();
      if (this.setupForm.invalid) { this.vaultError.set('Complete the required fields before creating the vault.'); return; }
      const password = this.setupForm.value.password ?? '';
      const pin = this.setupForm.value.pin ?? '';
      if (pin !== this.setupForm.value.confirmPin) { this.vaultError.set('Recovery PIN confirmation does not match.'); return; }

      const salt = randomBase64(16);
      const recoverySalt = randomBase64(16);
      this.key = await generateVaultKey();
      const masterKey = await deriveKey(password, salt, ITERATIONS);
      const recoveryKey = await deriveKey(pin, recoverySalt, RECOVERY_ITERATIONS);
      const verifier = await encryptText(this.key, VERIFIER);
      const masterWrapped = await wrapVaultKey(this.key, masterKey);
      const recoveryWrapped = await wrapVaultKey(this.key, recoveryKey);
      const recoveryVerifier = await encryptText(recoveryKey, VERIFIER);

      this.status.set(await firstValueFrom(this.api.initialize({
        salt,
        verifierCipherText: verifier.cipherText,
        verifierIv: verifier.iv,
        kdfIterations: ITERATIONS,
        masterWrappedKeyCipherText: masterWrapped.cipherText,
        masterWrappedKeyIv: masterWrapped.iv,
        recoverySalt,
        recoveryVerifierCipherText: recoveryVerifier.cipherText,
        recoveryVerifierIv: recoveryVerifier.iv,
        recoveryWrappedKeyCipherText: recoveryWrapped.cipherText,
        recoveryWrappedKeyIv: recoveryWrapped.iv,
        recoveryKdfIterations: RECOVERY_ITERATIONS
      })));
      this.unlocked.set(true);
      await this.loadVault();
    } catch (e: any) {
      this.vaultError.set(e?.error?.message ?? 'Vault setup failed.');
    }
  }

  async unlock() {
    this.vaultError.set(null);
    try {
      const s = this.status()!;
      const masterKey = await deriveKey(this.unlockForm.value.password ?? '', s.salt!, s.kdfIterations!);
      this.key = s.masterWrappedKeyCipherText && s.masterWrappedKeyIv
        ? await unwrapVaultKey({ cipherText: s.masterWrappedKeyCipherText, iv: s.masterWrappedKeyIv }, masterKey)
        : masterKey;
      const value = await decryptText(this.key, { cipherText: s.verifierCipherText!, iv: s.verifierIv! });
      if (value !== VERIFIER) throw new Error('Bad password');
      this.unlocked.set(true);
      await this.loadVault();
    } catch {
      this.vaultError.set('Master password is incorrect.');
    }
  }

  async resetMasterPassword() {
    this.vaultError.set(null);
    try {
      this.resetForm.markAllAsTouched();
      if (!this.canRecover()) { this.vaultError.set('Recovery PIN is not configured for this vault.'); return; }
      if (this.resetForm.invalid) { this.vaultError.set('Recovery PIN and new master password are required.'); return; }
      if (this.resetForm.value.password !== this.resetForm.value.confirmPassword) { this.vaultError.set('New master password confirmation does not match.'); return; }

      const s = this.status()!;
      const recoveryKey = await deriveKey(this.resetForm.value.pin ?? '', s.recoverySalt!, s.recoveryKdfIterations!);
      const recoveryVerifier = await decryptText(recoveryKey, { cipherText: s.recoveryVerifierCipherText!, iv: s.recoveryVerifierIv! });
      if (recoveryVerifier !== VERIFIER) throw new Error('Bad PIN');
      this.key = await unwrapVaultKey({ cipherText: s.recoveryWrappedKeyCipherText!, iv: s.recoveryWrappedKeyIv! }, recoveryKey);

      const salt = randomBase64(16);
      const masterKey = await deriveKey(this.resetForm.value.password ?? '', salt, ITERATIONS);
      const verifier = await encryptText(this.key, VERIFIER);
      const masterWrapped = await wrapVaultKey(this.key, masterKey);
      this.status.set(await firstValueFrom(this.api.resetMasterPassword({
        salt,
        verifierCipherText: verifier.cipherText,
        verifierIv: verifier.iv,
        kdfIterations: ITERATIONS,
        masterWrappedKeyCipherText: masterWrapped.cipherText,
        masterWrappedKeyIv: masterWrapped.iv
      })));

      this.unlocked.set(true);
      this.showReset.set(false);
      this.resetForm.reset();
      this.unlockForm.reset();
      await this.loadVault();
    } catch {
      this.key = null;
      this.vaultError.set('Recovery PIN is incorrect or the vault recovery data is unavailable.');
    }
  }

  toggleReset() { this.vaultError.set(null); this.showReset.update(v => !v); }
  lock() { this.key = null; this.unlocked.set(false); this.decryptedEntries.set([]); this.unlockForm.reset(); this.resetForm.reset(); }
  async loadVault() { const [groups] = await Promise.all([this.loadGroups(), this.loadEntries()]); return groups; }
  async loadGroups() { this.groups.set(await firstValueFrom(this.api.groups())); }
  async loadEntries() {
    this.entries.set(await firstValueFrom(this.api.entries({ groupId: this.groupFilter || undefined, search: this.search || undefined })));
    await this.decryptEntries();
  }
  async decryptEntries() {
    if (!this.key) return;
    const rows = [];
    for (const e of this.entries()) {
      rows.push({ ...e, raw: e, username: await this.decryptField(e.username), email: await this.decryptField(e.email), password: await this.decryptField(e.password), history: await Promise.all(e.history.map(async h => ({ ...h, previousValue: await this.decryptField(h.previousPassword) }))) });
    }
    this.decryptedEntries.set(rows);
  }
  async decryptField(f: EncryptedField | null): Promise<string> { return f && this.key ? decryptText(this.key, f) : ''; }
  openGroup() { this.editingGroup = null; this.groupError.set(null); this.groupForm.reset({ name: '', description: '' }); this.groupOpen.set(true); }
  editGroup(g: PasswordGroup) { this.editingGroup = g.id; this.groupForm.patchValue(g as any); this.groupOpen.set(true); }
  async saveGroup() {
    const body = this.groupForm.getRawValue();
    if (!body.name?.trim()) { this.groupError.set('Group name is required.'); return; }
    this.editingGroup ? await firstValueFrom(this.api.updateGroup(this.editingGroup, body)) : await firstValueFrom(this.api.createGroup(body));
    this.groupOpen.set(false);
    await this.loadGroups();
  }
  async deleteGroup(g: PasswordGroup) { if (confirm(`Delete group ${g.name} and its entries?`)) { await firstValueFrom(this.api.deleteGroup(g.id)); await this.loadVault(); } }
  openEntry() { this.editingEntry = null; this.previousPassword = ''; this.entryError.set(null); this.entryForm.reset({ groupId: this.groups()[0]?.id ?? '', createdDate: today(), updatedDate: '' }); this.entryOpen.set(true); }
  editEntry(raw: PasswordEntry, plain: any) { this.editingEntry = raw.id; this.previousPassword = plain.password; this.entryForm.reset({ groupId: raw.groupId, name: raw.name, username: plain.username, email: plain.email, password: plain.password, createdDate: raw.createdDate, updatedDate: raw.updatedDate ?? today() }); this.entryOpen.set(true); }
  async saveEntry() {
    this.entryError.set(null);
    const f = this.entryForm.getRawValue();
    if (!f.name?.trim() || !f.groupId || (!f.username?.trim() && !f.email?.trim())) { this.entryError.set('Name and username or email are required.'); return; }
    if (!this.key) return;
    const body = {
      groupId: f.groupId, name: f.name.trim(), hasUsername: !!f.username?.trim(), hasEmail: !!f.email?.trim(),
      username: f.username?.trim() ? await encryptText(this.key, f.username.trim()) : null,
      email: f.email?.trim() ? await encryptText(this.key, f.email.trim()) : null,
      password: f.password ? await encryptText(this.key, f.password) : null,
      createdDate: f.createdDate, updatedDate: f.updatedDate || null
    };
    let saved: PasswordEntry;
    if (this.editingEntry) {
      saved = await firstValueFrom(this.api.updateEntry(this.editingEntry, body));
      if (this.previousPassword && this.previousPassword !== (f.password ?? '')) {
        await firstValueFrom(this.api.addHistory(saved.id, { changeDate: today(), previousPassword: await encryptText(this.key, this.previousPassword) }));
      }
    } else {
      saved = await firstValueFrom(this.api.createEntry(body));
    }
    this.entryOpen.set(false);
    await this.loadEntries();
    await this.loadGroups();
  }
  async deleteEntry(e: PasswordEntry) { if (confirm(`Delete ${e.name}?`)) { await firstValueFrom(this.api.deleteEntry(e.id)); await this.loadEntries(); await this.loadGroups(); } }
  toggleReveal(id: string) { this.revealed.update(v => ({ ...v, [id]: !v[id] })); }
  masked(value: string): string { return value ? '********' : '-'; }
  copy(value: string) { if (value) navigator.clipboard?.writeText(value); }
}

async function generateVaultKey(): Promise<CryptoKey> {
  return crypto.subtle.generateKey({ name: 'AES-GCM', length: 256 }, true, ['encrypt', 'decrypt']);
}

async function deriveKey(password: string, saltBase64: string, iterations: number): Promise<CryptoKey> {
  const enc = new TextEncoder();
  const material = await crypto.subtle.importKey('raw', enc.encode(password), 'PBKDF2', false, ['deriveKey']);
  return crypto.subtle.deriveKey({ name: 'PBKDF2', salt: fromBase64(saltBase64), iterations, hash: 'SHA-256' }, material, { name: 'AES-GCM', length: 256 }, false, ['encrypt', 'decrypt']);
}

async function wrapVaultKey(vaultKey: CryptoKey, wrappingKey: CryptoKey): Promise<EncryptedField> {
  const raw = new Uint8Array(await crypto.subtle.exportKey('raw', vaultKey));
  return encryptBytes(wrappingKey, raw);
}

async function unwrapVaultKey(field: EncryptedField, wrappingKey: CryptoKey): Promise<CryptoKey> {
  const raw = await decryptBytes(wrappingKey, field);
  return crypto.subtle.importKey('raw', raw, { name: 'AES-GCM', length: 256 }, true, ['encrypt', 'decrypt']);
}

async function encryptText(key: CryptoKey, value: string): Promise<EncryptedField> {
  return encryptBytes(key, new TextEncoder().encode(value));
}

async function encryptBytes(key: CryptoKey, value: Uint8Array): Promise<EncryptedField> {
  const iv = crypto.getRandomValues(new Uint8Array(12));
  const bytes = value.buffer.slice(value.byteOffset, value.byteOffset + value.byteLength) as ArrayBuffer;
  const data = await crypto.subtle.encrypt({ name: 'AES-GCM', iv }, key, bytes);
  return { cipherText: toBase64(new Uint8Array(data)), iv: toBase64(iv) };
}

async function decryptText(key: CryptoKey, field: EncryptedField): Promise<string> {
  const data = await decryptBytes(key, field);
  return new TextDecoder().decode(data);
}

async function decryptBytes(key: CryptoKey, field: EncryptedField): Promise<ArrayBuffer> {
  return crypto.subtle.decrypt({ name: 'AES-GCM', iv: fromBase64(field.iv) }, key, fromBase64(field.cipherText));
}

function randomBase64(bytes: number): string { return toBase64(crypto.getRandomValues(new Uint8Array(bytes))); }
function toBase64(bytes: Uint8Array): string { return btoa(String.fromCharCode(...bytes)); }
function fromBase64(value: string): ArrayBuffer {
  const bytes = Uint8Array.from(atob(value), c => c.charCodeAt(0));
  return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer;
}

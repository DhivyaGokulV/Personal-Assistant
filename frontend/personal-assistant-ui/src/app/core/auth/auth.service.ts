import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from './auth.models';

const STORAGE_KEY = 'pa.auth';

interface PersistedAuth {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/auth`;

  private readonly _state = signal<PersistedAuth | null>(this.read());

  readonly user = computed(() => this._state()?.user ?? null);
  readonly token = computed(() => this._state()?.token ?? null);
  readonly isAuthenticated = computed(() => {
    const s = this._state();
    if (!s) return false;
    return new Date(s.expiresAt).getTime() > Date.now();
  });

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/register`, req).pipe(
      tap(res => this.persist(res))
    );
  }

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login`, req).pipe(
      tap(res => this.persist(res))
    );
  }

  fetchProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.base}/me`);
  }

  logout(): void {
    this._state.set(null);
    try { localStorage.removeItem(STORAGE_KEY); } catch {}
  }

  private persist(res: AuthResponse): void {
    const data: PersistedAuth = { token: res.token, expiresAt: res.expiresAt, user: res.user };
    this._state.set(data);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(data)); } catch {}
  }

  private read(): PersistedAuth | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const parsed = JSON.parse(raw) as PersistedAuth;
      if (new Date(parsed.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return parsed;
    } catch {
      return null;
    }
  }
}

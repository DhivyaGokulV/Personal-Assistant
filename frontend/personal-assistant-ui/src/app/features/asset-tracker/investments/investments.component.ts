import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AssetTrackerApi } from '../asset-tracker.api';
import {
  AssetTag, Investment, InvestmentDetail, InvestmentEntry, InvestmentPriceEntry,
  InvestmentStatistics, InvestmentStatus, InvestmentTxType, PagedResult,
  fmtMoney, fmtUnits, isoToday
} from '../asset-tracker.models';
import { LineChartComponent } from '../shared/line-chart.component';

type HistoryTab = 'entries' | 'prices';
type Modal = 'investment' | 'status' | 'entry' | 'price' | 'statistics' | 'export' | null;
type ConfirmTarget = { kind: 'investment'; investment: Investment } |
  { kind: 'entry'; investment: Investment; entry: InvestmentEntry } |
  { kind: 'price'; investment: Investment; price: InvestmentPriceEntry };

@Component({
  selector: 'app-asset-tracker-investments',
  imports: [CommonModule, FormsModule, LineChartComponent],
  template: `
    <div class="d-flex flex-wrap align-items-start gap-3 mb-3">
      <div>
        <h2 class="section-title mb-1">Investments</h2>
        <p class="text-muted-soft small mb-0">Stocks, bonds, funds, deposits and other investments.</p>
      </div>
      <div class="ms-auto d-flex gap-2">
        <button class="btn-link-soft" type="button" (click)="openExport()">Export</button>
        <button class="btn-neon" type="button" (click)="openCreate()">Create Investment</button>
      </div>
    </div>

    <div class="surface filters mb-3">
      <input class="form-control form-control-sm search" placeholder="Search investments"
        aria-label="Search investments" [(ngModel)]="search" (ngModelChange)="scheduleLoad()" />
      <select class="form-select form-select-sm" aria-label="Status filter" [(ngModel)]="statusFilter" (ngModelChange)="pageNumber=1; load()">
        <option [ngValue]="1">Active</option><option [ngValue]="2">Inactive</option><option [ngValue]="0">All statuses</option>
      </select>
      <select class="form-select form-select-sm" aria-label="Type filter" [(ngModel)]="typeFilter" (ngModelChange)="pageNumber=1; load()">
        <option [ngValue]="0">All types</option><option [ngValue]="1">Unit based</option><option [ngValue]="2">Amount based</option>
      </select>
      <select class="form-select form-select-sm" aria-label="Tag filter" [(ngModel)]="tagFilter" (ngModelChange)="pageNumber=1; load()">
        <option value="">All tags</option>@for (tag of tags(); track tag.id) { <option [value]="tag.id">{{ tag.name }}</option> }
      </select>
      <select class="form-select form-select-sm" aria-label="Sort investments" [(ngModel)]="sortBy" (ngModelChange)="load()">
        <option value="Name">Name</option><option value="CreationDate">Creation date</option>
        <option value="CurrentValue">Current value</option><option value="ProfitLossPercent">Profit/loss</option><option value="Status">Status</option>
      </select>
      <button class="btn-link-soft" type="button" (click)="toggleSort()">{{ sortDirection === 'Asc' ? '↑ Asc' : '↓ Desc' }}</button>
    </div>

    @if (error()) { <div class="alert alert-danger small">{{ error() }}</div> }
    @if (loading()) {
      <div class="surface empty">Loading investments…</div>
    } @else if (page().items.length === 0) {
      <div class="surface empty">
        <h3>No investments found</h3>
        <p class="text-muted-soft">Create an investment or adjust the current filters.</p>
        <button class="btn-neon" type="button" (click)="openCreate()">Create Investment</button>
      </div>
    } @else {
      <div class="investment-list">
        @for (investment of page().items; track investment.id) {
          <article class="surface investment-card">
            <div class="investment-row">
              <div class="identity">
                <div class="d-flex align-items-center gap-2 flex-wrap">
                  <h3>{{ investment.name }}</h3>
                  <span class="badge-type">{{ investment.investmentType === 1 ? 'Unit based' : 'Amount based' }}</span>
                  <span class="status" [class.inactive]="investment.status === 2">{{ investment.status === 1 ? 'Active' : 'Inactive' }}</span>
                  @if (investment.tag) { <span class="tag">{{ investment.tag.name }}</span> }
                </div>
                @if (investment.description) { <p>{{ investment.description }}</p> }
                <span class="meta">Created {{ investment.creationDate }} · {{ investment.currencyCode }}</span>
              </div>
              <div class="metric">
                <span>{{ investment.investmentType === 1 ? 'Units held' : 'Amount invested' }}</span>
                <strong>{{ investment.investmentType === 1 ? fmtUnits(investment.units) : money(investment.amountInvested) }}</strong>
              </div>
              <div class="metric">
                <span>Current value</span><strong>{{ money(investment.currentValue) }}</strong>
                @if (investment.profitLossPercent !== null) {
                  <small [class.gain]="investment.profitLossPercent >= 0" [class.loss]="investment.profitLossPercent < 0">
                    {{ investment.profitLossPercent >= 0 ? '+' : '' }}{{ investment.profitLossPercent | number:'1.2-2' }}%
                  </small>
                }
              </div>
              <div class="actions" aria-label="Investment actions">
                <button type="button" title="Edit investment" (click)="openEdit(investment)">Edit</button>
                <button type="button" title="Change status" (click)="openStatus(investment)">Status</button>
                <button type="button" class="danger" title="Delete investment" (click)="confirmDeleteInvestment(investment)">Delete</button>
                <button type="button" title="Expand history" [attr.aria-expanded]="expandedId() === investment.id" (click)="toggleExpand(investment)">
                  {{ expandedId() === investment.id ? 'Collapse' : 'History' }}
                </button>
              </div>
            </div>

            @if (expandedId() === investment.id) {
              <div class="history">
                <div class="history-head">
                  <div class="history-tabs">
                    <button type="button" [class.active]="historyTab() === 'entries'" (click)="setHistoryTab('entries', investment)">
                      {{ investment.investmentType === 1 ? 'Buying / Selling' : 'Credit / Debit' }}
                    </button>
                    @if (investment.investmentType === 1) {
                      <button type="button" [class.active]="historyTab() === 'prices'" (click)="setHistoryTab('prices', investment)">Price History</button>
                    }
                  </div>
                  <div class="ms-auto d-flex gap-2">
                    <button class="btn-link-soft" type="button" (click)="openStatistics(investment)">View statistics</button>
                    <button class="btn-neon" type="button" (click)="historyTab() === 'entries' ? openEntry(investment) : openPrice(investment)">Add Entry</button>
                  </div>
                </div>

                @if (historyLoading()) { <div class="empty compact">Loading history…</div> }
                @else if (historyTab() === 'entries') {
                  @if (entries().items.length === 0) { <div class="empty compact">No entries yet.</div> }
                  @for (entry of entries().items; track entry.id) {
                    <div class="history-row">
                      <div><strong>{{ entryType(entry.type) }}</strong><span>{{ entry.date }}</span></div>
                      <div class="grow">{{ entry.note || '—' }}</div>
                      @if (investment.investmentType === 1) {
                        <div>{{ fmtUnits(entry.quantity) }} units</div><div>{{ money(entry.pricePerUnit) }} / unit</div>
                      } @else { <div>{{ money(entry.amount) }}</div> }
                      <div class="row-actions">
                        <button type="button" (click)="openEntry(investment, entry)">Edit</button>
                        <button type="button" class="danger" (click)="confirmDeleteEntry(investment, entry)">Delete</button>
                      </div>
                    </div>
                  }
                  <div class="pager compact-pager">
                    <button type="button" [disabled]="entries().page <= 1" (click)="loadEntries(investment, entries().page - 1)">Previous</button>
                    <span>Page {{ entries().page }} of {{ maxPage(entries()) }}</span>
                    <button type="button" [disabled]="entries().page >= maxPage(entries())" (click)="loadEntries(investment, entries().page + 1)">Next</button>
                  </div>
                } @else {
                  @if (prices().items.length === 0) { <div class="empty compact">No price entries yet.</div> }
                  @for (price of prices().items; track price.id) {
                    <div class="history-row">
                      <div><strong>{{ price.date }}</strong></div><div class="grow">{{ money(price.pricePerUnit) }} per unit</div>
                      <div class="row-actions">
                        <button type="button" (click)="openPrice(investment, price)">Edit</button>
                        <button type="button" class="danger" (click)="confirmDeletePrice(investment, price)">Delete</button>
                      </div>
                    </div>
                  }
                  <div class="pager compact-pager">
                    <button type="button" [disabled]="prices().page <= 1" (click)="loadPrices(investment, prices().page - 1)">Previous</button>
                    <span>Page {{ prices().page }} of {{ maxPage(prices()) }}</span>
                    <button type="button" [disabled]="prices().page >= maxPage(prices())" (click)="loadPrices(investment, prices().page + 1)">Next</button>
                  </div>
                }
                @if (detail(); as d) {
                  <div class="status-history"><strong>Status history:</strong>
                    @for (item of d.statusHistory; track item.id) {
                      <span>{{ item.effectiveDate }} — {{ item.status === 1 ? 'Active' : 'Inactive' }}</span>
                    }
                  </div>
                }
              </div>
            }
          </article>
        }
      </div>
      <div class="pager mt-3">
        <button type="button" [disabled]="page().page <= 1" (click)="goPage(page().page - 1)">Previous</button>
        <span>Page {{ page().page }} of {{ maxPage(page()) }} · {{ page().totalCount }} investments</span>
        <button type="button" [disabled]="page().page >= maxPage(page())" (click)="goPage(page().page + 1)">Next</button>
      </div>
    }

    @if (modal() !== null) {
      <div class="modal-backdrop-custom" (mousedown)="backdrop($event)">
        <section class="dialog surface" role="dialog" aria-modal="true" [attr.aria-label]="dialogTitle()">
          <header><div><span class="eyebrow">{{ modal() === 'investment' && investmentForm.id ? 'Edit' : 'Investment manager' }}</span><h3>{{ dialogTitle() }}</h3></div>
            <button type="button" class="close" aria-label="Close dialog" (click)="closeModal()">×</button></header>
          @if (modalError()) { <div class="alert alert-danger small">{{ modalError() }}</div> }

          @switch (modal()) {
            @case ('investment') {
              <div class="form-grid">
                <label class="wide">Name *<input class="form-control" maxlength="100" [(ngModel)]="investmentForm.name" /><small class="error">{{ nameError(investmentForm.name) }}</small></label>
                <label class="wide">Description<textarea class="form-control" rows="3" maxlength="500" [(ngModel)]="investmentForm.description"></textarea><small>{{ investmentForm.description.length }}/500</small></label>
                @if (!investmentForm.id) {
                  <label>Type *<select class="form-select" [(ngModel)]="investmentForm.investmentType"><option [ngValue]="1">Unit based</option><option [ngValue]="2">Amount based</option></select></label>
                  <label>Currency *<select class="form-select" [(ngModel)]="investmentForm.currencyCode" disabled><option value="INR">INR</option></select><small>More currencies will be available in Settings.</small></label>
                  <label>Creation date *<input type="date" class="form-control" [max]="today" [(ngModel)]="investmentForm.creationDate" /><small class="error">{{ dateError(investmentForm.creationDate) }}</small></label>
                }
                <label>Existing tag<select class="form-select" [(ngModel)]="investmentForm.tagId" [disabled]="!!investmentForm.newTagName">
                  <option [ngValue]="null">No tag</option>@for (tag of tags(); track tag.id) { <option [ngValue]="tag.id">{{ tag.name }}</option> }
                </select></label>
                @if (!investmentForm.id) {
                  <label>Or create tag<input class="form-control" maxlength="50" [(ngModel)]="investmentForm.newTagName" [disabled]="!!investmentForm.tagId" /><small>{{ investmentForm.newTagName.length }}/50</small></label>
                }
              </div>
              <footer><button class="btn-link-soft" type="button" (click)="closeModal()">Discard</button><button class="btn-neon" type="button" [disabled]="!investmentValid() || saving()" (click)="saveInvestment()">{{ saving() ? 'Saving…' : 'Save' }}</button></footer>
            }
            @case ('status') {
              <div class="form-grid">
                <label>Status *<select class="form-select" [(ngModel)]="statusForm.status"><option [ngValue]="1">Active</option><option [ngValue]="2">Inactive</option></select></label>
                <label>Effective date *<input type="date" class="form-control" [min]="selected()?.creationDate" [max]="today" [(ngModel)]="statusForm.effectiveDate" /><small class="error">{{ selected() ? boundedDateError(statusForm.effectiveDate, selected()!.creationDate) : '' }}</small></label>
              </div>
              <footer><button class="btn-link-soft" type="button" (click)="closeModal()">Cancel</button><button class="btn-neon" type="button" [disabled]="!statusValid() || saving()" (click)="saveStatus()">Confirm</button></footer>
            }
            @case ('entry') {
              <div class="form-grid">
                <label>Type *<select class="form-select" [(ngModel)]="entryForm.type">
                  @if (selected()?.investmentType === 1) { <option [ngValue]="1">Buy</option><option [ngValue]="2">Sell</option> }
                  @else { <option [ngValue]="3">Credit</option><option [ngValue]="4">Debit</option> }
                </select></label>
                <label>Date *<input type="date" class="form-control" [min]="selected()?.creationDate" [max]="today" [(ngModel)]="entryForm.date" /><small class="error">{{ selected() ? boundedDateError(entryForm.date, selected()!.creationDate) : '' }}</small></label>
                <label class="wide">Note<textarea class="form-control" maxlength="200" rows="2" [(ngModel)]="entryForm.note"></textarea><small>{{ entryForm.note.length }}/200</small></label>
                @if (selected()?.investmentType === 1) {
                  <label>Price per unit *<input type="number" class="form-control" min="0.0001" step="0.0001" [(ngModel)]="entryForm.pricePerUnit" /><small class="error">{{ positiveError(entryForm.pricePerUnit, 'Price') }}</small></label>
                  <label>Quantity *<input type="number" class="form-control" min="0.0001" step="0.0001" [(ngModel)]="entryForm.quantity" /><small class="error">{{ positiveError(entryForm.quantity, 'Quantity') }}</small></label>
                } @else {
                  <label>Amount *<input type="number" class="form-control" min="0.01" step="0.01" [(ngModel)]="entryForm.amount" /><small class="error">{{ positiveError(entryForm.amount, 'Amount') }}</small></label>
                }
              </div>
              <footer><button class="btn-link-soft" type="button" (click)="closeModal()">Discard</button><button class="btn-neon" type="button" [disabled]="!entryValid() || saving()" (click)="saveEntry()">Confirm</button></footer>
            }
            @case ('price') {
              <div class="form-grid">
                <label>Date *<input type="date" class="form-control" [min]="selected()?.creationDate" [max]="today" [(ngModel)]="priceForm.date" /><small class="error">{{ selected() ? boundedDateError(priceForm.date, selected()!.creationDate) : '' }}</small></label>
                <label>Price per unit *<input type="number" class="form-control" min="0" step="0.0001" [(ngModel)]="priceForm.pricePerUnit" /><small class="error">{{ nonNegativeError(priceForm.pricePerUnit) }}</small></label>
              </div>
              <footer><button class="btn-link-soft" type="button" (click)="closeModal()">Discard</button><button class="btn-neon" type="button" [disabled]="!priceValid() || saving()" (click)="savePrice()">Confirm</button></footer>
            }
            @case ('statistics') {
              <div class="d-flex align-items-end gap-2 mb-3">
                <label class="grow">Duration<select class="form-select" [(ngModel)]="statisticsDuration" (ngModelChange)="loadStatistics()">
                  <option value="OneMonth">1 month</option><option value="ThreeMonths">3 months</option><option value="SixMonths">6 months</option>
                  <option value="OneYear">1 year</option><option value="ThreeYears">3 years</option><option value="FiveYears">5 years</option>
                </select></label>
              </div>
              @if (statistics(); as chart) { <h4 class="chart-title">{{ chart.metric }}</h4><app-line-chart [series]="chart.points" /> }
              @else { <div class="empty compact">Loading statistics…</div> }
              <footer><button class="btn-link-soft" type="button" (click)="closeModal()">Close</button></footer>
            }
            @case ('export') {
              <div class="selection-actions"><button type="button" (click)="selectAll()">Select all</button><button type="button" (click)="exportSelection.clear()">Reset</button><span>{{ exportSelection.size }} selected</span></div>
              <div class="export-list">
                @for (item of exportOptions(); track item.id) {
                  <label><input type="checkbox" [checked]="exportSelection.has(item.id)" (change)="toggleExport(item.id)" /> {{ item.name }}</label>
                }
              </div>
              <div class="form-grid mt-3">
                <label>From *<input type="date" class="form-control" [max]="exportForm.to" [(ngModel)]="exportForm.from" /></label>
                <label>To *<input type="date" class="form-control" [max]="today" [(ngModel)]="exportForm.to" /></label>
                <label>Format<select class="form-select" [(ngModel)]="exportForm.format"><option value="Xlsx">Excel (.xlsx)</option><option value="Pdf">PDF (.pdf)</option></select></label>
              </div>
              <small class="error">{{ exportDateError() }}</small>
              <footer><button class="btn-link-soft" type="button" (click)="closeModal()">Cancel</button><button class="btn-neon" type="button" [disabled]="!exportValid() || saving()" (click)="exportData()">Generate report</button></footer>
            }
          }
        </section>
      </div>
    }

    @if (confirmTarget()) {
      <div class="modal-backdrop-custom">
        <section class="dialog confirm surface" role="alertdialog" aria-modal="true" aria-label="Confirm deletion">
          <header><h3>Confirm deletion</h3></header>
          <p>{{ confirmMessage() }}</p><p class="text-muted-soft small">This action is audited and cannot be undone from this screen.</p>
          <footer><button class="btn-link-soft" type="button" (click)="confirmTarget.set(null)">Cancel</button><button class="btn-danger-soft" type="button" [disabled]="saving()" (click)="performDelete()">Delete</button></footer>
        </section>
      </div>
    }
    @if (toast()) { <div class="toast-custom" role="status">{{ toast() }}</div> }
  `,
  styles: [`
    .section-title { font-size: 1.25rem; font-weight: 650; }
    .filters { padding: .8rem; display: grid; grid-template-columns: minmax(180px, 2fr) repeat(4, minmax(120px, 1fr)) auto; gap: .55rem; }
    .investment-list { display: grid; gap: .75rem; }
    .investment-card { overflow: hidden; }
    .investment-row { display: grid; grid-template-columns: minmax(260px, 2fr) minmax(120px, .7fr) minmax(130px, .8fr) auto; gap: 1rem; align-items: center; padding: 1rem; }
    .identity h3 { font-size: 1rem; margin: 0; }.identity p { margin: .3rem 0; font-size: .85rem; color: var(--fg-muted); }.meta { color: var(--fg-subtle); font-size: .72rem; }
    .badge-type,.status,.tag { border: 1px solid var(--border-strong); border-radius: 999px; padding: .12rem .45rem; font-size: .66rem; }.status { color: var(--neon); }.status.inactive { color: var(--fg-subtle); }.tag { color: var(--primary); }
    .metric { display: flex; flex-direction: column; }.metric span { color: var(--fg-muted); font-size: .7rem; text-transform: uppercase; }.metric strong { font-size: 1rem; }.gain { color: var(--success, #28c76f); }.loss { color: var(--danger); }
    .actions,.row-actions { display: flex; gap: .35rem; flex-wrap: wrap; }.actions button,.row-actions button,.pager button,.selection-actions button { background: transparent; color: var(--fg-muted); border: 1px solid var(--border); border-radius: var(--radius-sm); padding: .32rem .55rem; font-size: .75rem; }.actions button:hover,.row-actions button:hover { color: var(--neon); border-color: var(--neon); }.danger { color: var(--danger) !important; }
    .history { border-top: 1px solid var(--border); padding: .85rem 1rem 1rem; background: color-mix(in srgb, var(--surface) 93%, var(--neon) 7%); }.history-head { display: flex; gap: 1rem; align-items: center; margin-bottom: .7rem; }.history-tabs { display: flex; gap: .3rem; }.history-tabs button { border: 0; border-bottom: 2px solid transparent; color: var(--fg-muted); background: transparent; padding: .4rem .55rem; }.history-tabs button.active { color: var(--neon); border-color: var(--neon); }
    .history-row { display: flex; align-items: center; gap: 1rem; padding: .65rem .2rem; border-bottom: 1px solid var(--border); font-size: .8rem; }.history-row > div:first-child { display: flex; min-width: 105px; flex-direction: column; }.grow { flex: 1; }.status-history { display: flex; gap: .6rem; flex-wrap: wrap; padding-top: .8rem; font-size: .72rem; color: var(--fg-muted); }
    .empty { padding: 2.5rem 1rem; text-align: center; color: var(--fg-muted); }.empty h3 { color: var(--fg); font-size: 1.05rem; }.empty.compact { padding: 1.2rem; }.pager { display: flex; align-items: center; justify-content: center; gap: .8rem; color: var(--fg-muted); font-size: .75rem; }.pager button:disabled { opacity: .4; }.compact-pager { padding-top: .7rem; }
    .btn-link-soft,.btn-neon,.btn-danger-soft { padding: .42rem .75rem; border-radius: var(--radius-sm); font-size: .8rem; }.btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); }.btn-danger-soft { border: 1px solid var(--danger); background: transparent; color: var(--danger); }
    .modal-backdrop-custom { position: fixed; inset: 0; z-index: 1050; background: rgba(0,0,0,.62); display: grid; place-items: center; padding: 1rem; }.dialog { width: min(680px, 100%); max-height: calc(100vh - 2rem); overflow: auto; padding: 1.1rem; box-shadow: 0 20px 70px rgba(0,0,0,.45); }.dialog.confirm { width: min(440px, 100%); }.dialog header { display: flex; align-items: start; justify-content: space-between; margin-bottom: 1rem; }.dialog h3 { font-size: 1.15rem; margin: 0; }.close { border: 0; background: transparent; color: var(--fg-muted); font-size: 1.5rem; }.eyebrow { color: var(--neon); font-size: .65rem; text-transform: uppercase; letter-spacing: .1em; }.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: .8rem; }.form-grid label { display: flex; flex-direction: column; gap: .25rem; font-size: .78rem; }.wide { grid-column: 1 / -1; }.form-grid small { color: var(--fg-subtle); }.error { color: var(--danger) !important; min-height: 1em; }.dialog footer { display: flex; justify-content: flex-end; gap: .55rem; margin-top: 1.1rem; }
    .chart-title { font-size: .85rem; color: var(--fg-muted); }.selection-actions { display: flex; gap: .5rem; align-items: center; font-size: .75rem; }.export-list { max-height: 220px; overflow: auto; display: grid; grid-template-columns: 1fr 1fr; gap: .3rem; padding: .7rem; margin-top: .5rem; border: 1px solid var(--border); border-radius: var(--radius-sm); }.export-list label { font-size: .8rem; }.toast-custom { position: fixed; right: 1rem; bottom: 1rem; z-index: 1100; padding: .7rem 1rem; border: 1px solid var(--neon); background: var(--surface-elevated); color: var(--fg); border-radius: var(--radius-sm); box-shadow: 0 0 18px var(--neon-soft); }
    @media (max-width: 1000px) { .filters { grid-template-columns: 1fr 1fr 1fr; }.investment-row { grid-template-columns: 1fr 1fr; }.identity,.actions { grid-column: 1 / -1; } }
    @media (max-width: 650px) { .filters,.form-grid { grid-template-columns: 1fr; }.wide { grid-column: auto; }.investment-row { grid-template-columns: 1fr; }.identity,.actions { grid-column: auto; }.history-head,.history-row { align-items: stretch; flex-direction: column; }.history-row { gap: .4rem; }.export-list { grid-template-columns: 1fr; } }
  `]
})
export class InvestmentsComponent {
  private readonly api = inject(AssetTrackerApi);
  readonly today = isoToday();
  readonly tags = signal<AssetTag[]>([]);
  readonly page = signal<PagedResult<Investment>>(emptyPage<Investment>());
  readonly entries = signal<PagedResult<InvestmentEntry>>(emptyPage<InvestmentEntry>());
  readonly prices = signal<PagedResult<InvestmentPriceEntry>>(emptyPage<InvestmentPriceEntry>());
  readonly detail = signal<InvestmentDetail | null>(null);
  readonly exportOptions = signal<Investment[]>([]);
  readonly statistics = signal<InvestmentStatistics | null>(null);
  readonly loading = signal(false);
  readonly historyLoading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly modalError = signal<string | null>(null);
  readonly expandedId = signal<string | null>(null);
  readonly historyTab = signal<HistoryTab>('entries');
  readonly modal = signal<Modal>(null);
  readonly selected = signal<Investment | null>(null);
  readonly confirmTarget = signal<ConfirmTarget | null>(null);
  readonly toast = signal<string | null>(null);

  search = ''; statusFilter: 0 | InvestmentStatus = 1; typeFilter: 0 | 1 | 2 = 0; tagFilter = '';
  sortBy = 'Name'; sortDirection: 'Asc' | 'Desc' = 'Asc'; pageNumber = 1;
  statisticsDuration = 'ThreeMonths';
  exportSelection = new Set<string>();
  private searchTimer?: ReturnType<typeof setTimeout>;

  investmentForm = this.newInvestmentForm();
  statusForm = { status: 2 as InvestmentStatus, effectiveDate: this.today };
  entryForm = { id: '', type: 1 as InvestmentTxType, date: this.today, note: '', pricePerUnit: null as number | null, quantity: null as number | null, amount: null as number | null };
  priceForm = { id: '', date: this.today, pricePerUnit: null as number | null };
  exportForm = { from: this.offsetYears(-1), to: this.today, format: 'Xlsx' as 'Xlsx' | 'Pdf' };

  constructor() { void this.initialize(); }
  fmtUnits = fmtUnits;
  money = (value: number | null | undefined) => fmtMoney(value);
  maxPage = <T,>(p: PagedResult<T>) => Math.max(1, p.totalPages || Math.ceil(p.totalCount / Math.max(1, p.pageSize)));

  async initialize() {
    try { this.tags.set(await firstValueFrom(this.api.listTags())); } catch { /* list still works */ }
    await this.load();
  }
  scheduleLoad() { clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.pageNumber = 1; void this.load(); }, 300); }
  async load() {
    this.loading.set(true); this.error.set(null);
    try {
      this.page.set(await firstValueFrom(this.api.listInvestments({
        search: this.search || undefined, status: this.statusFilter || undefined, type: this.typeFilter || undefined,
        tagId: this.tagFilter || undefined, sortBy: this.sortBy, sortDirection: this.sortDirection,
        page: this.pageNumber, pageSize: 25
      })));
    } catch (e: any) { this.error.set(apiMessage(e, 'Could not load investments.')); }
    finally { this.loading.set(false); }
  }
  goPage(page: number) { this.pageNumber = page; void this.load(); }
  toggleSort() { this.sortDirection = this.sortDirection === 'Asc' ? 'Desc' : 'Asc'; void this.load(); }

  openCreate() { this.selected.set(null); this.investmentForm = this.newInvestmentForm(); this.open('investment'); }
  openEdit(x: Investment) {
    this.selected.set(x);
    this.investmentForm = { id: x.id, name: x.name, description: x.description ?? '', investmentType: x.investmentType, currencyCode: x.currencyCode, tagId: x.tag?.id ?? null, newTagName: '', creationDate: x.creationDate };
    this.open('investment');
  }
  openStatus(x: Investment) { this.selected.set(x); this.statusForm = { status: x.status === 1 ? 2 : 1, effectiveDate: this.today }; this.open('status'); }
  openEntry(x: Investment, entry?: InvestmentEntry) {
    this.selected.set(x);
    this.entryForm = entry
      ? { id: entry.id, type: entry.type, date: entry.date, note: entry.note ?? '', pricePerUnit: entry.pricePerUnit, quantity: entry.quantity, amount: x.investmentType === 2 ? entry.amount : null }
      : { id: '', type: x.investmentType === 1 ? 1 : 3, date: this.today, note: '', pricePerUnit: null, quantity: null, amount: null };
    this.open('entry');
  }
  openPrice(x: Investment, price?: InvestmentPriceEntry) {
    this.selected.set(x); this.priceForm = price ? { id: price.id, date: price.date, pricePerUnit: price.pricePerUnit } : { id: '', date: this.today, pricePerUnit: null }; this.open('price');
  }
  async openStatistics(x: Investment) { this.selected.set(x); this.statistics.set(null); this.open('statistics'); await this.loadStatistics(); }
  async openExport() {
    this.exportSelection.clear(); this.exportOptions.set([]); this.open('export');
    try { this.exportOptions.set((await firstValueFrom(this.api.listInvestments({ pageSize: 100, sortBy: 'Name' }))).items); }
    catch (e: any) { this.modalError.set(apiMessage(e, 'Could not load export options.')); }
  }
  open(value: Modal) { this.modalError.set(null); this.modal.set(value); }
  closeModal() { if (!this.saving()) { this.modal.set(null); this.modalError.set(null); } }
  backdrop(event: MouseEvent) { if (event.target === event.currentTarget) this.closeModal(); }

  async toggleExpand(x: Investment) {
    if (this.expandedId() === x.id) { this.expandedId.set(null); return; }
    this.expandedId.set(x.id); this.historyTab.set('entries'); this.detail.set(null);
    await Promise.all([this.loadEntries(x), this.loadDetail(x)]);
  }
  async loadDetail(x: Investment) { try { this.detail.set(await firstValueFrom(this.api.getInvestment(x.id))); } catch { this.detail.set(null); } }
  async setHistoryTab(tab: HistoryTab, x: Investment) { this.historyTab.set(tab); tab === 'entries' ? await this.loadEntries(x) : await this.loadPrices(x); }
  async loadEntries(x: Investment, page = 1) { this.historyLoading.set(true); try { this.entries.set(await firstValueFrom(this.api.listInvestmentEntries(x.id, page))); } catch (e: any) { this.error.set(apiMessage(e, 'Could not load history.')); } finally { this.historyLoading.set(false); } }
  async loadPrices(x: Investment, page = 1) { this.historyLoading.set(true); try { this.prices.set(await firstValueFrom(this.api.listInvestmentPrices(x.id, page))); } catch (e: any) { this.error.set(apiMessage(e, 'Could not load price history.')); } finally { this.historyLoading.set(false); } }

  async saveInvestment() {
    if (!this.investmentValid()) return;
    this.saving.set(true); this.modalError.set(null);
    try {
      const f = this.investmentForm;
      if (f.id) await firstValueFrom(this.api.updateInvestment(f.id, { name: f.name.trim(), description: f.description.trim() || null, tagId: f.tagId }));
      else await firstValueFrom(this.api.createInvestment({ name: f.name.trim(), description: f.description.trim() || null, investmentType: f.investmentType, currencyCode: 'INR', tagId: f.tagId, newTagName: f.newTagName.trim() || null, creationDate: f.creationDate }));
      this.closeAfterSave(f.id ? 'Investment updated.' : 'Investment created.'); await this.initialize();
    } catch (e: any) { this.modalError.set(apiMessage(e, 'Could not save investment.')); } finally { this.saving.set(false); }
  }
  async saveStatus() {
    const x = this.selected(); if (!x || !this.statusValid()) return;
    await this.runSave(async () => { await firstValueFrom(this.api.changeInvestmentStatus(x.id, this.statusForm)); }, 'Status updated.');
    await this.load(); if (this.expandedId() === x.id) await this.loadDetail(x);
  }
  async saveEntry() {
    const x = this.selected(); if (!x || !this.entryValid()) return;
    const body = { type: this.entryForm.type, date: this.entryForm.date, note: this.entryForm.note.trim() || null, pricePerUnit: x.investmentType === 1 ? Number(this.entryForm.pricePerUnit) : null, quantity: x.investmentType === 1 ? Number(this.entryForm.quantity) : null, amount: x.investmentType === 2 ? Number(this.entryForm.amount) : null };
    await this.runSave(async () => { this.entryForm.id ? await firstValueFrom(this.api.updateInvestmentEntry(x.id, this.entryForm.id, body)) : await firstValueFrom(this.api.addInvestmentEntry(x.id, body)); }, this.entryForm.id ? 'Entry updated.' : 'Entry added.');
    await this.load(); await this.loadEntries(x);
  }
  async savePrice() {
    const x = this.selected(); if (!x || !this.priceValid()) return;
    const body = { date: this.priceForm.date, pricePerUnit: Number(this.priceForm.pricePerUnit) };
    await this.runSave(async () => { this.priceForm.id ? await firstValueFrom(this.api.updateInvestmentPrice(x.id, this.priceForm.id, body)) : await firstValueFrom(this.api.addInvestmentPrice(x.id, body)); }, this.priceForm.id ? 'Price updated.' : 'Price added.');
    await this.load(); await this.loadPrices(x);
  }
  async runSave(action: () => Promise<void>, success: string) {
    this.saving.set(true); this.modalError.set(null);
    try { await action(); this.closeAfterSave(success); } catch (e: any) { this.modalError.set(apiMessage(e, 'Could not save changes.')); } finally { this.saving.set(false); }
  }
  closeAfterSave(message: string) { this.modal.set(null); this.showToast(message); }

  confirmDeleteInvestment(x: Investment) { this.confirmTarget.set({ kind: 'investment', investment: x }); }
  confirmDeleteEntry(x: Investment, entry: InvestmentEntry) { this.confirmTarget.set({ kind: 'entry', investment: x, entry }); }
  confirmDeletePrice(x: Investment, price: InvestmentPriceEntry) { this.confirmTarget.set({ kind: 'price', investment: x, price }); }
  confirmMessage() {
    const target = this.confirmTarget();
    if (!target) return '';
    return target.kind === 'investment' ? `Delete “${target.investment.name}” and all of its history?`
      : target.kind === 'entry' ? `Delete the ${this.entryType(target.entry.type).toLowerCase()} entry dated ${target.entry.date}?`
      : `Delete the price entry dated ${target.price.date}?`;
  }
  async performDelete() {
    const target = this.confirmTarget(); if (!target) return;
    this.saving.set(true);
    try {
      if (target.kind === 'investment') await firstValueFrom(this.api.deleteInvestment(target.investment.id));
      else if (target.kind === 'entry') await firstValueFrom(this.api.deleteInvestmentEntry(target.investment.id, target.entry.id));
      else await firstValueFrom(this.api.deleteInvestmentPrice(target.investment.id, target.price.id));
      this.confirmTarget.set(null); this.showToast('Deleted successfully.'); await this.load();
      if (target.kind !== 'investment') target.kind === 'entry' ? await this.loadEntries(target.investment) : await this.loadPrices(target.investment);
      else this.expandedId.set(null);
    } catch (e: any) { this.confirmTarget.set(null); this.error.set(apiMessage(e, 'Could not delete item.')); } finally { this.saving.set(false); }
  }

  async loadStatistics() {
    const x = this.selected(); if (!x) return; this.statistics.set(null);
    try { this.statistics.set(await firstValueFrom(this.api.getInvestmentStatistics(x.id, this.historyTab() === 'prices' ? 'Prices' : 'Entries', this.statisticsDuration))); }
    catch (e: any) { this.modalError.set(apiMessage(e, 'Could not load statistics.')); }
  }
  toggleExport(id: string) { this.exportSelection.has(id) ? this.exportSelection.delete(id) : this.exportSelection.add(id); }
  selectAll() { this.exportOptions().forEach(x => this.exportSelection.add(x.id)); }
  async exportData() {
    if (!this.exportValid()) return; this.saving.set(true); this.modalError.set(null);
    try {
      const response = await firstValueFrom(this.api.exportInvestments({ investmentIds: [...this.exportSelection], ...this.exportForm }));
      const blob = response.body!; const disposition = response.headers.get('content-disposition');
      const filename = /filename="?([^";]+)"?/i.exec(disposition ?? '')?.[1] ?? `investments.${this.exportForm.format.toLowerCase()}`;
      const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = filename; anchor.click(); URL.revokeObjectURL(url);
      this.closeAfterSave('Report generated.');
    } catch (e: any) { this.modalError.set(apiMessage(e, 'Could not generate report.')); } finally { this.saving.set(false); }
  }

  dialogTitle() { return this.modal() === 'investment' ? (this.investmentForm.id ? 'Edit Investment' : 'Create Investment') : this.modal() === 'status' ? 'Change Status' : this.modal() === 'entry' ? (this.entryForm.id ? 'Edit Entry' : 'Add Entry') : this.modal() === 'price' ? (this.priceForm.id ? 'Edit Price' : 'Add Current Price') : this.modal() === 'statistics' ? 'Investment Statistics' : 'Export Investments'; }
  entryType(type: InvestmentTxType) { return ({ 1: 'Buy', 2: 'Sell', 3: 'Credit', 4: 'Debit' } as Record<number, string>)[type]; }
  nameError(name: string) { const n = name.trim(); return n.length < 2 ? 'Name must contain at least 2 characters.' : n.length > 100 ? 'Name cannot exceed 100 characters.' : ''; }
  dateError(value: string) { return !value ? 'Date is required.' : value > this.today ? 'Date cannot be in the future.' : ''; }
  boundedDateError(value: string, min: string) { return this.dateError(value) || (value < min ? 'Date cannot be before the investment was created.' : ''); }
  positiveError(value: number | null, label: string) { return value === null || Number(value) <= 0 ? `${label} must be greater than zero.` : ''; }
  nonNegativeError(value: number | null) { return value === null || Number(value) < 0 ? 'Price must be zero or greater.' : ''; }
  investmentValid() { const f = this.investmentForm; return !this.nameError(f.name) && f.description.length <= 500 && (!!f.id || (!this.dateError(f.creationDate) && [1, 2].includes(f.investmentType) && f.currencyCode === 'INR')) && f.newTagName.length <= 50 && !(f.tagId && f.newTagName.trim()); }
  statusValid() { const x = this.selected(); return !!x && !this.boundedDateError(this.statusForm.effectiveDate, x.creationDate) && [1, 2].includes(this.statusForm.status); }
  entryValid() { const x = this.selected(); if (!x || this.boundedDateError(this.entryForm.date, x.creationDate) || this.entryForm.note.length > 200) return false; return x.investmentType === 1 ? !this.positiveError(this.entryForm.pricePerUnit, 'Price') && !this.positiveError(this.entryForm.quantity, 'Quantity') && [1, 2].includes(this.entryForm.type) : !this.positiveError(this.entryForm.amount, 'Amount') && [3, 4].includes(this.entryForm.type); }
  priceValid() { const x = this.selected(); return !!x && !this.boundedDateError(this.priceForm.date, x.creationDate) && !this.nonNegativeError(this.priceForm.pricePerUnit); }
  exportDateError() { if (!this.exportForm.from || !this.exportForm.to) return 'Both dates are required.'; if (this.exportForm.from > this.exportForm.to) return 'From date must be on or before the To date.'; const max = new Date(this.exportForm.from + 'T00:00:00'); max.setFullYear(max.getFullYear() + 3); return this.exportForm.to > max.toISOString().slice(0, 10) ? 'Maximum export duration is 3 years.' : ''; }
  exportValid() { return this.exportSelection.size > 0 && !this.exportDateError() && ['Xlsx', 'Pdf'].includes(this.exportForm.format); }
  showToast(message: string) { this.toast.set(message); setTimeout(() => this.toast.set(null), 2800); }
  private newInvestmentForm() { return { id: '', name: '', description: '', investmentType: 1 as 1 | 2, currencyCode: 'INR', tagId: null as string | null, newTagName: '', creationDate: this.today }; }
  private offsetYears(years: number) { const d = new Date(); d.setFullYear(d.getFullYear() + years); return d.toISOString().slice(0, 10); }
}

function emptyPage<T>(): PagedResult<T> { return { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 }; }
function apiMessage(error: any, fallback: string): string { return error?.error?.detail ?? error?.error?.message ?? fallback; }

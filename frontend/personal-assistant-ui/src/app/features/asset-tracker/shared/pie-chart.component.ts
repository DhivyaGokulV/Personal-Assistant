import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SliceItem, fmtMoney } from '../asset-tracker.models';

interface Slice {
  key: string;
  value: number;
  percent: number;
  color: string;
  d: string;       // SVG path
  midX: number;    // for label
  midY: number;
}

const PALETTE = [
  '#ff2bd6', '#08fdd8', '#6c5ce7', '#fdcb6e', '#00b894',
  '#e74c3c', '#74b9ff', '#a29bfe', '#fd79a8', '#fab1a0'
];

@Component({
  selector: 'app-pie-chart',
  imports: [CommonModule],
  template: `
    @if (slices().length === 0) {
      <div class="empty-chart">No data to chart yet.</div>
    } @else {
      <div class="pie-shell">
        <svg viewBox="0 0 220 220" class="pie">
          @for (s of slices(); track s.key) {
            <path [attr.d]="s.d" [attr.fill]="s.color" class="slice" (mouseenter)="hover.set(s.key)" (mouseleave)="hover.set(null)" />
          }
          <circle cx="110" cy="110" r="42" class="hole" />
        </svg>
        <ul class="legend">
          @for (s of slices(); track s.key) {
            <li class="legend-row" [class.is-hover]="hover() === s.key">
              <span class="swatch" [style.background]="s.color"></span>
              <span class="key">{{ s.key }}</span>
              <span class="pct">{{ s.percent.toFixed(1) }}%</span>
              <span class="val">{{ fmt(s.value) }}</span>
            </li>
          }
        </ul>
      </div>
    }
  `,
  styles: [`
    .pie-shell { display: grid; grid-template-columns: 220px 1fr; gap: 1rem; align-items: center; }
    @media (max-width: 600px) { .pie-shell { grid-template-columns: 1fr; } }
    .pie { width: 220px; height: 220px; display: block; }
    .empty-chart {
      display: flex; align-items: center; justify-content: center;
      height: 180px; color: var(--fg-muted); font-size: 0.85rem;
      border: 1px dashed var(--border-strong); border-radius: var(--radius-sm);
    }
    .slice { transition: opacity 120ms ease; cursor: default; stroke: var(--surface); stroke-width: 1; }
    .slice:hover { opacity: 0.85; }
    .hole { fill: var(--surface); }

    .legend { list-style: none; margin: 0; padding: 0; }
    .legend-row {
      display: grid;
      grid-template-columns: 14px 1fr auto auto;
      gap: 0.5rem; align-items: center;
      padding: 0.25rem 0.5rem; border-radius: var(--radius-sm);
      font-size: 0.85rem;
    }
    .legend-row.is-hover { background: var(--surface-2); }
    .swatch { width: 12px; height: 12px; border-radius: 3px; display: block; }
    .pct { color: var(--fg-muted); font-variant-numeric: tabular-nums; }
    .val { color: var(--fg); font-variant-numeric: tabular-nums; }
  `]
})
export class PieChartComponent {
  @Input() set data(value: SliceItem[]) { this._data.set(value ?? []); }

  private readonly _data = signal<SliceItem[]>([]);
  readonly hover = signal<string | null>(null);

  fmt = (n: number) => fmtMoney(n, 0);

  readonly slices = computed<Slice[]>(() => {
    const items = this._data().filter(d => d.value > 0);
    if (items.length === 0) return [];

    const total = items.reduce((acc, x) => acc + x.value, 0);
    if (total <= 0) return [];

    const cx = 110, cy = 110, r = 100;
    let startAngle = -Math.PI / 2; // start at top
    const out: Slice[] = [];

    items.forEach((item, idx) => {
      const fraction = item.value / total;
      const sweep = fraction * Math.PI * 2;
      const endAngle = startAngle + sweep;

      const startX = cx + r * Math.cos(startAngle);
      const startY = cy + r * Math.sin(startAngle);
      const endX = cx + r * Math.cos(endAngle);
      const endY = cy + r * Math.sin(endAngle);
      const largeArc = sweep > Math.PI ? 1 : 0;

      // Single full circle case
      let d: string;
      if (items.length === 1 || sweep >= Math.PI * 2 - 0.0001) {
        d = `M${cx},${cy - r} A${r},${r} 0 1 1 ${cx - 0.001},${cy - r} Z`;
      } else {
        d = `M${cx},${cy} L${startX},${startY} A${r},${r} 0 ${largeArc} 1 ${endX},${endY} Z`;
      }

      const midAngle = startAngle + sweep / 2;
      const midX = cx + (r * 0.62) * Math.cos(midAngle);
      const midY = cy + (r * 0.62) * Math.sin(midAngle);

      out.push({
        key: item.key,
        value: item.value,
        percent: item.percentOfTotal,
        color: PALETTE[idx % PALETTE.length],
        d, midX, midY
      });

      startAngle = endAngle;
    });

    return out;
  });
}

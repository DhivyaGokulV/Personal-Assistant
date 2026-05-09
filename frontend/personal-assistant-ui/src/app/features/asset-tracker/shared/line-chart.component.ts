import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TimeSeriesPoint, fmtMoney } from '../asset-tracker.models';

interface ScaledPoint { x: number; y: number; date: string; value: number; }

@Component({
  selector: 'app-line-chart',
  imports: [CommonModule],
  template: `
    @if (points().length < 2) {
      <div class="empty-chart">Not enough data to draw a chart yet.</div>
    } @else {
      <svg [attr.viewBox]="viewBox" preserveAspectRatio="none" class="chart">
        <!-- Y grid lines -->
        @for (g of yGrid(); track $index) {
          <line [attr.x1]="padding.left" [attr.x2]="W - padding.right" [attr.y1]="g.y" [attr.y2]="g.y" class="grid" />
          <text [attr.x]="padding.left - 6" [attr.y]="g.y + 4" class="axis-label">{{ g.label }}</text>
        }

        <!-- X axis ends -->
        <text [attr.x]="padding.left" [attr.y]="H - 4" class="axis-label">{{ points()[0].date }}</text>
        <text [attr.x]="W - padding.right" [attr.y]="H - 4" text-anchor="end" class="axis-label">{{ points()[points().length - 1].date }}</text>

        <!-- Filled area -->
        <path [attr.d]="areaPath()" class="area" [attr.fill]="color" />
        <!-- Line -->
        <path [attr.d]="linePath()" class="line" [attr.stroke]="color" />

        <!-- Hover overlay -->
        <rect [attr.x]="padding.left" [attr.y]="padding.top" [attr.width]="W - padding.left - padding.right" [attr.height]="H - padding.top - padding.bottom" class="hover-rect" (mousemove)="onHover($event)" (mouseleave)="hover.set(null)" />

        @if (hover(); as h) {
          <line [attr.x1]="h.x" [attr.x2]="h.x" [attr.y1]="padding.top" [attr.y2]="H - padding.bottom" class="hover-line" />
          <circle [attr.cx]="h.x" [attr.cy]="h.y" r="4" class="hover-dot" [attr.fill]="color" />
          <g [attr.transform]="'translate(' + h.x + ',' + (h.y - 14) + ')'">
            <rect x="-70" y="-22" width="140" height="20" rx="3" class="tooltip" />
            <text x="0" y="-8" text-anchor="middle" class="tooltip-text">{{ h.date }} · {{ fmt(h.value) }}</text>
          </g>
        }
      </svg>
    }
  `,
  styles: [`
    .chart { width: 100%; height: 220px; display: block; }
    .empty-chart {
      display: flex; align-items: center; justify-content: center;
      height: 220px; color: var(--fg-muted); font-size: 0.85rem;
      border: 1px dashed var(--border-strong); border-radius: var(--radius-sm);
    }
    .grid { stroke: var(--border); stroke-width: 0.5; stroke-dasharray: 2 4; }
    .axis-label { fill: var(--fg-muted); font-size: 10px; font-family: inherit; }
    .area { opacity: 0.18; }
    .line { fill: none; stroke-width: 1.5; }
    .hover-rect { fill: transparent; }
    .hover-line { stroke: var(--fg-muted); stroke-width: 1; stroke-dasharray: 3 3; opacity: 0.6; }
    .hover-dot { stroke: var(--surface); stroke-width: 1.5; }
    .tooltip { fill: var(--surface-elevated); stroke: var(--border-strong); }
    .tooltip-text { fill: var(--fg); font-size: 11px; font-family: inherit; }
  `]
})
export class LineChartComponent {
  @Input() set series(value: TimeSeriesPoint[]) { this._series.set(value ?? []); }
  @Input() color = 'var(--neon)';

  protected readonly W = 600;
  protected readonly H = 220;
  protected readonly padding = { top: 12, right: 12, bottom: 18, left: 60 };

  private readonly _series = signal<TimeSeriesPoint[]>([]);
  readonly hover = signal<{ x: number; y: number; date: string; value: number } | null>(null);

  get viewBox(): string { return `0 0 ${this.W} ${this.H}`; }
  fmt = (n: number) => fmtMoney(n, 0);

  readonly points = computed<ScaledPoint[]>(() => {
    const s = this._series();
    if (s.length < 2) return [];

    const min = Math.min(...s.map(p => p.value));
    const max = Math.max(...s.map(p => p.value));
    const range = max - min || 1;

    const xMin = this.padding.left;
    const xMax = this.W - this.padding.right;
    const yMin = this.padding.top;
    const yMax = this.H - this.padding.bottom;

    return s.map((p, i) => ({
      x: xMin + (i / (s.length - 1)) * (xMax - xMin),
      y: yMax - ((p.value - min) / range) * (yMax - yMin),
      date: p.date,
      value: p.value
    }));
  });

  readonly yGrid = computed(() => {
    const s = this._series();
    if (s.length < 2) return [];
    const min = Math.min(...s.map(p => p.value));
    const max = Math.max(...s.map(p => p.value));
    if (min === max) return [];
    const yMin = this.padding.top;
    const yMax = this.H - this.padding.bottom;
    const ticks = 4;
    const out: { y: number; label: string }[] = [];
    for (let i = 0; i <= ticks; i++) {
      const v = min + (max - min) * (1 - i / ticks);
      const y = yMin + (i / ticks) * (yMax - yMin);
      out.push({ y, label: this.shortNumber(v) });
    }
    return out;
  });

  linePath(): string {
    const pts = this.points();
    if (pts.length === 0) return '';
    return pts.map((p, i) => (i === 0 ? `M${p.x},${p.y}` : `L${p.x},${p.y}`)).join(' ');
  }

  areaPath(): string {
    const pts = this.points();
    if (pts.length === 0) return '';
    const yBottom = this.H - this.padding.bottom;
    const start = `M${pts[0].x},${yBottom}`;
    const lines = pts.map(p => `L${p.x},${p.y}`).join(' ');
    const end = `L${pts[pts.length - 1].x},${yBottom} Z`;
    return `${start} ${lines} ${end}`;
  }

  onHover(evt: MouseEvent): void {
    const target = evt.currentTarget as SVGRectElement;
    const svg = target.ownerSVGElement!;
    const rect = svg.getBoundingClientRect();
    const scaleX = this.W / rect.width;
    const x = (evt.clientX - rect.left) * scaleX;

    const pts = this.points();
    if (pts.length === 0) return;
    let closest = pts[0];
    let minDist = Math.abs(closest.x - x);
    for (const p of pts) {
      const d = Math.abs(p.x - x);
      if (d < minDist) { minDist = d; closest = p; }
    }
    this.hover.set({ x: closest.x, y: closest.y, date: closest.date, value: closest.value });
  }

  private shortNumber(n: number): string {
    const abs = Math.abs(n);
    if (abs >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (abs >= 1_000) return (n / 1_000).toFixed(1) + 'K';
    return Math.round(n).toString();
  }
}

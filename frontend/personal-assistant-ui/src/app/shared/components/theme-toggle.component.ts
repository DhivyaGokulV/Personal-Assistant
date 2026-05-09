import { Component, inject } from '@angular/core';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-theme-toggle',
  template: `
    <button type="button"
            class="theme-toggle"
            [attr.aria-label]="theme.mode() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'"
            (click)="theme.toggle()">
      {{ theme.mode() === 'dark' ? '☀' : '☾' }}
    </button>
  `,
  styles: [`
    .theme-toggle {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      background: var(--surface);
      color: var(--fg);
      border: 1px solid var(--border-strong);
      font-size: 1.05rem;
      line-height: 1;
      cursor: pointer;
      transition: box-shadow 160ms ease, color 160ms ease;
    }
    .theme-toggle:hover {
      box-shadow: 0 0 10px var(--neon-soft);
      color: var(--neon);
    }
  `]
})
export class ThemeToggleComponent {
  readonly theme = inject(ThemeService);
}

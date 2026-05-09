import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppHeaderComponent } from '../shared/components/app-header.component';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, AppHeaderComponent],
  template: `
    <div class="app-shell">
      <app-header />
      <main class="flex-grow-1">
        <router-outlet />
      </main>
    </div>
  `
})
export class ShellComponent {}

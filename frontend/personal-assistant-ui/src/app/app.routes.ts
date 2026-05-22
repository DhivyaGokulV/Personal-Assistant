import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { LoginComponent } from './core/auth/login.component';
import { RegisterComponent } from './core/auth/register.component';
import { HomeComponent } from './features/home/home.component';
import { TaskManagementShellComponent } from './features/task-management/task-management-shell.component';
import { FinanceShellComponent } from './features/finance/finance-shell.component';
import { AssetTrackerShellComponent } from './features/asset-tracker/asset-tracker-shell.component';
import { TimeTrackerComponent } from './features/time-tracker/time-tracker.component';
import { HealthComponent } from './features/health/health.component';
import { GoalsComponent } from './features/goals/goals.component';
import { PasswordVaultComponent } from './features/password-vault/password-vault.component';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'home' },
  { path: 'login', canActivate: [guestGuard], component: LoginComponent },
  { path: 'register', canActivate: [guestGuard], component: RegisterComponent },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'home', component: HomeComponent },
      { path: 'tasks', component: TaskManagementShellComponent },
      { path: 'finance', component: FinanceShellComponent },
      { path: 'assets', component: AssetTrackerShellComponent },
      { path: 'time', component: TimeTrackerComponent },
      { path: 'health', component: HealthComponent },
      { path: 'goals', component: GoalsComponent },
      { path: 'passwords', component: PasswordVaultComponent }
    ]
  },
  { path: '**', redirectTo: 'home' }
];

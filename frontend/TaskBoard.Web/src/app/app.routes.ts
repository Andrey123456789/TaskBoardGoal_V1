import { Routes } from '@angular/router';

/** TaskBoard is a single-screen application; everything happens on the dashboard. */
export const routes: Routes = [
  {
    path: '',
    title: 'TaskBoard',
    loadComponent: () => import('./features/dashboard/dashboard-page').then((m) => m.DashboardPage),
  },
  { path: '**', redirectTo: '' },
];

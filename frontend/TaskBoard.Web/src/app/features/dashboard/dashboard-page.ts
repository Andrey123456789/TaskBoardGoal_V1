import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Notifier } from '../../core/notifications/notifier';
import { ProjectManagementDialog } from '../projects/components/project-management-dialog/project-management-dialog';
import { TaskCard } from '../tasks/components/task-card/task-card';
import { TaskDetailPanel } from '../tasks/components/task-detail-panel/task-detail-panel';
import {
  TaskFormDialog,
  TaskFormDialogData,
} from '../tasks/components/task-form-dialog/task-form-dialog';
import { TaskDetails } from '../tasks/models/task';
import { UserManagementDialog } from '../users/components/user-management-dialog/user-management-dialog';
import { DashboardStore } from './dashboard-store';

/**
 * The single TaskBoard screen: filters, status columns, the task detail drawer and dialogs for
 * creating tasks and managing users and projects.
 */
@Component({
  selector: 'app-dashboard-page',
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSidenavModule,
    MatSlideToggleModule,
    MatToolbarModule,
    TaskCard,
    TaskDetailPanel,
  ],
  providers: [DashboardStore],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage implements OnInit {
  protected readonly store = inject(DashboardStore);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);

  ngOnInit(): void {
    this.store.load();
  }

  /** Opens the create dialog; with a source task it creates a related follow-up task. */
  protected openNewTask(source?: TaskDetails): void {
    const data: TaskFormDialogData = {
      projects: this.store.projects(),
      users: this.store.users(),
      preset: source
        ? { projectId: source.project.id, relatedTaskIds: [source.id] }
        : { projectId: this.store.projectFilter() },
    };

    this.dialog
      .open<TaskFormDialog, TaskFormDialogData, TaskDetails>(TaskFormDialog, {
        data,
        width: '600px',
      })
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.notifier.success(`Task "${created.title}" created`);
          this.store.refresh();
          this.store.select(created.id);
        }
      });
  }

  protected openUsers(): void {
    this.dialog
      .open(UserManagementDialog, { width: '680px' })
      .afterClosed()
      .subscribe(() => this.reloadAll());
  }

  protected openProjects(): void {
    this.dialog
      .open(ProjectManagementDialog, { width: '680px' })
      .afterClosed()
      .subscribe(() => this.reloadAll());
  }

  protected onTaskDeleted(): void {
    this.store.clearSelection();
    this.store.reloadTasks();
  }

  private reloadAll(): void {
    this.store.reloadReferenceData();
    this.store.refresh();
  }
}

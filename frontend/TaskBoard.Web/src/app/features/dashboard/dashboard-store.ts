import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, forkJoin, of, switchMap, tap } from 'rxjs';
import { Notifier } from '../../core/notifications/notifier';
import { ProjectsApi } from '../projects/data-access/projects-api';
import { Project } from '../projects/models/project';
import { TasksApi } from '../tasks/data-access/tasks-api';
import {
  NEXT_STATUS,
  STATUS_LABELS,
  TASK_STATUSES,
  TaskStatus,
  TaskSummary,
} from '../tasks/models/task';
import { UsersApi } from '../users/data-access/users-api';
import { User } from '../users/models/user';

export interface BoardColumn {
  status: TaskStatus;
  label: string;
  tasks: TaskSummary[];
}

/** Dashboard state: reference data, server-side filters, board columns and the selected task. */
@Injectable()
export class DashboardStore {
  private readonly tasksApi = inject(TasksApi);
  private readonly usersApi = inject(UsersApi);
  private readonly projectsApi = inject(ProjectsApi);
  private readonly notifier = inject(Notifier);
  private readonly destroyRef = inject(DestroyRef);
  private readonly taskReloads = new Subject<void>();

  readonly users = signal<User[]>([]);
  readonly projects = signal<Project[]>([]);
  readonly tasks = signal<TaskSummary[]>([]);
  readonly projectFilter = signal<string | null>(null);
  readonly assigneeFilter = signal<string | null>(null);
  /** Closed tasks are hidden by default. */
  readonly showClosed = signal(false);
  readonly loading = signal(false);
  readonly selectedTaskId = signal<string | null>(null);
  /** Bumped after board-level changes so an open detail panel reloads. */
  readonly detailsVersion = signal(0);

  readonly columns = computed<BoardColumn[]>(() => {
    const statuses = this.showClosed()
      ? TASK_STATUSES
      : TASK_STATUSES.filter((status) => status !== 'Closed');
    const tasks = this.tasks();

    return statuses.map((status) => ({
      status,
      label: STATUS_LABELS[status],
      tasks: tasks.filter((task) => task.status === status),
    }));
  });

  constructor() {
    // switchMap drops responses of superseded filter combinations.
    this.taskReloads
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(() =>
          this.tasksApi
            .list({ projectId: this.projectFilter(), assigneeId: this.assigneeFilter() })
            .pipe(
              catchError((error: unknown) => {
                this.notifier.error(error);
                return of(null);
              }),
            ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((tasks) => {
        this.loading.set(false);
        if (tasks) {
          this.tasks.set(tasks);
        }
      });
  }

  load(): void {
    this.reloadReferenceData();
    this.reloadTasks();
  }

  reloadReferenceData(): void {
    forkJoin({ users: this.usersApi.list(), projects: this.projectsApi.list() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ users, projects }) => {
          this.users.set(users);
          this.projects.set(projects);
          this.dropStaleFilters(users, projects);
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  reloadTasks(): void {
    this.taskReloads.next();
  }

  /** Reloads the board and any open task details after data changed elsewhere. */
  refresh(): void {
    this.reloadTasks();
    this.detailsVersion.update((version) => version + 1);
  }

  setProjectFilter(projectId: string | null): void {
    this.projectFilter.set(projectId);
    this.reloadTasks();
  }

  setAssigneeFilter(assigneeId: string | null): void {
    this.assigneeFilter.set(assigneeId);
    this.reloadTasks();
  }

  setShowClosed(show: boolean): void {
    this.showClosed.set(show);
  }

  select(taskId: string): void {
    this.selectedTaskId.set(taskId);
  }

  clearSelection(): void {
    this.selectedTaskId.set(null);
  }

  /** Performs the single allowed next transition for a task shown on the board. */
  advance(task: TaskSummary): void {
    const next = NEXT_STATUS[task.status];
    if (!next) {
      return;
    }

    this.tasksApi
      .changeStatus(task.id, next)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notifier.success(`"${task.title}" moved to ${STATUS_LABELS[next]}`);
          this.refresh();
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  private dropStaleFilters(users: User[], projects: Project[]): void {
    let changed = false;

    const projectId = this.projectFilter();
    if (projectId && !projects.some((project) => project.id === projectId)) {
      this.projectFilter.set(null);
      changed = true;
    }

    const assigneeId = this.assigneeFilter();
    if (assigneeId && !users.some((user) => user.id === assigneeId)) {
      this.assigneeFilter.set(null);
      changed = true;
    }

    if (changed) {
      this.reloadTasks();
    }
  }
}

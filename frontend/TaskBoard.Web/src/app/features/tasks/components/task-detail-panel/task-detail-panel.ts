import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Observable, catchError, filter, finalize, forkJoin, of, switchMap, tap } from 'rxjs';
import { Notifier } from '../../../../core/notifications/notifier';
import { confirmAction } from '../../../../shared/confirm-dialog/confirm-dialog';
import { User } from '../../../users/models/user';
import { TasksApi } from '../../data-access/tasks-api';
import {
  NEXT_STATUS,
  STATUS_ACTION_LABELS,
  STATUS_LABELS,
  TASK_DESCRIPTION_MAX_LENGTH,
  TASK_TITLE_MAX_LENGTH,
  TaskDetails,
  TaskSummary,
  canAdvance,
  isLocked,
} from '../../models/task';

/**
 * Mirrors the API's assignee rules for a better editing experience: an InProgress task needs an
 * assignee, and the system Deleted User cannot be (re)selected manually.
 */
function assigneeValidator(task: TaskDetails): ValidatorFn {
  return (control: AbstractControl<string | null>): ValidationErrors | null => {
    const value = control.value;

    if (value !== null && task.assignee?.isDeletedUser && value === task.assignee.id) {
      return { deletedUser: true };
    }

    if (value === null && task.status === 'InProgress') {
      return { required: true };
    }

    return null;
  };
}

@Component({
  selector: 'app-task-detail-panel',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './task-detail-panel.html',
  styleUrl: './task-detail-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailPanel {
  private readonly tasksApi = inject(TasksApi);
  private readonly notifier = inject(Notifier);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly taskId = input.required<string>();
  /** Changes whenever the task may have been modified elsewhere on the dashboard. */
  readonly version = input(0);
  /** Active users that can be assigned. */
  readonly users = input<User[]>([]);

  readonly closed = output<void>();
  /** The task's core data or status changed; the board should refresh. */
  readonly changed = output<void>();
  readonly deleted = output<string>();
  readonly relatedTaskOpened = output<string>();
  readonly followUpRequested = output<TaskDetails>();

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly titleMaxLength = TASK_TITLE_MAX_LENGTH;
  protected readonly details = signal<TaskDetails | null>(null);
  protected readonly allTasks = signal<TaskSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly selectedRelatedTaskId = signal<string | null>(null);
  private readonly refreshCount = signal(0);

  protected readonly form = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(TASK_TITLE_MAX_LENGTH)]],
    description: ['', [Validators.maxLength(TASK_DESCRIPTION_MAX_LENGTH)]],
    assigneeId: this.formBuilder.control<string | null>(null),
  });

  protected readonly locked = computed(() => {
    const task = this.details();
    return task !== null && isLocked(task.status);
  });

  protected readonly actionLabel = computed(() => {
    const task = this.details();
    return task ? STATUS_ACTION_LABELS[task.status] : null;
  });

  protected readonly canAdvance = computed(() => {
    const task = this.details();
    return task !== null && canAdvance(task);
  });

  protected readonly allowUnassigned = computed(() => this.details()?.status === 'Created');

  protected readonly deletedAssignee = computed(() => {
    const assignee = this.details()?.assignee;
    return assignee?.isDeletedUser ? assignee : null;
  });

  protected readonly relatedCandidates = computed(() => {
    const task = this.details();
    if (!task) {
      return [];
    }

    const relatedIds = new Set(task.relatedTasks.map((related) => related.id));
    return this.allTasks().filter(
      (candidate) => candidate.id !== task.id && !relatedIds.has(candidate.id),
    );
  });

  constructor() {
    const request = computed(() => ({
      id: this.taskId(),
      version: this.version(),
      refresh: this.refreshCount(),
    }));

    toObservable(request)
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(({ id }) =>
          forkJoin({ details: this.tasksApi.get(id), allTasks: this.tasksApi.list() }).pipe(
            catchError((error: unknown) => {
              this.notifier.error(error);
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.loading.set(false);
        this.details.set(result?.details ?? null);
        this.allTasks.set(result?.allTasks ?? []);

        if (result) {
          this.resetForm(result.details);
        }
      });
  }

  protected save(): void {
    const task = this.details();
    if (!task || this.form.invalid) {
      return;
    }

    const { title, description, assigneeId } = this.form.getRawValue();

    this.run(
      this.tasksApi.update(task.id, {
        title: title.trim(),
        description: description.trim() || null,
        assigneeId,
      }),
      'Task saved',
    );
  }

  protected advance(): void {
    const task = this.details();
    const next = task ? NEXT_STATUS[task.status] : null;
    if (!task || !next) {
      return;
    }

    this.run(this.tasksApi.changeStatus(task.id, next), `Task moved to ${STATUS_LABELS[next]}`);
  }

  protected delete(): void {
    const task = this.details();
    if (!task) {
      return;
    }

    confirmAction(this.dialog, {
      title: 'Delete task',
      message: `Delete "${task.title}"? Its related-task links are removed as well.`,
      confirmLabel: 'Delete',
    })
      .pipe(
        filter(Boolean),
        tap(() => this.busy.set(true)),
        switchMap(() => this.tasksApi.delete(task.id).pipe(finalize(() => this.busy.set(false)))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifier.success('Task deleted');
          this.deleted.emit(task.id);
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  protected addRelated(): void {
    const task = this.details();
    const relatedId = this.selectedRelatedTaskId();
    if (!task || !relatedId) {
      return;
    }

    this.replaceRelated(task, [...task.relatedTasks.map((related) => related.id), relatedId]);
    this.selectedRelatedTaskId.set(null);
  }

  protected removeRelated(relatedId: string): void {
    const task = this.details();
    if (!task) {
      return;
    }

    this.replaceRelated(
      task,
      task.relatedTasks.map((related) => related.id).filter((id) => id !== relatedId),
    );
  }

  private replaceRelated(task: TaskDetails, relatedTaskIds: string[]): void {
    this.busy.set(true);

    this.tasksApi
      .replaceRelatedTasks(task.id, relatedTaskIds)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.refreshCount.update((count) => count + 1),
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  private run(operation: Observable<void>, successMessage: string): void {
    this.busy.set(true);

    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifier.success(successMessage);
          this.refreshCount.update((count) => count + 1);
          this.changed.emit();
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  private resetForm(task: TaskDetails): void {
    this.form.reset({
      title: task.title,
      description: task.description ?? '',
      assigneeId: task.assignee?.id ?? null,
    });

    const assignee = this.form.controls.assigneeId;
    assignee.setValidators(assigneeValidator(task));
    assignee.updateValueAndValidity();

    if (isLocked(task.status)) {
      this.form.disable();
    } else {
      this.form.enable();
    }
  }
}

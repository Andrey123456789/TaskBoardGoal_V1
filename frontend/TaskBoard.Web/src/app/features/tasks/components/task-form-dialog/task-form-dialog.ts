import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { describeHttpError } from '../../../../core/http/problem-details';
import { Project } from '../../../projects/models/project';
import { User } from '../../../users/models/user';
import { TasksApi } from '../../data-access/tasks-api';
import {
  TASK_DESCRIPTION_MAX_LENGTH,
  TASK_TITLE_MAX_LENGTH,
  TaskDetails,
  TaskSummary,
} from '../../models/task';

export interface TaskFormDialogData {
  projects: Project[];
  users: User[];
  /** Pre-filled values, e.g. for a follow-up task related to a completed task. */
  preset?: {
    projectId?: string | null;
    relatedTaskIds?: string[];
  };
}

/** Creates a task. New tasks always start as Created; the status is never chosen here. */
@Component({
  selector: 'app-task-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './task-form-dialog.html',
  styleUrl: './task-form-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskFormDialog {
  private readonly data = inject<TaskFormDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<TaskFormDialog, TaskDetails>>(MatDialogRef);
  private readonly tasksApi = inject(TasksApi);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly projects = this.data.projects;
  protected readonly users = this.data.users;
  protected readonly titleMaxLength = TASK_TITLE_MAX_LENGTH;
  protected readonly isFollowUp = (this.data.preset?.relatedTaskIds?.length ?? 0) > 0;
  protected readonly allTasks = signal<TaskSummary[]>([]);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(TASK_TITLE_MAX_LENGTH)]],
    description: ['', [Validators.maxLength(TASK_DESCRIPTION_MAX_LENGTH)]],
    projectId: [this.initialProjectId(), [Validators.required]],
    assigneeId: this.formBuilder.control<string | null>(null),
    relatedTaskIds: this.formBuilder.control<string[]>(this.data.preset?.relatedTaskIds ?? []),
  });

  protected readonly sourceTasks = computed(() => {
    const presetIds = new Set(this.data.preset?.relatedTaskIds ?? []);
    return this.allTasks().filter((task) => presetIds.has(task.id));
  });

  constructor() {
    this.tasksApi
      .list()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (tasks) => this.allTasks.set(tasks),
        error: (error: unknown) => this.errorMessage.set(describeHttpError(error)),
      });
  }

  protected submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);

    this.tasksApi
      .create({
        title: value.title.trim(),
        description: value.description.trim() || null,
        projectId: value.projectId,
        assigneeId: value.assigneeId,
        relatedTaskIds: value.relatedTaskIds,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (created) => this.dialogRef.close(created),
        error: (error: unknown) => this.errorMessage.set(describeHttpError(error)),
      });
  }

  private initialProjectId(): string {
    const preset = this.data.preset?.projectId;
    if (preset && this.data.projects.some((project) => project.id === preset)) {
      return preset;
    }

    return this.data.projects.length === 1 ? this.data.projects[0].id : '';
  }
}

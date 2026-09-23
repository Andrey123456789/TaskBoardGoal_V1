import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormGroupDirective,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Observable, filter, finalize, switchMap } from 'rxjs';
import { describeHttpError } from '../../../../core/http/problem-details';
import { Notifier } from '../../../../core/notifications/notifier';
import { confirmAction } from '../../../../shared/confirm-dialog/confirm-dialog';
import { ProjectsApi } from '../../data-access/projects-api';
import { Project } from '../../models/project';

/** Create, edit and delete projects without leaving the dashboard. */
@Component({
  selector: 'app-project-management-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './project-management-dialog.html',
  styleUrl: '../../../../shared/management-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectManagementDialog {
  private readonly projectsApi = inject(ProjectsApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly formDirective = viewChild.required(FormGroupDirective);

  protected readonly projects = signal<Project[]>([]);
  protected readonly editingId = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(2000)]],
  });

  constructor() {
    this.load();
  }

  protected edit(project: Project): void {
    this.editingId.set(project.id);
    this.errorMessage.set(null);
    this.formDirective().resetForm({ name: project.name, description: project.description ?? '' });
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
    this.errorMessage.set(null);
    // Resetting through the directive also clears the "submitted" state, so no errors show.
    this.formDirective().resetForm({ name: '', description: '' });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, description } = this.form.getRawValue();
    const request = { name: name.trim(), description: description.trim() || null };
    const editingId = this.editingId();

    const operation: Observable<unknown> = editingId
      ? this.projectsApi.update(editingId, request)
      : this.projectsApi.create(request);

    this.execute(operation, editingId ? 'Project updated' : 'Project created');
  }

  protected delete(project: Project): void {
    confirmAction(this.dialog, {
      title: 'Delete project',
      message: `Delete ${project.name}? Only projects without tasks can be deleted.`,
      confirmLabel: 'Delete',
    })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.busy.set(true);
          return this.projectsApi.delete(project.id).pipe(finalize(() => this.busy.set(false)));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifier.success('Project deleted');
          if (this.editingId() === project.id) {
            this.cancelEdit();
          }
          this.load();
        },
        error: (error: unknown) => this.errorMessage.set(describeHttpError(error)),
      });
  }

  private execute(operation: Observable<unknown>, successMessage: string): void {
    this.busy.set(true);
    this.errorMessage.set(null);

    operation
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifier.success(successMessage);
          this.cancelEdit();
          this.load();
        },
        error: (error: unknown) => this.errorMessage.set(describeHttpError(error)),
      });
  }

  private load(): void {
    this.projectsApi
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (projects) => this.projects.set(projects),
        error: (error: unknown) => this.errorMessage.set(describeHttpError(error)),
      });
  }
}

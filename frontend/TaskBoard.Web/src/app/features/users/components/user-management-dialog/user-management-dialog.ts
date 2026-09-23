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
import { UsersApi } from '../../data-access/users-api';
import { User } from '../../models/user';

/** Create, edit and delete ordinary users without leaving the dashboard. */
@Component({
  selector: 'app-user-management-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './user-management-dialog.html',
  styleUrl: '../../../../shared/management-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManagementDialog {
  private readonly usersApi = inject(UsersApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly formDirective = viewChild.required(FormGroupDirective);

  protected readonly users = signal<User[]>([]);
  protected readonly editingId = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
  });

  constructor() {
    this.load();
  }

  protected edit(user: User): void {
    this.editingId.set(user.id);
    this.errorMessage.set(null);
    this.formDirective().resetForm({ name: user.name, email: user.email });
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
    this.errorMessage.set(null);
    // Resetting through the directive also clears the "submitted" state, so no errors show.
    this.formDirective().resetForm({ name: '', email: '' });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, email } = this.form.getRawValue();
    const request = { name: name.trim(), email: email.trim() };
    const editingId = this.editingId();

    const operation: Observable<unknown> = editingId
      ? this.usersApi.update(editingId, request)
      : this.usersApi.create(request);

    this.execute(operation, editingId ? 'User updated' : 'User created');
  }

  protected delete(user: User): void {
    confirmAction(this.dialog, {
      title: 'Delete user',
      message: `Delete ${user.name}? Tasks assigned to this user are kept and reassigned to "Deleted User".`,
      confirmLabel: 'Delete',
    })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.busy.set(true);
          return this.usersApi.delete(user.id).pipe(finalize(() => this.busy.set(false)));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifier.success('User deleted');
          if (this.editingId() === user.id) {
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
    this.usersApi
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => this.users.set(users),
        error: (error: unknown) => this.errorMessage.set(describeHttpError(error)),
      });
  }
}

import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { describeHttpError } from '../http/problem-details';

/** Short, non-blocking user feedback. */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, undefined, { duration: 3000 });
  }

  error(error: unknown): void {
    this.snackBar.open(describeHttpError(error), 'Dismiss', {
      duration: 8000,
      panelClass: 'error-snackbar',
    });
  }
}

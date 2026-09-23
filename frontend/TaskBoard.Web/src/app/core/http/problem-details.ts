import { HttpErrorResponse } from '@angular/common/http';

/** RFC 9457 problem details as returned by the TaskBoard API. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Stable machine-readable error code for business errors, e.g. `task.invalid_transition`. */
  code?: string;
  /** Field-level messages for request validation failures (400). */
  errors?: Record<string, string[]>;
}

function asProblemDetails(value: unknown): ProblemDetails | null {
  return value !== null && typeof value === 'object' ? (value as ProblemDetails) : null;
}

/** Turns an HTTP failure into a short message suitable for display to the user. */
export function describeHttpError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong. Please try again.';
  }

  if (error.status === 0) {
    return 'The TaskBoard API cannot be reached. Check that it is running.';
  }

  const problem = asProblemDetails(error.error);

  if (problem?.errors) {
    const messages = Object.values(problem.errors).flat();
    if (messages.length > 0) {
      return messages.join(' ');
    }
  }

  if (error.status >= 500) {
    return problem?.detail ?? 'The server could not complete the request. Please try again later.';
  }

  return problem?.detail ?? problem?.title ?? `Request failed (${error.status}).`;
}

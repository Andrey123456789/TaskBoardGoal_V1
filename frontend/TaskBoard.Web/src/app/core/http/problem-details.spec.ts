import { HttpErrorResponse } from '@angular/common/http';
import { describeHttpError } from './problem-details';

describe('describeHttpError', () => {
  it('uses the ProblemDetails detail for business errors', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        title: 'Conflict',
        detail: 'A Completed task cannot be edited.',
        code: 'task.locked',
      },
    });

    expect(describeHttpError(error)).toBe('A Completed task cannot be edited.');
  });

  it('joins field messages for validation problems', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Validation',
        errors: { name: ["'Name' must not be empty."], email: ['Bad email.'] },
      },
    });

    expect(describeHttpError(error)).toBe("'Name' must not be empty. Bad email.");
  });

  it('explains when the API cannot be reached', () => {
    expect(describeHttpError(new HttpErrorResponse({ status: 0 }))).toContain('cannot be reached');
  });

  it('never shows raw server text for unexpected failures', () => {
    const error = new HttpErrorResponse({
      status: 500,
      error: 'System.Exception: boom at C:\\src',
    });

    expect(describeHttpError(error)).not.toContain('boom');
  });
});

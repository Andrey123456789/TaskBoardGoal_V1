import { NEXT_STATUS, STATUS_ACTION_LABELS, canAdvance, isLocked } from './task';

describe('task workflow helpers', () => {
  it('offers only the immediately following transition', () => {
    expect(NEXT_STATUS).toEqual({
      Created: 'InProgress',
      InProgress: 'Completed',
      Completed: 'Closed',
      Closed: null,
    });
  });

  it('labels the next action Start, Complete, Close and nothing for Closed', () => {
    expect(STATUS_ACTION_LABELS).toEqual({
      Created: 'Start',
      InProgress: 'Complete',
      Completed: 'Close',
      Closed: null,
    });
  });

  it('locks Completed and Closed tasks only', () => {
    expect(isLocked('Created')).toBe(false);
    expect(isLocked('InProgress')).toBe(false);
    expect(isLocked('Completed')).toBe(true);
    expect(isLocked('Closed')).toBe(true);
  });

  it('requires an active assignee to start a task', () => {
    const active = { id: 'u', name: 'Alice', isDeletedUser: false };
    const deleted = { id: 'd', name: 'Deleted User', isDeletedUser: true };

    expect(canAdvance({ status: 'Created', assignee: null })).toBe(false);
    expect(canAdvance({ status: 'Created', assignee: deleted })).toBe(false);
    expect(canAdvance({ status: 'Created', assignee: active })).toBe(true);
    expect(canAdvance({ status: 'InProgress', assignee: deleted })).toBe(true);
    expect(canAdvance({ status: 'Closed', assignee: active })).toBe(false);
  });
});

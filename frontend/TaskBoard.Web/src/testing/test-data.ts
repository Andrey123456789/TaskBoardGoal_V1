import { Project } from '../app/features/projects/models/project';
import { TaskDetails, TaskSummary } from '../app/features/tasks/models/task';
import { User } from '../app/features/users/models/user';

export const API = 'http://api.test';

export const DELETED_USER_ID = '00000000-0000-0000-0000-000000000001';

export const users: User[] = [
  { id: 'u-alice', name: 'Alice', email: 'alice@example.com', createdAt: '2026-09-01T09:00:00Z' },
  { id: 'u-bob', name: 'Bob', email: 'bob@example.com', createdAt: '2026-09-01T09:00:00Z' },
];

export const projects: Project[] = [
  { id: 'p-web', name: 'Website', description: null, createdAt: '2026-09-01T09:00:00Z' },
  { id: 'p-mobile', name: 'Mobile', description: 'App', createdAt: '2026-09-01T09:00:00Z' },
];

export function aTaskSummary(overrides: Partial<TaskSummary> = {}): TaskSummary {
  return {
    id: 't-1',
    title: 'Task',
    status: 'Created',
    project: { id: 'p-web', name: 'Website' },
    assignee: { id: 'u-alice', name: 'Alice', isDeletedUser: false },
    createdAt: '2026-09-01T09:00:00Z',
    updatedAt: '2026-09-01T09:00:00Z',
    ...overrides,
  };
}

export function aTaskDetails(overrides: Partial<TaskDetails> = {}): TaskDetails {
  return {
    id: 't-1',
    title: 'Task',
    description: null,
    status: 'Created',
    project: { id: 'p-web', name: 'Website' },
    assignee: { id: 'u-alice', name: 'Alice', isDeletedUser: false },
    createdAt: '2026-09-01T09:00:00Z',
    updatedAt: '2026-09-01T09:00:00Z',
    relatedTasks: [],
    ...overrides,
  };
}

/** A small board with one task per status. */
export const boardTasks: TaskSummary[] = [
  aTaskSummary({ id: 't-created', title: 'Write copy', status: 'Created', assignee: null }),
  aTaskSummary({ id: 't-progress', title: 'Build API', status: 'InProgress' }),
  aTaskSummary({
    id: 't-completed',
    title: 'Design page',
    status: 'Completed',
    project: { id: 'p-mobile', name: 'Mobile' },
    assignee: { id: 'u-bob', name: 'Bob', isDeletedUser: false },
  }),
  aTaskSummary({ id: 't-closed', title: 'Old release', status: 'Closed' }),
];

export type TaskStatus = 'Created' | 'InProgress' | 'Completed' | 'Closed';

/** Limits enforced by the API. */
export const TASK_TITLE_MAX_LENGTH = 200;
export const TASK_DESCRIPTION_MAX_LENGTH = 4000;

export const TASK_STATUSES: readonly TaskStatus[] = [
  'Created',
  'InProgress',
  'Completed',
  'Closed',
];

export interface ProjectSummary {
  id: string;
  name: string;
}

/** A task's current assignee. `null` on the task means unassigned. */
export interface Assignee {
  id: string;
  name: string;
  /** True when the original assignee was deleted and the system Deleted User took over. */
  isDeletedUser: boolean;
}

export interface TaskSummary {
  id: string;
  title: string;
  status: TaskStatus;
  project: ProjectSummary;
  assignee: Assignee | null;
  createdAt: string;
  updatedAt: string;
}

export interface RelatedTask {
  id: string;
  title: string;
  status: TaskStatus;
  project: ProjectSummary;
}

export interface TaskDetails {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  project: ProjectSummary;
  assignee: Assignee | null;
  createdAt: string;
  updatedAt: string;
  relatedTasks: RelatedTask[];
}

export interface TaskFilter {
  projectId?: string | null;
  assigneeId?: string | null;
  status?: TaskStatus | null;
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  projectId: string;
  assigneeId: string | null;
  relatedTaskIds: string[];
}

export interface UpdateTaskRequest {
  title: string;
  description: string | null;
  assigneeId: string | null;
}

export const STATUS_LABELS: Record<TaskStatus, string> = {
  Created: 'Created',
  InProgress: 'In progress',
  Completed: 'Completed',
  Closed: 'Closed',
};

/**
 * The only transition the UI offers for each status. The API remains authoritative and rejects
 * any other transition.
 */
export const NEXT_STATUS: Record<TaskStatus, TaskStatus | null> = {
  Created: 'InProgress',
  InProgress: 'Completed',
  Completed: 'Closed',
  Closed: null,
};

export const STATUS_ACTION_LABELS: Record<TaskStatus, string | null> = {
  Created: 'Start',
  InProgress: 'Complete',
  Completed: 'Close',
  Closed: null,
};

/** Completed and Closed tasks have read-only core data and cannot be deleted. */
export function isLocked(status: TaskStatus): boolean {
  return status === 'Completed' || status === 'Closed';
}

export function hasActiveAssignee(task: { assignee: Assignee | null }): boolean {
  return task.assignee !== null && !task.assignee.isDeletedUser;
}

/** Whether the next status action can be performed, mirroring the API's workflow rules. */
export function canAdvance(task: { status: TaskStatus; assignee: Assignee | null }): boolean {
  if (NEXT_STATUS[task.status] === null) {
    return false;
  }

  return task.status !== 'Created' || hasActiveAssignee(task);
}

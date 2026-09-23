# TaskBoard Specification

## 1. Purpose

TaskBoard is a single-page application for managing tasks across projects and users.

The application contains three primary business entities:

- User
- Project
- Task

There is no authentication or authorization.

All clients may view and modify all users, projects, and tasks according to the business rules in this specification.

A `User` is ordinary application data. It is not an ASP.NET Core Identity user and has no password, role, permission, token, or login behavior.

---

## 2. Functional Scope

The application must support:

- user management;
- project management;
- task creation, editing, deletion, status transitions, filtering, and viewing;
- assigning tasks to users;
- unassigned tasks;
- symmetric relationships between tasks;
- creating follow-up tasks related to previous tasks;
- a single-screen Angular dashboard;
- Development seed data;
- safe handling of unexpected server errors.

The following are explicitly out of scope:

- authentication;
- authorization;
- ASP.NET Core Identity;
- JWT;
- roles and permissions;
- sprints;
- teams;
- comments;
- attachments;
- notifications;
- background jobs;
- messaging;
- microservices;
- SignalR/WebSockets;
- audit subsystem;
- NgRx;
- Redis/cache;
- Docker;
- cloud deployment.

---

## 3. User

A user contains:

| Field | Required | Notes |
|---|---:|---|
| Id | Yes | Unique identifier |
| Name | Yes | Non-empty |
| Email | Yes | Valid email |
| CreatedAt | Yes | Creation timestamp |

### User Rules

- Email must be unique case-insensitively.
- Ordinary users may be created, viewed, edited, and deleted.
- Deleting a user must not delete that user's tasks.
- When a user is deleted, every task assigned to that user must be reassigned to the special system user `Deleted User`.
- User deletion and task reassignment must behave as one consistent operation.
- The reassignment to `Deleted User` is a system operation and is allowed even when a task is `Completed` or `Closed`.

### Deleted User

The system must contain a special user named:

`Deleted User`

This record has special semantics:

- it must always be available to the system independently of Development demo seeding;
- it must not appear in the normal user list used for assignment;
- it cannot be manually assigned to a task;
- it cannot be edited or deleted through ordinary user-management operations;
- tasks may reference it only because a previously assigned user was deleted;
- task responses may display `Deleted User` as the current assignee.

`Deleted User` is not equivalent to an unassigned task.

---

## 4. Project

A project contains:

| Field | Required | Notes |
|---|---:|---|
| Id | Yes | Unique identifier |
| Name | Yes | Non-empty |
| Description | No | Optional text |
| CreatedAt | Yes | Creation timestamp |

### Project Rules

- Project name must be unique.
- Projects may be created, viewed, edited, and deleted.
- A project that has one or more tasks cannot be deleted.
- Project deletion must never cascade-delete tasks.

---

## 5. Task

A task contains:

| Field | Required | Notes |
|---|---:|---|
| Id | Yes | Unique identifier |
| Title | Yes | Non-empty |
| Description | No | Optional text |
| ProjectId | Yes | Must reference an existing project |
| AssigneeId | No | May be unassigned while `Created` |
| Status | Yes | See task workflow |
| CreatedAt | Yes | Creation timestamp |
| UpdatedAt | Yes | Last modification timestamp |

The internal backend type name is an implementation detail. The public API resource is called `Task`.

### Task Creation

A newly created task:

- always starts in `Created`;
- must belong to an existing project;
- may be unassigned;
- may be assigned to an active ordinary user;
- may optionally be created with related-task IDs;
- cannot be created directly as `InProgress`, `Completed`, or `Closed`.

### Project Immutability

`ProjectId` is set only during task creation.

After creation, the task cannot be moved to another project.

The task update contract must not offer ordinary project reassignment.

---

## 6. Task Status Workflow

Supported statuses:

1. `Created`
2. `InProgress`
3. `Completed`
4. `Closed`

The workflow is strictly linear:

`Created -> InProgress -> Completed -> Closed`

Only the immediately following transition is allowed.

| Current state | Target state | Allowed |
|---|---|---:|
| Created | InProgress | Yes |
| InProgress | Completed | Yes |
| Completed | Closed | Yes |
| Created | Completed | No |
| Created | Closed | No |
| InProgress | Created | No |
| InProgress | Closed | No |
| Completed | Created | No |
| Completed | InProgress | No |
| Closed | Any other state | No |

A task status never moves backward.

A task also cannot skip required forward states.

An invalid status transition is a business conflict.

### Starting Work

A task may move from `Created` to `InProgress` only when it is assigned to an active ordinary user.

Therefore:

- `Created + Unassigned -> InProgress` is forbidden;
- `Created + Deleted User -> InProgress` is forbidden;
- `Created + active user -> InProgress` is allowed.

### Assignment While In Progress

An `InProgress` task must normally have an active user.

While a task is `InProgress`:

- it may be reassigned to another active ordinary user;
- it may not be manually changed to unassigned;
- it may not be manually assigned to `Deleted User`.

The exception is user deletion:

- if the current assignee is deleted, the system reassigns the task to `Deleted User`;
- the task remains `InProgress`;
- the task may later be reassigned from `Deleted User` to an active ordinary user.

### Completed

When a task becomes `Completed`:

- title is locked;
- description is locked;
- project is locked;
- assignee is locked;
- ordinary editing is no longer allowed;
- deletion is no longer allowed;
- the only status transition allowed is `Completed -> Closed`;
- related-task relationships remain editable.

If the assignee is later deleted, the system may still reassign the task to `Deleted User`.

### Closed

When a task becomes `Closed`:

- core task data is read-only;
- assignment is read-only;
- status is terminal;
- deletion is forbidden;
- related-task relationships remain editable.

---

## 7. Task Editing

### Created

Editable:

- Title
- Description
- Assignee

Assignee may be:

- an active ordinary user;
- unassigned.

Not editable:

- Project
- Status through the ordinary update operation
- Related tasks through the ordinary update operation

### InProgress

Editable:

- Title
- Description
- Assignee

Assignee may be changed only to another active ordinary user.

The task cannot be manually unassigned.

Not editable:

- Project
- Status through the ordinary update operation
- Related tasks through the ordinary update operation

### Completed and Closed

Ordinary task editing is forbidden.

Related-task relationships are handled separately and remain editable regardless of task status.

---

## 8. Task Deletion

A task may be deleted only while it is:

- `Created`;
- `InProgress`.

A task may not be deleted when it is:

- `Completed`;
- `Closed`.

Attempting to delete a `Completed` or `Closed` task is a business conflict.

When an allowed task is deleted, all relationship edges involving that task must also be removed.

---

## 9. Related Tasks

Tasks may be related to other tasks.

The ordinary relationship is symmetric.

If task A is related to task B:

`A <-> B`

then both tasks must report the relationship.

### Relationship Rules

- A task cannot be related to itself.
- A relationship between the same two tasks must exist at most once.
- `(A, B)` and `(B, A)` represent the same relationship.
- Related tasks may belong to different projects.
- Related tasks may have different assignees.
- Related tasks may have any statuses.
- Task status does not restrict adding or removing relationships.
- Deleting an eligible task removes all relationship edges involving that task.
- Duplicate task IDs supplied by a client must not create duplicate relationships.

### Replace-Set Semantics

The complete related-task set is updated with one request.

The request provides the complete desired collection of related task IDs.

The server must:

- validate the requested IDs;
- normalize duplicate IDs;
- compare the requested set with the currently stored set;
- add missing relationships;
- remove relationships no longer present;
- keep relationships that already exist;
- persist the final set atomically.

Sending the same desired set repeatedly must have the same final result.

An empty array removes all related-task relationships for the task.

---

## 10. Follow-Up Tasks

Completed or closed tasks are never reopened.

If additional work is required after a task has been completed or closed, a new task must be created.

The new task may be related to the previous task.

The UI must provide a convenient `Create related task` action from a completed or closed task.

The resulting new task:

- starts as `Created`;
- follows all ordinary task-creation rules;
- is related to the source task;
- does not change the source task's status or core data.

---

## 11. HTTP API

The following routes are part of the contract.

### Users

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/users` | List ordinary assignable users |
| GET | `/api/users/{id}` | Get ordinary user |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user and reassign its tasks to `Deleted User` |

`GET /api/users` must not include `Deleted User`.

### Projects

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/projects` | List projects |
| GET | `/api/projects/{id}` | Get project |
| POST | `/api/projects` | Create project |
| PUT | `/api/projects/{id}` | Update project |
| DELETE | `/api/projects/{id}` | Delete project if it has no tasks |

### Tasks

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/tasks` | List/filter tasks |
| GET | `/api/tasks/{id}` | Get task details |
| POST | `/api/tasks` | Create task |
| PUT | `/api/tasks/{id}` | Update editable task fields |
| PATCH | `/api/tasks/{id}/status` | Perform a status transition |
| PUT | `/api/tasks/{id}/related-tasks` | Replace the full related-task set |
| DELETE | `/api/tasks/{id}` | Delete task when allowed |

---

## 12. Task Requests

### Create Task

Conceptual request:

```json
{
  "title": "Implement task board",
  "description": "Optional description",
  "projectId": "project-guid",
  "assigneeId": "user-guid-or-null",
  "relatedTaskIds": [
    "task-guid-1",
    "task-guid-2"
  ]
}
```

`assigneeId` may be `null`.

`relatedTaskIds` may be omitted or empty.

The client cannot set the initial status.

### Update Task

Conceptual request:

```json
{
  "title": "Updated title",
  "description": "Updated description",
  "assigneeId": "user-guid-or-null"
}
```

The update request must not allow changing `ProjectId`.

The update request must not change `Status`.

For an `InProgress` task, `assigneeId` cannot be `null`.

### Change Status

Conceptual request:

```json
{
  "status": "InProgress"
}
```

The requested transition must satisfy the task workflow.

### Replace Related Tasks

Conceptual request:

```json
{
  "relatedTaskIds": [
    "task-guid-1",
    "task-guid-2",
    "task-guid-3"
  ]
}
```

This array represents the complete desired related-task set.

---

## 13. Task Query and Filtering

`GET /api/tasks` supports optional server-side filters:

- `projectId`
- `assigneeId`
- `status`

Filters may be combined.

Example:

`GET /api/tasks?projectId={id}&assigneeId={id}&status=InProgress`

No pagination is required for this version.

No sprint filtering is required.

No full-text search is required.

An empty result is:

`200 OK`

with an empty array.

It is not `404`.

---

## 14. Task List Response

The task-list response must contain enough information to render the dashboard without issuing one request per project or user.

A task summary should include at least:

- task ID;
- title;
- status;
- project ID and project name;
- assignee ID and assignee name when assigned.

An unassigned task must be distinguishable from a task assigned to `Deleted User`.

The implementation must avoid an N+1 request pattern between the Angular application and the API.

---

## 15. Task Detail Response

Task details must include at least:

- Id;
- Title;
- Description;
- Status;
- Project summary;
- Assignee summary or unassigned state;
- CreatedAt;
- UpdatedAt;
- related-task summaries.

A related-task summary should contain enough information to identify and open the related task.

---

## 16. HTTP Response Semantics

Use the following contract.

| Situation | Response |
|---|---|
| Successful GET | `200 OK` |
| Empty collection | `200 OK` with `[]` |
| Resource created | `201 Created` |
| Successful PUT | `204 No Content` |
| Successful PATCH status | `204 No Content` |
| Successful related-task replacement | `204 No Content` |
| Successful DELETE | `204 No Content` |
| Addressed resource does not exist | `404 Not Found` |
| Malformed/request-contract-invalid input | `400 Bad Request` |
| Structurally valid but semantically invalid input | `422 Unprocessable Content` |
| Business-state conflict | `409 Conflict` |
| Unexpected server failure | `500 Internal Server Error` with sanitized `ProblemDetails` |

### Examples of `409 Conflict`

- invalid task-state transition;
- editing a completed or closed task;
- deleting a completed or closed task;
- deleting a project that still has tasks;
- duplicate user email;
- duplicate project name.

### Examples of `422 Unprocessable Content`

- task references a non-existent project;
- task references a non-existent assignee;
- related-task array references a non-existent task;
- task is related to itself;
- `InProgress` task update attempts to set assignee to `null`;
- attempting to start an unassigned task;
- attempting to manually assign `Deleted User`.

---

## 17. Unexpected Error Handling

Unexpected server-side exceptions must be handled centrally.

The observable behavior is:

- the exception is logged server-side;
- the client receives a sanitized `ProblemDetails` response;
- the HTTP status is `500`;
- internal implementation details are not exposed.

The response must not expose:

- exception messages that contain internal details;
- stack traces;
- SQL;
- connection strings;
- internal filesystem paths;
- secrets;
- inner exception details.

The implementation mechanism is not prescribed by this specification.

---

## 18. Logging

The backend must use structured logging.

At minimum:

- unexpected exceptions must be logged;
- HTTP request completion information should be available in logs;
- passwords, tokens, secrets, connection strings, and sensitive implementation details must not be written to logs.

Because the application has no authentication, there are no authentication tokens or password data to log.

---

## 19. Frontend

The application is an Angular single-page application.

The primary user experience is one dashboard screen.

### Dashboard

The dashboard contains:

- project filter;
- user/assignee filter;
- `New Task` action;
- task columns for:
  - `Created`;
  - `InProgress`;
  - `Completed`;
- an option to show closed tasks.

Closed tasks are hidden by default.

When closed tasks are enabled, the UI may display a fourth `Closed` column.

There are no sprints.

### Task Cards

A task card should show enough information to understand:

- title;
- project;
- assignee or unassigned state;
- status.

### Status Actions

The UI must expose only the meaningful next transition:

| Current status | Action |
|---|---|
| Created | `Start` |
| InProgress | `Complete` |
| Completed | `Close` |
| Closed | No status action |

The backend remains authoritative and must reject invalid transitions even if the UI hides them.

### Task Detail and Editing

Selecting a task opens its details on the same dashboard screen.

Use a side panel/drawer or equivalent same-screen interaction rather than navigating to a separate task page.

The detail view includes:

- task core information;
- related tasks;
- edit controls when editing is allowed;
- status action;
- delete action when deletion is allowed;
- `Create related task` for completed/closed tasks.

For `Completed` and `Closed` tasks, core task fields are read-only.

Related tasks remain editable.

### User and Project Management

The application remains a single-screen dashboard.

User and project management must be accessible from the dashboard, for example through dialogs or panels.

Separate route-level user/project administration pages are not required.

---

## 20. CORS and Local Development

The Angular frontend and API may run on different local origins during Development.

The backend must support the configured local frontend origin.

CORS configuration should be limited to the frontend origins required by the application rather than opened broadly without a reason.

---

## 21. Development Data

The Development database must contain representative data when initialized according to the project seeding policy.

The system-level `Deleted User` is not demo seed data and must be guaranteed independently.

Development demo data must include at least:

- 3 ordinary visible users;
- 2 projects;
- at least 1 unassigned task;
- at least 2 `Created` tasks;
- at least 2 `InProgress` tasks;
- at least 2 `Completed` tasks;
- at least 1 `Closed` task;
- tasks distributed across both projects;
- tasks distributed across multiple users;
- at least one related-task pair;
- at least one completed task with a related follow-up task.

The seed dataset should make the dashboard useful immediately after local database initialization.

---

## 22. Validation Summary

### User

- Name required.
- Email required.
- Email format valid.
- Email unique case-insensitively.
- `Deleted User` cannot be created, edited, deleted, or selected manually through ordinary user operations.

### Project

- Name required.
- Name unique.
- Project with tasks cannot be deleted.

### Task

- Title required.
- Project must exist.
- Assignee, when supplied, must be an active ordinary user.
- Project cannot change after creation.
- Task starts in `Created`.
- Only the next status transition is allowed.
- Starting an unassigned task is forbidden.
- `InProgress` task cannot be manually unassigned.
- Core task editing is forbidden after completion.
- `Completed` and `Closed` tasks cannot be deleted.
- Related-task IDs must exist.
- Task cannot relate to itself.
- Duplicate related IDs must not create duplicate relationships.
- Related-task relationships remain editable in every status.

---

## 23. Acceptance Scenarios

The completed application should support the following observable scenarios.

### Users

1. Create a user.
2. Reject duplicate email.
3. Edit a user.
4. Delete a user with no tasks.
5. Delete a user with active and historical tasks.
6. Confirm all of that user's tasks now display `Deleted User`.
7. Confirm `Deleted User` is not available in the assignment list.

### Projects

1. Create a project.
2. Edit a project.
3. Reject duplicate project name.
4. Delete an empty project.
5. Reject deletion of a project containing tasks.

### Task Creation

1. Create an unassigned task.
2. Create a task assigned to a user.
3. Create a task with related-task IDs.
4. Reject a non-existent project.
5. Reject a non-existent assignee.
6. Reject manual assignment to `Deleted User`.

### Task Workflow

1. Reject `Created -> InProgress` when unassigned.
2. Allow `Created -> InProgress` when assigned.
3. Allow reassignment while `InProgress`.
4. Reject manual unassignment while `InProgress`.
5. Allow `InProgress -> Completed`.
6. Reject `Completed -> InProgress`.
7. Reject `Created -> Completed`.
8. Allow `Completed -> Closed`.
9. Reject any transition out of `Closed`.

### Completed / Closed Behavior

1. Reject core editing of a completed task.
2. Reject core editing of a closed task.
3. Reject deletion of a completed task.
4. Reject deletion of a closed task.
5. Allow related-task changes for completed and closed tasks.

### User Deletion During Workflow

1. Delete the assignee of a `Created` task and confirm the task becomes assigned to `Deleted User`.
2. Confirm that task cannot be started until assigned to an active user.
3. Delete the assignee of an `InProgress` task and confirm the task remains `InProgress` with `Deleted User`.
4. Reassign the task to an active user.
5. Delete the assignee of a `Completed` task and confirm the historical task now references `Deleted User` without reopening or otherwise modifying the task.

### Related Tasks

1. Add several related tasks with one replace-set request.
2. Repeat the identical request and confirm the same final state.
3. Remove one ID from the desired set and confirm that relationship is removed.
4. Add another ID and confirm it is added.
5. Send duplicate IDs and confirm only one logical relationship exists.
6. Reject self-reference.
7. Reject a non-existent related task.
8. Confirm relation symmetry from both task detail responses.
9. Send an empty array and confirm all relationships are removed.

### Filtering

1. Filter by project.
2. Filter by assignee.
3. Filter by status.
4. Combine filters.
5. Confirm no matches return `200 []`.

### Error Handling

1. Trigger a controlled validation/business error and confirm the documented 4xx behavior.
2. Trigger an unexpected server error in a test environment.
3. Confirm the response is sanitized `ProblemDetails`.
4. Confirm sensitive internal exception details are not returned.
5. Confirm the unexpected exception is logged server-side.

### Frontend

1. View Created, InProgress, and Completed columns.
2. Filter the board by project.
3. Filter the board by user.
4. Open task details without navigating away from the dashboard.
5. Edit an editable task.
6. Start, complete, and close a task through allowed actions.
7. Create a related follow-up task from a completed task.
8. Edit related tasks independently of core task status.
9. Show and hide closed tasks.
10. Manage users and projects without leaving the main application experience.

---

## 24. Experiment Constraint

This specification is shared unchanged by both TaskBoard implementations in the experiment.

The specification defines required behavior and public contracts.

It intentionally does not prescribe:

- internal architecture beyond what the repository template already defines;
- repository implementation details;
- concrete service/class names;
- EF Core mapping style;
- validation invocation details;
- exception-handling implementation type;
- Angular component decomposition;
- test implementation strategy.

Both implementations must satisfy the same observable requirements.

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatInputHarness } from '@angular/material/input/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import {
  API,
  DELETED_USER_ID,
  aTaskDetails,
  aTaskSummary,
  users,
} from '../../../../../testing/test-data';
import { API_BASE_URL } from '../../../../core/http/api-base-url';
import { TaskDetails, TaskSummary } from '../../models/task';
import { TaskDetailPanel } from './task-detail-panel';

describe('TaskDetailPanel', () => {
  let fixture: ComponentFixture<TaskDetailPanel>;
  let http: HttpTestingController;
  let loader: HarnessLoader;
  let rootLoader: HarnessLoader;

  const otherTasks: TaskSummary[] = [
    aTaskSummary({ id: 't-2', title: 'Second task' }),
    aTaskSummary({ id: 't-3', title: 'Third task', project: { id: 'p-mobile', name: 'Mobile' } }),
  ];

  const text = (): string => (fixture.nativeElement as HTMLElement).textContent ?? '';

  /** Answers the panel's (re)load: task details plus all tasks for the related-task picker. */
  async function respondWith(task: TaskDetails): Promise<void> {
    await fixture.whenStable();
    http.expectOne(`${API}/api/tasks/${task.id}`).flush(task);
    http
      .expectOne(`${API}/api/tasks`)
      .flush([aTaskSummary({ id: task.id, title: task.title }), ...otherTasks]);
    await fixture.whenStable();
  }

  async function render(task: TaskDetails): Promise<void> {
    fixture.componentRef.setInput('taskId', task.id);
    fixture.componentRef.setInput('users', users);
    fixture.detectChanges();
    await respondWith(task);
  }

  function noContent(url: string, method: string): unknown {
    const request = http.expectOne(url);
    expect(request.request.method).toBe(method);
    const body = request.request.body;
    request.flush(null, { status: 204, statusText: 'No Content' });
    return body;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TaskDetailPanel],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: API },
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });

    fixture = TestBed.createComponent(TaskDetailPanel);
    http = TestBed.inject(HttpTestingController);
    loader = TestbedHarnessEnvironment.loader(fixture);
    rootLoader = TestbedHarnessEnvironment.documentRootLoader(fixture);
  });

  afterEach(() => http.verify());

  it('edits an editable task and notifies the board', async () => {
    const task = aTaskDetails({ title: 'Old title' });
    await render(task);
    const changed = vi.fn();
    fixture.componentInstance.changed.subscribe(changed);

    const title = await loader.getHarness(
      MatInputHarness.with({ selector: '[formControlName="title"]' }),
    );
    await title.setValue('New title');
    const assignee = await loader.getHarness(
      MatSelectHarness.with({ selector: '[formControlName="assigneeId"]' }),
    );
    await assignee.clickOptions({ text: 'Bob' });
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Save changes' }))).click();

    const body = noContent(`${API}/api/tasks/t-1`, 'PUT');
    expect(body).toEqual({ title: 'New title', description: null, assigneeId: 'u-bob' });

    await respondWith({
      ...task,
      title: 'New title',
      assignee: { id: 'u-bob', name: 'Bob', isDeletedUser: false },
    });
    expect(changed).toHaveBeenCalled();
  });

  it('offers Unassigned for Created tasks but not for tasks in progress', async () => {
    await render(aTaskDetails({ status: 'InProgress' }));

    const assignee = await loader.getHarness(
      MatSelectHarness.with({ selector: '[formControlName="assigneeId"]' }),
    );
    await assignee.open();
    const options = await assignee.getOptions();
    const labels = await Promise.all(options.map((option) => option.getText()));

    expect(labels).toEqual(['Alice', 'Bob']);
  });

  it('starts an assigned task with the Start action', async () => {
    await render(aTaskDetails({ status: 'Created' }));

    await (await loader.getHarness(MatButtonHarness.with({ text: 'Start' }))).click();

    expect(noContent(`${API}/api/tasks/t-1/status`, 'PATCH')).toEqual({ status: 'InProgress' });
    await respondWith(aTaskDetails({ status: 'InProgress' }));

    expect(
      await (await loader.getHarness(MatButtonHarness.with({ text: 'Complete' }))).isDisabled(),
    ).toBe(false);
  });

  it('disables Start for an unassigned task and explains why', async () => {
    await render(aTaskDetails({ assignee: null }));

    const start = await loader.getHarness(MatButtonHarness.with({ text: 'Start' }));
    expect(await start.isDisabled()).toBe(true);
    expect(text()).toContain('Assign an active user to start this task.');
  });

  it('shows Completed tasks read-only with Close and Create related task, but no Delete', async () => {
    const task = aTaskDetails({ status: 'Completed', title: 'Done work', description: 'Finished' });
    await render(task);
    const followUp = vi.fn();
    fixture.componentInstance.followUpRequested.subscribe(followUp);

    expect(await loader.getHarnessOrNull(MatInputHarness)).toBeNull();
    expect(await loader.getHarnessOrNull(MatButtonHarness.with({ text: 'Delete' }))).toBeNull();
    expect(await loader.getHarnessOrNull(MatButtonHarness.with({ text: 'Close' }))).not.toBeNull();
    expect(text()).toContain('Done work');
    expect(text()).toContain('read-only');

    await (await loader.getHarness(MatButtonHarness.with({ text: 'Create related task' }))).click();
    expect(followUp).toHaveBeenCalledWith(task);
  });

  it('shows no status action for Closed tasks', async () => {
    await render(aTaskDetails({ status: 'Closed' }));

    for (const label of ['Start', 'Complete', 'Close', 'Delete']) {
      expect(await loader.getHarnessOrNull(MatButtonHarness.with({ text: label }))).toBeNull();
    }
  });

  it('edits related tasks of a Completed task with full replace-set requests', async () => {
    const task = aTaskDetails({
      status: 'Completed',
      relatedTasks: [
        {
          id: 't-2',
          title: 'Second task',
          status: 'Created',
          project: { id: 'p-web', name: 'Website' },
        },
      ],
    });
    await render(task);

    const related = await loader.getHarness(MatSelectHarness.with({ selector: '.related-select' }));
    await related.clickOptions({ text: 'Third task (Mobile)' });
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Add' }))).click();

    expect(noContent(`${API}/api/tasks/t-1/related-tasks`, 'PUT')).toEqual({
      relatedTaskIds: ['t-2', 't-3'],
    });
    await respondWith(task);

    await (await loader.getHarness(MatButtonHarness.with({ text: 'Remove' }))).click();
    expect(noContent(`${API}/api/tasks/t-1/related-tasks`, 'PUT')).toEqual({ relatedTaskIds: [] });
    await respondWith({ ...task, relatedTasks: [] });

    expect(text()).toContain('No related tasks.');
  });

  it('opens a related task from the list', async () => {
    await render(
      aTaskDetails({
        relatedTasks: [
          {
            id: 't-2',
            title: 'Second task',
            status: 'Closed',
            project: { id: 'p-web', name: 'Website' },
          },
        ],
      }),
    );
    const opened = vi.fn();
    fixture.componentInstance.relatedTaskOpened.subscribe(opened);

    (
      (fixture.nativeElement as HTMLElement).querySelector('.related-item .link') as HTMLElement
    ).click();

    expect(opened).toHaveBeenCalledWith('t-2');
  });

  it('asks for an active user when the assignee was deleted', async () => {
    await render(
      aTaskDetails({
        status: 'InProgress',
        assignee: { id: DELETED_USER_ID, name: 'Deleted User', isDeletedUser: true },
      }),
    );

    expect(text()).toContain('The assigned user was deleted');
    const title = await loader.getHarness(
      MatInputHarness.with({ selector: '[formControlName="title"]' }),
    );
    await title.setValue('Changed');
    expect(
      await (await loader.getHarness(MatButtonHarness.with({ text: 'Save changes' }))).isDisabled(),
    ).toBe(true);

    const assignee = await loader.getHarness(
      MatSelectHarness.with({ selector: '[formControlName="assigneeId"]' }),
    );
    await assignee.clickOptions({ text: 'Alice' });
    expect(
      await (await loader.getHarness(MatButtonHarness.with({ text: 'Save changes' }))).isDisabled(),
    ).toBe(false);
  });

  it('deletes an editable task after confirmation', async () => {
    await render(aTaskDetails({ status: 'InProgress' }));
    const deleted = vi.fn();
    fixture.componentInstance.deleted.subscribe(deleted);

    await (await loader.getHarness(MatButtonHarness.with({ text: 'Delete' }))).click();
    await (
      await rootLoader.getHarness(
        MatButtonHarness.with({ text: 'Delete', ancestor: 'mat-dialog-container' }),
      )
    ).click();
    await fixture.whenStable();

    noContent(`${API}/api/tasks/t-1`, 'DELETE');
    await fixture.whenStable();
    expect(deleted).toHaveBeenCalledWith('t-1');
  });
});

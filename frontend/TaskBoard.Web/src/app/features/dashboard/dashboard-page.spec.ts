import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatInputHarness } from '@angular/material/input/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { MatSlideToggleHarness } from '@angular/material/slide-toggle/testing';
import { Router, provideRouter } from '@angular/router';
import {
  API,
  aTaskDetails,
  aTaskSummary,
  boardTasks,
  projects,
  users,
} from '../../../testing/test-data';
import { API_BASE_URL } from '../../core/http/api-base-url';
import { TaskSummary } from '../tasks/models/task';
import { DashboardPage } from './dashboard-page';

describe('DashboardPage', () => {
  let fixture: ComponentFixture<DashboardPage>;
  let http: HttpTestingController;
  let loader: HarnessLoader;

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;

  const columnTitles = (): string[] =>
    Array.from(element().querySelectorAll('.column-title')).map((title) =>
      (title.textContent ?? '').replace(/\s+/g, ' ').trim(),
    );

  const cardTitles = (status: string): string[] =>
    Array.from(
      element().querySelectorAll(`.column[data-status="${status}"] app-task-card .title`),
    ).map((title) => (title.textContent ?? '').trim());

  const expectTaskList = (): TestRequest => http.expectOne((req) => req.url === `${API}/api/tasks`);

  const flushTasks = async (tasks: TaskSummary[], request: TestRequest = expectTaskList()) => {
    request.flush(tasks);
    await fixture.whenStable();
  };

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: API },
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });

    fixture = TestBed.createComponent(DashboardPage);
    http = TestBed.inject(HttpTestingController);
    loader = TestbedHarnessEnvironment.loader(fixture);

    fixture.detectChanges();
    http.expectOne(`${API}/api/users`).flush(users);
    http.expectOne(`${API}/api/projects`).flush(projects);
    await flushTasks(boardTasks);
  });

  afterEach(() => http.verify());

  it('shows Created, InProgress and Completed columns and hides closed tasks by default', () => {
    expect(columnTitles()).toEqual(['Created 1', 'In progress 1', 'Completed 1']);
    expect(element().textContent).not.toContain('Old release');
  });

  it('shows and hides the Closed column with the closed-tasks option', async () => {
    const toggle = await loader.getHarness(MatSlideToggleHarness);

    await toggle.toggle();
    expect(columnTitles()).toEqual(['Created 1', 'In progress 1', 'Completed 1', 'Closed 1']);
    expect(cardTitles('Closed')).toEqual(['Old release']);

    await toggle.toggle();
    expect(columnTitles()).toEqual(['Created 1', 'In progress 1', 'Completed 1']);
  });

  it('shows project, assignee or unassigned state and status on each card', () => {
    const created = element().querySelector('.column[data-status="Created"] app-task-card')!;
    const completed = element().querySelector('.column[data-status="Completed"] app-task-card')!;

    expect(created.textContent).toContain('Website');
    expect(created.textContent).toContain('Unassigned');
    expect(created.textContent).toContain('Created');
    expect(completed.textContent).toContain('Mobile');
    expect(completed.textContent).toContain('Bob');
  });

  it('filters the board by project on the server', async () => {
    const projectFilter = await loader.getHarness(
      MatSelectHarness.with({ selector: '.project-filter' }),
    );

    await projectFilter.clickOptions({ text: 'Mobile' });

    const request = expectTaskList();
    expect(request.request.params.get('projectId')).toBe('p-mobile');
    expect(request.request.params.has('assigneeId')).toBe(false);
    await flushTasks([boardTasks[2]], request);

    expect(cardTitles('Completed')).toEqual(['Design page']);
    expect(cardTitles('Created')).toEqual([]);
  });

  it('filters the board by user and combines it with the project filter', async () => {
    const projectFilter = await loader.getHarness(
      MatSelectHarness.with({ selector: '.project-filter' }),
    );
    await projectFilter.clickOptions({ text: 'Mobile' });
    await flushTasks([boardTasks[2]]);

    const assigneeFilter = await loader.getHarness(
      MatSelectHarness.with({ selector: '.assignee-filter' }),
    );
    await assigneeFilter.clickOptions({ text: 'Bob' });

    const request = expectTaskList();
    expect(request.request.params.get('projectId')).toBe('p-mobile');
    expect(request.request.params.get('assigneeId')).toBe('u-bob');
    await flushTasks([boardTasks[2]], request);
  });

  it('opens task details in the side panel without navigating away', async () => {
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl');

    (
      element().querySelector(
        '.column[data-status="Completed"] app-task-card .title',
      ) as HTMLElement
    ).click();
    await fixture.whenStable();

    http
      .expectOne(`${API}/api/tasks/t-completed`)
      .flush(aTaskDetails({ id: 't-completed', title: 'Design page', status: 'Completed' }));
    await flushTasks(boardTasks);

    const panel = element().querySelector('app-task-detail-panel');
    expect(panel?.textContent).toContain('Design page');
    expect(panel?.textContent).toContain('Create related task');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('performs the next transition from a card and refreshes the board', async () => {
    const startedTask = aTaskSummary({
      id: 't-progress',
      title: 'Build API',
      status: 'InProgress',
    });

    const completeButton = element().querySelector(
      '.column[data-status="InProgress"] app-task-card button.advance',
    ) as HTMLButtonElement;
    expect(completeButton.textContent?.trim()).toBe('Complete');

    completeButton.click();

    const patch = http.expectOne(`${API}/api/tasks/t-progress/status`);
    expect(patch.request.method).toBe('PATCH');
    expect(patch.request.body).toEqual({ status: 'Completed' });
    patch.flush(null, { status: 204, statusText: 'No Content' });

    await flushTasks([boardTasks[0], { ...startedTask, status: 'Completed' }, boardTasks[2]]);
    expect(cardTitles('Completed')).toEqual(['Build API', 'Design page']);
  });

  it('creates a related follow-up task from a completed task on the same screen', async () => {
    (
      element().querySelector(
        '.column[data-status="Completed"] app-task-card .title',
      ) as HTMLElement
    ).click();
    await fixture.whenStable();
    http.expectOne(`${API}/api/tasks/t-completed`).flush(
      aTaskDetails({
        id: 't-completed',
        title: 'Design page',
        status: 'Completed',
        project: projects[1],
      }),
    );
    await flushTasks(boardTasks);

    await (await loader.getHarness(MatButtonHarness.with({ text: 'Create related task' }))).click();
    await flushTasks(boardTasks);

    const dialog = document.querySelector('mat-dialog-container');
    expect(dialog?.textContent).toContain('Create related task');
    expect(dialog?.textContent).toContain('Design page');

    const rootLoader = TestbedHarnessEnvironment.documentRootLoader(fixture);
    await (
      await rootLoader.getHarness(MatInputHarness.with({ selector: '[formControlName="title"]' }))
    ).setValue('Follow-up');
    await (await rootLoader.getHarness(MatButtonHarness.with({ text: 'Create task' }))).click();

    const create = http.expectOne((req) => req.method === 'POST' && req.url === `${API}/api/tasks`);
    expect(create.request.body).toEqual({
      title: 'Follow-up',
      description: null,
      projectId: 'p-mobile',
      assigneeId: null,
      relatedTaskIds: ['t-completed'],
    });
    create.flush(aTaskDetails({ id: 't-follow-up', title: 'Follow-up' }));
    await fixture.whenStable();

    // The board refreshes and the new task opens in the side panel (which also lists all tasks).
    const followUpSummary = aTaskSummary({ id: 't-follow-up', title: 'Follow-up', assignee: null });
    for (const list of http.match(
      (req) => req.method === 'GET' && req.url === `${API}/api/tasks`,
    )) {
      list.flush([...boardTasks, followUpSummary]);
    }
    http
      .expectOne(`${API}/api/tasks/t-follow-up`)
      .flush(aTaskDetails({ id: 't-follow-up', title: 'Follow-up' }));
    await fixture.whenStable();

    expect(cardTitles('Created')).toContain('Follow-up');
    const panelTitle = element().querySelector<HTMLInputElement>(
      'app-task-detail-panel input[formcontrolname="title"]',
    );
    expect(panelTitle?.value).toBe('Follow-up');
  });

  it('manages users and projects in dialogs without leaving the dashboard', async () => {
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl');

    await (await loader.getHarness(MatButtonHarness.with({ text: 'Users' }))).click();
    http.expectOne(`${API}/api/users`).flush(users);
    await fixture.whenStable();

    expect(document.querySelector('mat-dialog-container')?.textContent).toContain(
      'alice@example.com',
    );
    expect(navigate).not.toHaveBeenCalled();
    expect(element().querySelector('.board')).not.toBeNull();
  });

  it('does not allow starting an unassigned task from its card', () => {
    const startButton = element().querySelector(
      '.column[data-status="Created"] app-task-card button.advance',
    ) as HTMLButtonElement;

    expect(startButton.textContent?.trim()).toBe('Start');
    expect(startButton.disabled).toBe(true);
  });
});

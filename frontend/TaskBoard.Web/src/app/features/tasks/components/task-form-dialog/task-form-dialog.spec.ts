import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatInputHarness } from '@angular/material/input/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { API, aTaskDetails, aTaskSummary, projects, users } from '../../../../../testing/test-data';
import { API_BASE_URL } from '../../../../core/http/api-base-url';
import { TaskFormDialog, TaskFormDialogData } from './task-form-dialog';

describe('TaskFormDialog', () => {
  let fixture: ComponentFixture<TaskFormDialog>;
  let http: HttpTestingController;
  let loader: HarnessLoader;
  let close: ReturnType<typeof vi.fn>;

  async function open(data: TaskFormDialogData): Promise<void> {
    close = vi.fn();

    TestBed.configureTestingModule({
      imports: [TaskFormDialog],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: API },
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
      ],
    });

    fixture = TestBed.createComponent(TaskFormDialog);
    http = TestBed.inject(HttpTestingController);
    loader = TestbedHarnessEnvironment.loader(fixture);

    fixture.detectChanges();
    http
      .expectOne(`${API}/api/tasks`)
      .flush([
        aTaskSummary({ id: 't-source', title: 'Finished design', status: 'Completed' }),
        aTaskSummary({ id: 't-other', title: 'Other task' }),
      ]);
    await fixture.whenStable();
  }

  afterEach(() => http.verify());

  it('creates a new task that is unassigned by default', async () => {
    await open({ projects, users });

    await (
      await loader.getHarness(MatInputHarness.with({ selector: '[formControlName="title"]' }))
    ).setValue('  New work ');
    await (
      await loader.getHarness(MatSelectHarness.with({ selector: '[formControlName="projectId"]' }))
    ).clickOptions({
      text: 'Mobile',
    });
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Create task' }))).click();

    const request = http.expectOne(`${API}/api/tasks`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      title: 'New work',
      description: null,
      projectId: 'p-mobile',
      assigneeId: null,
      relatedTaskIds: [],
    });

    const created = aTaskDetails({ id: 't-new', title: 'New work' });
    request.flush(created);
    expect(close).toHaveBeenCalledWith(created);
  });

  it('creates a follow-up task related to the completed source task', async () => {
    await open({ projects, users, preset: { projectId: 'p-web', relatedTaskIds: ['t-source'] } });

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Create related task');
    expect(text).toContain('Finished design');

    await (
      await loader.getHarness(MatInputHarness.with({ selector: '[formControlName="title"]' }))
    ).setValue('Follow-up');
    await (
      await loader.getHarness(MatSelectHarness.with({ selector: '[formControlName="assigneeId"]' }))
    ).clickOptions({
      text: 'Alice',
    });
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Create task' }))).click();

    const request = http.expectOne(`${API}/api/tasks`);
    expect(request.request.body).toEqual({
      title: 'Follow-up',
      description: null,
      projectId: 'p-web',
      assigneeId: 'u-alice',
      relatedTaskIds: ['t-source'],
    });
    request.flush(aTaskDetails({ id: 't-follow-up' }));
  });

  it('shows the server explanation when the task is rejected', async () => {
    await open({ projects, users, preset: { projectId: 'p-web' } });

    await (
      await loader.getHarness(MatInputHarness.with({ selector: '[formControlName="title"]' }))
    ).setValue('Task');
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Create task' }))).click();

    http.expectOne(`${API}/api/tasks`).flush(
      {
        status: 422,
        title: 'Unprocessable request',
        detail: "Project 'p-web' does not exist.",
        code: 'task.project_not_found',
      },
      { status: 422, statusText: 'Unprocessable Content' },
    );
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      "Project 'p-web' does not exist.",
    );
    expect(close).not.toHaveBeenCalled();
  });

  it('does not submit without a title and project', async () => {
    await open({ projects, users });

    await (await loader.getHarness(MatButtonHarness.with({ text: 'Create task' }))).click();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Title is required.');
  });
});

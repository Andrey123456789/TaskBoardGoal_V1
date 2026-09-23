import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatInputHarness } from '@angular/material/input/testing';
import { API, projects } from '../../../../../testing/test-data';
import { API_BASE_URL } from '../../../../core/http/api-base-url';
import { ProjectManagementDialog } from './project-management-dialog';

describe('ProjectManagementDialog', () => {
  let fixture: ComponentFixture<ProjectManagementDialog>;
  let http: HttpTestingController;
  let loader: HarnessLoader;
  let rootLoader: HarnessLoader;

  const text = (): string => (fixture.nativeElement as HTMLElement).textContent ?? '';

  async function answerList(list = projects): Promise<void> {
    await fixture.whenStable();
    http.expectOne((req) => req.method === 'GET' && req.url === `${API}/api/projects`).flush(list);
    await fixture.whenStable();
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [ProjectManagementDialog],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: API },
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });

    fixture = TestBed.createComponent(ProjectManagementDialog);
    http = TestBed.inject(HttpTestingController);
    loader = TestbedHarnessEnvironment.loader(fixture);
    rootLoader = TestbedHarnessEnvironment.documentRootLoader(fixture);

    fixture.detectChanges();
    await answerList();
  });

  afterEach(() => http.verify());

  it('creates a project with an optional description', async () => {
    await (
      await loader.getHarness(MatInputHarness.with({ selector: '[formControlName="name"]' }))
    ).setValue('Backend');
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Add project' }))).click();

    const request = http.expectOne((req) => req.method === 'POST');
    expect(request.request.body).toEqual({ name: 'Backend', description: null });
    request.flush({ id: 'p-new', name: 'Backend', description: null, createdAt: '' });
    await answerList();
  });

  it('edits a project', async () => {
    await (await loader.getAllHarnesses(MatButtonHarness.with({ text: 'Edit' })))[1].click();
    await (
      await loader.getHarness(MatInputHarness.with({ selector: '[formControlName="description"]' }))
    ).setValue('Apps');
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Save project' }))).click();

    const request = http.expectOne(`${API}/api/projects/p-mobile`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Mobile', description: 'Apps' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await answerList();
  });

  it('explains why a project with tasks cannot be deleted', async () => {
    await (await loader.getAllHarnesses(MatButtonHarness.with({ text: 'Delete' })))[0].click();
    await (
      await rootLoader.getHarness(
        MatButtonHarness.with({ text: 'Delete', ancestor: 'mat-dialog-container' }),
      )
    ).click();
    await fixture.whenStable();

    http.expectOne(`${API}/api/projects/p-web`).flush(
      {
        status: 409,
        detail: 'A project that contains tasks cannot be deleted.',
        code: 'project.has_tasks',
      },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();

    expect(text()).toContain('A project that contains tasks cannot be deleted.');
    expect(text()).toContain('Website');
  });
});

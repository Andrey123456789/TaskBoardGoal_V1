import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatInputHarness } from '@angular/material/input/testing';
import { API, users } from '../../../../../testing/test-data';
import { API_BASE_URL } from '../../../../core/http/api-base-url';
import { UserManagementDialog } from './user-management-dialog';

describe('UserManagementDialog', () => {
  let fixture: ComponentFixture<UserManagementDialog>;
  let http: HttpTestingController;
  let loader: HarnessLoader;
  let rootLoader: HarnessLoader;

  const text = (): string => (fixture.nativeElement as HTMLElement).textContent ?? '';

  async function answerList(list = users): Promise<void> {
    await fixture.whenStable();
    http.expectOne((req) => req.method === 'GET' && req.url === `${API}/api/users`).flush(list);
    await fixture.whenStable();
  }

  async function fill(name: string, email: string): Promise<void> {
    await (
      await loader.getHarness(MatInputHarness.with({ selector: '[formControlName="name"]' }))
    ).setValue(name);
    await (
      await loader.getHarness(MatInputHarness.with({ selector: '[formControlName="email"]' }))
    ).setValue(email);
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [UserManagementDialog],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: API },
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });

    fixture = TestBed.createComponent(UserManagementDialog);
    http = TestBed.inject(HttpTestingController);
    loader = TestbedHarnessEnvironment.loader(fixture);
    rootLoader = TestbedHarnessEnvironment.documentRootLoader(fixture);

    fixture.detectChanges();
    await answerList();
  });

  afterEach(() => http.verify());

  it('lists ordinary users', () => {
    expect(text()).toContain('alice@example.com');
    expect(text()).toContain('bob@example.com');
  });

  it('creates a user and clears the form without showing errors', async () => {
    await fill(' Carol ', 'carol@example.com');
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Add user' }))).click();

    const request = http.expectOne((req) => req.method === 'POST');
    expect(request.request.body).toEqual({ name: 'Carol', email: 'carol@example.com' });
    request.flush({ id: 'u-carol', name: 'Carol', email: 'carol@example.com', createdAt: '' });

    await answerList([
      ...users,
      { id: 'u-carol', name: 'Carol', email: 'carol@example.com', createdAt: '' },
    ]);
    expect(text()).toContain('carol@example.com');
    expect(text()).not.toContain('is required');
  });

  it('edits an existing user', async () => {
    const editButtons = await loader.getAllHarnesses(MatButtonHarness.with({ text: 'Edit' }));
    await editButtons[0].click();
    await fill('Alice Smith', 'alice@example.com');
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Save user' }))).click();

    const request = http.expectOne(`${API}/api/users/u-alice`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Alice Smith', email: 'alice@example.com' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await answerList();
  });

  it('shows the conflict message for a duplicate email', async () => {
    await fill('Other', 'ALICE@example.com');
    await (await loader.getHarness(MatButtonHarness.with({ text: 'Add user' }))).click();

    http
      .expectOne((req) => req.method === 'POST')
      .flush(
        {
          status: 409,
          detail: 'A user with this email already exists.',
          code: 'user.duplicate_email',
        },
        { status: 409, statusText: 'Conflict' },
      );
    await fixture.whenStable();

    expect(text()).toContain('A user with this email already exists.');
  });

  it('deletes a user after confirming that tasks move to Deleted User', async () => {
    const deleteButtons = await loader.getAllHarnesses(MatButtonHarness.with({ text: 'Delete' }));
    await deleteButtons[1].click();

    const confirmation = document.querySelector('mat-dialog-container')?.textContent ?? '';
    expect(confirmation).toContain('Deleted User');

    await (
      await rootLoader.getHarness(
        MatButtonHarness.with({ text: 'Delete', ancestor: 'mat-dialog-container' }),
      )
    ).click();
    await fixture.whenStable();

    const request = http.expectOne(`${API}/api/users/u-bob`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await answerList([users[0]]);

    expect(text()).not.toContain('bob@example.com');
  });
});

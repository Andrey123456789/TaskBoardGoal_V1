import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API } from '../../../../testing/test-data';
import { API_BASE_URL } from '../../../core/http/api-base-url';
import { TasksApi } from './tasks-api';

describe('TasksApi', () => {
  let api: TasksApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: API },
      ],
    });

    api = TestBed.inject(TasksApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends only the filters that are set', () => {
    api.list({ projectId: 'p-1', assigneeId: null, status: 'InProgress' }).subscribe();

    const request = http.expectOne((req) => req.url === `${API}/api/tasks`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('projectId')).toBe('p-1');
    expect(request.request.params.has('assigneeId')).toBe(false);
    expect(request.request.params.get('status')).toBe('InProgress');
    request.flush([]);
  });

  it('changes status with PATCH on the status sub-resource', () => {
    api.changeStatus('t-1', 'Completed').subscribe();

    const request = http.expectOne(`${API}/api/tasks/t-1/status`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ status: 'Completed' });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('replaces the complete related-task set with PUT', () => {
    api.replaceRelatedTasks('t-1', ['t-2', 't-3']).subscribe();

    const request = http.expectOne(`${API}/api/tasks/t-1/related-tasks`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ relatedTaskIds: ['t-2', 't-3'] });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('updates only title, description and assignee', () => {
    api.update('t-1', { title: 'T', description: null, assigneeId: 'u-1' }).subscribe();

    const request = http.expectOne(`${API}/api/tasks/t-1`);
    expect(request.request.method).toBe('PUT');
    expect(Object.keys(request.request.body).sort()).toEqual([
      'assigneeId',
      'description',
      'title',
    ]);
    request.flush(null, { status: 204, statusText: 'No Content' });
  });
});

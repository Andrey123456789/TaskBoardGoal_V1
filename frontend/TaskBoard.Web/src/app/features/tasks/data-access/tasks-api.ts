import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/http/api-base-url';
import {
  CreateTaskRequest,
  TaskDetails,
  TaskFilter,
  TaskStatus,
  TaskSummary,
  UpdateTaskRequest,
} from '../models/task';

@Injectable({ providedIn: 'root' })
export class TasksApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/api/tasks`;

  /** One request returns every matching task including project and assignee names. */
  list(filter: TaskFilter = {}): Observable<TaskSummary[]> {
    let params = new HttpParams();

    if (filter.projectId) {
      params = params.set('projectId', filter.projectId);
    }

    if (filter.assigneeId) {
      params = params.set('assigneeId', filter.assigneeId);
    }

    if (filter.status) {
      params = params.set('status', filter.status);
    }

    return this.http.get<TaskSummary[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<TaskDetails> {
    return this.http.get<TaskDetails>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateTaskRequest): Observable<TaskDetails> {
    return this.http.post<TaskDetails>(this.baseUrl, request);
  }

  update(id: string, request: UpdateTaskRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  changeStatus(id: string, status: TaskStatus): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/status`, { status });
  }

  /** Replaces the complete related-task set; an empty array removes all relationships. */
  replaceRelatedTasks(id: string, relatedTaskIds: string[]): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/related-tasks`, { relatedTaskIds });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

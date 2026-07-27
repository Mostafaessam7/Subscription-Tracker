import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CancelSubscriptionRequest,
  CreateSubscriptionRequest,
  GetSubscriptionsParams,
  PagedList,
  Subscription,
  UpdateSubscriptionRequest,
} from '../models/subscription.models';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/subscriptions`;

  getSubscriptions(params: GetSubscriptionsParams): Observable<PagedList<Subscription>> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber ?? 1)
      .set('pageSize', params.pageSize ?? 20)
      .set('sortDescending', params.sortDescending ?? false);

    if (params.searchTerm) httpParams = httpParams.set('searchTerm', params.searchTerm);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.tagId) httpParams = httpParams.set('tagId', params.tagId);
    if (params.status !== null && params.status !== undefined) httpParams = httpParams.set('status', params.status);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);

    return this.http.get<PagedList<Subscription>>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<Subscription> {
    return this.http.get<Subscription>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateSubscriptionRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  update(id: string, request: UpdateSubscriptionRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  cancel(id: string, request: CancelSubscriptionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, request);
  }

  pause(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/pause`, {});
  }

  resume(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/resume`, {});
  }
}

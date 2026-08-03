import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SubscriptionStatus } from '../models/subscription.models';

export interface ReportFilters {
  searchTerm?: string | null;
  categoryId?: string | null;
  tagId?: string | null;
  status?: SubscriptionStatus | null;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/reports`;

  exportSubscriptionsCsv(filters: ReportFilters): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/subscriptions/csv`, { params: this.toParams(filters), responseType: 'blob' });
  }

  exportSubscriptionsExcel(filters: ReportFilters): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/subscriptions/excel`, { params: this.toParams(filters), responseType: 'blob' });
  }

  exportSubscriptionsPdf(filters: ReportFilters): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/subscriptions/pdf`, { params: this.toParams(filters), responseType: 'blob' });
  }

  private toParams(filters: ReportFilters): HttpParams {
    let params = new HttpParams();
    if (filters.searchTerm) params = params.set('searchTerm', filters.searchTerm);
    if (filters.categoryId) params = params.set('categoryId', filters.categoryId);
    if (filters.tagId) params = params.set('tagId', filters.tagId);
    if (filters.status !== null && filters.status !== undefined) params = params.set('status', filters.status);
    return params;
  }
}

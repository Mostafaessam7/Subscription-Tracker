import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Category,
  CreateCategoryRequest,
  CreatePaymentMethodRequest,
  CreateTagRequest,
  PaymentMethod,
  Tag,
  UpdateCategoryRequest,
  UpdatePaymentMethodRequest,
  UpdateTagRequest,
} from '../models/catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${environment.apiBaseUrl}/categories`);
  }

  createCategory(request: CreateCategoryRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/categories`, request);
  }

  updateCategory(id: string, request: UpdateCategoryRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/categories/${id}`, request);
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/categories/${id}`);
  }

  getTags(): Observable<Tag[]> {
    return this.http.get<Tag[]>(`${environment.apiBaseUrl}/tags`);
  }

  createTag(request: CreateTagRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/tags`, request);
  }

  updateTag(id: string, request: UpdateTagRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/tags/${id}`, request);
  }

  deleteTag(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/tags/${id}`);
  }

  getPaymentMethods(): Observable<PaymentMethod[]> {
    return this.http.get<PaymentMethod[]>(`${environment.apiBaseUrl}/payment-methods`);
  }

  createPaymentMethod(request: CreatePaymentMethodRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/payment-methods`, request);
  }

  updatePaymentMethod(id: string, request: UpdatePaymentMethodRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/payment-methods/${id}`, request);
  }

  deletePaymentMethod(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/payment-methods/${id}`);
  }
}

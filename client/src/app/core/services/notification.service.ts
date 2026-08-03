import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenStorageService } from './token-storage.service';
import { AppNotification, NotificationPage } from '../models/notification.models';

/**
 * HTTP for history/mark-as-read, SignalR for live push. The hub connection is started once (from the shell) and
 * survives route navigation; ReceiveNotification pushes prepend into the in-memory list and bump the unread
 * count without a round-trip, while the initial list/count still come from HTTP on load.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly baseUrl = `${environment.apiBaseUrl}/notifications`;
  private readonly hubUrl = `${environment.apiBaseUrl.replace(/\/api\/v\d+$/, '')}/hubs/notifications`;

  private connection: HubConnection | null = null;

  readonly notifications = signal<AppNotification[]>([]);
  readonly unreadCount = signal(0);

  connect(): void {
    if (this.connection && this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => this.tokenStorage.getAccessToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('ReceiveNotification', (notification: AppNotification) => {
      this.notifications.update((list) => [notification, ...list]);
      this.unreadCount.update((count) => count + 1);
    });

    this.connection.start().catch(() => {
      // Live push is a nice-to-have on top of the HTTP-polled history/count - a failed connection (e.g. no
      // server reachable yet) shouldn't surface as a user-facing error.
    });

    this.loadInitial();
  }

  disconnect(): void {
    void this.connection?.stop();
    this.connection = null;
  }

  loadInitial(): void {
    this.getNotifications(1, 20).subscribe({ next: (page) => this.notifications.set(page.items) });
    this.getUnreadCount().subscribe({ next: (count) => this.unreadCount.set(count) });
  }

  getNotifications(pageNumber: number, pageSize: number): Observable<NotificationPage> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<NotificationPage>(this.baseUrl, { params });
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/unread-count`);
  }

  markAsRead(id: string): void {
    this.http.post<void>(`${this.baseUrl}/${id}/read`, {}).subscribe({
      next: () => {
        this.notifications.update((list) => list.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
        this.unreadCount.update((count) => Math.max(0, count - 1));
      },
    });
  }

  markAllAsRead(): void {
    this.http.post<void>(`${this.baseUrl}/read-all`, {}).subscribe({
      next: () => {
        this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
        this.unreadCount.set(0);
      },
    });
  }
}

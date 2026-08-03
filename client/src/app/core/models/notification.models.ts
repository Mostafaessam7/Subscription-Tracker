import { PagedList } from './subscription.models';

export enum NotificationType {
  RenewalReminder = 0,
  BudgetAlert = 1,
  General = 2,
}

export interface AppNotification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  relatedEntityId: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

export type NotificationPage = PagedList<AppNotification>;

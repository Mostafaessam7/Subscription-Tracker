export enum BillingFrequency {
  Weekly = 0,
  Monthly = 1,
  Quarterly = 2,
  Yearly = 3,
  Custom = 4,
  Lifetime = 5,
}

export enum SubscriptionStatus {
  Trial = 0,
  Active = 1,
  Paused = 2,
  Cancelled = 3,
  Expired = 4,
}

export interface Subscription {
  id: string;
  name: string;
  provider: string;
  logoUrl: string | null;
  websiteUrl: string | null;
  notes: string | null;
  categoryId: string | null;
  paymentMethodId: string | null;
  amount: number;
  currencyCode: string;
  billingFrequency: BillingFrequency;
  customIntervalDays: number | null;
  startDate: string;
  trialEndDate: string | null;
  nextRenewalDate: string | null;
  endDate: string | null;
  autoRenewal: boolean;
  status: SubscriptionStatus;
  tagIds: string[];
  sharedUserIds: string[];
  attachments: SubscriptionAttachment[];
}

export interface SubscriptionAttachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAtUtc: string;
}

export interface PagedList<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface GetSubscriptionsParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string | null;
  categoryId?: string | null;
  tagId?: string | null;
  status?: SubscriptionStatus | null;
  sortBy?: string | null;
  sortDescending?: boolean;
}

export interface CreateSubscriptionRequest {
  name: string;
  provider: string;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  notes?: string | null;
  categoryId?: string | null;
  paymentMethodId?: string | null;
  amount: number;
  currencyCode: string;
  billingFrequency: BillingFrequency;
  customIntervalDays?: number | null;
  startDate: string;
  trialEndDate?: string | null;
  autoRenewal: boolean;
  tagIds?: string[] | null;
}

export interface UpdateSubscriptionRequest {
  name: string;
  provider: string;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  notes?: string | null;
  categoryId?: string | null;
  paymentMethodId?: string | null;
  amount: number;
  currencyCode: string;
  tagIds?: string[] | null;
}

export interface CancelSubscriptionRequest {
  effectiveDate: string;
  reason?: string | null;
}

import { PagedList } from './subscription.models';

export interface AuditLogEntry {
  id: string;
  userEmail: string | null;
  action: string;
  entityId: string | null;
  isSuccess: boolean;
  errorCode: string | null;
  details: string | null;
  occurredAtUtc: string;
}

export type AuditLogPage = PagedList<AuditLogEntry>;

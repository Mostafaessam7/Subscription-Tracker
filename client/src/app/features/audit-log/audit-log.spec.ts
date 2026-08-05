import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuditLog } from './audit-log';
import { AuditLogService } from '../../core/services/audit-log.service';
import { AuditLogPage } from '../../core/models/audit-log.models';

function emptyPage(): AuditLogPage {
  return { items: [], totalCount: 0, pageNumber: 1, pageSize: 25, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

function createAuditLog(getAuditLogs: ReturnType<typeof vi.fn>): AuditLog {
  TestBed.configureTestingModule({
    providers: [{ provide: AuditLogService, useValue: { getAuditLogs } }],
  });

  return TestBed.runInInjectionContext(() => new AuditLog());
}

describe('AuditLog', () => {
  it('loads the first page on construction', () => {
    const getAuditLogs = vi.fn().mockReturnValue(of(emptyPage()));

    createAuditLog(getAuditLogs);

    expect(getAuditLogs).toHaveBeenCalledWith(1, 25);
  });

  it('sets a generic error and stops loading when the request fails', () => {
    const auditLog = createAuditLog(vi.fn().mockReturnValue(throwError(() => new Error('boom'))));

    expect(auditLog.isLoading()).toBe(false);
    expect(auditLog.errorMessage()).toBe('error.generic');
    expect(auditLog.page()).toBeNull();
  });

  it('updates pageNumber and reloads when going to a different page', () => {
    const getAuditLogs = vi.fn().mockReturnValue(of(emptyPage()));
    const auditLog = createAuditLog(getAuditLogs);

    auditLog.goToPage(3);

    expect(auditLog.pageNumber).toBe(3);
    expect(getAuditLogs).toHaveBeenLastCalledWith(3, 25);
  });

  describe('formatAction', () => {
    it('drops a trailing "Command" suffix', () => {
      const auditLog = createAuditLog(vi.fn().mockReturnValue(of(emptyPage())));

      expect(auditLog.formatAction('CreateSubscriptionCommand')).toBe('Create Subscription');
    });

    it('inserts a space before each subsequent capital letter (PascalCase -> spaced words)', () => {
      const auditLog = createAuditLog(vi.fn().mockReturnValue(of(emptyPage())));

      expect(auditLog.formatAction('Logout')).toBe('Logout');
      expect(auditLog.formatAction('DisableUserCommand')).toBe('Disable User');
    });
  });
});

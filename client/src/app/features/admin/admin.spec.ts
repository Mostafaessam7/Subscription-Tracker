import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Admin } from './admin';
import { AdminService } from '../../core/services/admin.service';
import { AdminUserSummary } from '../../core/models/admin.models';

function fakeUser(overrides: Partial<AdminUserSummary> = {}): AdminUserSummary {
  return {
    id: 'user-1',
    email: 'a@example.com',
    firstName: 'Ada',
    lastName: 'Lovelace',
    status: 'Active',
    isSystemAdmin: false,
    isEmailVerified: true,
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

function createAdmin(overrides: Partial<Record<string, ReturnType<typeof vi.fn>>> = {}): Admin {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: AdminService,
        useValue: {
          getSystemHealth: vi.fn().mockReturnValue(
            of({ totalUsers: 1, totalWorkspaces: 1, totalSubscriptions: 1, activeSubscriptions: 1, totalBudgets: 0 }),
          ),
          getWorkspaces: vi.fn().mockReturnValue(of([])),
          getUsers: vi.fn().mockReturnValue(of([])),
          enableUser: vi.fn().mockReturnValue(of(undefined)),
          disableUser: vi.fn().mockReturnValue(of(undefined)),
          ...overrides,
        },
      },
    ],
  });

  const component = TestBed.runInInjectionContext(() => new Admin());
  component.ngOnInit();
  return component;
}

describe('Admin', () => {
  it('loads health, workspaces, and users on init', () => {
    const admin = createAdmin({ getUsers: vi.fn().mockReturnValue(of([fakeUser()])) });

    expect(admin.isLoading()).toBe(false);
    expect(admin.health()?.totalUsers).toBe(1);
    expect(admin.users().length).toBe(1);
  });

  it('sets a generic error and stops loading when fetching users fails', () => {
    const admin = createAdmin({ getUsers: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) });

    expect(admin.isLoading()).toBe(false);
    expect(admin.errorMessage()).toBe('error.generic');
  });

  describe('toggleUserStatus', () => {
    it('disables an active user', () => {
      const disableUser = vi.fn().mockReturnValue(of(undefined));
      const enableUser = vi.fn().mockReturnValue(of(undefined));
      const admin = createAdmin({ disableUser, enableUser });

      admin.toggleUserStatus(fakeUser({ status: 'Active' }));

      expect(disableUser).toHaveBeenCalledWith('user-1');
      expect(enableUser).not.toHaveBeenCalled();
    });

    it('enables a disabled user', () => {
      const disableUser = vi.fn().mockReturnValue(of(undefined));
      const enableUser = vi.fn().mockReturnValue(of(undefined));
      const admin = createAdmin({ disableUser, enableUser });

      admin.toggleUserStatus(fakeUser({ status: 'Disabled' }));

      expect(enableUser).toHaveBeenCalledWith('user-1');
      expect(disableUser).not.toHaveBeenCalled();
    });

    it('reloads the page data after a successful toggle', () => {
      const getUsers = vi.fn().mockReturnValue(of([]));
      const admin = createAdmin({ getUsers });
      getUsers.mockClear();

      admin.toggleUserStatus(fakeUser({ status: 'Active' }));

      expect(getUsers).toHaveBeenCalledTimes(1);
    });

    it('sets a generic error when the toggle fails', () => {
      const admin = createAdmin({ disableUser: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) });

      admin.toggleUserStatus(fakeUser({ status: 'Active' }));

      expect(admin.errorMessage()).toBe('error.generic');
    });
  });
});

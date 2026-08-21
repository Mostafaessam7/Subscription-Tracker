import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Shell } from './shell';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { TranslationService } from '../../core/services/translation.service';
import { PermissionsService } from '../../core/services/permissions.service';
import { WorkspaceContextService } from '../../core/services/workspace-context.service';
import { NotificationService } from '../../core/services/notification.service';

function createShell(
  overrides: Partial<{
    authService: Partial<AuthService>;
    navigate: ReturnType<typeof vi.fn>;
  }> = {},
): { shell: Shell; navigate: ReturnType<typeof vi.fn>; clearSession: ReturnType<typeof vi.fn> } {
  const navigate = overrides.navigate ?? vi.fn();
  const clearSession = vi.fn();

  TestBed.configureTestingModule({
    providers: [
      {
        provide: AuthService,
        useValue: {
          logout: () => of(undefined),
          switchWorkspace: () => of({}),
          clearSession,
          ...overrides.authService,
        },
      },
      { provide: WorkspaceContextService, useValue: { workspaces: () => [], refresh: vi.fn() } },
      {
        provide: NotificationService,
        useValue: { connect: vi.fn(), disconnect: vi.fn(), notifications: () => [], unreadCount: () => 0 },
      },
      { provide: PermissionsService, useValue: { hasPermission: () => false, isSystemAdmin: () => false } },
      { provide: ThemeService, useValue: { theme: () => 'light', toggle: vi.fn() } },
      { provide: TranslationService, useValue: { locale: () => 'en', setLocale: vi.fn() } },
      { provide: Router, useValue: { navigateByUrl: navigate } },
    ],
  });

  const shell = TestBed.runInInjectionContext(() => new Shell());
  return { shell, navigate, clearSession };
}

describe('Shell', () => {
  describe('logout', () => {
    it('clears the session and navigates to login when the backend call succeeds', () => {
      const { shell, navigate, clearSession } = createShell();

      shell.logout();

      expect(navigate).toHaveBeenCalledWith('/auth/login');
      // AuthService.logout() itself clears the session on success (via its own tap operator), not shell.logout()
      // directly - this test only asserts the navigation half of the success path.
      expect(clearSession).not.toHaveBeenCalled();
    });

    it('still clears the local session and navigates to login when the backend call fails', () => {
      // Regression test: the backend logout call revokes the refresh token server-side, but a network drop,
      // a backend outage, or an already-invalidated token must not trap the user on the current page unable
      // to log out - see the comment on Shell.logout() for the incident this was written for.
      const { shell, navigate, clearSession } = createShell({
        authService: { logout: () => throwError(() => new Error('network error')) },
      });

      shell.logout();

      expect(clearSession).toHaveBeenCalled();
      expect(navigate).toHaveBeenCalledWith('/auth/login');
    });

    it('disconnects the notification hub before attempting to log out either way', () => {
      const disconnect = vi.fn();
      TestBed.configureTestingModule({
        providers: [
          { provide: AuthService, useValue: { logout: () => of(undefined), clearSession: vi.fn() } },
          { provide: WorkspaceContextService, useValue: { workspaces: () => [], refresh: vi.fn() } },
          {
            provide: NotificationService,
            useValue: { connect: vi.fn(), disconnect, notifications: () => [], unreadCount: () => 0 },
          },
          { provide: PermissionsService, useValue: { hasPermission: () => false, isSystemAdmin: () => false } },
          { provide: ThemeService, useValue: { theme: () => 'light', toggle: vi.fn() } },
          { provide: TranslationService, useValue: { locale: () => 'en', setLocale: vi.fn() } },
          { provide: Router, useValue: { navigateByUrl: vi.fn() } },
        ],
      });
      const shell = TestBed.runInInjectionContext(() => new Shell());

      shell.logout();

      expect(disconnect).toHaveBeenCalled();
    });
  });

  describe('switchWorkspace', () => {
    it('does a full page navigation on success rather than routing, so every component re-fetches under the new workspace', () => {
      // jsdom's window.location.assign isn't spy-able directly (non-configurable), so swap the whole
      // location object out for the duration of this test.
      const originalLocation = window.location;
      const assign = vi.fn();
      Object.defineProperty(window, 'location', { value: { ...originalLocation, assign }, writable: true });

      const { shell } = createShell();
      shell.switchWorkspace('workspace-2');

      expect(assign).toHaveBeenCalledWith('/dashboard');

      Object.defineProperty(window, 'location', { value: originalLocation, writable: true });
    });

    it('resets the switching flag and stays put if the switch fails', () => {
      const { shell } = createShell({
        authService: { switchWorkspace: () => throwError(() => new Error('forbidden')) },
      });

      shell.switchWorkspace('workspace-2');

      expect(shell.isSwitchingWorkspace()).toBe(false);
    });

    it('ignores a second switch attempt while one is already in flight', () => {
      const switchWorkspace = vi.fn().mockReturnValue(of({}));
      const { shell } = createShell({ authService: { switchWorkspace } });
      shell.isSwitchingWorkspace.set(true);

      shell.switchWorkspace('workspace-2');

      expect(switchWorkspace).not.toHaveBeenCalled();
    });
  });

  describe('nav toggling', () => {
    it('toggles the mobile nav open state', () => {
      const { shell } = createShell();

      expect(shell.isNavOpen()).toBe(false);
      shell.toggleNav();
      expect(shell.isNavOpen()).toBe(true);
      shell.closeNav();
      expect(shell.isNavOpen()).toBe(false);
    });
  });
});

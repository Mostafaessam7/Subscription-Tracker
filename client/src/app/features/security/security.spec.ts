import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Security } from './security';
import { SecurityService } from '../../core/services/security.service';
import { CurrentUser, Session } from '../../core/models/security.models';

function fakeUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
  return { id: 'user-1', email: 'a@example.com', firstName: 'Ada', lastName: 'Lovelace', twoFactorEnabled: false, ...overrides };
}

function fakeSession(overrides: Partial<Session> = {}): Session {
  return { id: 'session-1', createdAtUtc: '2026-01-01T00:00:00Z', expiresAtUtc: '2026-02-01T00:00:00Z', createdByIp: '1.2.3.4', ...overrides };
}

function createSecurity(overrides: Partial<Record<string, ReturnType<typeof vi.fn>>> = {}): Security {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: SecurityService,
        useValue: {
          getCurrentUser: vi.fn().mockReturnValue(of(fakeUser())),
          getSessions: vi.fn().mockReturnValue(of([])),
          setupTwoFactor: vi.fn().mockReturnValue(of({ secret: 'SECRET', provisioningUri: 'otpauth://x' })),
          enableTwoFactor: vi.fn().mockReturnValue(of({ recoveryCodes: ['AAAAA-11111', 'BBBBB-22222'] })),
          disableTwoFactor: vi.fn().mockReturnValue(of(undefined)),
          revokeSession: vi.fn().mockReturnValue(of(undefined)),
          ...overrides,
        },
      },
    ],
  });

  const component = TestBed.runInInjectionContext(() => new Security());
  component.ngOnInit();
  return component;
}

describe('Security', () => {
  it('loads the current user and sessions on init', () => {
    const security = createSecurity({ getSessions: vi.fn().mockReturnValue(of([fakeSession()])) });

    expect(security.isLoading()).toBe(false);
    expect(security.currentUser()?.email).toBe('a@example.com');
    expect(security.sessions().length).toBe(1);
  });

  it('sets a generic error and stops loading if fetching the current user fails', () => {
    const security = createSecurity({ getCurrentUser: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) });

    expect(security.isLoading()).toBe(false);
    expect(security.errorMessage()).toBe('error.generic');
  });

  describe('two-factor setup', () => {
    it('stores the setup info returned by the server', () => {
      const security = createSecurity();

      security.startSetup();

      expect(security.setupInfo()?.secret).toBe('SECRET');
    });

    it('clears the setup info and resets the enable form on cancel', () => {
      const security = createSecurity();
      security.startSetup();

      security.cancelSetup();

      expect(security.setupInfo()).toBeNull();
      expect(security.enableForm.controls.code.value).toBe('');
    });

    it('does not confirm-enable without a setup in progress, even with a valid code', () => {
      const enableTwoFactor = vi.fn().mockReturnValue(of(undefined));
      const security = createSecurity({ enableTwoFactor });

      security.enableForm.controls.code.setValue('123456');
      security.confirmEnable();

      expect(enableTwoFactor).not.toHaveBeenCalled();
    });

    it('does not confirm-enable with an invalid (non-6-digit) code', () => {
      const enableTwoFactor = vi.fn().mockReturnValue(of(undefined));
      const security = createSecurity({ enableTwoFactor });
      security.startSetup();

      security.enableForm.controls.code.setValue('abc');
      security.confirmEnable();

      expect(enableTwoFactor).not.toHaveBeenCalled();
      expect(security.enableForm.controls.code.touched).toBe(true);
    });

    it('enables two-factor with the setup secret and entered code, then reloads', () => {
      const enableTwoFactor = vi.fn().mockReturnValue(of({ recoveryCodes: [] }));
      const security = createSecurity({ enableTwoFactor });
      security.startSetup();

      security.enableForm.controls.code.setValue('654321');
      security.confirmEnable();

      expect(enableTwoFactor).toHaveBeenCalledWith('SECRET', '654321');
      expect(security.successMessage()).toBe('security.twoFactor.enabled');
      expect(security.setupInfo()).toBeNull();
    });

    it('stores the recovery codes returned once by the server, and clears them on acknowledge', () => {
      const enableTwoFactor = vi.fn().mockReturnValue(of({ recoveryCodes: ['AAAAA-11111', 'BBBBB-22222'] }));
      const security = createSecurity({ enableTwoFactor });
      security.startSetup();
      security.enableForm.controls.code.setValue('654321');

      security.confirmEnable();

      expect(security.recoveryCodes()).toEqual(['AAAAA-11111', 'BBBBB-22222']);

      security.acknowledgeRecoveryCodes();

      expect(security.recoveryCodes()).toBeNull();
    });

    it('maps a 400 enable response to an invalid-code message, other errors to the generic one', () => {
      const security = createSecurity({
        enableTwoFactor: vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400 }))),
      });
      security.startSetup();
      security.enableForm.controls.code.setValue('111111');

      security.confirmEnable();

      expect(security.errorMessage()).toBe('security.twoFactor.invalidCode');
    });
  });

  describe('two-factor disable', () => {
    it('requires a valid 6-digit code before disabling', () => {
      const disableTwoFactor = vi.fn().mockReturnValue(of(undefined));
      const security = createSecurity({ disableTwoFactor });

      security.confirmDisable();

      expect(disableTwoFactor).not.toHaveBeenCalled();
    });

    it('disables two-factor with the entered code and shows a success message', () => {
      const disableTwoFactor = vi.fn().mockReturnValue(of(undefined));
      const security = createSecurity({ disableTwoFactor });

      security.disableForm.controls.code.setValue('222222');
      security.confirmDisable();

      expect(disableTwoFactor).toHaveBeenCalledWith('222222');
      expect(security.successMessage()).toBe('security.twoFactor.disabled');
    });
  });

  describe('revokeSession', () => {
    it('removes the revoked session from the list', () => {
      const security = createSecurity({ getSessions: vi.fn().mockReturnValue(of([fakeSession({ id: 's1' }), fakeSession({ id: 's2' })])) });

      security.revokeSession(fakeSession({ id: 's1' }));

      expect(security.sessions().map((s) => s.id)).toEqual(['s2']);
    });

    it('sets a generic error when revoking fails', () => {
      const security = createSecurity({ revokeSession: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) });

      security.revokeSession(fakeSession({ id: 's1' }));

      expect(security.errorMessage()).toBe('error.generic');
    });
  });
});

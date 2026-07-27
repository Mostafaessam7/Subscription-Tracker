import { TestBed } from '@angular/core/testing';
import { UrlTree, provideRouter } from '@angular/router';
import { authGuard, guestGuard } from './auth.guard';
import { TokenStorageService } from '../services/token-storage.service';

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('should allow activation when both tokens are present', () => {
    const tokenStorage = TestBed.inject(TokenStorageService);
    tokenStorage.save({
      accessToken: 'access',
      accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'refresh',
      workspaceId: null,
      userId: 'user-1',
    });

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBe(true);
  });

  it('should redirect to login when no access token is present', () => {
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/auth/login');
  });
});

describe('guestGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('should allow activation when unauthenticated', () => {
    const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));

    expect(result).toBe(true);
  });

  it('should redirect to the dashboard when already authenticated', () => {
    const tokenStorage = TestBed.inject(TokenStorageService);
    tokenStorage.save({
      accessToken: 'access',
      accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'refresh',
      workspaceId: null,
      userId: 'user-1',
    });

    const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/dashboard');
  });
});

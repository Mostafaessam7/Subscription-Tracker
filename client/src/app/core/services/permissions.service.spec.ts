import { TestBed } from '@angular/core/testing';
import { PermissionsService } from './permissions.service';
import { TokenStorageService } from './token-storage.service';

function fakeJwt(payload: unknown): string {
  const base64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

  return `${base64url({ alg: 'HS256', typ: 'JWT' })}.${base64url(payload)}.fake-signature`;
}

describe('PermissionsService', () => {
  let service: PermissionsService;
  let tokenStorage: TokenStorageService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(PermissionsService);
    tokenStorage = TestBed.inject(TokenStorageService);
  });

  function seedToken(payload: unknown): void {
    tokenStorage.save({
      accessToken: fakeJwt(payload),
      accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'refresh-1',
      workspaceId: 'workspace-1',
      userId: 'user-1',
    });
  }

  it('grants a permission present as a single string claim', () => {
    seedToken({ permission: 'subscriptions:view' });

    expect(service.hasPermission('subscriptions:view')).toBe(true);
    expect(service.hasPermission('subscriptions:create')).toBe(false);
  });

  it('grants permissions present as an array claim', () => {
    seedToken({ permission: ['subscriptions:view', 'budgets:view'] });

    expect(service.hasPermission('subscriptions:view')).toBe(true);
    expect(service.hasPermission('budgets:view')).toBe(true);
    expect(service.hasPermission('budgets:manage')).toBe(false);
  });

  it('reports no permissions when the user is not authenticated', () => {
    expect(service.hasPermission('subscriptions:view')).toBe(false);
    expect(service.hasAnyPermission(['subscriptions:view', 'budgets:view'])).toBe(false);
  });

  it('reports no permissions when the token has no permission claim', () => {
    seedToken({ sub: 'user-1' });

    expect(service.hasPermission('subscriptions:view')).toBe(false);
  });

  it('hasAnyPermission returns true if at least one code matches', () => {
    seedToken({ permission: ['catalog:view'] });

    expect(service.hasAnyPermission(['catalog:manage', 'catalog:view'])).toBe(true);
    expect(service.hasAnyPermission(['catalog:manage'])).toBe(false);
  });

  it('reports isSystemAdmin true only when the claim is exactly "true"', () => {
    seedToken({ system_admin: 'true' });
    expect(service.isSystemAdmin()).toBe(true);
  });

  it('reports isSystemAdmin false when the claim is absent', () => {
    seedToken({ permission: ['subscriptions:view'] });
    expect(service.isSystemAdmin()).toBe(false);
  });

  it('reports isSystemAdmin false when unauthenticated', () => {
    expect(service.isSystemAdmin()).toBe(false);
  });

  it('tolerates a malformed token by reporting no permissions', () => {
    tokenStorage.save({
      accessToken: 'not-a-real-jwt',
      accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'refresh-1',
      workspaceId: null,
      userId: 'user-1',
    });

    expect(service.hasPermission('subscriptions:view')).toBe(false);
  });
});

import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
  let service: TokenStorageService;

  beforeEach(() => {
    localStorage.clear();
    service = new TokenStorageService();
  });

  it('should save and retrieve a full session', () => {
    service.save({
      accessToken: 'access-1',
      accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'refresh-1',
      workspaceId: 'workspace-1',
      userId: 'user-1',
    });

    expect(service.getAccessToken()).toBe('access-1');
    expect(service.getRefreshToken()).toBe('refresh-1');
    expect(service.getWorkspaceId()).toBe('workspace-1');
    expect(service.getUserId()).toBe('user-1');
    expect(service.isAccessTokenExpired()).toBe(false);
  });

  it('should treat a missing expiry as expired', () => {
    expect(service.isAccessTokenExpired()).toBe(true);
  });

  it('should treat a past expiry as expired', () => {
    service.save({
      accessToken: 'access-1',
      accessTokenExpiresAtUtc: new Date(Date.now() - 60_000).toISOString(),
      refreshToken: 'refresh-1',
      workspaceId: null,
      userId: 'user-1',
    });

    expect(service.isAccessTokenExpired()).toBe(true);
  });

  it('should not persist a workspaceId when null', () => {
    service.save({
      accessToken: 'access-1',
      accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'refresh-1',
      workspaceId: null,
      userId: 'user-1',
    });

    expect(service.getWorkspaceId()).toBeNull();
  });

  it('should clear all stored session data', () => {
    service.save({
      accessToken: 'access-1',
      accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'refresh-1',
      workspaceId: 'workspace-1',
      userId: 'user-1',
    });

    service.clear();

    expect(service.getAccessToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
    expect(service.getWorkspaceId()).toBeNull();
    expect(service.getUserId()).toBeNull();
  });

  it('should update only the token fields, leaving workspace/user untouched', () => {
    service.save({
      accessToken: 'access-1',
      accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'refresh-1',
      workspaceId: 'workspace-1',
      userId: 'user-1',
    });

    const newExpiry = new Date(Date.now() + 120_000).toISOString();
    service.updateTokens('access-2', newExpiry, 'refresh-2');

    expect(service.getAccessToken()).toBe('access-2');
    expect(service.getRefreshToken()).toBe('refresh-2');
    expect(service.getWorkspaceId()).toBe('workspace-1');
    expect(service.getUserId()).toBe('user-1');
  });
});

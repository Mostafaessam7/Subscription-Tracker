import { Injectable } from '@angular/core';

const ACCESS_TOKEN_KEY = 'st.accessToken';
const ACCESS_TOKEN_EXPIRES_KEY = 'st.accessTokenExpiresAtUtc';
const REFRESH_TOKEN_KEY = 'st.refreshToken';
const WORKSPACE_ID_KEY = 'st.workspaceId';
const USER_ID_KEY = 'st.userId';

export interface StoredSession {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  workspaceId: string | null;
  userId: string;
}

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  save(session: StoredSession): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken);
    localStorage.setItem(ACCESS_TOKEN_EXPIRES_KEY, session.accessTokenExpiresAtUtc);
    localStorage.setItem(REFRESH_TOKEN_KEY, session.refreshToken);
    localStorage.setItem(USER_ID_KEY, session.userId);

    if (session.workspaceId) {
      localStorage.setItem(WORKSPACE_ID_KEY, session.workspaceId);
    } else {
      localStorage.removeItem(WORKSPACE_ID_KEY);
    }
  }

  updateTokens(accessToken: string, accessTokenExpiresAtUtc: string, refreshToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(ACCESS_TOKEN_EXPIRES_KEY, accessTokenExpiresAtUtc);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getAccessTokenExpiresAtUtc(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_EXPIRES_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  getWorkspaceId(): string | null {
    return localStorage.getItem(WORKSPACE_ID_KEY);
  }

  setWorkspaceId(workspaceId: string): void {
    localStorage.setItem(WORKSPACE_ID_KEY, workspaceId);
  }

  getUserId(): string | null {
    return localStorage.getItem(USER_ID_KEY);
  }

  isAccessTokenExpired(): boolean {
    const expiresAt = this.getAccessTokenExpiresAtUtc();
    if (!expiresAt) {
      return true;
    }

    return new Date(expiresAt).getTime() <= Date.now();
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(ACCESS_TOKEN_EXPIRES_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(WORKSPACE_ID_KEY);
    localStorage.removeItem(USER_ID_KEY);
  }
}

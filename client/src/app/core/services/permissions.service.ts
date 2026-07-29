import { Injectable, inject } from '@angular/core';
import { TokenStorageService } from './token-storage.service';

interface DecodedAccessTokenPayload {
  permission?: string | string[];
}

/**
 * Decodes the JWT access token's `permission` claims client-side so the UI can hide actions the current
 * user's role doesn't grant. This is UX only, not a security boundary - the API enforces every permission
 * independently via [HasPermission(...)] regardless of what the UI shows.
 */
@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly tokenStorage = inject(TokenStorageService);

  hasPermission(code: string): boolean {
    return this.currentPermissions().has(code);
  }

  hasAnyPermission(codes: readonly string[]): boolean {
    const granted = this.currentPermissions();
    return codes.some((code) => granted.has(code));
  }

  private currentPermissions(): Set<string> {
    const token = this.tokenStorage.getAccessToken();
    if (!token) {
      return new Set();
    }

    const payload = this.decodePayload(token);
    const raw = payload?.permission;
    if (!raw) {
      return new Set();
    }

    return new Set(Array.isArray(raw) ? raw : [raw]);
  }

  private decodePayload(token: string): DecodedAccessTokenPayload | null {
    const segments = token.split('.');
    if (segments.length !== 3) {
      return null;
    }

    try {
      const base64 = segments[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
      const json = decodeURIComponent(
        atob(padded)
          .split('')
          .map((char) => '%' + char.charCodeAt(0).toString(16).padStart(2, '0'))
          .join(''),
      );
      return JSON.parse(json) as DecodedAccessTokenPayload;
    } catch {
      return null;
    }
  }
}

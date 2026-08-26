export interface Session {
  id: string;
  createdAtUtc: string;
  expiresAtUtc: string;
  createdByIp: string | null;
}

export interface SetupTwoFactorResponse {
  secret: string;
  provisioningUri: string;
}

export interface EnableTwoFactorResponse {
  recoveryCodes: string[];
}

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  twoFactorEnabled: boolean;
}

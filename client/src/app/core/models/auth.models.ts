export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  workspaceName?: string | null;
}

export interface LoginResponse {
  userId: string;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  workspaceId: string | null;
}

export interface RefreshTokenResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
}

export interface RegisterResponse {
  userId: string;
  workspaceId: string;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

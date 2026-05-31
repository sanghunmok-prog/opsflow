export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  user: UserSummary;
}

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

export type UserSummary = CurrentUser;

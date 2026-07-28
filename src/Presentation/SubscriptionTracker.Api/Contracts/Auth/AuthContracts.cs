namespace SubscriptionTracker.Api.Contracts.Auth;

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? WorkspaceName);

public sealed record LoginRequest(string Email, string Password, string? TotpCode = null);

public sealed record EnableTwoFactorRequest(string Secret, string Code);

public sealed record DisableTwoFactorRequest(string Code);

public sealed record RefreshTokenRequest(string RefreshToken, Guid? WorkspaceId);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record VerifyEmailRequest(Guid UserId, string Token);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

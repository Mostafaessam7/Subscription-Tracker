using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity.Events;

namespace SubscriptionTracker.Domain.Identity;

public sealed class User : AuditableAggregateRoot<Guid>
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<VerificationToken> _verificationTokens = [];

    private User(Guid id, Email email, string passwordHash, string firstName, string lastName)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Status = UserStatus.PendingVerification;
    }

    private User()
    {
    }

    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public UserStatus Status { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<VerificationToken> VerificationTokens => _verificationTokens.AsReadOnly();

    public bool IsLockedOut => LockedUntilUtc is not null && LockedUntilUtc > DateTimeOffset.UtcNow;

    public static Result<User> Register(Email email, string passwordHash, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result.Failure<User>(Error.Validation("User.EmptyPasswordHash", "Password hash cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result.Failure<User>(Error.Validation("User.EmptyFirstName", "First name cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Failure<User>(Error.Validation("User.EmptyLastName", "Last name cannot be empty."));
        }

        var user = new User(Guid.NewGuid(), email, passwordHash, firstName.Trim(), lastName.Trim());
        user.RaiseDomainEvent(new UserRegistered(user.Id, email.Value));

        return user;
    }

    public void VerifyEmail()
    {
        if (IsEmailVerified)
        {
            return;
        }

        IsEmailVerified = true;
        Status = UserStatus.Active;
        RaiseDomainEvent(new UserEmailVerified(Id));
    }

    public Result ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            return Result.Failure(Error.Validation("User.EmptyPasswordHash", "Password hash cannot be empty."));
        }

        PasswordHash = newPasswordHash;
        RaiseDomainEvent(new UserPasswordChanged(Id));
        return Result.Success();
    }

    public void RecordSuccessfulLogin(DateTimeOffset occurredOnUtc)
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        LastLoginAtUtc = occurredOnUtc;
    }

    public void RecordFailedLogin(DateTimeOffset occurredOnUtc)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts < MaxFailedLoginAttempts)
        {
            return;
        }

        LockedUntilUtc = occurredOnUtc.Add(LockoutDuration);
        Status = UserStatus.Locked;
        RaiseDomainEvent(new UserLockedOut(Id, LockedUntilUtc.Value));
    }

    public void Unlock()
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        Status = UserStatus.Active;
    }

    public void Disable() => Status = UserStatus.Disabled;

    public void EnableTwoFactor(string secret)
    {
        TwoFactorEnabled = true;
        TwoFactorSecret = secret;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset expiresAtUtc, string? createdByIp)
    {
        var token = RefreshToken.Issue(Id, tokenHash, expiresAtUtc, createdByIp);
        _refreshTokens.Add(token);
        return token;
    }

    public Result RevokeRefreshToken(string tokenHash, string? revokedByIp)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (token is null || !token.IsActive)
        {
            return Result.Failure(Error.NotFound("User.RefreshTokenNotFound", "Active refresh token was not found."));
        }

        token.Revoke(revokedByIp);
        return Result.Success();
    }

    public void RevokeAllRefreshTokens(string? revokedByIp)
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke(revokedByIp);
        }
    }

    public VerificationToken IssueVerificationToken(VerificationTokenPurpose purpose, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        var token = VerificationToken.Issue(Id, purpose, tokenHash, expiresAtUtc);
        _verificationTokens.Add(token);
        return token;
    }

    public Result<VerificationToken> ConsumeVerificationToken(string tokenHash, VerificationTokenPurpose purpose)
    {
        var token = _verificationTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.Purpose == purpose);
        if (token is null || !token.IsValid)
        {
            return Result.Failure<VerificationToken>(
                Error.NotFound("User.VerificationTokenInvalid", "This verification token is invalid or has expired."));
        }

        token.Consume();
        return token;
    }
}

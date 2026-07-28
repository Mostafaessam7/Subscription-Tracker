using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SubscriptionTracker.Api.Contracts.Auth;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Application.Identity.ChangePassword;
using SubscriptionTracker.Application.Identity.DisableTwoFactor;
using SubscriptionTracker.Application.Identity.EnableTwoFactor;
using SubscriptionTracker.Application.Identity.ForgotPassword;
using SubscriptionTracker.Application.Identity.GetCurrentUser;
using SubscriptionTracker.Application.Identity.Login;
using SubscriptionTracker.Application.Identity.Logout;
using SubscriptionTracker.Application.Identity.RefreshToken;
using SubscriptionTracker.Application.Identity.Register;
using SubscriptionTracker.Application.Identity.ResetPassword;
using SubscriptionTracker.Application.Identity.SetupTwoFactor;
using SubscriptionTracker.Application.Identity.VerifyEmail;

namespace SubscriptionTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.Password, request.FirstName, request.LastName, request.WorkspaceName);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new LoginCommand(request.Email, request.Password, ipAddress, request.TotpCode);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new RefreshTokenCommand(request.RefreshToken, request.WorkspaceId, ipAddress);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [EnableRateLimiting(DependencyInjection.AuthSensitivePolicy)]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyEmailCommand(request.UserId, request.Token), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(DependencyInjection.AuthSensitivePolicy)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(DependencyInjection.AuthSensitivePolicy)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("2fa/setup")]
    [Authorize]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetupTwoFactorQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var command = new EnableTwoFactorCommand(request.Secret, request.Code);
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DisableTwoFactorCommand(request.Code), cancellationToken);
        return result.ToActionResult(this);
    }
}

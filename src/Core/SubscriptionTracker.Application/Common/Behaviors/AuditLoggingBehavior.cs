using System.Reflection;
using System.Text.Json;
using MediatR;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Auditing;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Common.Behaviors;

/// <summary>
/// Stages an AuditLogEntry for every command (success or failure), capturing who did what and when. Registered
/// after UnitOfWorkBehavior in the pipeline (see DependencyInjection.AddApplication) so the staged entry rides
/// along in the same SaveChangesAsync call as the command's own changes - one transaction, no extra round-trip.
/// Only commands are audited; queries never reach this behavior (mirrors UnitOfWorkBehavior's IBaseCommand check).
/// </summary>
public sealed class AuditLoggingBehavior<TRequest, TResponse>(IAuditLogWriter auditLogWriter, ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly string[] SensitivePropertyNameFragments =
        ["password", "token", "code", "secret", "key"];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IBaseCommand)
        {
            var entry = AuditLogEntry.Create(
                currentUserService.WorkspaceId,
                currentUserService.UserId,
                currentUserService.Email,
                typeof(TRequest).Name,
                TryExtractEntityId(request),
                response.IsSuccess,
                response.IsFailure ? response.Error.Code : null,
                BuildDetails(request),
                DateTimeOffset.UtcNow);

            auditLogWriter.Stage(entry);
        }

        return response;
    }

    private static Guid? TryExtractEntityId(TRequest request)
    {
        var idProperty = typeof(TRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType == typeof(Guid) && p.Name is "Id" or "SubscriptionId" or "BudgetId"
                or "CategoryId" or "TagId" or "PaymentMethodId" or "MemberId" or "AttachmentId" or "WorkspaceMemberId");

        return idProperty?.GetValue(request) as Guid?;
    }

    private static string? BuildDetails(TRequest request)
    {
        var properties = typeof(TRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => !SensitivePropertyNameFragments.Any(fragment => p.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(p => p.Name, p => SafeGetValue(p, request));

        if (properties.Count == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(properties);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static object? SafeGetValue(PropertyInfo property, TRequest request)
    {
        try
        {
            return property.GetValue(request);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }
}

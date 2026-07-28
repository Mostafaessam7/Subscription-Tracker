using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.UpdateWorkspaceSettings;

public sealed record UpdateWorkspaceSettingsCommand(string DefaultCurrencyCode, string TimeZoneId, string Locale) : ICommand;

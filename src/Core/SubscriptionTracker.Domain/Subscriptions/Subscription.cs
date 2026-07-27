using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Domain.Subscriptions.Events;

namespace SubscriptionTracker.Domain.Subscriptions;

public sealed class Subscription : AuditableAggregateRoot<Guid>
{
    private readonly List<RenewalHistoryEntry> _renewalHistory = [];
    private readonly List<SubscriptionAttachment> _attachments = [];
    // EF Core's primitive-collection value comparer requires an ordered IList<T>, so these are Lists
    // with manual deduplication rather than HashSet/SortedSet, even though they are semantically sets.
    private readonly List<Guid> _tagIds = [];
    private readonly List<Guid> _sharedUserIds = [];
    private readonly List<int> _reminderDaysBeforeRenewal = [3, 7];

    private Subscription(
        Guid id,
        Guid workspaceId,
        Guid ownerId,
        string name,
        string provider,
        Money price,
        BillingCycle billingCycle,
        DateOnly startDate,
        DateOnly? trialEndDate,
        bool autoRenewal)
        : base(id)
    {
        WorkspaceId = workspaceId;
        OwnerId = ownerId;
        Name = name;
        Provider = provider;
        Price = price;
        BillingCycle = billingCycle;
        StartDate = startDate;
        TrialEndDate = trialEndDate;
        AutoRenewal = autoRenewal;
        Status = trialEndDate is not null ? SubscriptionStatus.Trial : SubscriptionStatus.Active;
        NextRenewalDate = billingCycle.Frequency == Enums.BillingFrequency.Lifetime
            ? null
            : trialEndDate ?? billingCycle.CalculateNextRenewalDate(startDate);
    }

    private Subscription()
    {
    }

    public Guid WorkspaceId { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? Notes { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? PaymentMethodId { get; private set; }

    public Money Price { get; private set; } = null!;
    public BillingCycle BillingCycle { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }
    public DateOnly? TrialEndDate { get; private set; }
    public DateOnly? NextRenewalDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool AutoRenewal { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public IReadOnlyCollection<RenewalHistoryEntry> RenewalHistory => _renewalHistory.AsReadOnly();
    public IReadOnlyCollection<SubscriptionAttachment> Attachments => _attachments.AsReadOnly();
    public IReadOnlyCollection<Guid> TagIds => _tagIds.ToList().AsReadOnly();
    public IReadOnlyCollection<Guid> SharedUserIds => _sharedUserIds.ToList().AsReadOnly();
    public IReadOnlyCollection<int> ReminderDaysBeforeRenewal => _reminderDaysBeforeRenewal.ToList().AsReadOnly();

    public static Result<Subscription> Create(
        Guid workspaceId,
        Guid ownerId,
        string name,
        string provider,
        Money price,
        BillingCycle billingCycle,
        DateOnly startDate,
        DateOnly? trialEndDate = null,
        bool autoRenewal = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Subscription>(Error.Validation("Subscription.EmptyName", "Subscription name cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result.Failure<Subscription>(Error.Validation("Subscription.EmptyProvider", "Provider cannot be empty."));
        }

        if (trialEndDate is not null && trialEndDate < startDate)
        {
            return Result.Failure<Subscription>(
                Error.Validation("Subscription.InvalidTrialEndDate", "Trial end date cannot be before the start date."));
        }

        var subscription = new Subscription(
            Guid.NewGuid(), workspaceId, ownerId, name.Trim(), provider.Trim(), price, billingCycle, startDate, trialEndDate, autoRenewal);

        subscription.RaiseDomainEvent(new SubscriptionCreated(subscription.Id, workspaceId, ownerId, subscription.Name));

        return subscription;
    }

    public void UpdateDetails(string name, string provider, string? logoUrl, string? websiteUrl, string? notes)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(provider))
        {
            Provider = provider.Trim();
        }

        LogoUrl = logoUrl;
        WebsiteUrl = websiteUrl;
        Notes = notes;
    }

    public void UpdatePricing(Money price) => Price = price;

    public void ChangeBillingCycle(BillingCycle billingCycle)
    {
        BillingCycle = billingCycle;
        if (Status is SubscriptionStatus.Active && billingCycle.Frequency != Enums.BillingFrequency.Lifetime)
        {
            NextRenewalDate = billingCycle.CalculateNextRenewalDate(DateOnly.FromDateTime(DateTime.UtcNow));
        }
    }

    public void ChangeCategory(Guid? categoryId) => CategoryId = categoryId;

    public void ChangePaymentMethod(Guid? paymentMethodId) => PaymentMethodId = paymentMethodId;

    public void EnableAutoRenewal() => AutoRenewal = true;

    public void DisableAutoRenewal() => AutoRenewal = false;

    public void AddTag(Guid tagId)
    {
        if (!_tagIds.Contains(tagId))
        {
            _tagIds.Add(tagId);
        }
    }

    public void RemoveTag(Guid tagId) => _tagIds.Remove(tagId);

    public void ShareWith(Guid userId)
    {
        if (!_sharedUserIds.Contains(userId))
        {
            _sharedUserIds.Add(userId);
        }
    }

    public void Unshare(Guid userId) => _sharedUserIds.Remove(userId);

    public Result SetReminderDaysBeforeRenewal(IEnumerable<int> days)
    {
        var distinctDays = days.Distinct().ToList();

        if (distinctDays.Any(d => d <= 0))
        {
            return Result.Failure(
                Error.Validation("Subscription.InvalidReminderDay", "Reminder days must be positive integers."));
        }

        distinctDays.Sort();

        _reminderDaysBeforeRenewal.Clear();
        _reminderDaysBeforeRenewal.AddRange(distinctDays);

        return Result.Success();
    }

    public Result<SubscriptionAttachment> AddAttachment(
        string fileName, string contentType, long sizeBytes, string storagePath, Guid uploadedBy)
    {
        var attachmentResult = SubscriptionAttachment.Create(Id, fileName, contentType, sizeBytes, storagePath, uploadedBy);
        if (attachmentResult.IsFailure)
        {
            return Result.Failure<SubscriptionAttachment>(attachmentResult.Error);
        }

        _attachments.Add(attachmentResult.Value);
        return attachmentResult;
    }

    public Result RemoveAttachment(Guid attachmentId)
    {
        var attachment = _attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment is null)
        {
            return Result.Failure(Error.NotFound("Subscription.AttachmentNotFound", "Attachment was not found."));
        }

        _attachments.Remove(attachment);
        return Result.Success();
    }

    public Result Renew(DateTimeOffset occurredOnUtc)
    {
        if (Status is not (SubscriptionStatus.Active or SubscriptionStatus.Trial))
        {
            return Result.Failure(Error.Conflict("Subscription.CannotRenew", $"A subscription in '{Status}' status cannot be renewed."));
        }

        if (BillingCycle.Frequency == Enums.BillingFrequency.Lifetime)
        {
            return Result.Failure(Error.Conflict("Subscription.LifetimeCannotRenew", "Lifetime subscriptions do not renew."));
        }

        var previousRenewalDate = NextRenewalDate ?? StartDate;
        var newRenewalDate = BillingCycle.CalculateNextRenewalDate(previousRenewalDate);

        _renewalHistory.Add(RenewalHistoryEntry.Create(Id, occurredOnUtc, Price, previousRenewalDate, newRenewalDate));

        NextRenewalDate = newRenewalDate;
        Status = SubscriptionStatus.Active;
        TrialEndDate = null;

        RaiseDomainEvent(new SubscriptionRenewed(Id, newRenewalDate));

        return Result.Success();
    }

    public Result Cancel(DateOnly effectiveDate, string? reason = null)
    {
        if (Status is SubscriptionStatus.Cancelled)
        {
            return Result.Failure(Error.Conflict("Subscription.AlreadyCancelled", "This subscription is already cancelled."));
        }

        Status = SubscriptionStatus.Cancelled;
        EndDate = effectiveDate;
        AutoRenewal = false;
        NextRenewalDate = null;

        RaiseDomainEvent(new SubscriptionCancelled(Id, reason));

        return Result.Success();
    }

    public Result Pause()
    {
        if (Status is not (SubscriptionStatus.Active or SubscriptionStatus.Trial))
        {
            return Result.Failure(Error.Conflict("Subscription.CannotPause", $"A subscription in '{Status}' status cannot be paused."));
        }

        Status = SubscriptionStatus.Paused;
        RaiseDomainEvent(new SubscriptionPaused(Id));

        return Result.Success();
    }

    public Result Resume()
    {
        if (Status is not SubscriptionStatus.Paused)
        {
            return Result.Failure(Error.Conflict("Subscription.CannotResume", "Only a paused subscription can be resumed."));
        }

        Status = SubscriptionStatus.Active;
        RaiseDomainEvent(new SubscriptionResumed(Id));

        return Result.Success();
    }

    public void MarkExpiredIfPastRenewalDate(DateOnly today)
    {
        if (Status is SubscriptionStatus.Active && !AutoRenewal && NextRenewalDate is not null && NextRenewalDate < today)
        {
            Status = SubscriptionStatus.Expired;
        }
    }
}

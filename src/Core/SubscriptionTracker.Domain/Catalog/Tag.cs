using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Catalog;

public sealed class Tag : AuditableAggregateRoot<Guid>
{
    private Tag(Guid id, Guid workspaceId, string name, string? color)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Color = color;
    }

    private Tag()
    {
    }

    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Color { get; private set; }

    public static Result<Tag> Create(Guid workspaceId, string name, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Tag>(Error.Validation("Tag.EmptyName", "Tag name cannot be empty."));
        }

        return new Tag(Guid.NewGuid(), workspaceId, name.Trim(), color);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Tag.EmptyName", "Tag name cannot be empty."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void UpdateColor(string? color) => Color = color;
}

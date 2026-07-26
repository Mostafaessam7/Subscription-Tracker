using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Catalog;

public sealed class Category : AuditableAggregateRoot<Guid>
{
    private Category(Guid id, Guid workspaceId, string name, string? color, string? icon)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Color = color;
        Icon = icon;
    }

    private Category()
    {
    }

    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Color { get; private set; }
    public string? Icon { get; private set; }

    public static Result<Category> Create(Guid workspaceId, string name, string? color = null, string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Category>(Error.Validation("Category.EmptyName", "Category name cannot be empty."));
        }

        return new Category(Guid.NewGuid(), workspaceId, name.Trim(), color, icon);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Category.EmptyName", "Category name cannot be empty."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void UpdateAppearance(string? color, string? icon)
    {
        Color = color;
        Icon = icon;
    }
}

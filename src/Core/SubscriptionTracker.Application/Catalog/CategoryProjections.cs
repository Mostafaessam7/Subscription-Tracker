using System.Linq.Expressions;
using SubscriptionTracker.Domain.Catalog;

namespace SubscriptionTracker.Application.Catalog;

internal static class CategoryProjections
{
    public static readonly Expression<Func<Category, CategoryDto>> ToDto = c =>
        new CategoryDto(c.Id, c.Name, c.Color, c.Icon);
}

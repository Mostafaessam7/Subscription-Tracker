using System.Linq.Expressions;
using SubscriptionTracker.Domain.Catalog;

namespace SubscriptionTracker.Application.Catalog;

internal static class TagProjections
{
    public static readonly Expression<Func<Tag, TagDto>> ToDto = t => new TagDto(t.Id, t.Name, t.Color);
}

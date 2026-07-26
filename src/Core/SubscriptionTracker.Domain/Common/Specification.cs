using System.Linq.Expressions;

namespace SubscriptionTracker.Domain.Common;

public abstract class Specification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    public List<Expression<Func<T, object>>> Includes { get; } = [];

    public List<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public bool IgnoreQueryFilters { get; private set; }

    protected void AddCriteria(Expression<Func<T, bool>> criteria) =>
        Criteria = Criteria is null ? criteria : Criteria.And(criteria);

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);

    protected void AddOrderBy(Expression<Func<T, object>> keySelector, bool descending = false) =>
        OrderBy.Add((keySelector, descending));

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    protected void ApplyIgnoreQueryFilters() => IgnoreQueryFilters = true;
}

internal static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftBody = new ParameterReplacer(parameter).Visit(left.Body);
        var rightBody = new ParameterReplacer(parameter).Visit(right.Body);

        var combined = Expression.AndAlso(leftBody!, rightBody!);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression parameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => parameter;
    }
}

using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.Categories.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    IRepository<Category, Guid> categoryRepository, ICurrentUserService currentUserService)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("DeleteCategory.NotFound", "Category was not found."));
        }

        categoryRepository.Remove(category);

        return Result.Success();
    }
}

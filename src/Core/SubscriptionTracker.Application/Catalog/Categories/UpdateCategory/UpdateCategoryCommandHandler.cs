using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Catalog.Specifications;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    IRepository<Category, Guid> categoryRepository, ICurrentUserService currentUserService)
    : ICommandHandler<UpdateCategoryCommand>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("UpdateCategory.NotFound", "Category was not found."));
        }

        var name = request.Name.Trim();

        var existing = await categoryRepository.FirstOrDefaultAsync(
            new CategoryByWorkspaceAndNameSpecification(category.WorkspaceId, name), cancellationToken);

        if (existing is not null && existing.Id != category.Id)
        {
            return Result.Failure(Error.Conflict("UpdateCategory.DuplicateName", "A category with this name already exists."));
        }

        var renameResult = category.Rename(name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        category.UpdateAppearance(request.Color, request.Icon);
        categoryRepository.Update(category);

        return Result.Success();
    }
}

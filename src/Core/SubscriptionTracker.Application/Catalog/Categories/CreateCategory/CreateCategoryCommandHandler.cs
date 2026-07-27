using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Catalog.Specifications;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.Categories.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IRepository<Category, Guid> categoryRepository, ICurrentUserService currentUserService)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("CreateCategory.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var workspaceId = currentUserService.WorkspaceId.Value;
        var name = request.Name.Trim();

        var existing = await categoryRepository.FirstOrDefaultAsync(
            new CategoryByWorkspaceAndNameSpecification(workspaceId, name), cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Guid>(
                Error.Conflict("CreateCategory.DuplicateName", "A category with this name already exists."));
        }

        var categoryResult = Category.Create(workspaceId, name, request.Color, request.Icon);
        if (categoryResult.IsFailure)
        {
            return Result.Failure<Guid>(categoryResult.Error);
        }

        categoryRepository.Add(categoryResult.Value);

        return Result.Success(categoryResult.Value.Id);
    }
}

using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess ? controller.NoContent() : ToProblem(result.Error, controller);

    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value) : ToProblem(result.Error, controller);

    public static IActionResult ToCreatedActionResult<T>(
        this Result<T> result, ControllerBase controller, string actionName, Func<T, object> routeValues) =>
        result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues(result.Value), result.Value)
            : ToProblem(result.Error, controller);

    private static ObjectResult ToProblem(Error error, ControllerBase controller) =>
        controller.Problem(detail: error.Message, title: error.Code, statusCode: MapStatusCode(error.Type));

    private static int MapStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };
}

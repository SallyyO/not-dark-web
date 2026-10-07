namespace API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services;

[ApiController]
[Route("categories")]
[Produces("application/json")]
public class AdminCategoriesController(ICategoryService service) : ControllerBase
{
    public const string AdminRole = "Admin";

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await service.GetAllAsync(ct));

    [HttpGet("{id}", Name = "GetCategory")]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result);
    }

    [HttpPost]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CategoryRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetCategory", new { id = result.Value!.Id }, result.Value)
            : ToProblem(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(string id, CategoryRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AdminRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToProblem(result);
    }

    private ActionResult ToProblem(ServiceResult result) => result.Error switch
    {
        ServiceError.Validation => ValidationProblem(
            new ValidationProblemDetails(result.ValidationErrors ?? [])),
        ServiceError.NotFound => Problem(
            title: "Not Found",
            detail: result.Message,
            statusCode: StatusCodes.Status404NotFound),
        ServiceError.Conflict => Problem(
            title: "Conflict",
            detail: result.Message,
            statusCode: StatusCodes.Status409Conflict),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}
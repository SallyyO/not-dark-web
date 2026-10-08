namespace Service.Services;

using Infra.Entities;
using Infra.Repositories;

// Models

public record CategoryDto(string Id, string Name, string? Description);

public record CategoryRequest(string? Name, string? Description = null);

public enum ServiceError { None, Validation, NotFound, Conflict }

public class ServiceResult
{
    public ServiceError Error { get; }
    public string? Message { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }
    public bool IsSuccess => Error == ServiceError.None;

    protected ServiceResult(ServiceError error, string? message = null, Dictionary<string, string[]>? validationErrors = null)
    {
        Error = error;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public static ServiceResult Ok() => new(ServiceError.None);
    public static ServiceResult NotFound(string message) => new(ServiceError.NotFound, message);
}

public sealed class ServiceResult<T> : ServiceResult
{
    public T? Value { get; }

    private ServiceResult(T value) : base(ServiceError.None) => Value = value;

    private ServiceResult(ServiceError error, string? message, Dictionary<string, string[]>? validationErrors = null)
        : base(error, message, validationErrors) { }

    public static ServiceResult<T> Ok(T value) => new(value);
    public static new ServiceResult<T> NotFound(string message) => new(ServiceError.NotFound, message);
    public static ServiceResult<T> Conflict(string message) => new(ServiceError.Conflict, message);
    public static ServiceResult<T> Invalid(Dictionary<string, string[]> errors) =>
        new(ServiceError.Validation, "One or more validation errors occurred.", errors);
}

// Service

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<CategoryDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ServiceResult<CategoryDto>> CreateAsync(CategoryRequest request, CancellationToken ct = default);
    Task<ServiceResult<CategoryDto>> UpdateAsync(string id, CategoryRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default);
}

public class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await repository.GetAllAsync(ct);
        return categories.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<CategoryDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(id, ct);
        return category is null
            ? ServiceResult<CategoryDto>.NotFound(NotFoundMessage(id))
            : ServiceResult<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<ServiceResult<CategoryDto>> CreateAsync(CategoryRequest request, CancellationToken ct = default)
    {
        if (!TryValidate(request, out var name, out var description, out var errors))
            return ServiceResult<CategoryDto>.Invalid(errors);

        if (await repository.NameExistsAsync(name, excludeId: null, ct))
            return ServiceResult<CategoryDto>.Conflict(DuplicateMessage(name));

        var category = await repository.AddAsync(new Category { Name = name, Description = description }, ct);
        return ServiceResult<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<ServiceResult<CategoryDto>> UpdateAsync(string id, CategoryRequest request, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
            return ServiceResult<CategoryDto>.NotFound(NotFoundMessage(id));

        if (!TryValidate(request, out var name, out var description, out var errors))
            return ServiceResult<CategoryDto>.Invalid(errors);

        if (await repository.NameExistsAsync(name, excludeId: id, ct))
            return ServiceResult<CategoryDto>.Conflict(DuplicateMessage(name));

        category.Name = name;
        category.Description = description;

        if (!await repository.UpdateAsync(category, ct))
            return ServiceResult<CategoryDto>.NotFound(NotFoundMessage(id));

        return ServiceResult<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        return await repository.DeleteAsync(id, ct)
            ? ServiceResult.Ok()
            : ServiceResult.NotFound(NotFoundMessage(id));
    }

    //Helpers

    private static bool TryValidate(
        CategoryRequest? request,
        out string name,
        out string? description,
        out Dictionary<string, string[]> errors)
    {
        errors = [];
        name = request?.Name?.Trim() ?? "";
        description = string.IsNullOrWhiteSpace(request?.Description) ? null : request.Description.Trim();

        if (name.Length == 0)
            errors["name"] = ["Name is required."];
        else if (name.Length > MaxNameLength)
            errors["name"] = [$"Name must be at most {MaxNameLength} characters."];

        if (description is { Length: > MaxDescriptionLength })
            errors["description"] = [$"Description must be at most {MaxDescriptionLength} characters."];

        return errors.Count == 0;
    }

    private static CategoryDto ToDto(Category c) => new(c.CategoryId, c.Name, c.Description);

    private static string NotFoundMessage(string id) => $"Category '{id}' was not found.";
    private static string DuplicateMessage(string name) => $"A category named '{name}' already exists.";
}
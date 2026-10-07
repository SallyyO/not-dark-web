namespace Infra.Repositories;

using Infra.Entities;
using LinqToDB;
using LinqToDB.Async;

/// <summary>Persistence operations for product categories.</summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Case-insensitive name lookup. Pass <paramref name="excludeId"/> to ignore the category being updated.</summary>
    Task<bool> NameExistsAsync(string name, string? excludeId = null, CancellationToken ct = default);

    Task<Category> AddAsync(Category category, CancellationToken ct = default);

    /// <returns><c>true</c> if a row was updated, <c>false</c> if the category no longer exists.</returns>
    Task<bool> UpdateAsync(Category category, CancellationToken ct = default);

    /// <returns><c>true</c> if a row was deleted, <c>false</c> if the category did not exist.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}

public class CategoryRepository(AppDb db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        await db.Categories
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public Task<Category?> GetByIdAsync(string id, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id, ct);

    public Task<bool> NameExistsAsync(string name, string? excludeId = null, CancellationToken ct = default)
    {
        var lowered = name.ToLower();
        var query = db.Categories.Where(c => c.Name.ToLower() == lowered);

        if (excludeId is not null)
            query = query.Where(c => c.CategoryId != excludeId);

        return query.AnyAsync(ct);
    }

    public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
    {
        await db.InsertAsync(category, token: ct);
        return category;
    }

    public async Task<bool> UpdateAsync(Category category, CancellationToken ct = default) =>
        await db.UpdateAsync(category, token: ct) > 0;

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default) =>
        await db.Categories
            .Where(c => c.CategoryId == id)
            .DeleteAsync(ct) > 0;
}
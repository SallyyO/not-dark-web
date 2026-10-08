using Infra.Entities;
using Infra.Repositories;
using Service.Services;

namespace not_dark_web.UnitTests.Services;

public class CategoryServiceTests
{
    [Fact]
    public async Task GetAllAsync_MapsRepositoryCategoriesToDtos()
    {
        var repository = new FakeCategoryRepository
        {
            Categories =
            [
                new Category { CategoryId = "1", Name = " Books ", Description = " Reading " },
                new Category { CategoryId = "2", Name = "Games", Description = null }
            ]
        };
        var service = new CategoryService(repository);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(new CategoryDto("1", " Books ", " Reading "), result[0]);
        Assert.Equal(new CategoryDto("2", "Games", null), result[1]);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExists_ReturnsDto()
    {
        var repository = new FakeCategoryRepository
        {
            Categories = [new Category { CategoryId = "cat-1", Name = "Books", Description = "Reading" }]
        };
        var service = new CategoryService(repository);

        var result = await service.GetByIdAsync("cat-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(new CategoryDto("cat-1", "Books", "Reading"), result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var service = new CategoryService(new FakeCategoryRepository());

        var result = await service.GetByIdAsync("missing");

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceError.NotFound, result.Error);
        Assert.Equal("Category 'missing' was not found.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsMissing_ReturnsValidationError()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var result = await service.CreateAsync(new CategoryRequest("   ", "Description"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.Contains("name", result.ValidationErrors!.Keys);
        Assert.Empty(repository.AddedCategories);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsTooLong_ReturnsValidationError()
    {
        var service = new CategoryService(new FakeCategoryRepository());

        var result = await service.CreateAsync(new CategoryRequest(new string('x', CategoryService.MaxNameLength + 1)));

        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.Contains("at most 100", result.ValidationErrors!["name"][0]);
    }

    [Fact]
    public async Task CreateAsync_WhenDescriptionIsTooLong_ReturnsValidationError()
    {
        var service = new CategoryService(new FakeCategoryRepository());

        var result = await service.CreateAsync(
            new CategoryRequest("Books", new string('x', CategoryService.MaxDescriptionLength + 1)));

        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.Contains("at most 500", result.ValidationErrors!["description"][0]);
    }

    [Fact]
    public async Task CreateAsync_TrimsValuesAndStoresNullForWhitespaceDescription()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var result = await service.CreateAsync(new CategoryRequest("  Books  ", "   "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Books", result.Value!.Name);
        Assert.Null(result.Value.Description);
        Assert.Single(repository.AddedCategories);
        Assert.Equal("Books", repository.AddedCategories[0].Name);
        Assert.Null(repository.AddedCategories[0].Description);
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ReturnsConflict()
    {
        var repository = new FakeCategoryRepository
        {
            ExistingNames = ["Books"]
        };
        var service = new CategoryService(repository);

        var result = await service.CreateAsync(new CategoryRequest("  books  "));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceError.Conflict, result.Error);
        Assert.Equal("A category named 'books' already exists.", result.Message);
        Assert.Empty(repository.AddedCategories);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsCategoryAndReturnsDto()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var result = await service.CreateAsync(new CategoryRequest("  Books  ", "  Reading  "));

        Assert.True(result.IsSuccess);
        Assert.Single(repository.AddedCategories);
        var added = repository.AddedCategories[0];
        Assert.Equal("Books", added.Name);
        Assert.Equal("Reading", added.Description);
        Assert.Equal(added.CategoryId, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var result = await service.UpdateAsync("missing", new CategoryRequest("Books"));

        Assert.Equal(ServiceError.NotFound, result.Error);
        Assert.False(repository.UpdateWasCalled);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsInvalid_ReturnsValidationError()
    {
        var repository = new FakeCategoryRepository
        {
            Categories = [new Category { CategoryId = "1", Name = "Books" }]
        };
        var service = new CategoryService(repository);

        var result = await service.UpdateAsync("1", new CategoryRequest(""));

        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.False(repository.UpdateWasCalled);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameConflictsWithAnotherCategory_ReturnsConflict()
    {
        var repository = new FakeCategoryRepository
        {
            Categories = [new Category { CategoryId = "1", Name = "Books" }],
            ExistingNames = ["Games"]
        };
        var service = new CategoryService(repository);

        var result = await service.UpdateAsync("1", new CategoryRequest("Games"));

        Assert.Equal(ServiceError.Conflict, result.Error);
        Assert.False(repository.UpdateWasCalled);
        Assert.Equal("Books", repository.Categories[0].Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryUpdateFails_ReturnsNotFound()
    {
        var repository = new FakeCategoryRepository
        {
            Categories = [new Category { CategoryId = "1", Name = "Books" }],
            UpdateResult = false
        };
        var service = new CategoryService(repository);

        var result = await service.UpdateAsync("1", new CategoryRequest("Games"));

        Assert.Equal(ServiceError.NotFound, result.Error);
        Assert.Equal("Category '1' was not found.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesCategoryAndReturnsDto()
    {
        var repository = new FakeCategoryRepository
        {
            Categories = [new Category { CategoryId = "1", Name = "Books", Description = "Old" }]
        };
        var service = new CategoryService(repository);

        var result = await service.UpdateAsync("1", new CategoryRequest(" Games ", " New description "));

        Assert.True(result.IsSuccess);
        Assert.Equal(new CategoryDto("1", "Games", "New description"), result.Value);
        Assert.True(repository.UpdateWasCalled);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryDeletesRow_ReturnsSuccess()
    {
        var repository = new FakeCategoryRepository { DeleteResult = true };
        var service = new CategoryService(repository);

        var result = await service.DeleteAsync("1");

        Assert.True(result.IsSuccess);
        Assert.Equal("1", repository.LastDeletedId);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryDoesNotDeleteRow_ReturnsNotFound()
    {
        var repository = new FakeCategoryRepository { DeleteResult = false };
        var service = new CategoryService(repository);

        var result = await service.DeleteAsync("missing");

        Assert.Equal(ServiceError.NotFound, result.Error);
        Assert.Equal("Category 'missing' was not found.", result.Message);
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Categories { get; set; } = [];
        public HashSet<string> ExistingNames { get; set; } = [];
        public List<Category> AddedCategories { get; } = [];
        public bool UpdateResult { get; set; } = true;
        public bool DeleteResult { get; set; }
        public bool UpdateWasCalled { get; private set; }
        public string? LastDeletedId { get; private set; }
        public string? LastExcludeId { get; private set; }

        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Category>>(Categories.ToList());

        public Task<Category?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Categories.FirstOrDefault(c => c.CategoryId == id));

        public Task<bool> NameExistsAsync(string name, string? excludeId = null, CancellationToken ct = default)
        {
            LastExcludeId = excludeId;
            var exists = ExistingNames.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                exists = Categories.Any(x =>
                    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) && x.CategoryId != excludeId);
            return Task.FromResult(exists);
        }

        public Task<Category> AddAsync(Category category, CancellationToken ct = default)
        {
            AddedCategories.Add(category);
            Categories.Add(category);
            return Task.FromResult(category);
        }

        public Task<bool> UpdateAsync(Category category, CancellationToken ct = default)
        {
            UpdateWasCalled = true;
            if (!UpdateResult)
                return Task.FromResult(false);

            var index = Categories.FindIndex(c => c.CategoryId == category.CategoryId);
            if (index < 0)
                return Task.FromResult(false);

            Categories[index] = category;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
        {
            LastDeletedId = id;
            if (!DeleteResult)
                return Task.FromResult(false);

            var removed = Categories.RemoveAll(c => c.CategoryId == id) > 0;
            return Task.FromResult(removed || DeleteResult);
        }
    }
}

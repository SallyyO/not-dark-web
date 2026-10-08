using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Service.Services;

namespace not_dark_web.UnitTests.Controllers;

public class AdminCategoriesControllerTests
{
    [Fact]
    public async Task List_ReturnsOkWithCategories()
    {
        var service = new FakeCategoryService
        {
            Categories = [new CategoryDto("1", "Books", "Reading")]
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.List(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IReadOnlyList<CategoryDto>>(ok.Value);
        Assert.Single(categories);
        Assert.Equal("Books", categories[0].Name);
    }

    [Fact]
    public async Task Get_WhenSuccessful_ReturnsOk()
    {
        var service = new FakeCategoryService
        {
            GetByIdResult = ServiceResult<CategoryDto>.Ok(new CategoryDto("1", "Books", null))
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Get("1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(new CategoryDto("1", "Books", null), ok.Value);
    }

    [Fact]
    public async Task Get_WhenNotFound_Returns404ProblemDetails()
    {
        var service = new FakeCategoryService
        {
            GetByIdResult = ServiceResult<CategoryDto>.NotFound("Category '1' was not found.")
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Get("1", CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Not Found", problem.Title);
        Assert.Equal("Category '1' was not found.", problem.Detail);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtRoute()
    {
        var service = new FakeCategoryService
        {
            CreateResult = ServiceResult<CategoryDto>.Ok(new CategoryDto("1", "Books", "Reading"))
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Create(new CategoryRequest("Books", "Reading"), CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result);
        Assert.Equal("GetCategory", created.RouteName);
        Assert.Equal("1", created.RouteValues!["id"]);
        Assert.Equal(new CategoryDto("1", "Books", "Reading"), created.Value);
    }

    [Fact]
    public async Task Create_WhenValidationFails_Returns400ValidationProblem()
    {
        var service = new FakeCategoryService
        {
            CreateResult = ServiceResult<CategoryDto>.Invalid(
                new Dictionary<string, string[]> { ["name"] = ["Name is required."] })
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Create(new CategoryRequest(""), CancellationToken.None);

        var badRequest = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }

    [Fact]
    public async Task Create_WhenDuplicate_Returns409Conflict()
    {
        var service = new FakeCategoryService
        {
            CreateResult = ServiceResult<CategoryDto>.Conflict("A category named 'Books' already exists.")
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Create(new CategoryRequest("Books"), CancellationToken.None);

        var conflict = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal("Conflict", problem.Title);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsOk()
    {
        var service = new FakeCategoryService
        {
            UpdateResult = ServiceResult<CategoryDto>.Ok(new CategoryDto("1", "Games", null))
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Update("1", new CategoryRequest("Games"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(new CategoryDto("1", "Games", null), ok.Value);
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        var service = new FakeCategoryService
        {
            UpdateResult = ServiceResult<CategoryDto>.NotFound("Category '1' was not found.")
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Update("1", new CategoryRequest("Games"), CancellationToken.None);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenValidationFails_Returns400()
    {
        var service = new FakeCategoryService
        {
            UpdateResult = ServiceResult<CategoryDto>.Invalid(
                new Dictionary<string, string[]> { ["name"] = ["Name is required."] })
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Update("1", new CategoryRequest(""), CancellationToken.None);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, response.StatusCode);
        Assert.IsType<ValidationProblemDetails>(response.Value);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var service = new FakeCategoryService
        {
            DeleteResult = ServiceResult.Ok()
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Delete("1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var service = new FakeCategoryService
        {
            DeleteResult = ServiceResult.NotFound("Category '1' was not found.")
        };
        var controller = new AdminCategoriesController(service);

        var result = await controller.Delete("1", CancellationToken.None);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public void Controller_UsesCategoriesRoute()
    {
        var route = Assert.Single(typeof(AdminCategoriesController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false));

        Assert.Equal("categories", ((RouteAttribute)route).Template);
    }

    private sealed class FakeCategoryService : ICategoryService
    {
        public IReadOnlyList<CategoryDto> Categories { get; set; } = [];
        public ServiceResult<CategoryDto> GetByIdResult { get; set; } = ServiceResult<CategoryDto>.NotFound("Missing");
        public ServiceResult<CategoryDto> CreateResult { get; set; } = ServiceResult<CategoryDto>.Conflict("Conflict");
        public ServiceResult<CategoryDto> UpdateResult { get; set; } = ServiceResult<CategoryDto>.NotFound("Missing");
        public ServiceResult DeleteResult { get; set; } = ServiceResult.NotFound("Missing");

        public Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult(Categories);

        public Task<ServiceResult<CategoryDto>> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(GetByIdResult);

        public Task<ServiceResult<CategoryDto>> CreateAsync(CategoryRequest request, CancellationToken ct = default) =>
            Task.FromResult(CreateResult);

        public Task<ServiceResult<CategoryDto>> UpdateAsync(string id, CategoryRequest request, CancellationToken ct = default) =>
            Task.FromResult(UpdateResult);

        public Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(DeleteResult);
    }
}

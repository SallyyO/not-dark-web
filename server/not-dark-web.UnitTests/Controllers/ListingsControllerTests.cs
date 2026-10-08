using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace not_dark_web.UnitTests.Controllers;

public class ListingsControllerTests
{
    [Theory]
    [InlineData(-0.01, 1)]
    [InlineData(-10, 10)]
    public void Create_WhenPriceIsNegative_ReturnsBadRequest(decimal price, int stock)
    {
        var controller = new ListingsController(null!);
        var request = new ListingsController.ListingRequest(
            Guid.NewGuid(), "books", "Title", "Description", price, stock);

        var result = controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Price or stock cannot be negative.", badRequest.Value);
    }

    [Theory]
    [InlineData(10, -1)]
    [InlineData(10, -100)]
    public void Create_WhenStockIsNegative_ReturnsBadRequest(decimal price, int stock)
    {
        var controller = new ListingsController(null!);
        var request = new ListingsController.ListingRequest(
            Guid.NewGuid(), "books", "Title", "Description", price, stock);

        var result = controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Price or stock cannot be negative.", badRequest.Value);
    }

    [Fact]
    public void Controller_UsesListingsRoute()
    {
        var route = typeof(ListingsController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal("api/listings", route!.Template);
    }

    [Theory]
    [InlineData(nameof(ListingsController.GetAll), "")]
    [InlineData(nameof(ListingsController.Get), "{id:guid}")]
    [InlineData(nameof(ListingsController.GetForVendor), "vendor/{vendorId:guid}")]
    [InlineData(nameof(ListingsController.Create), "")]
    [InlineData(nameof(ListingsController.Update), "{id:guid}")]
    [InlineData(nameof(ListingsController.Delete), "{id:guid}")]
    public void Actions_HaveExpectedHttpRoute(string methodName, string expectedTemplate)
    {
        var method = typeof(ListingsController).GetMethod(methodName);
        Assert.NotNull(method);

        var httpAttribute = method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single();

        Assert.Equal(expectedTemplate, httpAttribute.Template ?? "");
    }
}

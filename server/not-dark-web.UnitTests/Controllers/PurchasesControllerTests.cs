using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace not_dark_web.UnitTests.Controllers;

public class PurchasesControllerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Purchase_WhenQuantityIsNotPositive_ReturnsBadRequest(int quantity)
    {
        var controller = new PurchasesController(null!);
        var request = new PurchasesController.PurchaseRequest(
            Guid.NewGuid(), Guid.NewGuid(), quantity);

        var result = controller.Purchase(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please actually buy something lol", badRequest.Value);
    }

    [Fact]
    public void Controller_UsesPurchasesRoute()
    {
        var route = typeof(PurchasesController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal("api/purchases", route!.Template);
    }

    [Theory]
    [InlineData(nameof(PurchasesController.GetForBuyer), "buyer/{buyerId:guid}")]
    [InlineData(nameof(PurchasesController.GetForVendor), "vendor/{vendorId:guid}")]
    [InlineData(nameof(PurchasesController.Purchase), "")]
    public void Actions_HaveExpectedHttpRoute(string methodName, string expectedTemplate)
    {
        var method = typeof(PurchasesController).GetMethod(methodName);
        Assert.NotNull(method);

        var httpAttribute = method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single();

        Assert.Equal(expectedTemplate, httpAttribute.Template ?? "");
    }
}

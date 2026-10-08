using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace not_dark_web.UnitTests.Controllers;

public class CustomersControllerTests
{
    [Theory]
    [InlineData(null, "BOB@legal.com")]
    [InlineData("   ", "BOB@legal.com")]
    [InlineData("BOB", null)]
    [InlineData("BOB", "   ")]
    public void Create_WhenUsernameOrEmailIsMissing_ReturnsBadRequest(string? username, string? email)
    {
        var controller = new CustomersController(null!);

        var result = controller.Create(new CustomersController.CreateCustomerRequest(username!, email!));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username and email are required.", badRequest.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Deposit_WhenAmountIsNotPositive_ReturnsBadRequest(decimal amount)
    {
        var controller = new CustomersController(null!);

        var result = controller.Deposit(Guid.NewGuid(), new CustomersController.DepositRequest(amount));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Deposit amount must be greater than zero.", badRequest.Value);
    }

    [Fact]
    public void Controller_UsesCustomersRoute()
    {
        var route = typeof(CustomersController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal("api/customers", route!.Template);
    }

    [Theory]
    [InlineData(nameof(CustomersController.GetAll), "")]
    [InlineData(nameof(CustomersController.GetById), "{id:guid}")]
    [InlineData(nameof(CustomersController.Create), "")]
    [InlineData(nameof(CustomersController.Update), "{id:guid}")]
    [InlineData(nameof(CustomersController.Delete), "{id:guid}")]
    [InlineData(nameof(CustomersController.GetWallet), "{id:guid}/wallet")]
    [InlineData(nameof(CustomersController.Deposit), "{id:guid}/wallet/deposit")]
    public void Actions_HaveExpectedHttpRoute(string methodName, string expectedTemplate)
    {
        var method = typeof(CustomersController).GetMethod(methodName);
        Assert.NotNull(method);

        var httpAttribute = method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single();

        Assert.Equal(expectedTemplate, httpAttribute.Template ?? "");
    }
}

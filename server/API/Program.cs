using System.Security.Claims;
using System.Text.Encodings.Web;
using API.Controllers;
using Infra;
using Infra.Entities;
using Infra.Repositories;
using LinqToDB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Service.Services;

var builder = WebApplication.CreateBuilder(args);

// DB
var dbOptions = new DataOptions()
    .UseSQLite("Data Source=app.db");
var appDbOptions = new DataOptions<AppDb>(dbOptions);

builder.Services.AddScoped<AppDb>(_ =>
    new AppDb(appDbOptions));

// Categories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Authentication / authorization
// The project currently has no real identity provider configured. For local
// Swagger development we use a development-only authentication scheme so
// [Authorize(Roles = "Admin")] endpoints return a proper authenticated user
// instead of throwing "No authenticationScheme was specified" (HTTP 500).
if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            DevelopmentAuthenticationHandler.SchemeName,
            _ => { });
}

builder.Services.AddAuthorization();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument();

var app = builder.Build();

// Swagger
app.UseOpenApi();
app.UseSwaggerUi();

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();


// Db setup
using var scope = app.Services.CreateScope();

var db = scope.ServiceProvider.GetRequiredService<AppDb>();

db.CreateTable<Customer>(
    tableOptions: TableOptions.CreateIfNotExists);

db.CreateTable<Category>(
    tableOptions: TableOptions.CreateIfNotExists);

db.CreateTable<Listing>(
    tableOptions: TableOptions.CreateIfNotExists);

db.CreateTable<WalletTransaction>(
    tableOptions: TableOptions.CreateIfNotExists);

db.CreateTable<Purchase>(
    tableOptions: TableOptions.CreateIfNotExists);

// Seed customer data
if (db.Customers.Count() == 0)
{
    var michael = new Customer
    {
        Username = "MJ",
        Email = "HEHEE@legal.com",
        Balance = 1738m
    };

    var harry = new Customer
    {
        Username = "HP",
        Email = "wizard@legal.com",
        Balance = 3m
    };

    var karen = new Customer
    {
        Username = "kysKaren",
        Email = "wheresthemanager@legal.com",
        Balance = 350m
    };

    db.Insert(michael);
    db.Insert(harry);
    db.Insert(karen);

    db.Insert(new WalletTransaction
    {
        CustomerId = michael.CustomerId,
        Amount = 1738m,
        Type = "Deposit",
        Description = "Initial test balance"
    });

    db.Insert(new WalletTransaction
    {
        CustomerId = harry.CustomerId,
        Amount = 3m,
        Type = "Deposit",
        Description = "Initial test balance"
    });

    db.Insert(new WalletTransaction
    {
        CustomerId = karen.CustomerId,
        Amount = 350m,
        Type = "Deposit",
        Description = "Initial test balance"
    });

// Seed categories
if (db.Categories.Count() == 0)
{
    db.Insert(new Category
    {
        Name = "Electronics",
        Description = "Totally not stolen phones, computers, and other electronic products"
    });

    db.Insert(new Category
    {
        Name = "Stolen Artifacts",
        Description = "Religious paraphernalia"
    });

    db.Insert(new Category
    {
        Name = "Weaponry",
        Description = "GUNZZZZZZZ"
    });

    db.Insert(new Category
    {
        Name = "Drugs",
        Description = "All of em"
    });
}

// Temp database test endpoint
app.MapGet("/customers", (AppDb db) =>
    db.Customers.ToList());
    db.Insert(new Listing
    {
        VendorId = michael.CustomerId,
        CategoryId = "electronics",
        Title = "Test Laptop",
        Description = "Laptop for testing, wink wink",
        Price = 250m,
        Stock = 5,
        IsActive = true
    });

    db.Insert(new Listing
    {
        VendorId = harry.CustomerId,
        CategoryId = "clothing",
        Title = "Not stolen Jacket",
        Description = "No receipt, but it's a good jacket",
        Price = 75m,
        Stock = 1,
        IsActive = true
    });
}

app.Run();

// Development-only authentication handler used by local Swagger/testing.
// It authenticates the request as an Admin so the existing category endpoints
// can be exercised without an external identity provider.

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "development-admin"),
            new Claim(ClaimTypes.Name, "Development Admin"),
            new Claim(ClaimTypes.Role, AdminCategoriesController.AdminRole)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
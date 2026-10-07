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

// Test endpoint
app.MapGet("/hello", () => "Hello World!");

// Db setup
using var scope = app.Services.CreateScope();

var db = scope.ServiceProvider.GetRequiredService<AppDb>();

db.CreateTable<Customer>(
    tableOptions: TableOptions.CreateIfNotExists);

db.CreateTable<Category>(
    tableOptions: TableOptions.CreateIfNotExists);

if (db.Customers.Count() == 0)
{
    db.Insert(new Customer
    {
        Username = "Michael",
        Email = "HEHEE@legal.com",
        Balance = 1738
    });

    db.Insert(new Customer
    {
        Username = "John",
        Email = "Doe@legal.com",
        Balance = 350
    });
}

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
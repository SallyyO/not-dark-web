using Infra;
using Infra.Entities;
using LinqToDB;

var builder = WebApplication.CreateBuilder(args);

// DB
var dbOptions = new DataOptions()
    .UseSQLite("Data Source=app.db");

var appDbOptions = new DataOptions<AppDb>(dbOptions);

builder.Services.AddScoped<AppDb>(_ =>
    new AppDb(appDbOptions));

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument();

var app = builder.Build();

// Swagger
app.UseOpenApi();
app.UseSwaggerUi();

// Controllers
app.MapControllers();

// Test endpoint
app.MapGet("/hello", () => "Hello World!");

// Db setup
using var scope = app.Services.CreateScope();

var db = scope.ServiceProvider.GetRequiredService<AppDb>();

db.CreateTable<Customer>(
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

// Temp database test endpoint
app.MapGet("/customers", (AppDb db) =>
    db.Customers.ToList());

app.Run();

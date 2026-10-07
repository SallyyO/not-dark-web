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


// Db setup
using var scope = app.Services.CreateScope();

var db = scope.ServiceProvider.GetRequiredService<AppDb>();

db.CreateTable<Customer>(
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

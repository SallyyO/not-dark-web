using Infra;
using LinqToDB;
using Infra.Entities;

var builder = WebApplication.CreateBuilder(args);

var dbOptions = new DataOptions().UseSQLite("Data Source=app.db");
var appDbOptions = new DataOptions<AppDb>(dbOptions);
builder.Services.AddScoped<AppDb>(serviceProvider =>
    new AppDb(appDbOptions));
var app = builder.Build();
app.MapGet("/hello", () => "Hello World!");
app.MapGet("/customers", (AppDb db) =>
    db.Customers.ToList());
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

app.Run();

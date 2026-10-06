namespace Infra.Entities;
using LinqToDB.Mapping;

[Table("Customers")]
public class Customer
{
    [PrimaryKey] public string CustomerId { get; set; } = Guid.NewGuid().ToString();
    [Column, NotNull] public string Username { get; set; } = "";
    [Column, NotNull] public string Email { get; set; } = "";
    [Column, NotNull] public decimal Balance { get; set; }
}
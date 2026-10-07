namespace Infra.Entities;
using LinqToDB.Mapping;

[Table("Customers")]
public class Customer
{
    [PrimaryKey] public Guid CustomerId { get; set; } = Guid.NewGuid();
    [Column, NotNull] public string Username { get; set; } = "";
    [Column, NotNull] public string Email { get; set; } = "";
    [Column, NotNull] public decimal Balance { get; set; }
}
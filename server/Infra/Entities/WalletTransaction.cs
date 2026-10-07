using LinqToDB.Mapping;

namespace Infra.Entities;

[Table("WalletTransactions")]
public class WalletTransaction
{
    [PrimaryKey] public Guid TransactionId { get; set; } = Guid.NewGuid();
    [Column, NotNull] public Guid CustomerId { get; set; }
    [Column, NotNull] public decimal Amount { get; set; }
    [Column, NotNull] public string Type { get; set; } = ""; // deposit, purchase or sale?
    [Column, NotNull] public string Description { get; set; } = "";
    [Column, NotNull] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
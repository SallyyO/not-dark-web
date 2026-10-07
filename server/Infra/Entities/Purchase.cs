using LinqToDB.Mapping;

namespace Infra.Entities;

[Table("Purchases")]
public class Purchase
{
    [PrimaryKey] public Guid PurchaseId { get; set; } = Guid.NewGuid();
    [Column, NotNull] public Guid BuyerId { get; set; }
    [Column, NotNull] public Guid VendorId { get; set; }
    [Column, NotNull] public Guid ListingId { get; set; }
    [Column, NotNull] public int Quantity { get; set; }
    [Column, NotNull] public decimal UnitPrice { get; set; }
    [Column, NotNull] public decimal TotalPrice { get; set; }
    [Column, NotNull] public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
}
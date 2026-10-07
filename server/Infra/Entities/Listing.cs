using LinqToDB.Mapping;

namespace Infra.Entities;

[System.ComponentModel.DataAnnotations.Schema.Table("Listings")]
public class Listing
{
    [PrimaryKey] public Guid ListingId { get; set; } = Guid.NewGuid();
    [Column, NotNull] public Guid VendorId { get; set; }
    [Column, NotNull] public string CategoryId { get; set; } = ""; //Need this from another branch, will be updated when possible
    [Column, NotNull] public string Title { get; set; } = "";
    [Column, NotNull] public string Description { get; set; } = "";
    [Column, NotNull] public decimal Price { get; set; }
    [Column, NotNull] public int Stock { get; set; }
    [Column, NotNull] public bool IsActive { get; set; } = true;
}
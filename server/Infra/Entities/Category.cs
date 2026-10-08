namespace Infra.Entities;
using LinqToDB.Mapping;

[Table("Categories")]
public class Category
{
    [PrimaryKey] public string CategoryId { get; set; } = Guid.NewGuid().ToString();
    [Column, NotNull] public string Name { get; set; } = "";
    [Column, Nullable] public string? Description { get; set; }
}
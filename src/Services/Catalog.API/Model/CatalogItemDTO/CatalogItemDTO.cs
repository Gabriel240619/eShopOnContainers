using System.ComponentModel.DataAnnotations;

namespace Catalog.API.Model.CatalogItemDTO
{
    public class CatalogItemDTO
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string CatalogType { get; set; } = string.Empty;

        public string CatalogBrand { get; set; } = string.Empty;

        public int AvailableStock { get; set; }

        public int RestockThreshold { get; set; }

        public int MaxStockThreshold { get; set; }

        public bool OnReorder { get; set; }
    }
}
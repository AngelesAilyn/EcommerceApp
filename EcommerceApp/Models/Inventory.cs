using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Inventory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa.")]
        [Display(Name = "Cantidad actual")]
        public int CurrentQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
        [Display(Name = "Stock mínimo")]
        public int MinimumStock { get; set; }

        [Display(Name = "Fecha de actualización")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Product? Product { get; set; }

        public string StockStatus
        {
            get
            {
                if (CurrentQuantity <= 0)
                    return "Agotado";

                if (CurrentQuantity <= MinimumStock)
                    return "Stock bajo";

                return "Disponible";
            }
        }
    }
}
using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre del producto")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0.")]
        [Display(Name = "Precio")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [StringLength(500, ErrorMessage = "La URL de imagen no puede superar los 500 caracteres.")]
        [Url(ErrorMessage = "Ingrese una URL de imagen válida.")]
        [Display(Name = "URL de imagen")]
        public string? ImageUrl { get; set; }


        // Categoria, Se conserva temporalmente para migrar los productos existentes.
        [StringLength(50, ErrorMessage = "La categoría no puede superar los 50 caracteres.")]
        [Display(Name = "Categoría anterior")]
        public string? Category { get; set; }

        // Nueva relación con Category.
        // Se mantiene nullable durante esta primera
        // etapa para no romper los productos existentes.
        [Display(Name = "Categoría")]
        public int? CategoryId { get; set; }

        public Category? CategoryNavigation { get; set; }


        public Inventory? Inventory { get; set; }

        [Display(Name = "Disponible")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Archivado")]
        public bool IsArchived { get; set; } = false;

        [Display(Name = "Fecha de vencimiento")]
        [DataType(DataType.Date)]
        public DateTime? ExpirationDate { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
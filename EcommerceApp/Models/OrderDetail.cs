using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, 999999999.99)]
        public decimal UnitPrice { get; set; }

        [Range(0, 999999999.99)]
        public decimal Subtotal { get; set; }

        public Order? Order { get; set; }

        public Product? Product { get; set; }
    }
}
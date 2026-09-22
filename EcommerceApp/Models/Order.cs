using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pendiente";

        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Pendiente";

        [Required]
        [StringLength(30)]
        public string DeliveryMethod { get; set; } = string.Empty;

        [Range(0, 999999999.99)]
        public decimal Total { get; set; }

        public ApplicationUser? User { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
            = new List<OrderDetail>();
    }
}
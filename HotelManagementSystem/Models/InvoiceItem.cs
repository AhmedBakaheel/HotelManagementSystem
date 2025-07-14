using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models
{
    public class InvoiceItem
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "الخدمة مطلوبة.")]
        [Display(Name = "الخدمة")]
        public int ServiceId { get; set; } 
        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }

        [Required(ErrorMessage = "اسم البند مطلوب.")]
        [Display(Name = "البند")]
        public string ItemName { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة.")]
        [Display(Name = "الكمية")]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "سعر الوحدة مطلوب.")]
        [Display(Name = "سعر الوحدة")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "سعر الوحدة يجب أن يكون أكبر من صفر.")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "المجموع الفرعي")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal => Quantity * UnitPrice; 

        [Required]
        public int InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }
    }
}

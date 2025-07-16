using HotelManagementSystem.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "الفاتورة المرتبطة مطلوبة.")]
        [Display(Name = "رقم الفاتورة")]
        public int InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public virtual Invoice? Invoice { get; set; } 

        [Required(ErrorMessage = "المبلغ المدفوع مطلوب.")]
        [Display(Name = "المبلغ المدفوع")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "المبلغ المدفوع يجب أن يكون أكبر من صفر.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "تاريخ الدفع مطلوب.")]
        [Display(Name = "تاريخ الدفع")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "طريقة الدفع مطلوبة.")]
        [Display(Name = "طريقة الدفع")]
        public PaymentMethod Method { get; set; }

        [Display(Name = "ملاحظات الدفع")]
        [StringLength(500, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 500 حرف.")]
        public string? Notes { get; set; }
    }
}

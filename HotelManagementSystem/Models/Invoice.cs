using HotelManagementSystem.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HotelManagementSystem.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        
        [Display(Name = "رقم الفاتورة")]
        public string? InvoiceNumber { get; set; }  

        [Required(ErrorMessage = "تاريخ الفاتورة مطلوب.")]
        [Display(Name = "تاريخ الفاتورة")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "تاريخ الاستحقاق مطلوب.")]
        [Display(Name = "تاريخ الاستحقاق")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7); 
        [Display(Name = "المبلغ الإجمالي")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "المبلغ الإجمالي يجب أن يكون رقماً موجباً.")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "المبلغ المدفوع")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "المبلغ المدفوع يجب أن يكون رقماً موجباً.")]
        public decimal PaidAmount { get; set; } = 0;
        [Display(Name = "المبلغ المتبقي")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal RemainingAmount => TotalAmount - PaidAmount;
        [Display(Name = "حالة الفاتورة")]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;

        // العلاقة مع Booking
        [Display(Name = "رقم الحجز")]
        public int? BookingId { get; set; } 
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; } 
        [Display(Name = "العميل")]
        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [Display(Name = "ملاحظات")]
        [StringLength(1000, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 1000 حرف.")]
        public string? Notes { get; set; }

        [ValidateNever]
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}

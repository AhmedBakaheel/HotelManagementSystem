using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Enums
{
    public enum InvoiceStatus
    {
        [Display(Name = "غير مدفوعة")]
        Unpaid,
        [Display(Name = "مدفوعة جزئياً")]
        PartiallyPaid,
        [Display(Name = "مدفوعة بالكامل")]
        Paid,
        [Display(Name = "ملغاة")]
        Cancelled
    }
}

using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Enums
{
    public enum PaymentMethod
    {
        [Display(Name = "نقداً")] 
        Cash,
        [Display(Name = "بطاقة ائتمان")]
        CreditCard,
        [Display(Name = "تحويل بنكي")]
        BankTransfer,
        [Display(Name = "شيك")]
        Check,
        [Display(Name = "أخرى")]
        Other
    }
}

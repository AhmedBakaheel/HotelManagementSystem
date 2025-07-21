using System.ComponentModel.DataAnnotations;
namespace HotelManagementSystem.Enums
{
    public enum BookingStatus
    {
     
        [Display(Name = "مؤكد")]
        Confirmed,
        [Display(Name = "ملغى")]
        Cancelled,
        [Display(Name = "تم تسجيل الدخول")]
        CheckedIn,
        [Display(Name = "تم تسجيل الخروج")]
        CheckedOut,
        [Display(Name = "معلق")] 
        Pending
    }
}

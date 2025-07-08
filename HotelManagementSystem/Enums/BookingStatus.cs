using System.ComponentModel.DataAnnotations;
namespace HotelManagementSystem.Enums
{
    public enum BookingStatus
    {
        [Display(Name = "تم التأكيد")]
        Confirmed,
        [Display(Name = "تم الإلغاء")]
        Cancelled,
        [Display(Name = "تم إكماله")]
        Completed,
        [Display(Name = "معلق")]
        Pending
    }
}

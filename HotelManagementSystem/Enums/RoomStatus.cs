using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Enums
{
    public enum RoomStatus
    {
        [Display(Name = "متاح")]
        Available,
        [Display(Name = "محجوز")]
        Booked,
        [Display(Name = "صيانة")]
        Maintenance
    }
}

using System.ComponentModel.DataAnnotations;
namespace HotelManagementSystem.Enums
{
    public enum RoomType
    {
        [Display(Name = "مفردة")]
        Single,
        [Display(Name = "مزدوجة")]
        Double,
        [Display(Name = "جناح")]
        Suite,
        [Display(Name = "عائلية")]
        Family 
    }
}

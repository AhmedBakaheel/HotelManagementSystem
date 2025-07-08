using HotelManagementSystem.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace HotelManagementSystem.Models
{
    public class Room
    {
        [Key] 
        public int Id { get; set; }

        [Required(ErrorMessage = "رقم الغرفة مطلوب.")] 
        [Display(Name = "رقم الغرفة")] 
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "نوع الغرفة مطلوب.")]
        [Display(Name = "نوع الغرفة")]
        public RoomType RoomType { get; set; }

        [Required(ErrorMessage = "سعر الليلة مطلوب.")]
        [Column(TypeName = "decimal(18, 2)")] 
        [Display(Name = "السعر لليلة")]
        public decimal PricePerNight { get; set; }

        [Display(Name = "متاحة؟")]
        public bool IsAvailable { get; set; } = true; 

        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        [Display(Name = "الحالة")]
        public RoomStatus Status { get; set; }
        [NotMapped] 
        public IEnumerable<SelectListItem> RoomTypeOptions { get; set; } 

        public ICollection<Booking>? Bookings { get; set; }
    }
}
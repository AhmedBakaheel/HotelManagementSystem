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

        [Required(ErrorMessage = "السعر الليلي مطلوب.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "السعر الليلي يجب أن يكون أكبر من صفر.")]
        [Display(Name = "السعر الليلي")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerNight { get; set; }

        // <== استخدم هذه الخاصية بدلاً من IsAvailable (إذا كنت تريد RoomStatus)
        [Required(ErrorMessage = "حالة الغرفة مطلوبة.")]
        [Display(Name = "الحالة")]
        public RoomStatus Status { get; set; } // <== هذه هي الخاصية التي يجب أن تستخدم الـ enum

        [Display(Name = "الوصف")]
        [StringLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف.")]
        public string? Description { get; set; }
        [Display(Name = "نشطة")] 
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public IEnumerable<SelectListItem> RoomTypeOptions { get; set; }

        public ICollection<Booking> Bookings { get; set; }
    }
}
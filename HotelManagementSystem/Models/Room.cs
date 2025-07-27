using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using HotelManagementSystem.Enums; 

namespace HotelManagementSystem.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

       
        [Display(Name = "رقم الغرفة")]
        [StringLength(10, ErrorMessage = "رقم الغرفة لا يمكن أن يتجاوز 10 أحرف.")]
        public string RoomNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع الغرفة مطلوب.")]
        [Display(Name = "نوع الغرفة")]
       
        public RoomType RoomType { get; set; }

        [Required(ErrorMessage = "سعر الليلة الواحدة مطلوب.")]
        [Display(Name = "سعر الليلة")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "سعر الليلة يجب أن يكون أكبر من صفر.")]
        public decimal PricePerNight { get; set; }

        [Required(ErrorMessage = "حالة الغرفة مطلوبة.")]
        [Display(Name = "الحالة")]
        public RoomStatus Status { get; set; }
        [Display(Name = "الوصف")]
        [StringLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف.")]
        public string? Description { get; set; }

        [Display(Name = "الحد الأقصى للضيوف")]
        [Range(1, 10, ErrorMessage = "الحد الأقصى للضيوف يجب أن يكون بين 1 و 10.")]
        public int MaxGuests { get; set; }

        [Display(Name = "نشطة")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "رابط الصورة")]
        [StringLength(1000, ErrorMessage = "رابط الصورة لا يمكن أن يتجاوز 1000 حرف.")]
        [DataType(DataType.ImageUrl)]
        public string? ImageUrl { get; set; }

        // Navigation property for related bookings
        public virtual ICollection<Booking>? Bookings { get; set; }
    }
}


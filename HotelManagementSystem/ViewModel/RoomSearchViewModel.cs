using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // لـ List<Room>
using HotelManagementSystem.Models;
using HotelManagementSystem.Enums; // لـ Room و BookingStatus

namespace HotelManagementSystem.ViewModels
{
    public class RoomSearchViewModel
    {
        [Required(ErrorMessage = "تاريخ الدخول مطلوب.")]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الدخول")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CheckInDate { get; set; } = DateTime.Today; // قيمة افتراضية لتاريخ اليوم

        [Required(ErrorMessage = "تاريخ المغادرة مطلوب.")]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ المغادرة")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1); // قيمة افتراضية لتاريخ الغد

        [Display(Name = "نوع الغرفة")]
        public RoomType? RoomType { get; set; } // اختيار نوع الغرفة (اختياري)

        // قائمة للغرف المتاحة التي تتوافق مع البحث لعرضها في الـ View
        public List<Room>? AvailableRooms { get; set; }

        // خصائص لرسائل الخطأ أو النجاح
        public string? Message { get; set; }
    }
}
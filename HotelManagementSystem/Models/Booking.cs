using HotelManagementSystem.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagementSystem.Models
{
   

    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "تاريخ الدخول مطلوب.")]
        [Display(Name = "تاريخ الدخول")]
        [DataType(DataType.Date)] // يحدد نوع البيانات كتاريخ فقط
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "تاريخ المغادرة مطلوب.")]
        [Display(Name = "تاريخ المغادرة")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required(ErrorMessage = "المبلغ الإجمالي مطلوب.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "المبلغ الإجمالي")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "تاريخ الحجز")]
        public DateTime BookingDate { get; set; } = DateTime.Now; // القيمة الافتراضية هي الوقت الحالي

        [Display(Name = "حالة الحجز")]
        public BookingStatus Status { get; set; } = BookingStatus.Pending; // القيمة الافتراضية هي معلق

        // مفتاح خارجي للغرفة
        [Required(ErrorMessage = "يجب تحديد الغرفة.")]
        [Display(Name = "الغرفة")]
        public int RoomId { get; set; }
        [ForeignKey("RoomId")] // يحدد أن RoomId هو مفتاح خارجي لجدول Rooms
        public Room? Room { get; set; } // خاصية التنقل للغرفة

        // مفتاح خارجي للعميل
        [Required(ErrorMessage = "يجب تحديد العميل.")]
        [Display(Name = "العميل")]
        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")] // يحدد أن CustomerId هو مفتاح خارجي لجدول Customers
        public Customer? Customer { get; set; } // خاصية التنقل للعميل
        public string? ApplicationUserId { get; set; } // يجب أن يكون من نوع string
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; } // خاصية تنقلية

    }
}
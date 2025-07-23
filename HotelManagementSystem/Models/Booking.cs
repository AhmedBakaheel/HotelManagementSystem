using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Required for ICollection
using Microsoft.AspNetCore.Identity;
using HotelManagementSystem.Enums; // Assuming ApplicationUser is from Identity

namespace HotelManagementSystem.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Check-in date is required.")]
        [Display(Name = "Check-in Date")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Check-out date is required.")]
        [Display(Name = "Check-out Date")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required(ErrorMessage = "Room ID is required.")]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; } // Room navigation property

        [Required(ErrorMessage = "Customer ID is required.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; } // Customer navigation property

        [Display(Name = "Booking Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Booking status is required.")]
        [Display(Name = "Booking Status")]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [Display(Name = "Total Amount")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.00, double.MaxValue, ErrorMessage = "Total amount cannot be negative.")]
        public decimal TotalAmount { get; set; } = 0;

        // <== جديد: تمت إعادة إضافة خاصية تاريخ الحجز
        [Required(ErrorMessage = "Booking date is required.")]
        [Display(Name = "Booking Date")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        // <== جديد: تمت إعادة إضافة خصائص المستخدم المرتبطة
        // Assuming ApplicationUser is your Identity user model
        [Display(Name = "Application User")]
        public string? ApplicationUserId { get; set; } // Foreign key to ApplicationUser

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; } // Navigation property to ApplicationUser

        // Navigation property for related invoices (one-to-many)
        public virtual ICollection<Invoice>? Invoices { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using HotelManagementSystem.Enums;
using HotelManagementSystem.Models;
namespace HotelManagementSystem.ViewModel
{
    public class BookRoomViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public RoomType? RoomType { get; set; }
        public decimal PricePerNight { get; set; }
        public string? Description { get; set; }

        [Required(ErrorMessage = "تاريخ الدخول مطلوب.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "تاريخ المغادرة مطلوب.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CheckOutDate { get; set; }

        public int NumberOfNights => (CheckOutDate - CheckInDate).Days;
        public decimal TotalPrice => NumberOfNights * PricePerNight;
    }
}

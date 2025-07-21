namespace HotelManagementSystem.Models
{
    public class RoomOccupancyDetail
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public int RoomAvailableNights { get; set; }
        public int RoomBookedNights { get; set; }
        public decimal RoomOccupancyRate { get; set; }
    }
}

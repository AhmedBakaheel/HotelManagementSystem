using HotelManagementSystem.Models;

namespace HotelManagementSystem.ViewModel
{
    public class RoomOccupancyReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalAvailableNights { get; set; } 
        public int TotalBookedNights { get; set; }   
        public decimal OccupancyRate { get; set; }    
        public List<RoomOccupancyDetail> RoomDetails { get; set; } = new List<RoomOccupancyDetail>();
    }
}

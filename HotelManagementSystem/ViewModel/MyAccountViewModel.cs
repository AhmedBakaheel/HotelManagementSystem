using HotelManagementSystem.Models;

namespace HotelManagementSystem.ViewModel
{
    public class MyAccountViewModel
    {
        public ApplicationUser? User { get; set; }
        public List<Booking>? Bookings { get; set; }
    }
}

using HotelManagementSystem.Models;

namespace HotelManagementSystem.ViewModel
{
    public class RevenueSummaryReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalOverallRevenue { get; set; }
        public decimal TotalBookingRevenue { get; set; }
        public decimal TotalServiceRevenue { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public List<Invoice> InvoicesIncluded { get; set; } = new List<Invoice>();
    }
}

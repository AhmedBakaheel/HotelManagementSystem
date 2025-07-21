using HotelManagementSystem.Models;

namespace HotelManagementSystem.ViewModel
{
    public class OutstandingPaymentsReportViewModel
    {
        public DateTime ReportDate { get; set; }
        public List<OutstandingInvoice> OutstandingInvoices { get; set; } = new List<OutstandingInvoice>();

        
        public decimal Current { get; set; } 
        public decimal Days1_30 { get; set; }
        public decimal Days31_60 { get; set; } 
        public decimal Days61_90 { get; set; } 
        public decimal Days90Plus { get; set; } 
        public decimal TotalOutstanding { get; set; } 
    }
}

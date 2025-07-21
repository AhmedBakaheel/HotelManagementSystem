namespace HotelManagementSystem.Models
{
    public class OutstandingInvoice
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public int DaysOverdue { get; set; } // عدد الأيام المتأخرة (إذا كانت DueDate < ReportDate)
    }
}

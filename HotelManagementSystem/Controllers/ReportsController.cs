using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using HotelManagementSystem.ViewModel;

namespace HotelManagementSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        // GET: Reports/RevenueSummary
       
        public IActionResult RevenueSummary()
        {
          
            var viewModel = new RevenueSummaryReportViewModel
            {
                StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = DateTime.Now.Date 
            };
            return View(viewModel);
        }

        // POST: Reports/RevenueSummary
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevenueSummary(RevenueSummaryReportViewModel viewModel)
        {
            Debug.WriteLine($"--- Report Request ---");
            Debug.WriteLine($"StartDate received: {viewModel.StartDate}");
            Debug.WriteLine($"EndDate received: {viewModel.EndDate}");
            Debug.WriteLine($"----------------------");

            if (ModelState.IsValid)
            {
               
                if (viewModel.StartDate > viewModel.EndDate)
                {
                    ModelState.AddModelError("", "تاريخ البدء لا يمكن أن يكون بعد تاريخ الانتهاء.");
                    return View(viewModel);
                }

              
                var adjustedEndDate = viewModel.EndDate.Date.AddDays(1).AddTicks(-1);

                Debug.WriteLine($"Adjusted EndDate for query: {adjustedEndDate}");

               
                var invoices = await _context.Invoices
                    .Include(i => i.Booking)
                        .ThenInclude(b => b.Room)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Service)
                    .Where(i => i.InvoiceDate >= viewModel.StartDate && i.InvoiceDate <= adjustedEndDate)
                    .ToListAsync();

                Debug.WriteLine($"Number of invoices fetched: {invoices.Count}");
                if (invoices.Any())
                {
                    Debug.WriteLine($"First invoice date: {invoices.First().InvoiceDate}");
                    Debug.WriteLine($"Last invoice date: {invoices.Last().InvoiceDate}");
                }
                Debug.WriteLine($"----------------------");

                viewModel.InvoicesIncluded = invoices;
                viewModel.TotalOverallRevenue = 0;
                viewModel.TotalBookingRevenue = 0;
                viewModel.TotalServiceRevenue = 0;
                viewModel.TotalOutstandingAmount = 0; 

                foreach (var invoice in invoices)
                {
                    viewModel.TotalOverallRevenue += invoice.TotalAmount;

                   
                    if (invoice.Booking != null)
                    {
                        if (invoice.Booking.Room != null)
                        {
                            int numberOfNights = (int)(invoice.Booking.CheckOutDate - invoice.Booking.CheckInDate).TotalDays;
                            if (numberOfNights < 0) numberOfNights = 0;
                            viewModel.TotalBookingRevenue += invoice.Booking.Room.PricePerNight * numberOfNights;
                        }
                    }

                   
                    if (invoice.InvoiceItems != null)
                    {
                        viewModel.TotalServiceRevenue += invoice.InvoiceItems.Sum(item => item.Quantity * item.UnitPrice);
                    }

                    viewModel.TotalOutstandingAmount += invoice.RemainingAmount; 
                }
            }
            return View(viewModel);
        }

        // GET: Reports/OutstandingPayments
        // Displays the form for the Outstanding Payments Report
        public IActionResult OutstandingPayments()
        {
            var viewModel = new OutstandingPaymentsReportViewModel
            {
                ReportDate = DateTime.Now.Date // Default to today's date
            };
            return View(viewModel);
        }

        // POST: Reports/OutstandingPayments
        // Processes the request and calculates outstanding amounts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OutstandingPayments(OutstandingPaymentsReportViewModel viewModel)
        {
            Debug.WriteLine($"--- Outstanding Payments Report Request ---");
            Debug.WriteLine($"ReportDate received: {viewModel.ReportDate}");
            Debug.WriteLine($"-----------------------------------------");

            if (ModelState.IsValid)
            {
                // Fetch all invoices with remaining amount > 0
                // Include Customer for display purposes
                var outstandingInvoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Where(i => i.RemainingAmount > 0)
                    .ToListAsync();

                viewModel.OutstandingInvoices = new List<OutstandingInvoice>();
                viewModel.Current = 0;
                viewModel.Days1_30 = 0;
                viewModel.Days31_60 = 0;
                viewModel.Days61_90 = 0;
                viewModel.Days90Plus = 0;
                viewModel.TotalOutstanding = 0;

                foreach (var invoice in outstandingInvoices)
                {
                    var daysOverdue = (int)(viewModel.ReportDate - invoice.DueDate).TotalDays;

                    var outstandingItem = new OutstandingInvoice
                    {
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        CustomerName = invoice.Customer?.FullName ?? "غير معروف",
                        InvoiceDate = invoice.InvoiceDate,
                        DueDate = invoice.DueDate,
                        TotalAmount = invoice.TotalAmount,
                        PaidAmount = invoice.PaidAmount,
                        RemainingAmount = invoice.RemainingAmount,
                        DaysOverdue = daysOverdue > 0 ? daysOverdue : 0 // Only positive days overdue
                    };

                    viewModel.OutstandingInvoices.Add(outstandingItem);
                    viewModel.TotalOutstanding += invoice.RemainingAmount;

                    // Categorize into aging buckets
                    if (daysOverdue <= 0) // Not yet due or due today
                    {
                        viewModel.Current += invoice.RemainingAmount;
                    }
                    else if (daysOverdue >= 1 && daysOverdue <= 30)
                    {
                        viewModel.Days1_30 += invoice.RemainingAmount;
                    }
                    else if (daysOverdue >= 31 && daysOverdue <= 60)
                    {
                        viewModel.Days31_60 += invoice.RemainingAmount;
                    }
                    else if (daysOverdue >= 61 && daysOverdue <= 90)
                    {
                        viewModel.Days61_90 += invoice.RemainingAmount;
                    }
                    else // More than 90 days overdue
                    {
                        viewModel.Days90Plus += invoice.RemainingAmount;
                    }
                }

                // Sort invoices by DueDate for better readability
                viewModel.OutstandingInvoices = viewModel.OutstandingInvoices.OrderBy(oi => oi.DueDate).ToList();

                Debug.WriteLine($"Total Outstanding: {viewModel.TotalOutstanding}");
                Debug.WriteLine($"Current: {viewModel.Current}");
                Debug.WriteLine($"1-30 Days: {viewModel.Days1_30}");
                Debug.WriteLine($"31-60 Days: {viewModel.Days31_60}");
                Debug.WriteLine($"61-90 Days: {viewModel.Days61_90}");
                Debug.WriteLine($"90+ Days: {viewModel.Days90Plus}");
            }

            return View(viewModel);
        }
    }
}

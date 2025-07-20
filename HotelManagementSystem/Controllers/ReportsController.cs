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
    }
}

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
            // Set default dates (e.g., current month)
            var viewModel = new RevenueSummaryReportViewModel
            {
                StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = DateTime.Now.Date // Up to today
            };
            return View(viewModel);
        }

        // POST: Reports/RevenueSummary
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevenueSummary(RevenueSummaryReportViewModel viewModel)
        {
            Debug.WriteLine($"--- Revenue Report Request ---");
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
        
        public IActionResult OutstandingPayments()
        {
            var viewModel = new OutstandingPaymentsReportViewModel
            {
                ReportDate = DateTime.Now.Date 
            };
            return View(viewModel);
        }

        // POST: Reports/OutstandingPayments
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OutstandingPayments(OutstandingPaymentsReportViewModel viewModel)
        {
            Debug.WriteLine($"--- Outstanding Payments Report Request ---");
            Debug.WriteLine($"ReportDate received: {viewModel.ReportDate}");
            Debug.WriteLine($"-----------------------------------------");

            if (ModelState.IsValid)
            {
                
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
                        DaysOverdue = daysOverdue > 0 ? daysOverdue : 0 
                    };

                    viewModel.OutstandingInvoices.Add(outstandingItem);
                    viewModel.TotalOutstanding += invoice.RemainingAmount;

                  
                    if (daysOverdue <= 0) 
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
                    else
                    {
                        viewModel.Days90Plus += invoice.RemainingAmount;
                    }
                }

               
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

        // GET: Reports/RoomOccupancy
       
        public IActionResult RoomOccupancy()
        {
            var viewModel = new RoomOccupancyReportViewModel
            {
                StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = DateTime.Now.Date
            };
            return View(viewModel);
        }

        // POST: Reports/RoomOccupancy
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoomOccupancy(RoomOccupancyReportViewModel viewModel)
        {
            Debug.WriteLine($"--- Room Occupancy Report Request ---");
            Debug.WriteLine($"StartDate received: {viewModel.StartDate}");
            Debug.WriteLine($"EndDate received: {viewModel.EndDate}");
            Debug.WriteLine($"-----------------------------------");

            if (ModelState.IsValid)
            {
                if (viewModel.StartDate > viewModel.EndDate)
                {
                    ModelState.AddModelError("", "تاريخ البدء لا يمكن أن يكون بعد تاريخ الانتهاء.");
                    return View(viewModel);
                }

                
                var adjustedEndDate = viewModel.EndDate.Date.AddDays(1).AddTicks(-1);

                Debug.WriteLine($"Adjusted EndDate for query: {adjustedEndDate}");

                
                var allRooms = await _context.Rooms.ToListAsync();

               
                viewModel.TotalAvailableNights = 0;
                foreach (var room in allRooms)
                {
                    
                    var periodStart = viewModel.StartDate.Date;
                    var periodEnd = viewModel.EndDate.Date;

                    

                    int daysInPeriod = (int)(periodEnd - periodStart).TotalDays + 1; 
                    if (daysInPeriod < 0) daysInPeriod = 0;

                    viewModel.TotalAvailableNights += daysInPeriod;
                }

                
                var overlappingBookings = await _context.Bookings
                    .Include(b => b.Room)
                    .Where(b => (b.CheckInDate < adjustedEndDate && b.CheckOutDate > viewModel.StartDate))
                    .ToListAsync();

                viewModel.TotalBookedNights = 0;
                var roomOccupancyDetails = new Dictionary<int, RoomOccupancyDetail>();

                foreach (var room in allRooms)
                {
                    roomOccupancyDetails[room.Id] = new RoomOccupancyDetail
                    {
                        RoomId = room.Id,
                        RoomNumber = room.RoomNumber,
                        RoomAvailableNights = (int)(viewModel.EndDate.Date - viewModel.StartDate.Date).TotalDays + 1,
                        RoomBookedNights = 0,
                        RoomOccupancyRate = 0
                    };
                }

                foreach (var booking in overlappingBookings)
                {
                    
                    var bookingStart = booking.CheckInDate > viewModel.StartDate ? booking.CheckInDate : viewModel.StartDate;
                    var bookingEnd = booking.CheckOutDate < viewModel.EndDate.AddDays(1) ? booking.CheckOutDate : viewModel.EndDate.AddDays(1); // Use EndDate + 1 for exclusive end

                    
                    if (bookingEnd <= bookingStart) continue;

                    var bookedNights = (int)(bookingEnd - bookingStart).TotalDays;
                    if (bookedNights < 0) bookedNights = 0;

                    viewModel.TotalBookedNights += bookedNights;

                    
                    if (roomOccupancyDetails.ContainsKey(booking.RoomId))
                    {
                        roomOccupancyDetails[booking.RoomId].RoomBookedNights += bookedNights;
                    }
                }

                
                foreach (var detail in roomOccupancyDetails.Values)
                {
                    if (detail.RoomAvailableNights > 0)
                    {
                        detail.RoomOccupancyRate = (decimal)detail.RoomBookedNights / detail.RoomAvailableNights * 100;
                    }
                }

                viewModel.RoomDetails = roomOccupancyDetails.Values.OrderBy(rd => rd.RoomNumber).ToList();

                
                if (viewModel.TotalAvailableNights > 0)
                {
                    viewModel.OccupancyRate = (decimal)viewModel.TotalBookedNights / viewModel.TotalAvailableNights * 100;
                }
                else
                {
                    viewModel.OccupancyRate = 0;
                }

                Debug.WriteLine($"Total Available Nights: {viewModel.TotalAvailableNights}");
                Debug.WriteLine($"Total Booked Nights: {viewModel.TotalBookedNights}");
                Debug.WriteLine($"Overall Occupancy Rate: {viewModel.OccupancyRate:F2}%");
            }

            return View(viewModel);
        }
    }
}

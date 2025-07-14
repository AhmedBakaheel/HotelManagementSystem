using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;

namespace HotelManagementSystem.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Invoices
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Invoices
                                       .Include(i => i.Booking)
                                           .ThenInclude(b => b.Customer) 
                                       .Include(i => i.Booking)
                                           .ThenInclude(b => b.Room) 
                                       .Include(i => i.Customer);

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer) 
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Room)
                .Include(i => i.Customer) 
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Invoices/Create
        public async Task<IActionResult> Create()
        {
            var bookings = await _context.Bookings
                                        .Include(b => b.Customer)
                                        .Include(b => b.Room)
                                        .ToListAsync();

         
            ViewData["BookingId"] = new SelectList(bookings.Select(b => new
            {
                Id = b.Id,               
                Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
            }), "Id", "Display");

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName");
            ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
            return View();
        }

        // POST: Invoices/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Create([Bind("Id,InvoiceNumber,InvoiceDate,DueDate,TotalAmount,PaidAmount,Status,BookingId,CustomerId,Notes,InvoiceItems")] Invoice invoice)
        {
            if (string.IsNullOrEmpty(invoice.InvoiceNumber))
            {
                string prefix = $"INV-{DateTime.Now.Year}-";
                var lastInvoice = await _context.Invoices
                                                .Where(i => i.InvoiceNumber != null && i.InvoiceNumber.StartsWith(prefix))
                                                .OrderByDescending(i => i.InvoiceNumber)
                                                .Select(i => i.InvoiceNumber)
                                                .FirstOrDefaultAsync();

                int lastNumber = 0;
                if (lastInvoice != null && lastInvoice.Length > prefix.Length && int.TryParse(lastInvoice.Substring(prefix.Length), out lastNumber))
                {
                    lastNumber++;
                }
                else
                {
                    lastNumber = 1;
                }
                invoice.InvoiceNumber = $"{prefix}{lastNumber:D4}";
            }
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                var serviceIds = invoice.InvoiceItems.Select(item => item.ServiceId).Distinct().ToList();
                var services = await _context.Services.Where(s => serviceIds.Contains(s.Id)).ToListAsync();

                foreach (var item in invoice.InvoiceItems)
                {
                    var service = services.FirstOrDefault(s => s.Id == item.ServiceId);
                    if (service != null)
                    {
                        item.UnitPrice = service.UnitPrice; 
                    }
                    else
                    {                       
                        ModelState.AddModelError($"InvoiceItems[{invoice.InvoiceItems.ToList().IndexOf(item)}].ServiceId", "الخدمة المختارة غير صالحة.");
                    }
                }
            }

            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                invoice.TotalAmount = invoice.InvoiceItems.Sum(item => item.Quantity * item.UnitPrice);
            }
            else
            {
                invoice.TotalAmount = 0;
            }

            if (ModelState.IsValid)
            {
                _context.Add(invoice);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookingId"] = new SelectList(_context.Bookings, "Id", "Id", invoice.BookingId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);
            ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
            return View(invoice);
        }

        // GET: Invoices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            var bookings = await _context.Bookings
                                         .Include(b => b.Customer)
                                         .Include(b => b.Room)
                                         .ToListAsync();

            ViewData["BookingId"] = new SelectList(bookings.Select(b => new
            {
                Id = b.Id,
                Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
            }), "Id", "Display", invoice.BookingId); 
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);
            ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
            return View(invoice);
        }

        // POST: Invoices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InvoiceNumber,InvoiceDate,DueDate,TotalAmount,PaidAmount,Status,BookingId,CustomerId,Notes")] Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return NotFound();
            }
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                var serviceIds = invoice.InvoiceItems.Select(item => item.ServiceId).Distinct().ToList();
                var services = await _context.Services.Where(s => serviceIds.Contains(s.Id)).ToListAsync();

                foreach (var item in invoice.InvoiceItems)
                {
                    var service = services.FirstOrDefault(s => s.Id == item.ServiceId);
                    if (service != null)
                    {
                        item.UnitPrice = service.UnitPrice;
                    }
                    else
                    {
                        ModelState.AddModelError($"InvoiceItems[{invoice.InvoiceItems.ToList().IndexOf(item)}].ServiceId", "الخدمة المختارة غير صالحة.");
                    }
                }
            }
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                invoice.TotalAmount = invoice.InvoiceItems.Sum(item => item.Quantity * item.UnitPrice);
            }
            else
            {
                invoice.TotalAmount = 0;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    
                    var existingInvoice = await _context.Invoices
                        .Include(i => i.InvoiceItems)
                        .FirstOrDefaultAsync(i => i.Id == id);

                    if (existingInvoice == null)
                    {
                        return NotFound();
                    }

                    
                    _context.Entry(existingInvoice).CurrentValues.SetValues(invoice);

                   
                    var itemsToRemove = existingInvoice.InvoiceItems
                                                        .Where(existingItem => !invoice.InvoiceItems
                                                        .Any(newItem => newItem.Id == existingItem.Id))
                                                        .ToList();
                    _context.InvoiceItems.RemoveRange(itemsToRemove);

                    foreach (var newItem in invoice.InvoiceItems)
                    {
                        if (newItem.Id == 0) 
                        {
                            newItem.InvoiceId = existingInvoice.Id;
                            _context.InvoiceItems.Add(newItem);
                        }
                        else
                        {
                            var existingItem = existingInvoice.InvoiceItems.FirstOrDefault(i => i.Id == newItem.Id);
                            if (existingItem != null)
                            {
                                _context.Entry(existingItem).CurrentValues.SetValues(newItem);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvoiceExists(invoice.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["BookingId"] = new SelectList(_context.Bookings, "Id", "Id", invoice.BookingId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);
            ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
            return View(invoice);
        }

        // GET: Invoices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Print(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer) 
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Room) 
                .Include(i => i.Customer) 
                .Include(i => i.InvoiceItems) 
                    .ThenInclude(ii => ii.Service) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }
        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.Id == id);
        }
    }
}

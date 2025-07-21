using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using HotelManagementSystem.Enums;

namespace HotelManagementSystem.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Customer);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Bookings/Create
        public IActionResult Create()
        {
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "RoomNumber");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName");
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CheckInDate,CheckOutDate,RoomId,CustomerId,Notes,Status")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                
                if (booking.Status == default(BookingStatus)) 
                {
                    booking.Status = BookingStatus.Pending; 
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "RoomNumber", booking.RoomId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            return View(booking);
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "RoomNumber", booking.RoomId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CheckInDate,CheckOutDate,RoomId,CustomerId,Notes,Status")] Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id))
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
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "RoomNumber", booking.RoomId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Bookings/Cancel/5
       
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

           
            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut || booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "لا يمكن إلغاء حجز تم تسجيل الدخول إليه، أو تم تسجيل الخروج منه، أو ملغي بالفعل.";
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }

            return View(booking);
        }

        // POST: Bookings/CancelConfirmed/5
       
        [HttpPost, ActionName("Cancel")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            
            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut || booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "لا يمكن إلغاء حجز تم تسجيل الدخول إليه، أو تم تسجيل الخروج منه، أو ملغي بالفعل.";
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }

            booking.Status = BookingStatus.Cancelled; 
            _context.Update(booking);
            await _context.SaveChangesAsync();

           

            TempData["SuccessMessage"] = "تم إلغاء الحجز بنجاح.";
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }


        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}

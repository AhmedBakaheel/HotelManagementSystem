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
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsActive), "Id", "RoomNumber");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName");
            ViewBag.RoomsForJs = _context.Rooms.Where(r => r.IsActive)
                                       .Select(r => new { r.Id, r.RoomNumber, r.PricePerNight })
                                       .ToList();
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CheckInDate,CheckOutDate,RoomId,CustomerId,Notes,Status,BookingDate,TotalAmount,ApplicationUserId")] Booking booking)
        {
            if (booking.Status == default(BookingStatus))
            {
                booking.Status = BookingStatus.Pending;
            }

            if (booking.BookingDate == default(DateTime))
            {
                booking.BookingDate = DateTime.Now;
            }

            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "The selected room does not exist.");
            }
            else
            {
                int numberOfNights = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays;
                if (numberOfNights < 0) numberOfNights = 0;
                booking.TotalAmount = room.PricePerNight * numberOfNights;
            }

            if (ModelState.IsValid)
            {
                if (!await IsRoomAvailable(booking.RoomId, booking.CheckInDate, booking.CheckOutDate, booking.Id))
                {
                    ModelState.AddModelError(string.Empty, "The room is not available for the selected period.");
                    ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsActive), "Id", "RoomNumber", booking.RoomId);
                    ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
                    return View(booking);
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsActive), "Id", "RoomNumber", booking.RoomId);
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
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsActive), "Id", "RoomNumber", booking.RoomId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            ViewBag.RoomsForJs = _context.Rooms.Where(r => r.IsActive)
                                       .Select(r => new { r.Id, r.RoomNumber, r.PricePerNight })
                                       .ToList();
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CheckInDate,CheckOutDate,RoomId,CustomerId,Notes,Status,BookingDate,TotalAmount,ApplicationUserId")] Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "The selected room does not exist.");
            }
            else
            {
                int numberOfNights = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays;
                if (numberOfNights < 0) numberOfNights = 0;
                booking.TotalAmount = room.PricePerNight * numberOfNights;
            }

            if (ModelState.IsValid)
            {
                if (!await IsRoomAvailable(booking.RoomId, booking.CheckInDate, booking.CheckOutDate, booking.Id))
                {
                    ModelState.AddModelError(string.Empty, "The room is not available for the selected period.");
                    ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsActive), "Id", "RoomNumber", booking.RoomId);
                    ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
                    return View(booking);
                }

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
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsActive), "Id", "RoomNumber", booking.RoomId);
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
                TempData["ErrorMessage"] = "Cannot cancel a booking that has been checked in, checked out, or already cancelled.";
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
                TempData["ErrorMessage"] = "Cannot cancel a booking that has been checked in, checked out, or already cancelled.";
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }

            booking.Status = BookingStatus.Cancelled;
            _context.Update(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking cancelled successfully.";
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }

        // GET: Bookings/CheckIn/5
        // Displays the confirmation page for checking in a booking
        public async Task<IActionResult> CheckIn(int? id)
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

            // Prevent check-in if already checked in, checked out, or cancelled
            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut || booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cannot check in a booking that has already been checked in, checked out, or cancelled.";
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }

            return View(booking);
        }

        // POST: Bookings/CheckInConfirmed/5
        // Updates booking status to CheckedIn
        [HttpPost, ActionName("CheckIn")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            // Re-check status before updating
            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut || booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cannot check in a booking that has already been checked in, checked out, or cancelled.";
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }

            booking.Status = BookingStatus.CheckedIn; // Update status to CheckedIn
            _context.Update(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking checked in successfully.";
            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }

        // GET: Bookings/CheckOut/5
        // Displays the confirmation page for checking out a booking
        public async Task<IActionResult> CheckOut(int? id)
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

            // Prevent check-out if not checked in, already checked out, or cancelled
            if (booking.Status != BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut || booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cannot check out a booking that is not checked in, or already checked out/cancelled.";
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }

            return View(booking);
        }

        // POST: Bookings/CheckOutConfirmed/5
        // Updates booking status to CheckedOut
        [HttpPost, ActionName("CheckOut")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            // Re-check status before updating
            if (booking.Status != BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut || booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cannot check out a booking that is not checked in, or already checked out/cancelled.";
                return RedirectToAction(nameof(Details), new { id = booking.Id });
            }

            booking.Status = BookingStatus.CheckedOut; // Update status to CheckedOut
            _context.Update(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking checked out successfully.";
            return RedirectToAction(nameof(Index)); // Redirect to Index after checkout
        }


        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        // Helper function to check room availability
        private async Task<bool> IsRoomAvailable(int roomId, DateTime checkInDate, DateTime checkOutDate, int? currentBookingId = null)
        {
            if (checkOutDate <= checkInDate)
            {
                return false; // Invalid period
            }

            var conflictingBookings = await _context.Bookings
                .Where(b => b.RoomId == roomId &&
                            b.Status != BookingStatus.Cancelled &&
                            b.Status != BookingStatus.CheckedOut &&
                            b.Id != currentBookingId &&
                            (b.CheckInDate < checkOutDate && b.CheckOutDate > checkInDate))
                .AnyAsync();

            return !conflictingBookings;
        }
    }
}

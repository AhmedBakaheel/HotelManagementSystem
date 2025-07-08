using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; 
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Identity; // لـ UserManager
using System.Security.Claims;
using HotelManagementSystem.ViewModel;
using HotelManagementSystem.Enums;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagementSystem.Controllers
{
    [Authorize(Roles = "Customer")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

           
            var userBookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Customer) 
                .Where(b => b.ApplicationUserId == user.Id)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

            var viewModel = new MyAccountViewModel
            {
                User = user,
                Bookings = userBookings
            };

            return View(viewModel);
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room) // Include related Room data
                .Include(b => b.Customer) // Include related Customer data
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
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "Id", "RoomNumber");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Email"); // أو "FullName" إذا أضفتها لنموذج العميل
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RoomId,CustomerId,CheckInDate,CheckOutDate,TotalAmount,Status")] Booking booking)
        {
            // Set BookingDate automatically
            booking.BookingDate = DateTime.Now;
            booking.Status = BookingStatus.Pending; // Initial status

            // Ensure CheckInDate is not in the past and CheckOutDate is after CheckInDate
            if (booking.CheckInDate < DateTime.Today)
            {
                ModelState.AddModelError("CheckInDate", "تاريخ الدخول لا يمكن أن يكون في الماضي.");
            }
            if (booking.CheckOutDate <= booking.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "تاريخ المغادرة يجب أن يكون بعد تاريخ الدخول.");
            }

            // Custom validation: Check room availability for the specified dates
            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "الغرفة المحددة غير موجودة.");
            }
            else
            {
                // Check for overlapping bookings for the same room
                var existingBookings = await _context.Bookings
                    .Where(b => b.RoomId == booking.RoomId &&
                                b.Status != BookingStatus.Cancelled &&
                                ((booking.CheckInDate < b.CheckOutDate && booking.CheckOutDate > b.CheckInDate) ||
                                 (booking.CheckInDate == b.CheckInDate && booking.CheckOutDate == b.CheckOutDate)))
                    .ToListAsync();

                if (existingBookings.Any())
                {
                    ModelState.AddModelError("RoomId", "هذه الغرفة محجوزة بالفعل في التواريخ المحددة.");
                    room.IsAvailable = false; // Optionally mark as unavailable if conflicting booking found
                }
                else
                {
                    room.IsAvailable = true; // Mark as available if no conflict (important for a new booking if the room was marked unavailable)
                }
            }

            if (ModelState.IsValid)
            {
                // Update room availability status
                if (room != null)
                {
                    room.IsAvailable = false; // Mark room as unavailable after a successful booking
                    _context.Update(room);
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If ModelState is not valid, re-populate SelectLists
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "Id", "RoomNumber", booking.RoomId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Email", booking.CustomerId);
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
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Email", booking.CustomerId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RoomId,CustomerId,CheckInDate,CheckOutDate,TotalAmount,BookingDate,Status")] Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            // Add validation similar to Create action for dates and room availability
            if (booking.CheckInDate < DateTime.Today)
            {
                ModelState.AddModelError("CheckInDate", "تاريخ الدخول لا يمكن أن يكون في الماضي.");
            }
            if (booking.CheckOutDate <= booking.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "تاريخ المغادرة يجب أن يكون بعد تاريخ الدخول.");
            }

            // Check room availability for the updated dates, excluding the current booking itself
            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "الغرفة المحددة غير موجودة.");
            }
            else
            {
                var existingBookings = await _context.Bookings
                    .Where(b => b.RoomId == booking.RoomId &&
                                b.Id != booking.Id && // Exclude the current booking being edited
                                b.Status != BookingStatus.Cancelled &&
                                ((booking.CheckInDate < b.CheckOutDate && booking.CheckOutDate > b.CheckInDate) ||
                                 (booking.CheckInDate == b.CheckInDate && booking.CheckOutDate == b.CheckOutDate)))
                    .ToListAsync();

                if (existingBookings.Any())
                {
                    ModelState.AddModelError("RoomId", "هذه الغرفة محجوزة بالفعل في التواريخ المحددة.");
                    room.IsAvailable = false; // Temporarily mark as unavailable for validation feedback
                }
                else
                {
                    // If the booking is confirmed/pending, mark the room as unavailable.
                    // If it's cancelled/completed, ensure room is marked as available if no other bookings conflict.
                    if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Pending)
                    {
                        room.IsAvailable = false;
                    }
                    // A more robust solution would involve checking if the room is available for other bookings as well
                    // For simplicity, we'll keep it straightforward here.
                }
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // Handle Room availability based on Booking Status
                    if (room != null)
                    {
                        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                        {
                            // Check if there are other active bookings for this room
                            var activeBookingsForRoom = await _context.Bookings
                                .AnyAsync(b => b.RoomId == booking.RoomId &&
                                               b.Id != booking.Id && // Exclude current booking
                                               (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending));
                            if (!activeBookingsForRoom)
                            {
                                room.IsAvailable = true; // Make room available if no other active bookings
                            }
                        }
                        else // Confirmed or Pending
                        {
                            room.IsAvailable = false; // Mark room as unavailable
                        }
                        _context.Update(room);
                    }

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
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Email", booking.CustomerId);
            return View(booking);
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
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
                // Before deleting, consider making the room available if this was the only active booking
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null)
                {
                    // Check if there are other active bookings for this room
                    var activeBookingsForRoom = await _context.Bookings
                        .AnyAsync(b => b.RoomId == booking.RoomId &&
                                       b.Id != booking.Id && // Exclude the booking being deleted
                                       (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending));
                    if (!activeBookingsForRoom)
                    {
                        room.IsAvailable = true; // Make room available if no other active bookings
                        _context.Update(room);
                    }
                }
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

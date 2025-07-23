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
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.IsActive), "Id", "RoomType");
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
                ModelState.AddModelError("RoomId", "الغرفة المختارة غير موجودة.");
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
                    ModelState.AddModelError(string.Empty, "الغرفة غير متاحة للفترة الزمنية المختارة.");                    
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

            // <== جديد: حساب TotalAmount بناءً على سعر الغرفة ومدة الإقامة
            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "الغرفة المختارة غير موجودة.");
            }
            else
            {
                int numberOfNights = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays;
                if (numberOfNights < 0) numberOfNights = 0;
                booking.TotalAmount = room.PricePerNight * numberOfNights;
            }

            if (ModelState.IsValid)
            {
                // التحقق من توفر الغرفة قبل تعديل الحجز
                // نمرر booking.Id لاستبعاد الحجز الحالي من التحقق (لتجنب التضارب مع نفسه)
                if (!await IsRoomAvailable(booking.RoomId, booking.CheckInDate, booking.CheckOutDate, booking.Id))
                {
                    ModelState.AddModelError(string.Empty, "الغرفة غير متاحة للفترة الزمنية المختارة.");
                    // إعادة تعبئة SelectLists عند فشل التحقق
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
            // إعادة تعبئة SelectLists عند فشل التحقق
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

        // دالة مساعدة للتحقق من توفر الغرفة
        private async Task<bool> IsRoomAvailable(int roomId, DateTime checkInDate, DateTime checkOutDate, int? currentBookingId = null)
        {
            // تأكد من أن تاريخ تسجيل الخروج بعد تاريخ تسجيل الدخول
            if (checkOutDate <= checkInDate)
            {
                return false; // فترة غير صالحة
            }

            // ابحث عن أي حجوزات موجودة لنفس الغرفة تتداخل مع الفترة المطلوبة
            var conflictingBookings = await _context.Bookings
                .Where(b => b.RoomId == roomId &&
                            b.Status != BookingStatus.Cancelled && // استبعاد الحجوزات الملغاة
                            b.Status != BookingStatus.CheckedOut && // استبعاد الحجوزات التي انتهت
                            b.Id != currentBookingId && // استبعاد الحجز الحالي إذا كان للتعديل
                                                        // التحقق من التداخل:
                                                        // (تاريخ دخول الحجز الحالي < تاريخ خروج الحجز الجديد) && (تاريخ خروج الحجز الحالي > تاريخ دخول الحجز الجديد)
                            (b.CheckInDate < checkOutDate && b.CheckOutDate > checkInDate))
                .AnyAsync(); // إذا تم العثور على أي حجز متضارب، فليست الغرفة متاحة

            return !conflictingBookings; // الغرفة متاحة إذا لم يتم العثور على حجوزات متضاربة
        }
    }
}

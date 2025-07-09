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
    [Authorize(Roles = "Receptionist,Customer")]
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
            ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.Status == RoomStatus.Available), "Id", "RoomNumber");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Email"); 
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RoomId,CustomerId,CheckInDate,CheckOutDate,TotalAmount,Status")] Booking booking)
        {
            // 1. تعيين التواريخ والحالة الأولية
            booking.BookingDate = DateTime.Now;
            booking.Status = BookingStatus.Pending; // الحالة الأولية للحجز

            // 2. التحقق من صحة تواريخ الدخول والخروج (كما كان لديك)
            if (booking.CheckInDate < DateTime.Today)
            {
                ModelState.AddModelError("CheckInDate", "تاريخ الدخول لا يمكن أن يكون في الماضي.");
            }
            if (booking.CheckOutDate <= booking.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "تاريخ المغادرة يجب أن يكون بعد تاريخ الدخول.");
            }

            // 3. ربط الحجز بالمستخدم المسجل دخوله
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // إذا لم يكن هناك مستخدم مسجل دخوله (وهذا لا ينبغي أن يحدث إذا كانت الصفحة محمية بـ [Authorize])
                // يمكنك إعادة التوجيه لصفحة تسجيل الدخول أو إرجاع خطأ.
                ModelState.AddModelError("", "يجب تسجيل الدخول لإتمام عملية الحجز.");
            }
            else
            {
                booking.ApplicationUserId = currentUser.Id; // ربط الحجز بمعرف المستخدم

                // 4. ربط الحجز بالعميل:
                // ابحث عن عميل موجود ببريد المستخدم، أو أنشئ عميل جديد إذا لم يكن موجوداً.
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == currentUser.Email);
                if (customer == null)
                {
                    // إنشاء عميل جديد إذا لم يتم العثور عليه
                    customer = new Customer
                    {
                        FirstName = currentUser.FirstName ?? "غير معروف", // استخدم بيانات المستخدم
                        LastName = currentUser.LastName ?? "غير معروف",
                        Email = currentUser.Email,
                        PhoneNumber = currentUser.PhoneNumber // يمكن أن يكون null
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync(); // حفظ العميل للحصول على CustomerId
                }
                booking.CustomerId = customer.Id; // ربط الحجز بمعرف العميل
            }

            // 5. التحقق من توفر الغرفة (منطقك المخصص)
            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "الغرفة المحددة غير موجودة.");
            }
            else
            {
                // التحقق من الحجوزات المتداخلة (منطقك الذي كان لديك)
                var existingBookings = await _context.Bookings
                    .Where(b => b.RoomId == booking.RoomId &&
                                b.Status != BookingStatus.Cancelled && // استبعد الحجوزات الملغاة
                                ((booking.CheckInDate < b.CheckOutDate && booking.CheckOutDate > b.CheckInDate) || // تداخل الفترات
                                 (booking.CheckInDate == b.CheckInDate && booking.CheckOutDate == b.CheckOutDate))) // نفس التواريخ
                    .ToListAsync();

                if (existingBookings.Any())
                {
                    ModelState.AddModelError("RoomId", "هذه الغرفة محجوزة بالفعل في التواريخ المحددة.");
                }
            }


            // 6. التحقق من صحة النموذج بالكامل قبل الحفظ
            if (ModelState.IsValid)
            {
                // تحديث حالة الغرفة بعد التأكد من صحة كل شيء
                if (room != null) // تأكد أن الغرفة ليست null قبل التحديث
                {
                    room.Status = RoomStatus.Booked; // أو RoomStatus.Unavailable، حسب تعريفك
                    _context.Update(room); // تحديث حالة الغرفة في الـ DB
                }

                _context.Add(booking); // إضافة الحجز الجديد
                await _context.SaveChangesAsync(); // حفظ التغييرات

                // العودة إلى صفحة Index (أو صفحة تأكيد الحجز)
                return RedirectToAction(nameof(Index));
            }

            // 7. إذا كان النموذج غير صالح، أعد تهيئة الـ SelectLists وأعد الـ View
            // تأكد من جلب الغرف المتاحة فقط للقائمة المنسدلة
            ViewData["RoomId"] = new SelectList(
                await _context.Rooms
                              .Where(r => r.Status == RoomStatus.Available || r.Id == booking.RoomId) // أظهر الغرفة المختارة حتى لو لم تكن متاحة الآن
                              .ToListAsync(),
                "Id",
                "RoomNumber",
                booking.RoomId);

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Email", booking.CustomerId); // يمكنك تغيير "Email" إلى "FullName" إذا كان لديك هذه الخاصية
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
                    room.Status = RoomStatus.Booked; // Temporarily mark as unavailable for validation feedback
                }
                else
                {
                   
                    if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Pending)
                    {
                        room.Status = RoomStatus.Booked;
                    }
                   
                }
            }


            if (ModelState.IsValid)
            {
                try
                {
                    
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
                                room.Status = RoomStatus.Available;
                            }
                        }
                        else // Confirmed or Pending
                        {
                            room.Status = RoomStatus.Booked; // Mark room as unavailable
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
                        room.Status = RoomStatus.Available; // Make room available if no other active bookings
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

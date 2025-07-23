using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;
using HotelManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using HotelManagementSystem.ViewModel; 
using HotelManagementSystem.Enums;
namespace HotelManagementSystem.Controllers
{
    public class OnlineBookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // سنحتاجه لربط الحجز بالمستخدم

        public OnlineBookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /OnlineBooking/Search - لعرض نموذج البحث
        [HttpGet]
        public IActionResult Search()
        {
            var viewModel = new RoomSearchViewModel();
            // يمكنك تهيئة التواريخ الافتراضية هنا أيضاً
            return View(viewModel);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(RoomSearchViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (viewModel.CheckInDate >= viewModel.CheckOutDate)
                {
                    ModelState.AddModelError(string.Empty, "تاريخ المغادرة يجب أن يكون بعد تاريخ الدخول.");
                    viewModel.AvailableRooms = new List<Room>(); 
                    return View(viewModel);
                }

                var availableRooms = await _context.Rooms
                    .Where(r => r.Status == RoomStatus.Available && 
                        (viewModel.RoomType == null || r.RoomType == viewModel.RoomType) &&                                                                                            
                        !_context.Bookings.Any(b =>
                            b.RoomId == r.Id &&
                            b.Status != BookingStatus.Cancelled && 
                            (
                                (viewModel.CheckInDate < b.CheckOutDate && viewModel.CheckOutDate > b.CheckInDate) 
                            )
                        ))
                    .ToListAsync();

                if (availableRooms.Any())
                {
                    viewModel.AvailableRooms = availableRooms;
                    viewModel.Message = $"تم العثور على {availableRooms.Count} غرف متاحة.";
                }
                else
                {
                    viewModel.AvailableRooms = new List<Room>();
                    viewModel.Message = "عذراً، لا توجد غرف متاحة بالمعايير المحددة.";
                }
            }
            return View(viewModel);
        }

        // GET: /OnlineBooking/Book/{id} - لعرض صفحة تأكيد الحجز لغرفة معينة
        [HttpGet]
        [Authorize(Roles = "Customer")] // يجب أن يكون العميل مسجلاً للدخول للحجز
        public async Task<IActionResult> Book(int id, DateTime checkInDate, DateTime checkOutDate)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null || room.Status != RoomStatus.Available)
            {
                TempData["Message"] = "الغرفة غير موجودة أو غير متاحة للحجز.";
                return RedirectToAction(nameof(Search));
            }

            // تأكد من أن تواريخ الحجز صالحة وغير متداخلة مرة أخرى
            if (checkInDate >= checkOutDate || checkInDate < DateTime.Today)
            {
                TempData["Message"] = "تواريخ الحجز غير صالحة.";
                return RedirectToAction(nameof(Search));
            }

            // تحقق مرة أخرى من توفر الغرفة في هذه التواريخ
            var isRoomAvailable = !await _context.Bookings.AnyAsync(b =>
                b.RoomId == id &&
                b.Status != BookingStatus.Cancelled &&
                (checkInDate < b.CheckOutDate && checkOutDate > b.CheckInDate));

            if (!isRoomAvailable)
            {
                TempData["Message"] = "عذراً، الغرفة لم تعد متاحة لهذه التواريخ.";
                return RedirectToAction(nameof(Search));
            }


            var viewModel = new BookRoomViewModel // سننشئ هذا الـ ViewModel في الخطوة التالية
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                PricePerNight = room.PricePerNight,
                Description = room.Description,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate
            };

            return View(viewModel);
        }

        // POST: /OnlineBooking/ConfirmBooking - لمعالجة تأكيد الحجز
        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(BookRoomViewModel viewModel) // استقبل ViewModel الجديد
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "بيانات الحجز غير صحيحة.";
                return View("Book", viewModel); // العودة إلى صفحة التأكيد مع الأخطاء
            }

            var room = await _context.Rooms.FindAsync(viewModel.RoomId);
            if (room == null || room.Status != RoomStatus.Available)
            {
                TempData["Message"] = "الغرفة غير موجودة أو غير متاحة.";
                return RedirectToAction(nameof(Search));
            }

            // تحقق نهائي من توفر الغرفة قبل الحفظ
            var isRoomAvailable = !await _context.Bookings.AnyAsync(b =>
                b.RoomId == room.Id &&
                b.Status != BookingStatus.Cancelled &&
                (viewModel.CheckInDate < b.CheckOutDate && viewModel.CheckOutDate > b.CheckInDate));

            if (!isRoomAvailable)
            {
                TempData["Message"] = "عذراً، الغرفة لم تعد متاحة لهذه التواريخ.";
                return RedirectToAction(nameof(Search));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Message"] = "يجب تسجيل الدخول لإكمال الحجز.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // البحث عن العميل في جدول Customers أو إنشاؤه
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == currentUser.Email);
            if (customer == null)
            {
                // إنشاء عميل جديد إذا لم يكن موجوداً
                customer = new Customer
                {
                    FirstName = currentUser.FirstName ?? "N/A",
                    LastName = currentUser.LastName ?? "N/A",
                    Email = currentUser.Email,
                    PhoneNumber = currentUser.PhoneNumber
                };
                _context.Add(customer);
                await _context.SaveChangesAsync();
            }

            var booking = new Booking
            {
                RoomId = viewModel.RoomId,
                CustomerId = customer.Id, // ربط الحجز بالعميل من جدول العملاء
                ApplicationUserId = currentUser.Id, // ربط الحجز بالـ ApplicationUser
                CheckInDate = viewModel.CheckInDate,
                CheckOutDate = viewModel.CheckOutDate,
                Status = BookingStatus.Pending // يمكن تغييرها إلى Confirmed مباشرة إذا كانت سياسة الفندق تسمح بذلك
            };

            _context.Add(booking);
            room.Status = RoomStatus.Booked; // تعيين الغرفة كغير متاحة بعد الحجز (يمكن تعديل هذا المنطق لاحقاً ليعتمد على حالة الحجز Confirmed)

            await _context.SaveChangesAsync();

            TempData["Message"] = "تم تأكيد حجزك بنجاح! رقم الحجز هو: " + booking.Id;
            return RedirectToAction(nameof(BookingConfirmation), new { id = booking.Id });
        }

        // GET: /OnlineBooking/BookingConfirmation/{id} - لعرض صفحة تأكيد الحجز بعد إجرائه
        public async Task<IActionResult> BookingConfirmation(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Customer)
               .Include(b => b.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // تأكد أن الحجز يخص المستخدم الحالي (لمنع عرض حجوزات الآخرين)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || booking.ApplicationUserId != currentUser.Id)
            {
                TempData["Message"] = "لا تملك صلاحية عرض هذا الحجز.";
                return RedirectToAction(nameof(Search));
            }

            return View(booking);
        }
    }

  
}
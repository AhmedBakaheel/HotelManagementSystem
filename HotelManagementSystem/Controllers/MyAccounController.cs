using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using HotelManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HotelManagementSystem.ViewModel; // لـ GetUserId

namespace HotelManagementSystem.Controllers
{
    [Authorize(Roles = "Customer")] 
    public class MyAccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public MyAccountController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // *** جلب حجوزات هذا المستخدم فقط باستخدام ApplicationUserId ***
            var userBookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Customer) // إذا كنت لا تزال ترغب في عرض تفاصيل العميل
                .Where(b => b.ApplicationUserId == user.Id) // <== التغيير هنا
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

            var viewModel = new MyAccountViewModel
            {
                User = user,
                Bookings = userBookings
            };

            return View(viewModel);
        }

        // POST: /MyAccount/UpdateProfile - لتحديث معلومات الملف الشخصي (اختياري الآن، يمكن إضافته لاحقاً)
        // في هذه المرحلة، سنركز على العرض فقط
    }

  
}
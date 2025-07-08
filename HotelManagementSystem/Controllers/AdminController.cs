using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Add for [Authorize]
using HotelManagementSystem.Models; // For BookingStatus enum (optional, depending on how you display it)

namespace HotelManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admin can access this controller
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Index - Displays list of users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            // You might want to create a ViewModel here to include roles with each user
            return View(users);
        }

        // GET: Admin/ManageUserRoles/userId
        [HttpGet] // لتحديد أنه يستقبل طلبات GET
        public async Task<IActionResult> ManageUserRoles(string userId)
        {
            ViewBag.UserId = userId; // تمرير UserId إلى الـ View
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                ViewBag.ErrorMessage = $"المستخدم ذو المعرف {userId} غير موجود.";
                return View("NotFound"); // أو RedirectToAction("Index")
            }

            ViewBag.UserName = user.UserName; // تمرير UserName إلى الـ View

            var model = new List<UserRoleViewModel>(); // ViewModel لقائمة الأدوار

            // جلب جميع الأدوار المتاحة في النظام
            foreach (var role in _roleManager.Roles)
            {
                var userRoleViewModel = new UserRoleViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                // التحقق مما إذا كان المستخدم يمتلك هذا الدور بالفعل
                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    userRoleViewModel.IsSelected = true;
                }
                model.Add(userRoleViewModel);
            }

            return View(model); // تمرير قائمة الأدوار إلى الـ View
        }


        // POST: Admin/ManageUserRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUserRoles(string userId, List<UserRoleViewModel> model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Where(m => m.IsSelected).Select(m => m.RoleName);

            // Add new roles to user
            foreach (var roleName in selectedRoles.Except(userRoles))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }

            // Remove roles user no longer has
            foreach (var roleName in userRoles.Except(selectedRoles))
            {
                await _userManager.RemoveFromRoleAsync(user, roleName);
            }

            return RedirectToAction(nameof(Index));
        }

        // ViewModel for managing user roles
        public class UserRoleViewModel
        {
            public string? RoleId { get; set; }
            public string? RoleName { get; set; }
            public bool IsSelected { get; set; }
        }
    }
}
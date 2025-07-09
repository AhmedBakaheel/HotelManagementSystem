using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using HotelManagementSystem.Models; // تأكد من هذا الـ using لـ ApplicationUser
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HotelManagementSystem.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } = string.Empty;
        [TempData]
        public string? StatusMessage { get; set; }
        public bool IsEmailConfirmed { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel(); 
        public class InputModel
        {
            [Display(Name = "اسم المستخدم")]
            public string Username { get; set; } = string.Empty;
            [Phone]
            [Display(Name = "رقم الهاتف")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "الاسم الأول")]
            public string? FirstName { get; set; }

            [Display(Name = "اسم العائلة")]
            public string? LastName { get; set; }
        }

       
        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName ?? string.Empty; 

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }


        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

           

            Username = user.UserName ?? string.Empty; // تعيين لخاصية Username العامة في IndexModel

            Input = new InputModel
            {
                PhoneNumber = user.PhoneNumber, // استخدام بيانات المستخدم مباشرة
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // تحديث رقم الهاتف
            if (Input.PhoneNumber != user.PhoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "خطأ غير متوقع عند محاولة تعيين رقم الهاتف.";
                    return RedirectToPage();
                }
            }

            // *** إضافة تحديث الاسم الأول واسم العائلة ***
            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
                await _userManager.UpdateAsync(user);
            }
            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "تم تحديث ملفك الشخصي.";
            return RedirectToPage();
        }
    }
}
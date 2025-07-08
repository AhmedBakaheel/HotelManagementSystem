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

        /// <summary>
        ///   This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///   directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Display(Name = "اسم المستخدم")] // إضافة Display هنا مباشرة لـ Username
        public string Username { get; set; } = string.Empty; // تهيئة لمنع NullReferenceException

        /// <summary>
        ///   This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///   directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string? StatusMessage { get; set; } // يفضل استخدام string? لتمكين القيمة null

        // *** إضافة هذه الخاصية التي كانت مفقودة وتسببت في الخطأ ***
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
        ///   This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///   directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel(); // تهيئة لتجنب NullReferenceException

        /// <summary>
        ///   This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///   directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///   This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///   directly from your code. This API may change or be removed in future releases.
            /// </summary>
            // [Display(Name = "اسم المستخدم")] // لا تكرر Username هنا إذا كانت موجودة في IndexModel
            // يمكنك إزالته من هنا إذا كنت ستعتمد على Username في IndexModel
            // وإذا أردت أن يكون قابلاً للتعديل من خلال InputModel، فاجعله هنا.
            // بناءً على الكود أعلاه، يبدو أنك تستخدم Username من IndexModel كخاصية عرض.
            // سأفترض أنك لا تحتاجها هنا، ولكن سأتركها مع ملاحظة
            // public string Username { get; set; } = string.Empty;
            [Display(Name = "اسم المستخدم")]
            public string Username { get; set; }
            [Phone]
            [Display(Name = "رقم الهاتف")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "الاسم الأول")]
            public string? FirstName { get; set; }

            [Display(Name = "اسم العائلة")]
            public string? LastName { get; set; }
        }

        // هذه الدالة LoadAsync هي جزء من الكود الذي يتم توليده عادة بواسطة Scaffold،
        // وهي تقوم بتحميل بيانات المستخدم في الخصائص. يمكنك الاحتفاظ بها أو دمجها
        // مباشرة في OnGetAsync إذا كنت لا تحتاج إلى فصل المنطق.
        // بما أن OnGetAsync يقوم بنفس العمل، يمكنك إزالة هذه الدالة أو جعلها خاصة
        // إذا كنت تريد فصل المنطق.
        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName ?? string.Empty; // تأكد من تعيينها من المستخدم الفعلي

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                // تأكد من تهيئة FirstName و LastName هنا أيضاً
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

            // يتم استدعاء LoadAsync هنا عادةً لتهيئة الخصائص.
            // بما أنك قمت بتهيئة Input يدوياً بالقيم، يمكنك إما استخدام LoadAsync
            // أو الاحتفاظ بتهيئة Input مباشرة هنا.
            // سأبقي على تهيئتك المباشرة، مع إضافة تعيين Username للخاصية العامة.

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

            // *** تحديث الاسم الأول واسم العائلة ***
            // يجب أن يتم تحديث الخصائص على كائن المستخدم ثم حفظ التغييرات.
            // مقارنة القيم قبل التحديث لتقليل العمليات على قاعدة البيانات.

            bool profileChanged = false;

            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
                profileChanged = true;
            }
            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
                profileChanged = true;
            }

            if (profileChanged)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "خطأ غير متوقع عند محاولة تحديث الملف الشخصي.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "تم تحديث ملفك الشخصي.";
            return RedirectToPage();
        }
    }
}
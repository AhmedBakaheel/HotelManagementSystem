using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الخدمة مطلوب.")]
        [Display(Name = "اسم الخدمة")]
        [StringLength(100, ErrorMessage = "اسم الخدمة لا يمكن أن يتجاوز 100 حرف.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "سعر الوحدة للخدمة مطلوب.")]
        [Display(Name = "سعر الوحدة")]
        [Column(TypeName = "decimal(18, 2)")] // لتحديد دقة الرقم العشري في قاعدة البيانات
        [Range(0.01, double.MaxValue, ErrorMessage = "سعر الوحدة يجب أن يكون أكبر من صفر.")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "الوصف")]
        [StringLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف.")]
        public string? Description { get; set; } // يمكن أن يكون الوصف اختيارياً

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;
    }
}

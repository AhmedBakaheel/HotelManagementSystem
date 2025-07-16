using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics; // <== أضف هذا الـ using لـ Debug.WriteLine

namespace HotelManagementSystem.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Invoices
        // يعرض قائمة بجميع الفواتير مع تضمين بيانات الحجز والعميل والغرفة المرتبطة
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Invoices
                                       .Include(i => i.Booking)
                                           .ThenInclude(b => b.Customer) // تضمين العميل المرتبط بالحجز
                                       .Include(i => i.Booking)
                                           .ThenInclude(b => b.Room) // تضمين الغرفة المرتبطة بالحجز
                                       .Include(i => i.Customer); // العميل المباشر للفاتورة (إذا لم تكن مرتبطة بحجز)

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Invoices/Details/5
        // يعرض تفاصيل فاتورة محددة مع جميع بياناتها المرتبطة
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer) // تضمين العميل المرتبط بالحجز
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Room) // تضمين الغرفة المرتبطة بالحجز
                .Include(i => i.Customer) // العميل المباشر للفاتورة
                .Include(i => i.InvoiceItems) // تضمين بنود الفاتورة
                    .ThenInclude(ii => ii.Service) // وتضمين الخدمة لكل بند فاتورة
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Invoices/Create
        // يعرض نموذج إنشاء فاتورة جديدة
        public async Task<IActionResult> Create()
        {
            // جلب الحجوزات إلى الذاكرة أولاً قبل بناء النص الوصفي لتجنب خطأ "null propagating operator"
            var bookings = await _context.Bookings
                                         .Include(b => b.Customer)
                                         .Include(b => b.Room)
                                         .ToListAsync();

            // إنشاء SelectList بنص وصفي للحجوزات (العميل - الغرفة)
            ViewData["BookingId"] = new SelectList(bookings.Select(b => new
            {
                Id = b.Id,
                Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
            }), "Id", "Display");

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName");
            ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList(); // توفير قائمة الخدمات النشطة
            return View();
        }

        // POST: Invoices/Create
        // يستقبل بيانات الفاتورة الجديدة من النموذج ويقوم بحفظها
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InvoiceNumber,InvoiceDate,DueDate,TotalAmount,PaidAmount,Status,BookingId,CustomerId,Notes,InvoiceItems")] Invoice invoice)
        {
            // 1. توليد رقم الفاتورة التسلسلي تلقائياً إذا لم يكن موجوداً
            if (string.IsNullOrEmpty(invoice.InvoiceNumber))
            {
                string prefix = $"INV-{DateTime.Now.Year}-";
                var lastInvoice = await _context.Invoices
                                                .Where(i => i.InvoiceNumber != null && i.InvoiceNumber.StartsWith(prefix))
                                                .OrderByDescending(i => i.InvoiceNumber)
                                                .Select(i => i.InvoiceNumber)
                                                .FirstOrDefaultAsync();

                int lastNumber = 0;
                if (lastInvoice != null && lastInvoice.Length > prefix.Length && int.TryParse(lastInvoice.Substring(prefix.Length), out lastNumber))
                {
                    lastNumber++;
                }
                else
                {
                    lastNumber = 1;
                }
                invoice.InvoiceNumber = $"{prefix}{lastNumber:D4}";
            }

            // Debugging: تحقق من البيانات الخام المرسلة من النموذج
            Debug.WriteLine("--- Raw Form Data for InvoiceItems (Create) ---");
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("InvoiceItems["))
                {
                    Debug.WriteLine($"{key}: {Request.Form[key]}");
                }
            }
            Debug.WriteLine("------------------------------------");

            // 2. تعبئة سعر الوحدة (UnitPrice) لكل بند فاتورة من قاعدة البيانات
            // ومعالجة البنود غير الصالحة أو الفارغة
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                var processedInvoiceItems = new List<InvoiceItem>();
                var allServices = await _context.Services.ToListAsync(); // جلب جميع الخدمات مرة واحدة

                for (int i = 0; i < invoice.InvoiceItems.Count; i++)
                {
                    var item = invoice.InvoiceItems.ElementAt(i);

                    // تحقق من أن البند ليس null وأن ServiceId صالح
                    if (item == null || item.ServiceId <= 0)
                    {
                        ModelState.AddModelError($"InvoiceItems[{i}].ServiceId", "الخدمة المختارة غير صالحة أو مفقودة لهذا البند.");
                        continue; // تخطي هذا البند
                    }

                    var service = allServices.FirstOrDefault(s => s.Id == item.ServiceId);
                    if (service != null)
                    {
                        item.UnitPrice = service.UnitPrice; // تعيين سعر الوحدة من الخدمة
                        processedInvoiceItems.Add(item); // إضافة البند المعالج والصالح
                    }
                    else
                    {
                        ModelState.AddModelError($"InvoiceItems[{i}].ServiceId", "الخدمة المختارة غير موجودة في قاعدة البيانات.");
                    }
                }
                // <== التعديل الحاسم: استبدال المجموعة الأصلية بالمجموعة المعالجة
                invoice.InvoiceItems = processedInvoiceItems;

                if (!invoice.InvoiceItems.Any())
                {
                    Debug.WriteLine("All InvoiceItems were filtered out due to being NULL, having ServiceId <= 0, or service not found.");
                }
            }
            else
            {
                Debug.WriteLine("InvoiceItems collection is NULL or EMPTY after model binding in Create POST action.");
            }

            // حساب تكلفة الحجز
            decimal bookingCost = 0;
            if (invoice.BookingId.HasValue)
            {
                var booking = await _context.Bookings
                                            .Include(b => b.Room) // تأكد من تضمين الغرفة لجلب سعر الليلة
                                            .FirstOrDefaultAsync(b => b.Id == invoice.BookingId.Value);
                if (booking != null)
                {
                    bookingCost = CalculateBookingCost(booking); // استخدام الدالة المساعدة
                    Debug.WriteLine($"Calculated Booking Cost: {bookingCost}");
                }
            }

            // 3. حساب TotalAmount بناءً على بنود الفاتورة المستلمة + تكلفة الحجز
            decimal invoiceItemsSum = 0;
            // <== هذا الشرط والـ Sum يعملان الآن على المجموعة المعالجة والصالحة
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                invoiceItemsSum = invoice.InvoiceItems.Sum(item => item.Quantity * item.UnitPrice);
            }
            invoice.TotalAmount = invoiceItemsSum + bookingCost;

            // 4. التحقق من صحة النموذج
            if (!ModelState.IsValid)
            {
                Debug.WriteLine("ModelState is NOT valid in Create POST action. Errors:");
                foreach (var modelStateEntry in ModelState)
                {
                    if (modelStateEntry.Value.Errors.Any())
                    {
                        Debug.WriteLine($"  Field: {modelStateEntry.Key}");
                        foreach (var error in modelStateEntry.Value.Errors)
                        {
                            Debug.WriteLine($"    Error: {error.ErrorMessage}");
                        }
                    }
                }

                // 5. إذا كان النموذج غير صالح، أعد تعبئة الـ SelectLists والـ ViewBag.Services
                var bookings = await _context.Bookings
                                             .Include(b => b.Customer)
                                             .Include(b => b.Room)
                                             .ToListAsync();
                ViewData["BookingId"] = new SelectList(bookings.Select(b => new
                {
                    Id = b.Id,
                    Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
                }), "Id", "Display", invoice.BookingId);

                ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);
                ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
                return View(invoice);
            }

            // إذا كان النموذج صالحاً، تابع عملية الحفظ
            Debug.WriteLine($"Invoice TotalAmount before saving in Create: {invoice.TotalAmount}");
            _context.Add(invoice);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Invoices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            var bookings = await _context.Bookings
                                         .Include(b => b.Customer)
                                         .Include(b => b.Room)
                                         .ToListAsync();

            ViewData["BookingId"] = new SelectList(bookings.Select(b => new
            {
                Id = b.Id,
                Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
            }), "Id", "Display", invoice.BookingId);

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);
            ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
            return View(invoice);
        }

        // POST: Invoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InvoiceNumber,InvoiceDate,DueDate,TotalAmount,PaidAmount,Status,BookingId,CustomerId,Notes,InvoiceItems")] Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return NotFound();
            }

            Debug.WriteLine("--- Raw Form Data for InvoiceItems (Edit) ---");
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("InvoiceItems["))
                {
                    Debug.WriteLine($"{key}: {Request.Form[key]}");
                }
            }
            Debug.WriteLine("------------------------------------");

           
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                var processedInvoiceItems = new List<InvoiceItem>();
                var allServices = await _context.Services.ToListAsync(); 

                for (int i = 0; i < invoice.InvoiceItems.Count; i++)
                {
                    var item = invoice.InvoiceItems.ElementAt(i);

                    if (item == null || item.ServiceId <= 0)
                    {
                        ModelState.AddModelError($"InvoiceItems[{i}].ServiceId", "الخدمة المختارة غير صالحة أو مفقودة لهذا البند.");
                        continue; 
                    }

                    var service = allServices.FirstOrDefault(s => s.Id == item.ServiceId);
                    if (service != null)
                    {
                        item.UnitPrice = service.UnitPrice;
                        processedInvoiceItems.Add(item);
                    }
                    else
                    {
                        ModelState.AddModelError($"InvoiceItems[{i}].ServiceId", "الخدمة المختارة غير موجودة في قاعدة البيانات.");
                    }
                }
                invoice.InvoiceItems = processedInvoiceItems;

                if (!invoice.InvoiceItems.Any())
                {
                    Debug.WriteLine("All InvoiceItems were filtered out due to being NULL, having ServiceId <= 0, or service not found.");
                }
            }
            else
            {
                Debug.WriteLine("InvoiceItems collection is NULL or EMPTY after model binding in Edit POST action.");
            }
            decimal bookingCost = 0;
            if (invoice.BookingId.HasValue)
            {
                var booking = await _context.Bookings
                                            .Include(b => b.Room) 
                                            .FirstOrDefaultAsync(b => b.Id == invoice.BookingId.Value);
                if (booking != null)
                {
                    bookingCost = CalculateBookingCost(booking);
                    Debug.WriteLine($"Calculated Booking Cost: {bookingCost}");
                }
            }
            decimal invoiceItemsSum = 0;
            
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                invoiceItemsSum = invoice.InvoiceItems.Sum(item => item.Quantity * item.UnitPrice);
            }
            invoice.TotalAmount = invoiceItemsSum + bookingCost;

           
            if (!ModelState.IsValid)
            {
                Debug.WriteLine("ModelState is NOT valid in Edit POST action. Errors:");
                foreach (var modelStateEntry in ModelState)
                {
                    if (modelStateEntry.Value.Errors.Any())
                    {
                        Debug.WriteLine($"  Field: {modelStateEntry.Key}");
                        foreach (var error in modelStateEntry.Value.Errors)
                        {
                            Debug.WriteLine($"    Error: {error.ErrorMessage}");
                        }
                    }
                }

                var bookings = await _context.Bookings
                                             .Include(b => b.Customer)
                                             .Include(b => b.Room)
                                             .ToListAsync();
                ViewData["BookingId"] = new SelectList(bookings.Select(b => new
                {
                    Id = b.Id,
                    Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
                }), "Id", "Display", invoice.BookingId);

                ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);
                ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
                return View(invoice);
            }

            // إذا كان النموذج صالحاً، تابع عملية الحفظ
            Debug.WriteLine($"Invoice TotalAmount before saving in Edit: {invoice.TotalAmount}");
            try
            {
                // 4. جلب الفاتورة الموجودة من قاعدة البيانات مع بنودها
                var existingInvoice = await _context.Invoices
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (existingInvoice == null)
                {
                    return NotFound();
                }

                // 5. تحديث خصائص الفاتورة الرئيسية
                _context.Entry(existingInvoice).CurrentValues.SetValues(invoice);

                // 6. التعامل مع بنود الفاتورة (إضافة، تعديل، حذف)
                var itemsToRemove = existingInvoice.InvoiceItems
                                                    .Where(existingItem => !invoice.InvoiceItems.Any(newItem => newItem.Id == existingItem.Id))
                                                    .ToList();
                _context.InvoiceItems.RemoveRange(itemsToRemove);

                foreach (var newItem in invoice.InvoiceItems)
                {
                    if (newItem.Id == 0) // هذا بند جديد (لم يكن له Id في DB)
                    {
                        newItem.InvoiceId = existingInvoice.Id; // ربط البند بالفاتورة الرئيسية
                        _context.InvoiceItems.Add(newItem); // إضافة البند الجديد
                    }
                    else // هذا بند موجود يحتاج للتحديث
                    {
                        var existingItem = existingInvoice.InvoiceItems.FirstOrDefault(i => i.Id == newItem.Id);
                        if (existingItem != null)
                        {
                            // تحديث خصائص البند الموجود
                            _context.Entry(existingItem).CurrentValues.SetValues(newItem);
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceExists(invoice.Id))
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

        // GET: Invoices/Delete/5
        // يعرض صفحة تأكيد حذف فاتورة
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Room)
                .Include(i => i.Customer)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Invoices/Delete/5
        // يقوم بحذف فاتورة مؤكدة
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.Invoices
                                        .Include(i => i.InvoiceItems) // تضمين البنود لحذفها مع الفاتورة
                                        .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Invoices/Print/5
        // يعرض الفاتورة بتنسيق مناسب للطباعة
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Room)
                .Include(i => i.Customer)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // دالة مساعدة للتحقق مما إذا كانت الفاتورة موجودة
        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.Id == id);
        }

        // دالة مساعدة لحساب تكلفة الحجز
        private decimal CalculateBookingCost(Booking booking)
        {
            // تأكد من أن الحجز والغرفة موجودان وأن تواريخ الدخول والخروج صالحة
            if (booking == null || booking.Room == null || booking.CheckInDate == default || booking.CheckOutDate == default)
            {
                return 0;
            }

            // حساب عدد الليالي
            int numberOfNights = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays;

            // تأكد من أن عدد الليالي ليس سالباً
            if (numberOfNights < 0)
            {
                numberOfNights = 0;
            }

            // سعر الحجز = سعر الليلة الواحدة * عدد الليالي
            return booking.Room.PricePerNight * numberOfNights;
        }
    }
}

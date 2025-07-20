using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics; // Required for Debug.WriteLine

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
        // يعرض قائمة بجميع الفواتير مع تضمين بيانات الحجز والعميل والغرفة المرتبطة.
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
        // يعرض تفاصيل فاتورة محددة مع جميع بياناتها المرتبطة.
        public async Task<IActionResult> Details(int? id)
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

        // GET: Invoices/Create
        // يعرض نموذج إنشاء فاتورة جديدة.
        public async Task<IActionResult> Create()
        {
            // جلب الحجوزات إلى الذاكرة أولاً قبل بناء النص الوصفي لتجنب خطأ "null propagating operator".
            var bookings = await _context.Bookings
                                         .Include(b => b.Customer)
                                         .Include(b => b.Room)
                                         .ToListAsync();

            // إعداد بيانات الحجوزات للـ JavaScript (لحساب الإجمالي من جانب العميل)
            var bookingsForJs = bookings.Select(b => new
            {
                b.Id,
                b.CheckInDate,
                b.CheckOutDate,
                RoomPricePerNight = b.Room?.PricePerNight ?? 0 // التعامل مع Room == null بأمان
            }).ToList();
            ViewBag.BookingsForJs = bookingsForJs; // تمريرها إلى ViewBag ليستهلكها الـ JS

            // Debugging: Log bookingsForJs content
            Debug.WriteLine("--- BookingsForJs (Create GET) ---");
            foreach (var b in bookingsForJs)
            {
                Debug.WriteLine($"BookingId: {b.Id}, CheckIn: {b.CheckInDate}, CheckOut: {b.CheckOutDate}, PricePerNight: {b.RoomPricePerNight}");
            }
            Debug.WriteLine("----------------------------------");

            // إنشاء SelectList بنص وصفي للحجوزات (العميل - الغرفة).
            ViewData["BookingId"] = new SelectList(bookings.Select(b => new
            {
                Id = b.Id,
                Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
            }), "Id", "Display");

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName");

            var services = _context.Services.Where(s => s.IsActive).ToList();
            ViewBag.Services = services; // توفير قائمة الخدمات النشطة

            // Debugging: Log services content
            Debug.WriteLine("--- Services (Create GET) ---");
            foreach (var s in services)
            {
                Debug.WriteLine($"ServiceId: {s.Id}, Name: {s.Name}, UnitPrice: {s.UnitPrice}");
            }
            Debug.WriteLine("-----------------------------");

            return View();
        }

        // POST: Invoices/Create
        // يستقبل بيانات الفاتورة الجديدة من النموذج ويقوم بحفظها.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InvoiceNumber,InvoiceDate,DueDate,TotalAmount,PaidAmount,Status,BookingId,CustomerId,Notes,InvoiceItems")] Invoice invoice)
        {
            // 1. توليد رقم الفاتورة التسلسلي تلقائياً إذا لم يكن موجوداً.
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

            // Debugging: Check raw form data for InvoiceItems (useful for model binding issues).
            Debug.WriteLine("--- Raw Form Data for InvoiceItems (Create POST) ---");
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("InvoiceItems["))
                {
                    Debug.WriteLine($"{key}: {Request.Form[key]}");
                }
            }
            Debug.WriteLine("------------------------------------");

            // 2. تعبئة سعر الوحدة (UnitPrice) لكل بند فاتورة من قاعدة البيانات.
            // ومعالجة البنود غير الصالحة أو الفارغة.
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                var processedInvoiceItems = new List<InvoiceItem>();
                var allServices = await _context.Services.ToListAsync(); // جلب جميع الخدمات مرة واحدة

                for (int i = 0; i < invoice.InvoiceItems.Count; i++)
                {
                    var item = invoice.InvoiceItems.ElementAt(i);

                    // Debugging: Log item details received
                    Debug.WriteLine($"Processing InvoiceItem[{i}]: ServiceId={item?.ServiceId}, Quantity={item?.Quantity}, UnitPrice={item?.UnitPrice}");

                    // تحقق إضافي: إذا كان البند نفسه null أو ServiceId غير صالح، تخطاه أو أضف خطأ.
                    if (item == null || item.ServiceId <= 0)
                    {
                        ModelState.AddModelError($"InvoiceItems[{i}].ServiceId", "الخدمة المختارة غير صالحة أو مفقودة لهذا البند.");
                        Debug.WriteLine($"  Error: ServiceId invalid or missing for item {i}. Skipping.");
                        continue; // Skip this item and continue the loop
                    }

                    var service = allServices.FirstOrDefault(s => s.Id == item.ServiceId);
                    if (service != null)
                    {
                        item.UnitPrice = service.UnitPrice; // تعيين سعر الوحدة من الخدمة
                        processedInvoiceItems.Add(item); // إضافة البند المعالج والصالح
                        Debug.WriteLine($"  Service found. UnitPrice set to: {item.UnitPrice}");
                    }
                    else
                    {
                        ModelState.AddModelError($"InvoiceItems[{i}].ServiceId", "الخدمة المختارة غير موجودة في قاعدة البيانات.");
                        Debug.WriteLine($"  Error: Service with ID {item.ServiceId} not found in DB.");
                    }
                }
                // الإصلاح الحاسم: استبدال المجموعة الأصلية بالمجموعة المعالجة.
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

            // حساب تكلفة الحجز.
            decimal bookingCost = 0;
            if (invoice.BookingId.HasValue)
            {
                var booking = await _context.Bookings
                                            .Include(b => b.Room) 
                                            .FirstOrDefaultAsync(b => b.Id == invoice.BookingId.Value);
                if (booking != null)
                {
                    bookingCost = CalculateBookingCost(booking); 
                    Debug.WriteLine($"Calculated Booking Cost (Server): {bookingCost}");
                }
            }

            decimal invoiceItemsSum = 0;
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                invoiceItemsSum = invoice.InvoiceItems.Sum(item => item.Quantity * item.UnitPrice);
            }
            invoice.TotalAmount = invoiceItemsSum + bookingCost;

            // 4. التحقق من صحة حالة النموذج.
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

                var bookings = await _context.Bookings
                                             .Include(b => b.Customer)
                                             .Include(b => b.Room)
                                             .ToListAsync();
                var bookingsForJs = bookings.Select(b => new
                {
                    b.Id,
                    b.CheckInDate,
                    b.CheckOutDate,
                    RoomPricePerNight = b.Room?.PricePerNight ?? 0
                }).ToList();
                ViewBag.BookingsForJs = bookingsForJs;

                ViewData["BookingId"] = new SelectList(bookings.Select(b => new
                {
                    Id = b.Id,
                    Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
                }), "Id", "Display", invoice.BookingId);

                ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);
                ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
                return View(invoice);
            }

            // إذا كان النموذج صالحاً، تابع عملية الحفظ.
            Debug.WriteLine($"Invoice TotalAmount before saving in Create: {invoice.TotalAmount}");
            _context.Add(invoice);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Invoices/Edit/5
        // يعرض نموذج تعديل فاتورة موجودة.
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

            // إعداد بيانات الحجوزات للـ JavaScript (لحساب الإجمالي من جانب العميل)
            var bookingsForJs = bookings.Select(b => new
            {
                b.Id,
                b.CheckInDate,
                b.CheckOutDate,
                RoomPricePerNight = b.Room?.PricePerNight ?? 0
            }).ToList();
            ViewBag.BookingsForJs = bookingsForJs;

            // Debugging: Log bookingsForJs content
            Debug.WriteLine("--- BookingsForJs (Edit GET) ---");
            foreach (var b in bookingsForJs)
            {
                Debug.WriteLine($"BookingId: {b.Id}, CheckIn: {b.CheckInDate}, CheckOut: {b.CheckOutDate}, PricePerNight: {b.RoomPricePerNight}");
            }
            Debug.WriteLine("--------------------------------");

            ViewData["BookingId"] = new SelectList(bookings.Select(b => new
            {
                Id = b.Id,
                Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
            }), "Id", "Display", invoice.BookingId);

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);

            var services = _context.Services.Where(s => s.IsActive).ToList();
            ViewBag.Services = services; // توفير قائمة الخدمات النشطة

            // Debugging: Log services content
            Debug.WriteLine("--- Services (Edit GET) ---");
            foreach (var s in services)
            {
                Debug.WriteLine($"ServiceId: {s.Id}, Name: {s.Name}, UnitPrice: {s.UnitPrice}");
            }
            Debug.WriteLine("---------------------------");

            return View(invoice);
        }

        // POST: Invoices/Edit/5
        // يستقبل بيانات الفاتورة المعدلة من النموذج ويقوم بحفظها.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InvoiceNumber,InvoiceDate,DueDate,TotalAmount,PaidAmount,Status,BookingId,CustomerId,Notes,InvoiceItems")] Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return NotFound();
            }

            // Debugging: Check raw form data for InvoiceItems (useful for model binding issues).
            Debug.WriteLine("--- Raw Form Data for InvoiceItems (Edit POST) ---");
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("InvoiceItems["))
                {
                    Debug.WriteLine($"{key}: {Request.Form[key]}");
                }
            }
            Debug.WriteLine("------------------------------------");

            // 1. تعبئة سعر الوحدة (UnitPrice) لكل بند فاتورة من قاعدة البيانات.
            // ومعالجة البنود غير الصالحة أو الفارغة.
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                var processedInvoiceItems = new List<InvoiceItem>();
                var allServices = await _context.Services.ToListAsync(); // جلب جميع الخدمات مرة واحدة

                for (int i = 0; i < invoice.InvoiceItems.Count; i++)
                {
                    var item = invoice.InvoiceItems.ElementAt(i);

                    // Debugging: Log item details received
                    Debug.WriteLine($"Processing InvoiceItem[{i}]: ServiceId={item?.ServiceId}, Quantity={item?.Quantity}, UnitPrice={item?.UnitPrice}");

                    // تحقق إضافي: إذا كان البند نفسه null أو ServiceId غير صالح، تخطاه أو أضف خطأ.
                    if (item == null || item.ServiceId <= 0)
                    {
                        ModelState.AddModelError($"InvoiceItems[{i}].ServiceId", "الخدمة المختارة غير صالحة أو مفقودة لهذا البند.");
                        Debug.WriteLine($"  Error: ServiceId invalid or missing for item {i}. Skipping.");
                        continue; // Skip this item and continue the loop
                    }

                    var service = allServices.FirstOrDefault(s => s.Id == item.ServiceId);
                    if (service != null)
                    {
                        item.UnitPrice = service.UnitPrice;
                        processedInvoiceItems.Add(item);
                        Debug.WriteLine($"  Service found. UnitPrice set to: {item.UnitPrice}");
                    }
                    else
                    {
                        ModelState.AddModelError($"InvoiceItems[{i}].ServiceId", "الخدمة المختارة غير موجودة في قاعدة البيانات.");
                        Debug.WriteLine($"  Error: Service with ID {item.ServiceId} not found in DB.");
                    }
                }
                // الإصلاح الحاسم: استبدال المجموعة الأصلية بالمجموعة المعالجة.
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

            // حساب تكلفة الحجز.
            decimal bookingCost = 0;
            if (invoice.BookingId.HasValue)
            {
                var booking = await _context.Bookings
                                            .Include(b => b.Room) // تأكد من تضمين الغرفة لجلب سعر الليلة
                                            .FirstOrDefaultAsync(b => b.Id == invoice.BookingId.Value);
                if (booking != null)
                {
                    bookingCost = CalculateBookingCost(booking); // استخدام الدالة المساعدة
                    Debug.WriteLine($"Calculated Booking Cost (Server): {bookingCost}");
                }
            }

            // 2. إعادة حساب TotalAmount بناءً على بنود الفاتورة المستلمة + تكلفة الحجز.
            decimal invoiceItemsSum = 0;
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                invoiceItemsSum = invoice.InvoiceItems.Sum(item => item.Quantity * item.UnitPrice);
            }
            invoice.TotalAmount = invoiceItemsSum + bookingCost;

            // 3. التحقق من صحة حالة النموذج.
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

                // 7. إذا كان النموذج غير صالح، أعد تعبئة SelectLists و ViewBag.Services.
                // إعادة تعبئة بيانات الحجوزات للـ JavaScript عند فشل التحقق.
                var bookings = await _context.Bookings
                                             .Include(b => b.Customer)
                                             .Include(b => b.Room)
                                             .ToListAsync();
                var bookingsForJs = bookings.Select(b => new
                {
                    b.Id,
                    b.CheckInDate,
                    b.CheckOutDate,
                    RoomPricePerNight = b.Room?.PricePerNight ?? 0
                }).ToList();
                ViewBag.BookingsForJs = bookingsForJs;

                ViewData["BookingId"] = new SelectList(bookings.Select(b => new
                {
                    Id = b.Id,
                    Display = $"العميل: {b.Customer?.FullName ?? "غير معروف"} - الغرفة: {b.Room?.RoomNumber ?? "غير معروف"}"
                }), "Id", "Display", invoice.BookingId);

                ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", invoice.CustomerId);
                ViewBag.Services = _context.Services.Where(s => s.IsActive).ToList();
                return View(invoice);
            }

            // إذا كان النموذج صالحاً، تابع عملية الحفظ.
            Debug.WriteLine($"Invoice TotalAmount before saving in Edit: {invoice.TotalAmount}");
            try
            {
                // 4. جلب الفاتورة الموجودة من قاعدة البيانات مع بنودها.
                var existingInvoice = await _context.Invoices
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (existingInvoice == null)
                {
                    return NotFound();
                }

                // 5. تحديث خصائص الفاتورة الرئيسية.
                _context.Entry(existingInvoice).CurrentValues.SetValues(invoice);

                // 6. التعامل مع بنود الفاتورة (إضافة، تعديل، حذف).
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
        // يعرض صفحة تأكيد حذف فاتورة.
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
        // يقوم بحذف فاتورة مؤكدة.
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
        // يعرض الفاتورة بتنسيق مناسب للطباعة.
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

        // دالة مساعدة للتحقق مما إذا كانت الفاتورة موجودة.
        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.Id == id);
        }

        // دالة مساعدة لحساب تكلفة الحجز.
        private decimal CalculateBookingCost(Booking booking)
        {
            // تأكد من أن الحجز والغرفة موجودان وأن تواريخ الدخول والخروج صالحة.
            if (booking == null || booking.Room == null || booking.CheckInDate == default || booking.CheckOutDate == default)
            {
                return 0;
            }
            int numberOfNights = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays;

            
            if (numberOfNights < 0)
            {
                numberOfNights = 0;
            }
            return booking.Room.PricePerNight * numberOfNights;
        }
    }
}

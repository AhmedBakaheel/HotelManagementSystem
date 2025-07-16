using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;
using HotelManagementSystem.Enums;

namespace HotelManagementSystem.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Payments.Include(p => p.Invoice);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        public IActionResult Create()
        {
            ViewData["InvoiceId"] = new SelectList(_context.Invoices.Select(i => new
            {
                Id = i.Id,
                Display = $"فاتورة #{i.InvoiceNumber} - الإجمالي: {i.TotalAmount:N2} - العميل: {i.Customer.FullName ?? "غير معروف"}" 
            }), "Id", "Display");
            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InvoiceId,Amount,PaymentDate,Method,Notes")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();

                
                await UpdateInvoicePaidAmountAndStatus(payment.InvoiceId);

                return RedirectToAction(nameof(Index));
            }
            ViewData["InvoiceId"] = new SelectList(_context.Invoices.Select(i => new
            {
                Id = i.Id,
                Display = $"فاتورة #{i.InvoiceNumber} - الإجمالي: {i.TotalAmount:N2} - العميل: {i.Customer.FullName ?? "غير معروف"}"
            }), "Id", "Display", payment.InvoiceId);
            return View(payment);
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            
            ViewData["InvoiceId"] = new SelectList(_context.Invoices.Select(i => new
            {
                Id = i.Id,
                Display = $"فاتورة #{i.InvoiceNumber} - الإجمالي: {i.TotalAmount:N2} - العميل: {i.Customer.FullName ?? "غير معروف"}"
            }), "Id", "Display", payment.InvoiceId);
            return View(payment);
        }

        // POST: Payments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InvoiceId,Amount,PaymentDate,Method,Notes")] Payment payment)
        {
            if (id != payment.Id)
            {
                return NotFound();
            }

            // جلب الدفعة القديمة لتحديد InvoiceId القديم (إذا تغيرت الفاتورة المرتبطة)
            var oldPayment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            int? oldInvoiceId = oldPayment?.InvoiceId; // حفظ الـ InvoiceId القديم

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();

                   
                    await UpdateInvoicePaidAmountAndStatus(payment.InvoiceId);

                   
                    if (oldInvoiceId.HasValue && oldInvoiceId.Value != payment.InvoiceId)
                    {
                        await UpdateInvoicePaidAmountAndStatus(oldInvoiceId.Value);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.Id))
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
            ViewData["InvoiceId"] = new SelectList(_context.Invoices.Select(i => new
            {
                Id = i.Id,
                Display = $"فاتورة #{i.InvoiceNumber} - الإجمالي: {i.TotalAmount:N2} - العميل: {i.Customer.FullName ?? "غير معروف"}"
            }), "Id", "Display", payment.InvoiceId);
            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            int? invoiceIdToUpdate = payment?.InvoiceId; // حفظ الـ InvoiceId قبل حذف الدفعة

            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();

           
            if (invoiceIdToUpdate.HasValue)
            {
                await UpdateInvoicePaidAmountAndStatus(invoiceIdToUpdate.Value);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }
        private async Task UpdateInvoicePaidAmountAndStatus(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice != null)
            {
               
                invoice.PaidAmount = await _context.Payments
                                                   .Where(p => p.InvoiceId == invoiceId)
                                                   .SumAsync(p => p.Amount);

              
                if (invoice.PaidAmount == 0)
                {
                    invoice.Status = InvoiceStatus.Unpaid;
                }
                else if (invoice.PaidAmount >= invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.Paid;
                }
                else
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }

                _context.Update(invoice);
                await _context.SaveChangesAsync(); 
            }
        }
    }
}

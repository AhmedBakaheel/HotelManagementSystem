using Microsoft.AspNetCore.Mvc;
using HotelManagementSystem.Data; 
using HotelManagementSystem.Models; 
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using HotelManagementSystem.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelManagementSystem.Extensions;

namespace HotelManagementSystem.Controllers
{
    //[Authorize(Roles = "Receptionist")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Rooms
        public async Task<IActionResult> Index()
        {
            return View(await _context.Rooms.ToListAsync());
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id); 
            if (room == null)
            {
                return NotFound(); 
            }

            return View(room); 
        }

        // GET: Rooms/Create
        public IActionResult Create()
        {
            var room = new Room();
            room.RoomTypeOptions = Enum.GetValues(typeof(RoomType))
                             .Cast<RoomType>()
                             .Select(e => new SelectListItem
                             {
                                 Value = e.ToString(),
                                 Text = e.GetDisplayName() 
                             });
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Create([Bind("Id,RoomNumber,RoomType,PricePerNight,IsAvailable,Description")] Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }            
            room.RoomTypeOptions = Enum.GetValues(typeof(RoomType))
                                       .Cast<RoomType>()
                                       .Select(e => new SelectListItem
                                       {
                                           Value = e.ToString(),
                                           Text = e.GetDisplayName()
                                       });
            return View(room);
        }

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            // تهيئة RoomTypeOptions هنا أيضاً لـ Edit View
            room.RoomTypeOptions = Enum.GetValues(typeof(RoomType))
                                       .Cast<RoomType>()
                                       .Select(e => new SelectListItem
                                       {
                                           Value = e.ToString(),
                                           Text = e.GetDisplayName(),
                                           Selected = e == room.RoomType 
                                       });

            return View(room);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RoomNumber,RoomType,PricePerNight,IsAvailable,Description")] Room room)
        {
            if (id != room.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.Id))
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
            room.RoomTypeOptions = Enum.GetValues(typeof(RoomType))
                                       .Cast<RoomType>()
                                       .Select(e => new SelectListItem
                                       {
                                           Value = e.ToString(),
                                           Text = e.GetDisplayName(),
                                           Selected = e == room.RoomType
                                       });
            return View(room);
        }



        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room); 
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room); 
                await _context.SaveChangesAsync(); 
            }

            return RedirectToAction(nameof(Index));
        }
        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id); 
        }
    }
}

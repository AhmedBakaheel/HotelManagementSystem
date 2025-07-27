using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Data;
using HotelManagementSystem.Models;
using HotelManagementSystem.Enums; 
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System;

namespace HotelManagementSystem.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public RoomsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
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
            return View();
        }

        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RoomType,PricePerNight,Description,MaxGuests,IsActive")] Room room, IFormFile RoomImageFile)
        {
            // توليد رقم الغرفة تلقائياً
            var lastRoom = await _context.Rooms
                                         .OrderByDescending(r => r.Id)
                                         .FirstOrDefaultAsync();

            int nextRoomNumber = 1;
            if (lastRoom != null && int.TryParse(lastRoom.RoomNumber, out int lastNum))
            {
                nextRoomNumber = lastNum + 1;
            }
            room.RoomNumber = nextRoomNumber.ToString();

            if (RoomImageFile != null && RoomImageFile.Length > 0)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string uploadsFolder = Path.Combine(wwwRootPath, "images", "rooms");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = Path.GetFileNameWithoutExtension(RoomImageFile.FileName);
                string extension = Path.GetExtension(RoomImageFile.FileName);
                string uniqueFileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await RoomImageFile.CopyToAsync(fileStream);
                }
                room.ImageUrl = Path.Combine("/images/rooms/", uniqueFileName).Replace("\\", "/");
            }
            else
            {
                room.ImageUrl = null;
            }

            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
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
            return View(room);
        }

        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RoomType,PricePerNight,Description,MaxGuests,IsActive")] Room room, IFormFile RoomImageFile)
        {
            if (id != room.Id)
            {
                return NotFound();
            }

            var existingRoom = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (existingRoom == null)
            {
                return NotFound();
            }
            room.RoomNumber = existingRoom.RoomNumber;

            if (RoomImageFile != null && RoomImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingRoom.ImageUrl))
                {
                    string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingRoom.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string wwwRootPath = _hostEnvironment.WebRootPath;
                string uploadsFolder = Path.Combine(wwwRootPath, "images", "rooms");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = Path.GetFileNameWithoutExtension(RoomImageFile.FileName);
                string extension = Path.GetExtension(RoomImageFile.FileName);
                string uniqueFileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await RoomImageFile.CopyToAsync(fileStream);
                }
                room.ImageUrl = Path.Combine("/images/rooms/", uniqueFileName).Replace("\\", "/");
            }
            else
            {
                room.ImageUrl = existingRoom.ImageUrl;
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
                if (!string.IsNullOrEmpty(room.ImageUrl))
                {
                    string imagePath = Path.Combine(_hostEnvironment.WebRootPath, room.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _context.Rooms.Remove(room);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}

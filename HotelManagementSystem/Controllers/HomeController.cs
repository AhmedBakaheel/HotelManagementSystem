using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HotelManagementSystem.Models;
using HotelManagementSystem.Data; 
using Microsoft.EntityFrameworkCore; 
using Microsoft.AspNetCore.Authorization;
using HotelManagementSystem.Enums;

namespace HotelManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context) 
    {
        _logger = logger;
        _context = context; 
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    // GET: /Home/Dashboard
    [Authorize(Roles = "Admin,Receptionist")] 
    public async Task<IActionResult> Dashboard()
    {
        
        var totalRooms = await _context.Rooms.CountAsync();
        var availableRooms = await _context.Rooms.Where(r => r.IsAvailable).CountAsync();
        var totalCustomers = await _context.Customers.CountAsync();

       
        var activeBookings = await _context.Bookings
            .Include(b => b.Room) 
            .Include(b => b.Customer) 
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending)
            .Where(b => b.CheckOutDate >= DateTime.Today) 
            .OrderBy(b => b.CheckInDate) 
            .Take(5) 
            .ToListAsync();

       
        ViewBag.TotalRooms = totalRooms;
        ViewBag.AvailableRooms = availableRooms;
        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.ActiveBookings = activeBookings;

       
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

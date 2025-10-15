using BusTrips.Domain.Entities;
using BusTrips.Web.Hubs;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BusTrips.Web.Controllers;

public class HomeController : Controller
{
    private readonly ITripService _tripService;
    private readonly IAccountService _accountService;
    private readonly INotificationService _notificationService;

    public HomeController(ITripService tripService, IAccountService accountService, INotificationService notificationService)
    {
        _tripService = tripService;
        _accountService = accountService;
        _notificationService = notificationService;
    }
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(TripFilterVM filter) // Dashboard with trip listings and filters
    {
        // default pagination setup
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var result = await _tripService.GetFilteredTripsAsync(filter, CurrentUserId); // Fetch filtered trips

        // send filter back to view for keeping selected values
        ViewBag.Filter = filter;

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> LoadTripsPartial(TripFilterVM filter) // Load trips table partial view with filters
    {
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var result = await _tripService.GetFilteredTripsAsync(filter, CurrentUserId); // Fetch filtered trips
        return PartialView("_TripsTablePartial", result);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> TripDetails(Guid tripId) // Load trip details partial view
    {
        var result = await _tripService.GetTripsDetailsAsync(tripId); // Fetch trip details
        ViewData["ActiveMenu"] = "Dashboard";

        return PartialView("~/Views/Home/_TripDetails.cshtml", result.Data);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> IndexAdmin() // Admin dashboard view
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> GetAllTrips() // Fetch all trips as JSON
    {
        var trips = await _tripService.GetAllTripsAsync(); // Fetch all trips
        return Json(trips);
    }

    [HttpPost("create-or-update-trip")]
    public async Task<IActionResult> CreateOrUpdateTrip([FromBody] CreateTripVm vm) // Create or update trip based on presence of Id
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        ResponseVM<string> result;

        if (vm.Id.HasValue)
        {
            result = await _tripService.UpdateTripAsync(vm.Id.Value, vm, CurrentUserId); // Update existing trip
        }
        else
        {
            result = await _tripService.CreateTripAsync(vm, CurrentUserId); // Create new trip
        }

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }

    public IActionResult FirstAccessPage() => View(); // Informational page for first-time access

    public IActionResult NotReady() // Informational page for features not yet available
    {
        ViewBag.Note = "ETA: Rolling out to all users by next week.";
        return View();
    }

    public IActionResult RenderAlert(bool isSuccess, string message) // Render alert partial view
    {
        var vm = new AlertViewModel
        {
            IsSuccess = isSuccess,
            Message = message
        };
        return PartialView("~/Views/Shared/_Alert.cshtml", vm);
    }

    [HttpGet]
    public IActionResult ContactUs() // Contact Us form view
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SubmitContactUs(ContactUsVM model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return Json(new { isSuccess = false, errors });
        }

        var res = await _accountService.ContactUsAsync(model);

        if (res.IsSuccess)
        {
            // Send notification to all admins Group("Users")
            //await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            //{
            //    Title = "New Contact Message",
            //    Message = $"{model.Name} sent a message: {model.Subject}",
            //    Date = DateTime.Now.ToString("g")
            //});

            await _notificationService.SaveAndSendNotification("New Contact Email",$"{model.Name} sent a email for   {model.Subject}", model.Message, null, "Admin");
        }

        return Json(new { isSuccess = res.IsSuccess, message = res.Message });
    }

    [HttpGet]
    public IActionResult Services() // Services view
    {
        return View();
    }

    [HttpGet]
    public IActionResult Help() // Help view
    {
        return View();
    }
}

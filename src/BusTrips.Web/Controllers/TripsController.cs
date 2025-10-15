using Azure;
using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Identity;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using BusTrips.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


[Authorize(Roles = AppRoles.User)]
public class TripsController : Controller
{
    private readonly ITripService _tripService;
    private readonly IOrganizationService _orgService;
    private readonly INotificationService _notificationService;
    public TripsController(ITripService tripService, IOrganizationService orgService, INotificationService notificationService)
    {
        _tripService = tripService;
        _orgService = orgService;
        _notificationService = notificationService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(Guid groupId, string? bucket = null) // Upcoming, InProgress, Past, All
    {
        // Pass TempData to ViewBag if you want
        ViewBag.IsSuccess = TempData["IsSuccess"];
        ViewBag.Message = TempData["Message"];
        ViewData["ActiveMenu"] = "Organizations";
        var list = await _tripService.GetTripsAsync(groupId, CurrentUserId, bucket);
        return View(list.Data);
    }

    [HttpGet]
    public async Task<IActionResult> CreateTrip(Guid groupId, string? controller) // Pass groupId to associate trip with a group
    {
        // Return view with model
        //return View(new CreateTripVm { groupId = groupId });
        ViewData["ActiveMenu"] = "Organizations";
        return View(new CreateTripVm { groupId = groupId, controller = controller });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrip(CreateTripVm vm) // Handle trip creation with validation
    {
        if (vm.NumberOfPassengers == null)
            ModelState.AddModelError("NumberOfPassengers", "Number of passengers is required.");
        if (vm.NumberOfPassengers == 0)
            ModelState.AddModelError("NumberOfPassengers", "Number of passengers must be greater than 0.");

        // Validate that destination arrival is after departure
        DateTime departureDateTime = vm.DepartureDate.ToDateTime(vm.DepartureTime);
        DateTime arrivalDateTime = vm.DestinationArrivalDate.ToDateTime(vm.DestinationArrivalTime);

        if (departureDateTime < DateTime.Now)
            ModelState.AddModelError(nameof(vm.DepartureDate), "Departure date/time must be future date/time.");

        if (arrivalDateTime <= departureDateTime)
            ModelState.AddModelError(nameof(vm.DestinationArrivalDate), "Arrival date/time must be later than departure date/time.");

        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }
            return PartialView("~/Views/Trips/_TripFormPartial.cshtml", vm);
        }

        var response = await _tripService.CreateTripAsync(vm, CurrentUserId); // Create trip via service

        if (response.IsSuccess)
        {
            var message = $"Trip of {vm.DestinationCity}, {vm.DestinationLocation} to {vm.DestinationLocation}, {vm.DestinationCity} created by {User.Identity.Name}";

            var getOrg = await _orgService.GetOrganizationDetailsAsync(Guid.Parse(response.Data));

            var fullMessage = $"{message}. Departure: {departureDateTime:g} Arrival: {arrivalDateTime:g} Passengers: {vm.NumberOfPassengers}, Organization Name is {getOrg.Data.OrgName}.";

            await _notificationService.SaveAndSendNotification("New Trip Created", message, fullMessage, null, "Admin");  // Notification for Admin
            await _notificationService.SaveAndSendNotification("New Trip Created", message, fullMessage, null, "Driver"); // Notification for Driver
        }

        return Json(new
        {
            success = response.IsSuccess,
            message = response.Message,
            redirectUrl = Url.Action(nameof(Index), new { groupId = vm.groupId })
        });
    }

    [HttpGet]
    public async Task<IActionResult> TripDetails(Guid tripId) // Load trip details partial view
    {
        var result = await _tripService.GetTripsDetailsAsync(tripId); // Fetch trip details 

        return PartialView("~/Views/Home/_TripDetails.cshtml", result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> SubmitForQuote(Guid id, Guid groupId) // Submit trip for quote
    {
        var response = await _tripService.SubmitForQuoteAsync(id); // Submit for quote via service
        TempData["IsSuccess"] = response.IsSuccess;
        TempData["Message"] = response.Message;
        return RedirectToAction(nameof(Index), new { groupId = groupId });
    }

    [HttpPost]
    public async Task<IActionResult> Activate(Guid id) // Activate trip after quote approval 
    {
        var response = await _tripService.ActivateTripAsync(id); // Activate trip via service
        TempData["IsSuccess"] = response.IsSuccess;
        TempData["Message"] = response.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, Guid groupId, string? controller) // Edit trip view with existing data
    {
        if (id == Guid.Empty)
        {
            // OrganizationId is invalid, redirect to Index (or show error)
            return View("Index", new { groupId = groupId });
        }

        var vm = await _tripService.GetTripAsync(id); // Fetch trip data for editing
        if (vm is null) return View("Index", new { groupId = groupId });

        ViewData["ActiveMenu"] = "Organizations";
        ViewBag.IsEdit = true;
        vm.controller = controller;
        return View("CreateTrip", vm);
    }

    [HttpPost]
    public async Task<IActionResult> EditTrip(Guid id, CreateTripVm vm) // Handle trip update with validation 
    {
        ViewData["ActiveMenu"] = "Organizations";
        ViewBag.IsEdit = true;

        if (vm.NumberOfPassengers == null)
            ModelState.AddModelError("NumberOfPassengers", "Number of passengers is required.");
        if (vm.NumberOfPassengers == 0)
            ModelState.AddModelError("NumberOfPassengers", "Number of passengers must be greater than 0.");

        // Validate that destination arrival is after departure
        DateTime departureDateTime = vm.DepartureDate.ToDateTime(vm.DepartureTime);
        DateTime arrivalDateTime = vm.DestinationArrivalDate.ToDateTime(vm.DestinationArrivalTime);

        if (departureDateTime < DateTime.Now)
            ModelState.AddModelError(nameof(vm.DepartureDate), "Departure date/time must be future date/time.");

        if (arrivalDateTime <= departureDateTime)
            ModelState.AddModelError(nameof(vm.DestinationArrivalDate), "Arrival date/time must be later than departure date/time.");

        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(new { success = false, errors });
            }
            return PartialView("~/Views/Trips/_TripFormPartial.cshtml", vm);
        }


        var response = await _tripService.UpdateTripAsync(id, vm, CurrentUserId); // Update trip via service


        if (response.IsSuccess)
        {
            var message = $"Trip of {vm.DestinationCity}, {vm.DestinationLocation} to {vm.DestinationLocation}, {vm.DestinationCity} created by {User.Identity.Name}";

            var getOrg = await _orgService.GetOrganizationDetailsAsync(vm.OrganizationId.Value);

            var fullMessage = $"Trip Name : {vm.TripName}, {message}. Departure: {departureDateTime:g} Arrival: {arrivalDateTime:g} Passengers: {vm.NumberOfPassengers}, Organization Name is {getOrg.Data.OrgName}.";

            await _notificationService.SaveAndSendNotification("Updated A Trip", message, fullMessage, null, "Admin"); // Notification for Admin
            await _notificationService.SaveAndSendNotification("Updated A Trip", message, fullMessage, null, "Driver"); // Notification for Driver
        }

        return Json(new
        {
            success = response.IsSuccess,
            message = response.Message,
            redirectUrl = Url.Action(nameof(Index), new { groupId = vm.groupId })
        });
    }

    [HttpGet]
    public async Task<IActionResult> CancelTrip(Guid id, Guid groupId) // Cancel trip action
    {
        var response = await _tripService.CancelTripAsync(id); // Cancel trip via service
        TempData["IsSuccess"] = response.IsSuccess;
        TempData["Message"] = response.Message;
        return RedirectToAction(nameof(Index), new { groupId = groupId });
    }

    [HttpGet]
    public async Task<IActionResult> GetTripForCopyTrip(Guid id, Guid groupId) // Load trip data for copying
    {
        if (id == Guid.Empty)
        {
            // OrganizationId is invalid, redirect to Index (or show error)
            return View("Index", new { groupId = groupId });
        }

        var vm = await _tripService.GetTripForCopyAsync(id); // Fetch trip data for copying
        if (vm is null) return View("Index", new { groupId = groupId });
        ViewData["Title"] = "Create Trip";
        ViewData["ActiveMenu"] = "Organizations";

        vm.groupId = groupId;
        vm.controller = "Trips";
        return View("CreateTrip", vm);
    }

    [HttpGet]
    public async Task<IActionResult> CopyTrip(Guid id, Guid groupId) // Copy trip action 
    {
        var response = await _tripService.CopyTripAsync(id, CurrentUserId); // Copy trip via service
        TempData["IsSuccess"] = response.IsSuccess;
        TempData["Message"] = response.Message;
        return RedirectToAction(nameof(Index), new { groupId = groupId });
    }
}

using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Identity;
using BusTrips.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BusTrips.Web.Controllers;

[Authorize(Roles = AppRoles.Driver + "," + AppRoles.Admin)]
public class DriverController : Controller
{
    private readonly AppDbContext _db;
    public DriverController(AppDbContext db) { _db = db; }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> MyTrips(string? bucket = null) // Upcoming, InProgress, Past
    {
        var now = DateTimeOffset.UtcNow;

        var q = _db.Trips
            .Include(t => t.BusAssignments)
            .Where(t => t.BusAssignments.Any(a => a.DriverId == CurrentUserId && a.Status == BusTrips.Domain.Entities.TripBusAssignmentStatus.Assigned));

        if (!string.IsNullOrEmpty(bucket))
        {
            q = q.AsEnumerable().Where(t =>
            {
                var departure = t.DepartureDate.ToDateTime(t.DepartureTime, DateTimeKind.Utc);
                var arrival = t.DestinationArrivalDate.ToDateTime(t.DestinationArrivalTime, DateTimeKind.Utc);

                return bucket.Equals("Upcoming", StringComparison.OrdinalIgnoreCase)
                    ? t.Status == BusTrips.Domain.Entities.TripStatus.Live && departure > now
                    : bucket.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                        ? departure <= now && arrival >= now
                        : bucket.Equals("Past", StringComparison.OrdinalIgnoreCase)
                            ? arrival < now
                            : true;
            }).AsQueryable();
        }

        var items = q.OrderByDescending(x => x.CreatedAt).Select(t => new { t.Id, Status = t.Status.ToString() }).ToList();
        return View(items);
    }

    public async Task<IActionResult> Unassigned() // Trips available for assignment
    {
        var items = await _db.TripBusAssignments
          .Include(t => t.Trip)
          .Where(t => t.Trip.Status == BusTrips.Domain.Entities.TripStatus.Live && (!t.Trip.BusAssignments.Any() || t.Trip.BusAssignments.Any(a => (a.DriverId == null || a.DriverId == CurrentUserId)))).OrderByDescending(x => x.CreatedAt)
          .Select(t => new { Id = t.Trip.Id, Assignments = t.Trip.BusAssignments.Count, Status = t.Status })
          .ToListAsync();
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> RequestAssignment(Guid tripId) //  Request assignment to a trip
    {
        var trip = await _db.TripBusAssignments.Include(t => t.Trip).FirstOrDefaultAsync(t => t.TripId == tripId);
        if (trip != null && trip.DriverId == CurrentUserId)
        {
            return RedirectToAction(nameof(Unassigned));
        }
        if (trip is null)
        {
            trip = new BusTrips.Domain.Entities.TripBusAssignment
            {
                TripId = trip.Trip.Id,
                //EquipmentId = Guid.Empty, 
                Status = BusTrips.Domain.Entities.TripBusAssignmentStatus.RequestedByDriver
            };
            _db.TripBusAssignments.Update(trip);
        }
            trip.DriverId = CurrentUserId;
        trip.Status = BusTrips.Domain.Entities.TripBusAssignmentStatus.RequestedByDriver;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(MyTrips));
    }
}

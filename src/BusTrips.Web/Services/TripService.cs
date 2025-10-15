using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BusTrips.Web.Services
{
    public class TripService : ITripService
    {
        private readonly AppDbContext _db;
        private readonly IOrganizationPermissionService _permissionService;
        public TripService(AppDbContext db, IOrganizationPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        // Get all trips with related details
        public async Task<dynamic> GetAllTripsAsync()
        {
            var allTrips = await (from t in _db.Trips
                                  join tb in _db.TripBusAssignments.Include(x => x.Driver).Include(x => x.Equipment)
                                      on t.Id equals tb.TripId into tba
                                  from tb in tba.DefaultIfEmpty()
                                  where !t.IsDeleted
                                  orderby t.CreatedAt descending
                                  select new TripListVm
                                  {
                                      Id = t.Id,
                                      TripName = t.TripName,
                                      GroupId = t.GroupId,
                                      GroupName = t.Group != null ? t.Group.GroupName : string.Empty,
                                      OrganizationName = t.Organization != null ? t.Organization.OrgName : string.Empty,
                                      OrganizationCreatorName = _db.OrganizationMemberships
                                          .Where(x => x.OrganizationId == t.OrganizationId && x.MemberType == MemberTypeEnum.Creator && !x.IsDeleted)
                                          .Select(x => _db.Users.Where(u => u.Id == x.AppUserId)
                                                               .Select(u => u.FirstName + " " + u.LastName)
                                                               .FirstOrDefault())
                                          .FirstOrDefault(),
                                      Status = t.Status.ToString(),
                                      DepartureDate = t.DepartureDate,
                                      DepartureTime = t.DepartureTime,
                                      StartLocation = t.StartCity + " " + t.StartLocation,
                                      DestinationArrivalDate = t.DestinationArrivalDate,
                                      DestinationArrivalTime = t.DestinationArrivalTime,
                                      DestinationLocation = t.DestinationCity + " " + t.DestinationLocation,
                                      Driver = tb != null && tb.Driver != null
                                          ? _db.BusDrivers
                                                .Where(d => d.AppUserId == tb.Driver.Id)
                                                .Select(d => new DriverDetailsVM
                                                {
                                                    DriverId = d.AppUserId.ToString(),
                                                    DriverName = tb.Driver.FirstName + " " + tb.Driver.LastName,
                                                    EmploymentType = d.EmploymentType,
                                                    LicenseNumber = d.LicenseNumber,
                                                    LicenseProvince = d.LicenseProvince
                                                })
                                                .FirstOrDefault()
                                          : null,
                                      Equipment = tb != null && tb.Equipment != null
                                          ? new EquipmentDetailsVM
                                          {
                                              Manufacturer = tb.Equipment.Manufacturer,
                                              Model = tb.Equipment.Model,
                                              LicensePlate = tb.Equipment.LicensePlate,
                                              SeatingCapacity = tb.Equipment.SeatingCapacity.ToString(),
                                              Year = tb.Equipment.Year.ToString()
                                          }
                                          : null,
                                      Passengers = t.NumberOfPassengers,
                                      TripDays = t.TripDays,
                                      Notes = t.Notes
                                  })
                      .ToListAsync();



            var today = DateOnly.FromDateTime(DateTime.Now);

            var upcoming = allTrips
                .Where(x => x.DepartureDate >= today)
                .OrderBy(x => x.DepartureDate)
                .ToList();

            return new { all = allTrips, upcoming = upcoming };
        }

        // Get trips by groupId with optional status filtering (bucket)
        public async Task<ResponseVM<GroupDetailsResponseVM>> GetTripsAsync(Guid groupId, Guid userId, string? bucket)
        {
            var now = DateTimeOffset.UtcNow;
            var q = _db.Trips.Where(t => t.GroupId == groupId && !t.IsDeleted);

            if (!string.IsNullOrEmpty(bucket))
            {
                q = q.AsEnumerable().Where(t =>
                {
                    var departure = t.DepartureDate.ToDateTime(t.DepartureTime, DateTimeKind.Utc);
                    var arrival = t.DestinationArrivalDate.ToDateTime(t.DestinationArrivalTime, DateTimeKind.Utc);

                    return bucket.Equals("Draft", StringComparison.OrdinalIgnoreCase)
                        ? (t.Status == TripStatus.Draft)
                        : bucket.Equals("Quoted", StringComparison.OrdinalIgnoreCase)
                            ? (t.Status == TripStatus.Quoted)
                        : bucket.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                            ? (t.Status == TripStatus.Rejected)
                        : bucket.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                            ? (t.Status == TripStatus.Approved)
                        : bucket.Equals("Upcoming", StringComparison.OrdinalIgnoreCase)
                            ? (t.Status == TripStatus.Live && departure > now)
                            : bucket.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                                ? (departure <= now && arrival >= now)
                                : bucket.Equals("Past", StringComparison.OrdinalIgnoreCase)
                                    ? (arrival < now)
                                    : true;
                }).AsQueryable();
            }

            var trip = q.OrderByDescending(x => x.CreatedAt).Select(t => new TripListItemVm
            {
                Id = t.Id,
                TripName = t.TripName,
                Status = t.Status.ToString(),
                DepartureDate = t.DepartureDate.ToString("dd-MM-yyyy"),
                DepartureTime = t.DepartureTime.ToString("HH:mm"),
                DestinationArrivalDate = t.DestinationArrivalDate.ToString("dd-MM-yyyy"),
                DestinationArrivalTime = t.DestinationArrivalTime.ToString("HH:mm")
            }).ToList();

            var group = await _db.Groups.FirstOrDefaultAsync(o => o.Id == groupId && !o.IsDeleted);
            if (group == null) return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = false, Message = "Group Not Found" };
            var org = await _db.OrganizationMemberships.Include(x => x.Organization)
            .Where(m => !m.IsDeleted && m.AppUserId == userId && m.Organization.Id == group.OrgId && !m.Organization.IsDeleted).FirstOrDefaultAsync();
            if (org == null) return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = false, Message = "Organization Not Found" };

            var groupData = new GroupResponseVM
            {
                Id = group.Id,
                GroupName = group.GroupName,
                ShortName = group.ShortName,
                Description = group.Description,
                IsActive = group.IsActive,
                DeActiveDiscription = group.DeActiveDiscription,
                OrgId = org.Id,
                OrgName = org.Organization.OrgName,
                CreatedDate = group.CreatedAt,
            } ?? new GroupResponseVM();

            var orgData = new OrgListItemVm
            {
                Id = org.Id,
                OrgName = org.Organization.OrgName,
                ShortName = org.Organization.ShortName,
                MemberType = org.MemberType.ToString(),
                tripCount = _db.Trips.Count(t => t.OrganizationId == org.Organization.Id && !t.IsDeleted),
                groupCount = _db.Groups.Count(g => g.OrgId == org.Organization.Id && !g.IsDeleted),
                CreatedDate = org.CreatedAt,
                IsActive = org.Organization.IsActive,
                IsPrimary = org.Organization.IsPrimary,
                Permissions = await _permissionService.GetOrgPermissionAsync(org.OrganizationId, org.MemberType),
            };

            var data = new GroupDetailsResponseVM
            {
                Org = orgData,
                Group = groupData,
                Trips = trip
            };

            return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = true, Message = "Trips fetched successfully", Data = data };
        }

        // Get trips by groupId with optional status filtering (bucket) - Admin version
        public async Task<ResponseVM<GroupDetailsResponseVM>> GetTripsByGroupIdAsync(Guid groupId, Guid userId, string? bucket)
        {
            var now = DateTimeOffset.UtcNow;
            var q = _db.Trips.Where(t => t.GroupId == groupId && !t.IsDeleted);

            if (!string.IsNullOrEmpty(bucket))
            {
                q = q.AsEnumerable().Where(t =>
                {
                    var departure = t.DepartureDate.ToDateTime(t.DepartureTime, DateTimeKind.Utc);
                    var arrival = t.DestinationArrivalDate.ToDateTime(t.DestinationArrivalTime, DateTimeKind.Utc);

                    return bucket.Equals("Draft", StringComparison.OrdinalIgnoreCase)
                        ? (t.Status == TripStatus.Draft)
                        : bucket.Equals("Quoted", StringComparison.OrdinalIgnoreCase)
                            ? (t.Status == TripStatus.Quoted)
                        : bucket.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                            ? (t.Status == TripStatus.Rejected)
                        : bucket.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                            ? (t.Status == TripStatus.Approved)
                        : bucket.Equals("Upcoming", StringComparison.OrdinalIgnoreCase)
                            ? (t.Status == TripStatus.Live && departure > now)
                            : bucket.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                                ? (departure <= now && arrival >= now)
                                : bucket.Equals("Past", StringComparison.OrdinalIgnoreCase)
                                    ? (arrival < now)
                                    : true;
                }).AsQueryable();
            }

            var trip = q.OrderByDescending(x => x.CreatedAt).Select(t => new TripListItemVm
            {
                Id = t.Id,
                TripName = t.TripName,
                Status = t.Status.ToString(),
                DepartureDate = t.DepartureDate.ToString("dd-MM-yyyy"),
                DepartureTime = t.DepartureTime.ToString("HH:mm"),
                DestinationArrivalDate = t.DestinationArrivalDate.ToString("dd-MM-yyyy"),
                DestinationArrivalTime = t.DestinationArrivalTime.ToString("HH:mm")
            }).ToList();

            var group = await _db.Groups.FirstOrDefaultAsync(o => o.Id == groupId && !o.IsDeleted);
            if (group == null) return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = false, Message = "Group Not Found" };
            var org = await _db.OrganizationMemberships.Include(x => x.Organization)
            .Where(m => !m.IsDeleted && m.AppUserId == userId && m.OrganizationId == group.OrgId && !m.Organization.IsDeleted).FirstOrDefaultAsync();
            if (org == null) return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = false, Message = "Organization Not Found" };

            var groupData = new GroupResponseVM
            {
                Id = group.Id,
                GroupName = group.GroupName,
                ShortName = group.ShortName,
                Description = group.Description,
                IsActive = group.IsActive,
                DeActiveDiscription = group.DeActiveDiscription,
                OrgId = org.Id,
                OrgName = org.Organization.OrgName,
                CreatedDate = group.CreatedAt,
            } ?? new GroupResponseVM();

            var orgData = new OrgListItemVm
            {
                Id = org.Organization.Id,
                OrgName = org.Organization.OrgName,
                ShortName = org.Organization.ShortName,
                MemberType = org.MemberType.ToString(),
                tripCount = _db.Trips.Count(t => t.OrganizationId == org.Organization.Id && !t.IsDeleted),
                groupCount = _db.Groups.Count(g => g.OrgId == org.Organization.Id && !g.IsDeleted),
                CreatedDate = org.CreatedAt,
                IsActive = org.Organization.IsActive,
                IsPrimary = org.Organization.IsPrimary,
                Permissions = await _permissionService.GetOrgPermissionAsync(org.OrganizationId, org.MemberType),
            };

            var data = new GroupDetailsResponseVM
            {
                Org = orgData,
                Group = groupData,
                Trips = trip
            };

            return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = true, Message = "Trips fetched successfully", Data = data };
        }

        // Get trip details by Id
        public async Task<CreateTripVm?> GetTripAsync(Guid id)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip is null || trip.IsDeleted) return null;

            return new CreateTripVm
            {
                Id = trip.Id,
                groupId = trip.GroupId,
                OrganizationId = trip.OrganizationId,
                TripName = trip.TripName,
                DepartureDate = trip.DepartureDate,
                DepartureTime = trip.DepartureTime,
                StartCity = trip.StartCity,
                StartLocation = trip.StartLocation,
                NumberOfPassengers = trip.NumberOfPassengers,
                DestinationCity = trip.DestinationCity,
                DestinationLocation = trip.DestinationLocation,
                TripDays = trip.TripDays,
                DestinationArrivalDate = trip.DestinationArrivalDate,
                DestinationArrivalTime = trip.DestinationArrivalTime,
                Notes = trip.Notes,
                SaveAsDraft = trip.Status == TripStatus.Draft ? true : false
            };
        }

        // Get trip details for copying by Id
        public async Task<CreateTripVm?> GetTripForCopyAsync(Guid id)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip is null || trip.IsDeleted) return null;

            var baseName = trip.TripName;
            baseName = Regex.Replace(baseName, @"\s*\(\d+\)$", "");   // remove "(n)" at the end
            baseName = Regex.Replace(baseName, @"\s*Copy$", "", RegexOptions.IgnoreCase); // remove trailing "Copy"

            // 2. Find a unique name
            string newTripName = $"{baseName} (1)";
            int counter = 1;

            while (await _db.Trips.AnyAsync(t => t.TripName == newTripName && !t.IsDeleted))
            {
                counter++;
                newTripName = $"{baseName} ({counter})";
            }

            return new CreateTripVm
            {
                groupId = trip.GroupId,
                OrganizationId = trip.OrganizationId,
                TripName = newTripName,
                DepartureDate = trip.DepartureDate,
                DepartureTime = trip.DepartureTime,
                StartCity = trip.StartCity,
                StartLocation = trip.StartLocation,
                NumberOfPassengers = trip.NumberOfPassengers,
                DestinationCity = trip.DestinationCity,
                DestinationLocation = trip.DestinationLocation,
                TripDays = trip.TripDays,
                DestinationArrivalDate = trip.DestinationArrivalDate,
                DestinationArrivalTime = trip.DestinationArrivalTime,
                Notes = trip.Notes,
                SaveAsDraft = trip.Status == TripStatus.Draft ? true : false
            };
        }

        // Create a new trip with uniqueness check
        public async Task<ResponseVM<string>> CreateTripAsync(CreateTripVm vm, Guid userId)
        {
            if (await _db.Trips.AnyAsync(o => o.TripName == vm.TripName && o.GroupId == vm.groupId && !o.IsDeleted)) return new ResponseVM<string> { IsSuccess = false, Message = "Trip Name must be unique" };
            var group = await _db.Groups.Include(x => x.Org).FirstOrDefaultAsync(o => o.Id == vm.groupId);
            if (group == null) return new ResponseVM<string> { IsSuccess = false, Message = "Group not found" };

            if (vm.TripDays != null || vm.TripDays != 0)
            {
                var departureDateTime = vm.DepartureDate.ToDateTime(vm.DepartureTime);
                var arrivalDateTime = departureDateTime.AddDays((double)vm.TripDays);

                vm.DestinationArrivalDate = DateOnly.FromDateTime(arrivalDateTime);
                vm.DestinationArrivalTime = TimeOnly.FromDateTime(arrivalDateTime);
            }

            var trip = new Trip
            {
                GroupId = vm.groupId,
                OrganizationId = group.OrgId,
                TripName = vm.TripName,
                DepartureDate = vm.DepartureDate,
                DepartureTime = vm.DepartureTime,
                StartCity = vm.StartCity,
                StartLocation = vm.StartLocation,
                NumberOfPassengers = vm.NumberOfPassengers ?? 0,
                DestinationCity = vm.DestinationCity,
                DestinationLocation = vm.DestinationLocation,
                TripDays = vm.TripDays,
                DestinationArrivalDate = vm.DestinationArrivalDate,
                DestinationArrivalTime = vm.DestinationArrivalTime,
                Notes = vm.Notes,
                Status = vm.SaveAsDraft ? TripStatus.Draft : TripStatus.Quoted,
                CreatedForId = group.Org.CreatedForId,
                CreatedBy = userId,
                UpdatedBy = userId
            };
            await _db.Trips.AddAsync(trip);
            int res = await _db.SaveChangesAsync();
            if (res > 0) return new ResponseVM<string> { IsSuccess = true, Message = "Trip Created Successfully!", Data = group.OrgId.ToString() };
            return new ResponseVM<string> { IsSuccess = false, Message = "Failed to create trip." };
        }

        // Update an existing trip with uniqueness check
        public async Task<ResponseVM<string>> UpdateTripAsync(Guid id, CreateTripVm vm, Guid userId)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip is null || trip.IsDeleted)
                return new ResponseVM<string> { IsSuccess = false, Message = "Trip not found" };

            // Check uniqueness against other trips (excluding the current one)
            bool nameExists = await _db.Trips
                .AnyAsync(t => t.TripName == vm.TripName && t.GroupId == vm.groupId && t.Id != id && !t.IsDeleted);

            if (nameExists)
                return new ResponseVM<string> { IsSuccess = false, Message = "Trip name must be unique" };

            if (vm.TripDays != null || vm.TripDays != 0)
            {
                var departureDateTime = vm.DepartureDate.ToDateTime(vm.DepartureTime);
                var arrivalDateTime = departureDateTime.AddDays((double)vm.TripDays);

                vm.DestinationArrivalDate = DateOnly.FromDateTime(arrivalDateTime);
                vm.DestinationArrivalTime = TimeOnly.FromDateTime(arrivalDateTime);
            }

            trip.TripName = vm.TripName;
            trip.DepartureDate = vm.DepartureDate;
            trip.DepartureTime = vm.DepartureTime;
            trip.StartCity = vm.StartCity;
            trip.StartLocation = vm.StartLocation;
            trip.NumberOfPassengers = vm.NumberOfPassengers ?? 0;
            trip.DestinationCity = vm.DestinationCity;
            trip.DestinationLocation = vm.DestinationLocation;
            trip.TripDays = vm.TripDays;
            trip.DestinationArrivalDate = vm.DestinationArrivalDate;
            trip.DestinationArrivalTime = vm.DestinationArrivalTime;
            trip.Status = vm.SaveAsDraft ? TripStatus.Draft : TripStatus.Quoted;
            trip.Notes = vm.Notes;
            trip.UpdatedBy = userId;

            _db.Trips.Update(trip);
            int res = await _db.SaveChangesAsync();
            if (res > 0) return new ResponseVM<string> { IsSuccess = true, Message = "Trip Updated Successfully!" };
            return new ResponseVM<string> { IsSuccess = false, Message = "Failed to update trip." };
        }

        // Soft delete a trip by setting IsDeleted flag
        public async Task<ResponseVM<Trip>> DeleteTripAsync(Guid id)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip is null || trip.IsDeleted) return new ResponseVM<Trip> { IsSuccess = false, Message = "Trip Not found" };
            trip.IsDeleted = true;
            int res = await _db.SaveChangesAsync();
            if (res > 0) return new ResponseVM<Trip> { IsSuccess = true, Message = "Trip Deleted Successfully!", Data = trip };
            return new ResponseVM<Trip> { IsSuccess = false, Message = "Failed to delete trip." };
        }

        // Cancel a trip by updating its status to Canceled
        public async Task<ResponseVM<string>> CancelTripAsync(Guid id)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip is null || trip.IsDeleted) return new ResponseVM<string> { IsSuccess = false, Message = "Trip Not found" };
            trip.Status = TripStatus.Canceled;
            int res = await _db.SaveChangesAsync();
            if (res > 0) return new ResponseVM<string> { IsSuccess = true, Message = "Trip Cancel Successfully!" };
            return new ResponseVM<string> { IsSuccess = false, Message = "Failed to cancel trip." };
        }

        // Copy a trip with a unique name
        public async Task<ResponseVM<string>> CopyTripAsync(Guid id, Guid userId)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip is null || trip.IsDeleted) return new ResponseVM<string> { IsSuccess = false, Message = "Trip Not found" };
            // 1. Normalize base name (remove " Copy" and any "(n)" suffix)
            var baseName = trip.TripName;
            baseName = Regex.Replace(baseName, @"\s*\(\d+\)$", "");   // remove "(n)" at the end
            baseName = Regex.Replace(baseName, @"\s*Copy$", "", RegexOptions.IgnoreCase); // remove trailing "Copy"

            // 2. Find a unique name
            string newTripName = $"{baseName} (1)";
            int counter = 1;

            while (await _db.Trips.AnyAsync(t => t.TripName == newTripName && !t.IsDeleted))
            {
                counter++;
                newTripName = $"{baseName} ({counter})";
            }

            // 3. Create the new trip
            var newTrip = new Trip
            {
                Id = Guid.NewGuid(),
                TripName = newTripName,
                OrganizationId = trip.OrganizationId,
                GroupId = trip.GroupId,
                DepartureDate = trip.DepartureDate,
                DepartureTime = trip.DepartureTime,
                StartCity = trip.StartCity,
                StartLocation = trip.StartLocation,
                NumberOfPassengers = trip.NumberOfPassengers,
                DestinationCity = trip.DestinationCity,
                DestinationLocation = trip.DestinationLocation,
                TripDays = trip.TripDays,
                DestinationArrivalDate = trip.DestinationArrivalDate,
                DestinationArrivalTime = trip.DestinationArrivalTime,
                Notes = trip.Notes,
                Status = TripStatus.Quoted,
                CreatedForId = trip.CreatedForId,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            await _db.Trips.AddAsync(newTrip);
            int res = await _db.SaveChangesAsync();
            if (res > 0) return new ResponseVM<string> { IsSuccess = true, Message = "Trip Copied Successfully!" };
            return new ResponseVM<string> { IsSuccess = false, Message = "Failed to copy trip." };
        }

        // Submit a trip for quote by updating its status and locking it
        public async Task<ResponseVM<string>> SubmitForQuoteAsync(Guid id)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip is null) return new ResponseVM<string> { IsSuccess = false, Message = "Trip Not found" };
            trip.Status = TripStatus.Quoted;
            trip.Locked = true;
            int res = await _db.SaveChangesAsync();
            if (res > 0) return new ResponseVM<string> { IsSuccess = true, Message = "Trip Submitted for Quote Successfully!" };
            return new ResponseVM<string> { IsSuccess = false, Message = "Failed to submit trip for quote." };
        }

        // Activate a trip by updating its status to Live
        public async Task<ResponseVM<string>> ActivateTripAsync(Guid id)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip is null) return new ResponseVM<string> { IsSuccess = false, Message = "Trip Not found" };
            trip.Status = TripStatus.Live;
            int res = await _db.SaveChangesAsync();
            if (res > 0) return new ResponseVM<string> { IsSuccess = true, Message = "Trip Activated Successfully!" };
            return new ResponseVM<string> { IsSuccess = false, Message = "Failed to activate trip." };
        }

        // Get trips for admin with optional status filtering (bucket)
        public async Task<List<TripListItemVm>> GetTripsForAdminAsync(string? bucket)
        {
            var now = DateTimeOffset.UtcNow;

            var query = from trip in _db.Trips
                        join user in _db.Users on trip.CreatedBy equals user.Id into userGroup
                        from user in userGroup.DefaultIfEmpty() // left join in case user is missing
                        join org in _db.Organizations on trip.OrganizationId equals org.Id into orgGroup
                        from org in orgGroup.DefaultIfEmpty()
                        where !trip.IsDeleted
                        select new
                        {
                            Trip = trip,
                            User = user,
                            Organization = org
                        };

            var trips = await query.ToListAsync();

            if (!string.IsNullOrEmpty(bucket))
            {
                trips = trips.Where(x =>
                {
                    var departure = x.Trip.DepartureDate.ToDateTime(x.Trip.DepartureTime, DateTimeKind.Utc);
                    var arrival = x.Trip.DestinationArrivalDate.ToDateTime(x.Trip.DestinationArrivalTime, DateTimeKind.Utc);

                    return bucket.Equals("Quoted", StringComparison.OrdinalIgnoreCase)
                        ? (x.Trip.Status == TripStatus.Quoted)
                        : bucket.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                        ? (x.Trip.Status == TripStatus.Rejected)
                        : bucket.Equals("Draft", StringComparison.OrdinalIgnoreCase)
                        ? (x.Trip.Status == TripStatus.Draft)
                        : bucket.Equals("Upcoming", StringComparison.OrdinalIgnoreCase)
                            ? (x.Trip.Status == TripStatus.Approved && departure > now)
                            : bucket.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                                ? (x.Trip.Status == TripStatus.Live && departure <= now && arrival >= now)
                                : bucket.Equals("Past", StringComparison.OrdinalIgnoreCase)
                                    ? (arrival < now)
                                    : true;
                }).ToList();
            }

            return trips.Select(x => new TripListItemVm
            {
                Id = x.Trip.Id,
                TripName = x.Trip.TripName,
                OrganizationName = x.Organization?.OrgName ?? string.Empty,
                UserName = x.User?.FirstName ?? string.Empty + " " + x.User?.LastName ?? string.Empty,
                Status = x.Trip.Status.ToString(),
                DepartureDate = x.Trip.DepartureDate.ToString("yyyy-MM-dd"),
                DestinationArrivalDate = x.Trip.DestinationArrivalDate.ToString("yyyy-MM-dd")
            }).ToList();
        }

        // Get detailed information about a specific trip
        public async Task<ResponseVM<TripDetailsResponseVm>> GetTripsDetailsAsync(Guid tripId)
        {
            var trip = await _db.Trips
                .Include(t => t.Organization)
                .Include(t => t.Group)
                .Where(t => t.Id == tripId && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (trip == null)
            {
                return new ResponseVM<TripDetailsResponseVm>
                {
                    IsSuccess = false,
                    Message = "Trip not found.",
                    Data = null
                };
            }

            var assignment = await _db.TripBusAssignments
             .Include(a => a.Driver)
             .Include(a => a.Equipment)
             .FirstOrDefaultAsync(a => a.TripId == trip.Id && !a.IsDeleted);

            BusDriver? driver = null;

            if (assignment != null && assignment.DriverId != null)
            {
                driver = await _db.BusDrivers
                    .Include(b => b.AppUser)
                    .FirstOrDefaultAsync(u => u.AppUserId == assignment.DriverId && !u.IsDeleted);
            }

            var user = await _db.Users
                .Where(u => u.Id == trip.CreatedBy)
                .FirstOrDefaultAsync();


            var data = new TripDetailsResponseVm
            {
                Id = trip.Id,
                TripName = trip.TripName,
                Status = trip.Status.ToString(),
                QuotedPrice = trip.QuotedPrice,
                EstimateLinkUrl = trip.EstimateLinkUrl,
                InvoiceLinkUrl = trip.InvoiceLinkUrl,
                TripCreatedAt = trip.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),

                // User info
                UserId = user?.Id.ToString() ?? string.Empty,
                UserName = user?.FirstName + " " + user?.LastName,
                Email = user?.Email ?? string.Empty,

                // Organization info
                OrganizationId = trip.OrganizationId,
                OrganizationName = trip.Organization != null ? trip.Organization.OrgName : null,
                OrgShortName = trip.Organization != null ? trip.Organization.ShortName : null,
                IsPrimary = trip.Organization != null ? trip.Organization.IsPrimary : false,


                // Dates & Times
                DepartureDate = trip.DepartureDate.ToString(),
                DepartureTime = trip.DepartureTime.ToString(),
                DestinationArrivalDate = trip.DestinationArrivalDate.ToString(),
                DestinationArrivalTime = trip.DestinationArrivalTime.ToString(),

                // Locations
                StartCity = trip.StartCity,
                StartLocation = trip.StartLocation,
                DestinationCity = trip.DestinationCity,
                DestinationLocation = trip.DestinationLocation,

                // Extra
                NumberOfPassengers = trip.NumberOfPassengers,
                Notes = trip.Notes,

                // Driver info
                DriverId = assignment?.DriverId.ToString(),
                DriverName = driver != null ? (driver.AppUser.FirstName + " " + driver.AppUser.LastName) : null,
                EmploymentType = driver?.EmploymentType,
                LicenseNumber = driver?.LicenseNumber,
                LicenseProvince = driver?.LicenseProvince,

                // Equipment info
                Manufacturer = assignment?.Equipment?.Manufacturer,
                Model = assignment?.Equipment?.Model,
                LicensePlate = assignment?.Equipment?.LicensePlate,
                SeatingCapacity = assignment?.Equipment != null ? assignment.Equipment.SeatingCapacity.ToString() : null,
                Year = assignment?.Equipment != null ? assignment.Equipment.Year.ToString() : null,

                // Group info
                GourpId = trip.GroupId != null ? trip.GroupId : null,
                IsActive = trip.Group != null ? trip.Group.IsActive : false,
                GroupName = trip.Group != null ? trip.Group.GroupName : null,
                ShortName = trip.Group != null ? trip.Group.ShortName : null,
                Creator = trip.Group != null ? (user != null ? (user.FirstName + " " + user.LastName) : null) : null,
                CreatedAt = trip.Group != null ? trip.Group.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") : null
            };

            return new ResponseVM<TripDetailsResponseVm>
            {
                IsSuccess = true,
                Message = "Trip details fetched successfully.",
                Data = data
            };
        }

        // Admin: Get trip requests that are live and either unassigned or requested by driver
        public async Task<List<object>> GetRequestsAsync()
        {
            return await _db.TripBusAssignments
                .Include(t => t.Trip)
                .Include(t => t.Driver)
                .Where(t => t.Trip.Status == TripStatus.Live &&
                            (!t.Trip.BusAssignments.Any() ||
                             t.Trip.BusAssignments.Any(a => a.Status == TripBusAssignmentStatus.RequestedByDriver))).OrderByDescending(x => x.CreatedAt)
                .Select(t => new
                {
                    Id = t.Id,
                    Assignments = t.Trip.BusAssignments.Count,
                    DriverId = t.DriverId,
                    DriverName = t.Driver != null ? (t.Driver.FirstName + " " + t.Driver.LastName) : "-",
                    DriverEmail = t.Driver != null ? t.Driver.Email : "-",
                    DriverNumber = t.Driver != null ? t.Driver.PhoneNumber : "-",
                    Status = t.Status
                }).ToListAsync<object>();
        }

        // Admin: Approve or reject a trip by updating its status
        public async Task<ResponseVM<string>> ApproveOrRejectTripAsync(Guid tripId, string status)
        {
            var trip = await _db.Trips.FindAsync(tripId);
            if (trip is null) return new ResponseVM<string> { IsSuccess = false, Message = "Trip Not found" };
            if (status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                trip.Status = TripStatus.Approved;
            }
            else if (status.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            {
                trip.Status = TripStatus.Rejected;
            }
            else
            {
                return new ResponseVM<string> { IsSuccess = false, Message = "Invalid status value" };
            }
            int res = await _db.SaveChangesAsync();
            if (res > 0)
            {
                if (status == "Reject")
                    status += "e";
                return new ResponseVM<string> { IsSuccess = true, Message = $"Trip {status}d Successfully!" };
            }

            return new ResponseVM<string> { IsSuccess = false, Message = $"Failed to {status.ToLower()} trip." };
        }

        // Admin: Assign a trip by updating the assignment status to Assigned
        public async Task<ResponseVM<string>> AssignTripAsync(Guid id)
        {
            var assignment = await _db.TripBusAssignments.Include(t => t.Trip).FirstOrDefaultAsync(t => t.Id == id);
            if (assignment is null) return new ResponseVM<string> { IsSuccess = false, Message = "Bus Assignments Not found" };
            assignment.Status = TripBusAssignmentStatus.Assigned;
            int res = await _db.SaveChangesAsync();
            if (res > 0) return new ResponseVM<string> { IsSuccess = true, Message = "Trip Assigned Successfully!" };
            return new ResponseVM<string> { IsSuccess = false, Message = "Failed to assign trip." };
        }

        // Admin: Get trips that are live and either unassigned or have unassigned drivers
        public async Task<List<object>> GetTripsToAssignAsync()
        {
            return await _db.TripBusAssignments
              .Include(t => t.Trip)
              .Where(t => t.Trip.Status == TripStatus.Live &&
                          (!t.Trip.BusAssignments.Any() ||
                           t.Trip.BusAssignments.Any(a => a.DriverId == null))).OrderByDescending(x => x.CreatedAt)
              .Select(t => new { Id = t.Trip.Id, Assignments = t.Trip.BusAssignments.Count, Status = t.Status })
              .ToListAsync<object>();
        }


        #region Home

        // Get upcoming trips for home dashboard
        public async Task<TripDashboardVM> GetFilteredTripsAsync(TripFilterVM filter, Guid userId)
        {
            var query =
                from trip in _db.Trips

                join orgMember in _db.OrganizationMemberships
                    on trip.OrganizationId equals orgMember.OrganizationId

                join org in _db.Organizations
                    on trip.OrganizationId equals org.Id into orgGroup
                from org in orgGroup.DefaultIfEmpty()

                join grp in _db.Groups
                    on trip.GroupId equals grp.Id into groupGroup
                from grp in groupGroup.DefaultIfEmpty()

                join tba in _db.TripBusAssignments
                    on trip.Id equals tba.TripId into tbaGroup
                from tba in tbaGroup.DefaultIfEmpty()

                join orgCreator in _db.Users
                    on org.CreatedBy equals orgCreator.Id into creatorGroup
                from orgCreator in creatorGroup.DefaultIfEmpty()

                where !trip.IsDeleted
                      && !orgMember.IsDeleted
                      && !org.IsDeleted
                      && orgMember.AppUserId == userId
                orderby trip.CreatedAt descending
                select new
                {
                    Trip = trip,
                    Organization = org,
                    Group = grp,
                    TripBusAssignment = tba,
                    OrganizationCreator = orgCreator
                };

            // Apply filters
            if (filter.OrganizationId.HasValue)
            {
                query = query.Where(x => x.Trip.OrganizationId == filter.OrganizationId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var keyword = filter.Search.ToLower();
                query = query.Where(x =>
                    (x.Organization != null && x.Organization.OrgName.ToLower().Contains(keyword)) ||
                    (x.Group != null && x.Group.GroupName.ToLower().Contains(keyword)) ||
                    x.Trip.TripName.ToLower().Contains(keyword));
            }

            if (filter.TripDate.HasValue)
            {
                var tripDate = filter.TripDate.Value; query = query.Where(x => x.Trip.DepartureDate.Year == tripDate.Year && x.Trip.DepartureDate.Month == tripDate.Month && x.Trip.DepartureDate.Day == tripDate.Day);
            }

            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "All")
            {
                if (Enum.TryParse<TripStatus>(filter.Status, out var status))
                {
                    query = query.Where(x => x.Trip.Status == status);
                }
            }

            // Count & pagination
            var totalCount = await query.CountAsync();
            var trips = await query
                .OrderByDescending(x => x.Trip.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new TripListItemVm
                {
                    Id = x.Trip.Id,
                    TripName = x.Trip.TripName,
                    OrganizationName = x.Organization != null ? x.Organization.OrgName : string.Empty,
                    OrganizationCreatorName = x.OrganizationCreator != null
                                                ? x.OrganizationCreator.FirstName + " " + x.OrganizationCreator.LastName
                                                : string.Empty,
                    GroupName = x.Group != null ? x.Group.GroupName : "-",
                    GroupId = x.Group != null ? x.Group.Id : Guid.Empty,
                    Status = x.Trip.Status.ToString(),
                    DepartureDate = x.Trip.DepartureDate.ToString("dd-MMM-yyyy") + " " + x.Trip.DepartureTime.ToString("HH:mm"),
                    DestinationArrivalDate = x.Trip.DestinationArrivalDate.ToString("dd-MMM-yyyy") + " " + x.Trip.DestinationArrivalTime.ToString("HH:mm"),
                    Driver = x.TripBusAssignment != null && x.TripBusAssignment.Driver != null
                                ? (x.TripBusAssignment.Driver.FirstName + " " + x.TripBusAssignment.Driver.LastName)
                                : "-",
                    BusNumber = x.TripBusAssignment != null && x.TripBusAssignment.Equipment != null
                                ? x.TripBusAssignment.Equipment.LicensePlate
                                : "-"
                }).ToListAsync();

            // Fetch organizations for filter dropdown
            var organizations = await _db.OrganizationMemberships
                .Include(m => m.Organization)
                .Where(o => !o.IsDeleted && !o.Organization.IsDeleted && o.AppUserId == userId).OrderByDescending(x => x.CreatedAt)
                .Select(o => new OrganizationDto
                {
                    Id = o.Organization.Id,
                    Name = o.Organization.OrgName
                })
                .ToListAsync();

            // Fetch statuses from enum
            var statuses = Enum.GetValues(typeof(TripStatus))
                .Cast<TripStatus>()
                .Select(s => new TripStatusDto { Key = s.ToString(), Value = s.ToString() })
                .ToList();

            return new TripDashboardVM
            {
                Trips = new PaginatedList<TripListItemVm>(trips, totalCount, filter.Page, filter.PageSize),
                Filter = filter,
                Organizations = organizations,
                Statuses = statuses
            };
        }

        // Get the most recent trip by groupId for pre-filling trip creation form
        public async Task<CreateTripVm> GetTripByGroupIdAsync(Guid groupId)
        {
            var trip = await _db.Trips.Where(t => t.GroupId == groupId && !t.IsDeleted).OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync();

            if (trip != null)
            {
                return new CreateTripVm
                {
                    groupId = trip.GroupId,
                    OrganizationId = trip.OrganizationId,
                    TripName = trip.TripName,
                    DepartureDate = trip.DepartureDate,
                    DepartureTime = trip.DepartureTime,
                    StartCity = trip.StartCity,
                    StartLocation = trip.StartLocation,
                    NumberOfPassengers = trip.NumberOfPassengers,
                    DestinationCity = trip.DestinationCity,
                    DestinationLocation = trip.DestinationLocation,
                    TripDays = trip.TripDays,
                    DestinationArrivalDate = trip.DestinationArrivalDate,
                    DestinationArrivalTime = trip.DestinationArrivalTime,
                    Notes = trip.Notes,
                    SaveAsDraft = trip.Status == TripStatus.Draft ? true : false
                };
            }
            return null;
        }

        #endregion
    }
}

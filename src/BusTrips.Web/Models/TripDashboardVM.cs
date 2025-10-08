namespace BusTrips.Web.Models
{
    public class TripDashboardVM
    {
        public PaginatedList<TripListItemVm> Trips { get; set; }
        public TripFilterVM Filter { get; set; }

        public List<OrganizationDto> Organizations { get; set; }
        public List<TripStatusDto> Statuses { get; set; }
    }

    public class OrganizationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class TripStatusDto
    {
        public string Key { get; set; }   // e.g. "Draft"
        public string Value { get; set; } // display name (same as Key for now)
    }

}

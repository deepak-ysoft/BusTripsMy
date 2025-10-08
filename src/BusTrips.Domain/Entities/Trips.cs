namespace BusTrips.Domain.Entities;

public class Equipment : BaseEntity
{
    public Guid Id { get; set; }

    // Core Details
    public string BusNumber { get; set; } = default!;
    public string Vin { get; set; } = default!;
    public string LicensePlate { get; set; } = default!;
    public string IssuingProvince { get; set; } = default!;  // NEW
    public string Manufacturer { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int Year { get; set; }
    public string Color { get; set; } = default!;
    public int SeatingCapacity { get; set; }
    public string? VehicleUrl { get; set; } = default!;
    public string VehicleType { get; set; } = default!;

    // Physical Specs
    public decimal Length { get; set; }   // in meters (or specify unit)
    public decimal Height { get; set; }   // in meters
    public decimal GrossVehicleWeight { get; set; } // in kg/tons

    // Active/Inactive Tracking
    public bool IsActive { get; set; } = true;
    public string? DeactivationReason { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    // Documents
    public ICollection<EquipmentDocument> Documents { get; set; } = new List<EquipmentDocument>();
}

public class EquipmentDocument : BaseEntity
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string FilePath { get; set; } = default!;   // store file path or blob URL
    public string Description { get; set; } = default!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Equipment Equipment { get; set; } = default!;
}


public class Trip : BaseEntity
{
    public Guid Id { get; set; }
    public string TripName { get; set; }
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? PrimaryContactId { get; set; }
    public Guid? SecondaryContactId { get; set; }

    public int? TripDays { get; set; }
    public DateOnly DepartureDate { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public string StartCity { get; set; } = default!;
    public string StartLocation { get; set; } = default!;
    public int NumberOfPassengers { get; set; }
    public string DestinationCity { get; set; } = default!;
    public string DestinationLocation { get; set; } = default!;
    public DateOnly DestinationArrivalDate { get; set; }
    public TimeOnly DestinationArrivalTime { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Quoted;
    public bool Locked { get; set; }

    public int? QuotedPrice { get; set; }
    public string? EstimateLinkUrl { get; set; }
    public string? InvoiceLinkUrl { get; set; }
    public string? TripDocsUrl { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeActiveDiscription { get; set; }

    public ICollection<TripBusAssignment> BusAssignments { get; set; } = new List<TripBusAssignment>();
    public ICollection<TripMembership> Managers { get; set; } = new List<TripMembership>();
    public ICollection<TripChangeLog> ChangeLogs { get; set; } = new List<TripChangeLog>();

}

public enum TripStatus { Draft, Quoted, Approved, Rejected, Live, Completed, Canceled }

public class TripBusAssignment : BaseEntity
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public Guid? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public Guid? DriverId { get; set; }     // AppUserId of the driver
    public AppUser? Driver { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public TripBusAssignmentStatus Status { get; set; } = TripBusAssignmentStatus.Assigned;
}

public enum TripBusAssignmentStatus { RequestedByDriver, Assigned }

public class TripMembership : BaseEntity
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = default!;
}

public class TripChangeLog : BaseEntity
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Summary { get; set; } = default!;
}

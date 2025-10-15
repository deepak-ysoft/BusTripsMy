using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTrips.Domain.Entities;

public class Equipment : BaseEntity
{
    public Guid Id { get; set; }
    public string BusNumber { get; set; }
    public string Vin { get; set; }
    public string LicensePlate { get; set; }
    public string IssuingProvince { get; set; }  // NEW
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public string Color { get; set; }
    public int SeatingCapacity { get; set; }
    public string? VehicleUrl { get; set; }
    public string VehicleType { get; set; }

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
    [ForeignKey("EquipmentId")]
    public Equipment Equipment { get; set; }
    public string FilePath { get; set; }   // store file path or blob URL
    public string Description { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}


public class Trip : BaseEntity
{
    public Guid Id { get; set; }
    [Required] public string TripName { get; set; }

    [Required]
    public Guid CreatedForId { get; set; }
    [ForeignKey("CreatedForId")]
    public AppUser CreatedForUser { get; set; }

    public Guid? GroupId { get; set; }
    [ForeignKey("GroupId")]
    public Group? Group { get; set; }

    public Guid? OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public Organization? Organization { get; set; }


    public Guid? PrimaryContactId { get; set; }
    public Guid? SecondaryContactId { get; set; }

    public int? TripDays { get; set; }
    public DateOnly DepartureDate { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public string StartCity { get; set; }
    public string StartLocation { get; set; }
    public int NumberOfPassengers { get; set; }
    public string DestinationCity { get; set; }
    public string DestinationLocation { get; set; }
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
    public ICollection<TripChangeLog> ChangeLogs { get; set; } = new List<TripChangeLog>();

}

public enum TripStatus { Draft, Quoted, Approved, Rejected, Live, Completed, Canceled }

public class TripBusAssignment : BaseEntity
{
    [Key] public Guid Id { get; set; }

    [Required] public Guid TripId { get; set; }
    [ForeignKey("TripId")]
    public Trip Trip { get; set; }

    public Guid? EquipmentId { get; set; }
    [ForeignKey("EquipmentId")]
    public Equipment? Equipment { get; set; }

    public Guid? DriverId { get; set; }
    [ForeignKey("DriverId")]
    public AppUser? Driver { get; set; }

    public TripBusAssignmentStatus Status { get; set; } = TripBusAssignmentStatus.Assigned;
}

public enum TripBusAssignmentStatus { RequestedByDriver, Assigned }


public class TripChangeLog : BaseEntity
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    [ForeignKey("TripId")]
    public Trip Trip { get; set; }
    public Guid ChangedByUserId { get; set; }
    public AppUser ChangedByUser { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Summary { get; set; }
}

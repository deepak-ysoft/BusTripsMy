using BusTrips.Domain.Entities;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BusTrips.Web.Models
{
    public class EquipmentVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "VIN is required")]
        [StringLength(17, MinimumLength = 6, ErrorMessage = "VIN must be 6-17 characters")]
        [DisplayName("VIN")]
        public string Vin { get; set; } = "";

        [Required(ErrorMessage = "License Plate is required")]
        [DisplayName("License Plate")]
        public string LicensePlate { get; set; } = "";

        [Required(ErrorMessage = "Issuing Province is required")]
        [DisplayName("Issuing Province")]
        public string IssuingProvince { get; set; } = "";

        [Required(ErrorMessage = "Bus Number is required")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Bus Number must be 4-10 characters")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Bus Number must contain numbers only")]
        [DisplayName("Bus Number")]
        public string BusNumber { get; set; } = "";


        [Required(ErrorMessage = "Manufacturer is required")]
        [DisplayName("Manufacturer")]
        public string Manufacturer { get; set; } = "";

        [Required(ErrorMessage = "Model is required")]
        [DisplayName("Model")]
        public string Model { get; set; } = "";

        [Required(ErrorMessage = "Year is required")]
        [PastOrCurrentYear(MinYear = 1900)]
        [DisplayName("Year")]
        public int Year { get; set; } = DateTime.Now.Year;
        [Required(ErrorMessage = "Color is required")]
        [DisplayName("Color")]
        public string Color { get; set; } = "";

        [Required(ErrorMessage = "Seating Capacity is required")]
        [Range(10, 300, ErrorMessage = "Seating Capacity must be at least 10 and max 300")] 
        [DisplayName("Seating Capacity")]
        public int? SeatingCapacity { get; set; } 

        [DisplayName("Vehicle Images")]
        public List<IFormFile?>? VehicleImages { get; set; } = null;

        [DisplayName("Vehicle Image URLs")]
        public string? VehicleUrl { get; set; }

        [Required(ErrorMessage = "Please select a vehicle type.")]
        [DisplayName("Vehicle Type")]
        public string VehicleType { get; set; } = "";

        [Required(ErrorMessage = "Length is required")]
        [Range(5, 50, ErrorMessage = "Length must be between 5 and 50 meters")]
        [DisplayName("Length (m)")]
        public decimal? Length { get; set; }

        [Required(ErrorMessage = "Height is required")]
        [Range(5, 10, ErrorMessage = "Height must be between 5 and 10 meters")]
        [DisplayName("Height (m)")]
        public decimal? Height { get; set; }

        [Required(ErrorMessage = "Gross Vehicle Weight is required")]
        [Range(200, 1000000, ErrorMessage = "Gross Vehicle Weight must be between 200 kg and 1,000,000 kg")]
        [DisplayName("Gross Vehicle Weight (kg)")]
        public decimal? GrossVehicleWeight { get; set; }

        [DisplayName("Active?")]
        public bool IsActive { get; set; } = true;

        [DisplayName("Deactivation Reason")]
        public string? DeactivationReason { get; set; }

        [DisplayName("Deactivated At")]
        public DateTime? DeactivatedAt { get; set; } = DateTime.Now;

        public List<EquipmentDocumentVM>? Documents { get; set; } = new();
    }

    public class EquipmentListVm
    {
        public Guid Id { get; set; }
        public string BusNumber { get; set; } = default!;
        public string LicensePlate { get; set; } = default!;
        public string IssuingProvince { get; set; } = default!;
        public string Manufacturer { get; set; } = default!;
        public string Model { get; set; } = default!;
        public int Year { get; set; }
        public bool IsActive { get; set; }
    }

    public class EquipmentDetailVm
    {
        public Guid Id { get; set; }
        public string BusNumber { get; set; } = default!;
        public string Vin { get; set; } = default!;
        public string LicensePlate { get; set; } = default!;
        public string IssuingProvince { get; set; } = default!;
        public string Manufacturer { get; set; } = default!;
        public string Model { get; set; } = default!;
        public int Year { get; set; }
        public string Color { get; set; } = default!;
        public int SeatingCapacity { get; set; }
        public string VehicleType { get; set; } = default!;
        public decimal Length { get; set; }
        public decimal Height { get; set; }
        public decimal GrossVehicleWeight { get; set; }
        public bool IsActive { get; set; }
        public string? DeactivationReason { get; set; }
        public DateTime? DeactivatedAt { get; set; }

        // Related docs
        public List<EquipmentDocumentVM> Documents { get; set; } = new();
    }

    public class EquipmentDocumentVM
    {
        public Guid Id { get; set; }
        [Required]
        public IFormFile? File { get; set; } // file being uploaded
        [Required]
        public string? Description { get; set; }
        public string? FilePath { get; set; } // saved path after upload
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

}
public class PastOrCurrentYearAttribute : ValidationAttribute
{
    public int MinYear { get; set; } = 1900;

    public PastOrCurrentYearAttribute()
    {
        ErrorMessage = "Enter a valid Year (not in the future)";
    }

    public override bool IsValid(object? value)
    {
        if (value is int year)
        {
            return year >= MinYear && year <= DateTime.Now.Year;
        }
        return true;
    }
}
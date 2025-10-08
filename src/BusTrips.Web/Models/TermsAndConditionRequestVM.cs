using System.ComponentModel.DataAnnotations;

namespace BusTrips.Web.Models
{
    public class TermsAndConditionRequestVM
    {
        public Guid? Id { get; set; }
        [Required]
        [Display(Name = "Terms For")]
        public string TermsFor { get; set; } = default!;
        [Required]
        public string Title { get; set; } = default!;
        [Required]
        public string Content { get; set; } = default!;
    }
    public class TermsAndConditionResponseVM
    {
        public Guid? Id { get; set; }
        public string TermsFor { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = default!;
    }
}

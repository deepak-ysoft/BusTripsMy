using System.ComponentModel.DataAnnotations;

namespace BusTrips.Web.Models
{
    public class TermsAndConditionRequestVM
    {
        public Guid? Id { get; set; }
        [Required]
        [Display(Name = "Terms For")]
        public string TermsFor { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Content { get; set; }
    }
    public class TermsAndConditionResponseVM
    {
        public Guid? Id { get; set; }
        public string TermsFor { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

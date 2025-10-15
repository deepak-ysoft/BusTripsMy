using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusTrips.Domain.Entities
{
    public class ContactUs
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class Notification
    {
        [Key] public Guid Id { get; set; }

        [Required] public string Title { get; set; }
        [Required] public string Message { get; set; }
        public string? FullMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? UserId { get; set; }   // null = global
        public string? Role { get; set; }    // null = global
        public bool IsGlobal => UserId == null && Role == null;
        public bool IsRead { get; set; } = false;

        public ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
    }

    public class UserNotification
    {
        [Key] public Guid Id { get; set; }

        [Required] public Guid NotificationId { get; set; }
        [ForeignKey("NotificationId")]
        public Notification Notification { get; set; }

        [Required] public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        public bool IsRead { get; set; } = false;
    }
}

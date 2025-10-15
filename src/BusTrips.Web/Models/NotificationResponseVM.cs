namespace BusTrips.Web.Models
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? FullMessage { get; set; }
        public string? Role { get; set; }
        public bool IsRead { get; set; }
        public string Date { get; set; } = string.Empty;
    }

}

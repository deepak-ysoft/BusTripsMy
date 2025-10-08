namespace BusTrips.Web.Models
{
    public class ResponseVM <T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
    }
}

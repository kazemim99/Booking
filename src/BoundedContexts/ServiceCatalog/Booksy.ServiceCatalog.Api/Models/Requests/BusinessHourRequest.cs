namespace Booksy.ServiceCatalog.API.Models.Requests
{
    public sealed class BusinessHourRequest
    {
        [Required(ErrorMessage = "Open time is required")]
        public TimeOnly OpenTime { get; set; }

        [Required(ErrorMessage = "Close time is required")]
        public TimeOnly CloseTime { get; set; }
    }
}

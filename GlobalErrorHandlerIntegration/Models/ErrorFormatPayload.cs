namespace GlobalErrorHandlerIntegration.Models
{
    public class ErrorFormatPayload
    {
        public string? Channel_id { get; set; }
        public List<Setting> Settings { get; set; }
        public string Message { get; set; }
    }
}

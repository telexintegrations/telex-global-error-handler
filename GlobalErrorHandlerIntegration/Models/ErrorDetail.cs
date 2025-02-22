namespace GlobalErrorHandlerIntegration.Models
{
    public class ErrorDetail
    {
        public string ErrorId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; }
        public string ExceptionType { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public string InnerExceptionMessage { get; set; }   
    }
}

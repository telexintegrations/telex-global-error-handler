using GlobalErrorHandlerIntegration.IServices;
using GlobalErrorHandlerIntegration.Models;
using Newtonsoft.Json;
using System.Net;
using System.Security.Authentication;

namespace GlobalErrorHandlerIntegration.Middlewares
{
    public class TelexGlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TelexGlobalExceptionMiddleware> _logger;
        private readonly ITelexErrorLogger _telexLogger;


        public TelexGlobalExceptionMiddleware(RequestDelegate next, ILogger<TelexGlobalExceptionMiddleware> logger, ITelexErrorLogger telexLogger)
        {
            _next = next;
            _logger = logger;
            _telexLogger = telexLogger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the error Locally
                _logger.LogError(ex, "An unhandled exception caught in middleware.");

                int status = DetermineStatusCode(ex);

                // Format error details into a structured JSON object
                var errorDetails = new ErrorDetail()
                {
                    Timestamp = DateTime.UtcNow,
                    ExceptionType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    HttpMethod = context.Request.Method,
                    Url = context.Request.Path,
                    StatusCode = status,
                    InnerExceptionMessage = ex?.InnerException?.Message
                };

                
                Task.Run(async () =>
                {
                    try
                    {
                        _telexLogger.SendInitialErrorReport(errorDetails);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send error report to Telex.");
                    }

                });

                // Return a generic error response to the client
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";

                var response = JsonConvert.SerializeObject(new
                {
                    error = "An internal server error occurred. Please try again later."
                });
                await context.Response.WriteAsync(response);

            }
        }


        private int DetermineStatusCode(Exception exception)
        {
            // Map certain exception types to specifc status codes
            return exception switch
            {
                ArgumentException _ => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException _ => (int)HttpStatusCode.Unauthorized,
                NotImplementedException _ => (int)HttpStatusCode.NotImplemented,
                AuthenticationException _ => (int)HttpStatusCode.Forbidden,               
                _ => (int)HttpStatusCode.InternalServerError,

            };
        }
    }
}

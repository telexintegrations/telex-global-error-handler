using GlobalErrorHandlerIntegration.Helper;
using GlobalErrorHandlerIntegration.IServices;
using GlobalErrorHandlerIntegration.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace GlobalErrorHandlerIntegration.Services
{
    public class TelexErrorLogger : ITelexErrorLogger
    {

        // Thread-safe queue to store error logs (as JSON strings)
        private readonly ConcurrentQueue<string> _errorQueue = new ConcurrentQueue<string>();
        private static readonly ConcurrentDictionary<string, ErrorDetail> _errorStorage = new();

        private readonly HttpClient _httpClient;
        private readonly ILogger<TelexErrorLogger> _logger;
        private readonly string _telexWebhookUrl;

        public TelexErrorLogger(ILogger<TelexErrorLogger> logger, IOptions<TelexSettings> telexSettings, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _telexWebhookUrl = telexSettings.Value.WebhookUrl;

            if (string.IsNullOrWhiteSpace(_telexWebhookUrl))
            {
                throw new ArgumentNullException(nameof(telexSettings.Value.WebhookUrl), "Telex Webhook URL is missing.");
            }
        }

        //public void EnqueueError(ErrorDetail error)
        //{
        //    _errorStorage[error.ErrorId] = error; // Store error
        //}

        //public ErrorDetail? GetErrorById(string errorId)
        //{
        //    return _errorStorage.TryGetValue(errorId, out var error) ? error : null;
        //}

        //public void EnqueueError(ErrorDetail errorDetail)
        //{
        //    // JsonSerializer the error detail to JSON
        //    var errorJson = JsonSerializer.Serialize(errorDetail);
        //    _errorQueue.Enqueue(errorJson);

        //}

        public async Task ProcessErrorFormattingRequest(ErrorFormatPayload payload)
        {
           // Deserialize error message payload back into ErrorDetail to format it.
            ErrorDetail? errorDetail;
            try
            {
                errorDetail = JsonSerializer.Deserialize<ErrorDetail>(
                    payload.Message,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (errorDetail == null)
                    throw new Exception("Deserialized error message is null.");
            }
            catch (JsonException)
            {
                throw new Exception("Malformed JSON in message payload.");
            }

            // Format the error message
            var fornattedError = FormatErrorReport(errorDetail, payload.Settings);

            var telexMessage = new
            {
                event_name = "Error Report",
                message = fornattedError,
                status = "success",
                username = "Global Error Handler"
            };

            //// Serialize the telexMessage object to JSON.
            var telexContentJson = JsonSerializer.Serialize(telexMessage);

            var content = new StringContent(telexContentJson, Encoding.UTF8, "application/json");

            
            bool success = false;
            int retryCount = 0;

            while (retryCount < 3 && !success)
            {
                try
                {
                    // Send the error log to the telex webhook URL defined in configuration
                    var response = await _httpClient.PostAsync(_telexWebhookUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Error report successfully formatted and sent to telex: {responseContent}");

                        success = true; // Exit retry loop if successful
                    }
                    else
                    {
                        retryCount++;
                        _logger.LogWarning($"Retry {retryCount + 1}: Failed to send the formatted error log to Telex. Status: {response.StatusCode}");
                        await Task.Delay(2000); // Wait 2 seconds before retrying
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError(ex, $"Retry {retryCount + 1}: Exception occurred while sending the formatted error log to Telex.");
                    await Task.Delay(2000);
                }
            }
            
        }

        public async Task SendInitialErrorReport(ErrorDetail error)
        {          
           
            // Define the payload (same structure as your Go response)
            var payload = new
            {
                event_name = "Error Report",
                message = error,
                status = "success",
                username = "Global Error Handler"
            };

            // Serialize payload to JSON
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Send the error log to the target URL defined in configuration
            int retryCount = 3;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    var response = await _httpClient.PostAsync(_telexWebhookUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Error report successfully sent to telex: {responseContent}");

                        return;
                    }
                    _logger.LogWarning("Attempt {Attempt}: trigger error log in telex. Status code: {StatusCode}", i + 1, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Attempt {Attempt}: Exception while sending error report.", i + 1);
                }
                await Task.Delay(1000); // Wait 1 second before retrying
            }
        }

        private string FormatErrorReport(ErrorDetail error, List<Setting> settings)
        {
            // Check if the client enabled stack trace inclusion
            bool includeStackTrace = settings.Any(s => s.Label == "Include StackTrace" && s.Type == "checkbox" && s.Default == "true");

            // Check if the client enabled inner exception inclusion
            bool includeInnerException = settings.Any(s => s.Label == "Include InnerException" && s.Type == "checkbox" && s.Default == "true");

            // Get max stack trace length
            int maxStackTraceLength = settings
                .Where(s => s.Label == "Max-Error Message-Length" && s.Type == "number")
                .Select(s => int.TryParse(s.Default, out int length) ? length : 210)
                .FirstOrDefault(); 

            // Build a readable message report.
            var sb = new StringBuilder();
            sb.AppendLine($"Error Timestamp: {error.Timestamp}\n");
            sb.AppendLine($"Exception: {error.ExceptionType}\n");
            sb.AppendLine($"Message: {error.Message}\n");
            if (includeInnerException && !string.IsNullOrEmpty(error.InnerExceptionMessage))
            {
                sb.AppendLine($"⚠️ Inner Exception: {error.InnerExceptionMessage}\n");
            }
            sb.AppendLine($"HTTP Method: {error.HttpMethod} ||  URL: {error.Url}  ||  Status Code: {error.StatusCode.ToString()}\n");           

            // Optionally include a truncated stack trace for readability.
            if (includeStackTrace && !string.IsNullOrEmpty(error.StackTrace))
            {
                var shortStack = error.StackTrace.Length > maxStackTraceLength
                    ? error.StackTrace.Substring(0, maxStackTraceLength) + "..."
                    : error.StackTrace;
                sb.AppendLine($"📌 Stack Trace: {shortStack}\n");
            }
            return sb.ToString();
        }
    }
}

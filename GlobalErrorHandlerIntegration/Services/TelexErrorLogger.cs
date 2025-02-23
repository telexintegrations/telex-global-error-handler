using GlobalErrorHandlerIntegration.Helpers;
using GlobalErrorHandlerIntegration.IServices;
using GlobalErrorHandlerIntegration.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GlobalErrorHandlerIntegration.Services
{
    public class TelexErrorLogger : ITelexErrorLogger
    {

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

        public async Task SendInitialErrorReport(ErrorDetail error)
        {
            
            // Serialize the error object properly
            var errorPayload = JsonSerializer.Serialize(error);

            // Define the payload (same structure as your Go response)
            var payload = new
            {
                event_name = "Error Report",
                message = errorPayload,
                status = "success",
                username = "Global Error Handler"
            };

            // Serialize payload to JSON
            var jsonPayload = JsonSerializer.Serialize(payload);


            // Send the error log to the target URL defined in configuration with a failure retry of 3 counts
            int retryCount = 3;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_telexWebhookUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Error log successfully sent to telex.\n" +
                            $"Response: {responseContent}");

                        return;
                    }
                    _logger.LogWarning("Attempt {Attempt}: failed to send error log to telex.\n" +
                        "Status code: {StatusCode}", i + 1, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Attempt {Attempt}: An Exception while sending error report.", i + 1);
                }
                await Task.Delay(1000); // Wait 1 second before retrying
            }
        }

        public string ProcessErrorFormattingRequest(ErrorFormatPayload payload)
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

            if (string.IsNullOrWhiteSpace(fornattedError))
            {
                _logger.Log(LogLevel.Information, "Failed to format error report....");
                throw new Exception("Error formatting failed.");    
            }

            _logger.Log(LogLevel.Information, "Successfully formatted error report....");           

            return fornattedError;           
            
        }

        public string FormatErrorReport(ErrorDetail error, List<Setting> settings)
        {
            _logger.LogInformation("Formatting error message...");

            // Check if the client enabled stack trace inclusion
            bool includeStackTrace = settings
                .Where(s => s.Label == "Include StackTrace" && s.Type == "checkbox")
                .Select(s => s.Default.ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            // Check if the client enabled inner exception inclusion
            bool includeInnerException = settings
                .Where(s => s.Label == "Include InnerException" && s.Type == "checkbox")
                .Select(s => s.Default.ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            // Get max stack trace length
            int maxStackTraceLength = settings
                .Where(s => s.Label == "Max-Error Message-Length" && s.Type == "number")
                .Select(s => int.TryParse(s.Default.ToString(), out int length) ? length : 0)
                .FirstOrDefault();

            // Build a readable message report.
            var sb = new StringBuilder();
            sb.AppendLine($"🆔 Error Id = {error.ErrorId}\n");
            sb.AppendLine($"⏳ Error Timestamp: {error.Timestamp}\n");
            sb.AppendLine($"⚠️ Exception: {error.ExceptionType}\n");
            sb.AppendLine($"💬 Message: {error.Message}\n");

            // Include a innerException if included in settings
            if (includeInnerException && !string.IsNullOrEmpty(error.InnerExceptionMessage))
            {
                sb.AppendLine($"🚨 Inner Exception: {error.InnerExceptionMessage}\n");
            }
            sb.AppendLine($"🌐 HTTP Method: {error.HttpMethod} || 🔗 URL: {error.Url}  || 🛑 Status Code: {error.StatusCode.ToString()}\n");           

            // Include stack trace and truncate if included in settings
            if (includeStackTrace && !string.IsNullOrEmpty(error.StackTrace))
            {
                var stackLength = maxStackTraceLength > 0 && error.StackTrace.Length > maxStackTraceLength 
                    ? error.StackTrace.Substring(0, maxStackTraceLength) + "..."
                    : error.StackTrace;
                sb.AppendLine($"📌 Stack Trace: {stackLength}\n");
            }
            return sb.ToString();
        }
    }
}

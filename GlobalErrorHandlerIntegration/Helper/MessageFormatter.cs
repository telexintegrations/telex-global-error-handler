using GlobalErrorHandlerIntegration.Models;
using System.Text;

namespace GlobalErrorHandlerIntegration.Helper
{
    public class MessageFormatter
    {
        private string FormatErrorReport(ErrorDetail error)
        {
            // Build a readable message report.
            var sb = new StringBuilder();
            sb.AppendLine($"Timestamp: {error.Timestamp}");
            sb.AppendLine($"Exception: {error.ExceptionType}");
            sb.AppendLine($"Message: {error.Message}");
            if (!string.IsNullOrEmpty(error.InnerExceptionMessage))
            {
                sb.AppendLine($"Inner Exception: {error.InnerExceptionMessage}");
            }
            // Optionally include a truncated stack trace for readability.
            if (!string.IsNullOrEmpty(error.StackTrace))
            {
                var shortStack = error.StackTrace.Length > 200
                    ? error.StackTrace.Substring(0, 200) + "..."
                    : error.StackTrace;
                sb.AppendLine($"Stack Trace: {shortStack}");
            }
            sb.AppendLine($"HTTP Method: {error.HttpMethod}  URL: {error.Url}");
            sb.AppendLine(error.StatusCode.ToString());
            return sb.ToString();
        }

    }
}

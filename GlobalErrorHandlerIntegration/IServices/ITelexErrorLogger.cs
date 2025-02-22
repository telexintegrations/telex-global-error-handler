using GlobalErrorHandlerIntegration.Models;

namespace GlobalErrorHandlerIntegration.IServices
{
    public interface ITelexErrorLogger
    {

        /// <summary>
        /// Processs the formatting of the error logs and sends them to the configurred telex webhook.
        /// </summary>
        string ProcessErrorFormattingRequest(ErrorFormatPayload payload);

        /// <summary>
        /// Sends the error initially to the configurred telex webhook.
        /// </summary>
        Task SendInitialErrorReport(ErrorDetail errorDetail);
    }
}

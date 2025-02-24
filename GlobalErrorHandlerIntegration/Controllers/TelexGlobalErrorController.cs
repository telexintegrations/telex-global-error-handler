using GlobalErrorHandlerIntegration.Helpers;
using GlobalErrorHandlerIntegration.IServices;
using GlobalErrorHandlerIntegration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GlobalErrorHandlerIntegration.Controllers
{
    [Route("api/v1/telex-global-error-handler")]
    [ApiController]
    public class TelexGlobalErrorController : ControllerBase
    {

        private readonly ITelexErrorLogger _errorLogger;
        private readonly ILogger<TelexGlobalErrorController> _logger;

        public TelexGlobalErrorController(ITelexErrorLogger errorLogger, ILogger<TelexGlobalErrorController> logger)
        {
            _errorLogger = errorLogger;
            _logger = logger;
        }

        [HttpGet("simulate-error")]
        public async Task<IActionResult> Get()
        {
            throw new InvalidOperationException("This is a test exception to simulate an error.");
        }   
        
        
        [HttpGet("integration.json")]
        public IActionResult GetIntegrationConfig()
        {
            try
            {
                var integrationJson = IntegrationJsonLoader.LoadTelexIntegration();

                if (string.IsNullOrWhiteSpace(integrationJson))
                {
                    return NotFound("Integration configuration is empty.");
                }

                return Ok(integrationJson);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Integration configuration not found.");
            }
        }
        
        
        [HttpPost("format-message")]
        public async Task<IActionResult> FormatErrorMessage([FromBody] ErrorFormatPayload payload)
        {
            _logger.LogInformation($"Received error payload for formatting");

            if (string.IsNullOrWhiteSpace(payload.Message) || !payload.Settings.Any())
            {
               _logger.LogInformation("Invalid error payload: Message payload or settings cannot be empty");
                return BadRequest();
            }

            _logger.LogInformation($"Proceeding to format error message {payload.Message}");
            // Process the error report.
            var formattedJson = _errorLogger.ProcessErrorFormattingRequest(payload);

            if (string.IsNullOrEmpty(formattedJson))
            {
                _logger.LogInformation("Failed to format error message");
                return BadRequest();
            }

            return Ok(new
            {
                message = formattedJson
               
            });
        }   
    }
}

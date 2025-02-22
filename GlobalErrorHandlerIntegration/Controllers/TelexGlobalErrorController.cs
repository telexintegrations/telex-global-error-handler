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

        public TelexGlobalErrorController(ITelexErrorLogger errorLogger)
        {
            _errorLogger = errorLogger;
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
            
            if (string.IsNullOrWhiteSpace(payload.Message) || !payload.Settings.Any())
            {
               throw new ArgumentException("Invalid error payload.");
            }

            // Process the error report in a seperate thread.
            var formattedJson = _errorLogger.ProcessErrorFormattingRequest(payload);

            return Ok(new
            {
                message = formattedJson
               
            });
        }   
    }
}

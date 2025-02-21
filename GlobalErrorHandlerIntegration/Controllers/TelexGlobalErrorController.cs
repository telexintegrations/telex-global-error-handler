using GlobalErrorHandlerIntegration.IServices;
using GlobalErrorHandlerIntegration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GlobalErrorHandlerIntegration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelexGlobalErrorController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, ErrorDetail> _errorStorage = new();

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
        
        [HttpPost("format-message")]
        public async Task<IActionResult> FormatErrorReport([FromBody] ErrorFormatPayload payload)
        {
            
            if (string.IsNullOrWhiteSpace(payload.Message) || !payload.Settings.Any())
            {
                return BadRequest("Invalid error payload.");
            }

            // Process the error report in a seperate thread.
            Task.Run(async () => _errorLogger.ProcessErrorFormattingRequest(payload));

            return StatusCode(202, "Accepted");
        }   
    }
}

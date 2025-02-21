using System.Collections.Generic;
using System.Threading.Tasks;
using GlobalErrorHandlerIntegration.Controllers;
using GlobalErrorHandlerIntegration.IServices;
using GlobalErrorHandlerIntegration.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GlobalErrorHandlerIntegration.Tests.Controllers
{
    public class TelexGlobalErrorControllerTests
    {
        private readonly Mock<ITelexErrorLogger> _mockLogger;
        private readonly TelexGlobalErrorController _controller;

        public TelexGlobalErrorControllerTests()
        {
            _mockLogger = new Mock<ITelexErrorLogger>();
            _controller = new TelexGlobalErrorController(_mockLogger.Object);
        }

        [Fact]
        public void GetIntegrationConfig_FileNotFound_ReturnsNotFound()
        {
            var result = _controller.GetIntegrationConfig();
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Integration configuration not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task FormatErrorReport_InvalidPayload_ReturnsBadRequest()
        {
            var payload = new ErrorFormatPayload { Message = "", Settings = new List<Setting>() };

            var result = await _controller.FormatErrorReport(payload);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid error payload.", badRequestResult.Value);
        }
    }
}

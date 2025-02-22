using System.Collections.Generic;
using System.IO;
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
        private readonly string _jsonFilePath;

        public TelexGlobalErrorControllerTests()
        {
            _mockLogger = new Mock<ITelexErrorLogger>();
            _controller = new TelexGlobalErrorController(_mockLogger.Object);

            // Path to the test JSON file
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Helpers", "integration.json");
        }

        private void SetupTestJsonFile(string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_jsonFilePath)); // Ensure directory exists
            File.WriteAllText(_jsonFilePath, content);
        }

        private void CleanupTestJsonFile()
        {
            if (File.Exists(_jsonFilePath))
            {
                File.Delete(_jsonFilePath);
            }
        }

        [Fact]
        public void GetIntegrationConfig_FileNotFound_ReturnsNotFound()
        {
            // Ensure the file does NOT exist
            CleanupTestJsonFile();

            // Act
            var result = _controller.GetIntegrationConfig();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Integration configuration not found.", notFoundResult.Value);
        }

        [Fact]
        public void GetIntegrationConfig_ReturnsValidJson()
        {
            // Arrange - Create a test JSON file
            var expectedJson = "{ \"app_name\": \"Global Error Handler\" }";
            SetupTestJsonFile(expectedJson);

            try
            {
                // Act
                var result = _controller.GetIntegrationConfig() as OkObjectResult;

                // Assert
                Assert.NotNull(result);
                Assert.Equal(200, result.StatusCode);
                Assert.Equal(expectedJson, result.Value);
            }
            finally
            {
                // Cleanup test file
                CleanupTestJsonFile();
            }
        }

        [Fact]
        public async Task FormatErrorMessage_ValidPayload_ReturnsFormattedMessage()
        {
            // Arrange
            var payload = new ErrorFormatPayload
            {
                Message = "{\"ErrorId\": \"12345\", \"Message\": \"Test error\"}",
                Settings = new List<Setting>
            {
                new Setting { Label = "Include StackTrace", Type = "checkbox", Default = "true" }
            }
            };

            _mockLogger
                .Setup(x => x.ProcessErrorFormattingRequest(payload))
                .Returns("Formatted error message");

            // Act
            var result = await _controller.FormatErrorMessage(payload) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var response = result.Value as dynamic;
            Assert.Equal("Formatted error message", response.message);

            _mockLogger.Verify(x => x.ProcessErrorFormattingRequest(payload), Times.Once);
        }

        [Fact]
        public async Task FormatErrorMessage_InvalidPayload_ThrowsArgumentException()
        {
            // Arrange
            var payload = new ErrorFormatPayload
            {
                Message = "", // Empty message
                Settings = new List<Setting>() // No settings
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _controller.FormatErrorMessage(payload));
            Assert.Equal("Invalid error payload.", ex.Message);

            _mockLogger.Verify(x => x.ProcessErrorFormattingRequest(It.IsAny<ErrorFormatPayload>()), Times.Never);
        }
    }
}

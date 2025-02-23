using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GlobalErrorHandlerIntegration.Helpers;
using GlobalErrorHandlerIntegration.Models;
using GlobalErrorHandlerIntegration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace GlobalErrorHandlerIntegration.Tests.Services
{
    public class TelexErrorLoggerTests
    {
        private readonly Mock<ILogger<TelexErrorLogger>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly TelexErrorLogger _errorLogger;
        private readonly string _telexWebhookUrl = "https://example.com/webhook";
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;


        public TelexErrorLoggerTests()
        {
            _mockLogger = new Mock<ILogger<TelexErrorLogger>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            var mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(mockHttpClient);

            var options = Options.Create(new TelexSettings { WebhookUrl = _telexWebhookUrl});
            _errorLogger = new TelexErrorLogger(_mockLogger.Object, options, _mockHttpClientFactory.Object);
        }

        [Fact]
        public void ProcessErrorFormattingRequest_ValidPayload_ReturnsFormattedError()
        {
            // Arrange
            var payload = new ErrorFormatPayload
            {
                Message = JsonSerializer.Serialize(new ErrorDetail
                {
                    ErrorId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    ExceptionType = "System.NullReferenceException",
                    Message = "Object reference not set to an instance of an object.",
                    HttpMethod = "GET",
                    Url = "https://api.example.com/resource",
                    StatusCode = 500,
                    StackTrace = "at ExampleClass.Method()",
                    InnerExceptionMessage = "Inner exception occurred."
                }),
                Settings = new List<Setting>
                {
                    new Setting { Label = "Include StackTrace", Type = "checkbox", Default = "true" },
                    new Setting { Label = "Include InnerException", Type = "checkbox", Default = "true" },
                    new Setting { Label = "Max-Error Message-Length", Type = "number", Default = "100" }
                }
            };

            // Act
            var result = _errorLogger.ProcessErrorFormattingRequest(payload);

            // Assert
            Assert.Contains("Exception: System.NullReferenceException", result);
            Assert.Contains("Object reference not set to an instance of an object.", result);
            Assert.Contains("📌 Stack Trace:", result);
            Assert.Contains("⚠️ Inner Exception:", result);
        }

        [Fact]
        public void ProcessErrorFormattingRequest_InvalidJson_ThrowsException()
        {
            // Arrange
            var payload = new ErrorFormatPayload { Message = "Invalid JSON", Settings = new List<Setting>() };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _errorLogger.ProcessErrorFormattingRequest(payload));
            Assert.Equal("Malformed JSON in message payload.", exception.Message);
        }

        [Fact]
        public void FormatErrorReport_ValidErrorAndSettings_ReturnsFormattedString()
        {
            // Arrange
            var error = new ErrorDetail
            {
                ErrorId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                ExceptionType = "System.InvalidOperationException",
                Message = "Invalid operation occurred.",
                HttpMethod = "POST",
                Url = "https://api.example.com/action",
                StatusCode = 400,
                StackTrace = "at ExampleClass.Method()",
                InnerExceptionMessage = "Inner error message."
            };

            var settings = new List<Setting>
            {
                new Setting { Label = "Include StackTrace", Type = "checkbox", Default = "true" },
                new Setting { Label = "Include InnerException", Type = "checkbox", Default = "false" },
                new Setting { Label = "Max-Error Message-Length", Type = "number", Default = "50" }
            };

            // Act
            var result = _errorLogger.FormatErrorReport(error, settings);

            // Assert
            Assert.Contains("Exception: System.InvalidOperationException", result);
            Assert.Contains("Message: Invalid operation occurred.", result);
            Assert.DoesNotContain("⚠️ Inner Exception:", result);
            Assert.Contains("📌 Stack Trace:", result);
        }

        
        [Fact]
        public void FormatErrorReport_StackTraceExceedsMaxLength_IsTruncated()
        {
            // Arrange
            var error = new ErrorDetail
            {
                ErrorId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                ExceptionType = "System.Exception",
                Message = "Test error message.",
                HttpMethod = "DELETE",
                Url = "https://api.example.com/delete",
                StatusCode = 500,
                StackTrace = new string('X', 200) // Long stack trace
            };

            var settings = new List<Setting>
            {
                new Setting { Label = "Include StackTrace", Type = "checkbox", Default = "true" },
                new Setting { Label = "Max-Error Message-Length", Type = "number", Default = "100" }
            };

            // Act
            var result = _errorLogger.FormatErrorReport(error, settings);

            // Assert
            Assert.Contains("📌 Stack Trace:", result);
            Assert.Contains("...", result);  // Truncated stack trace
        }


        [Fact]
        public async Task SendInitialErrorReport_SuccessfulRequest_LogsInformation()
        {
            var error = new ErrorDetail
            {
                Timestamp = DateTime.UtcNow,
                ExceptionType = "InvalidOperationException",
                Message = "Test error",
                StatusCode = 500
            };

            await _errorLogger.SendInitialErrorReport(error);

            _mockLogger.Verify(log => log.LogInformation(It.IsAny<string>()), Times.Once);
        }


        [Fact]
        public async Task SendInitialErrorReport_SuccessfulRequest_StopsRetrying()
        {
            // Arrange
            var errorDetail = new ErrorDetail
            {
                ErrorId = "123",
                Message = "Test error",
                Timestamp = DateTime.UtcNow,
                ExceptionType = "InvalidOperationException",
                StatusCode = 500
            };

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\": \"success\"}", Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(expectedResponse);

            // Act
            await _errorLogger.SendInitialErrorReport(errorDetail);

            // Assert
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(), // Should only call once because the request was successful
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception?, string>>()
                ),
                Times.AtLeastOnce() // Ensures logging happened
            );
        }

        [Fact]
        public async Task SendInitialErrorReport_FailsThreeTimes_LogsWarningsAndErrors()
        {
            // Arrange
            var errorDetail = new ErrorDetail
            {
                ErrorId = "123",
                Message = "Test error"
            };

            var failedResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(failedResponse);

            // Act
            await _errorLogger.SendInitialErrorReport(errorDetail);

            // Assert
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(3), // Should retry exactly 3 times
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception?, string>>()
                ),
                Times.Exactly(3) // Logs a warning for each retry
            );

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception?, string>>()
                ),
                Times.Exactly(3) // Logs an error for each failure
            );
        }
    }
   
}

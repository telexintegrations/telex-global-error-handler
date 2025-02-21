using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GlobalErrorHandlerIntegration.Helper;
using GlobalErrorHandlerIntegration.Models;
using GlobalErrorHandlerIntegration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GlobalErrorHandlerIntegration.Tests.Services
{
    public class TelexErrorLoggerTests
    {
        private readonly Mock<ILogger<TelexErrorLogger>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly TelexErrorLogger _errorLogger;

        public TelexErrorLoggerTests()
        {
            _mockLogger = new Mock<ILogger<TelexErrorLogger>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            var mockHttpClient = new HttpClient(new FakeHttpMessageHandler());
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(mockHttpClient);

            var options = Options.Create(new TelexSettings { WebhookUrl = "http://test-webhook.com" });
            _errorLogger = new TelexErrorLogger(_mockLogger.Object, options, _mockHttpClientFactory.Object);
        }

        [Fact]
        public async Task ProcessErrorFormattingRequest_InvalidJson_ThrowsException()
        {
            var payload = new ErrorFormatPayload { Message = "{ invalid json }", Settings = new List<Setting>() };

            await Assert.ThrowsAsync<Exception>(async () => await _errorLogger.ProcessErrorFormattingRequest(payload));
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
    }

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ \"status\": \"success\" }", Encoding.UTF8, "application/json")
            });
        }
    }
}

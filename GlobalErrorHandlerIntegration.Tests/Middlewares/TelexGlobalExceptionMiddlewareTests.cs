using System;
using System.Net;
using System.Threading.Tasks;
using GlobalErrorHandlerIntegration.IServices;
using GlobalErrorHandlerIntegration.Middlewares;
using GlobalErrorHandlerIntegration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GlobalErrorHandlerIntegration.Tests.Middlewares
{
    public class TelexGlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<TelexGlobalExceptionMiddleware>> _mockLogger;
        private readonly Mock<ITelexErrorLogger> _mockTelexLogger;
        private readonly RequestDelegate _next;

        public TelexGlobalExceptionMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<TelexGlobalExceptionMiddleware>>();
            _mockTelexLogger = new Mock<ITelexErrorLogger>();
            _next = (HttpContext _) => throw new InvalidOperationException("Test Exception");
        }

        [Fact]
        public async Task Middleware_CatchesException_ReturnsInternalServerError()
        {
            var context = new DefaultHttpContext();
            var middleware = new TelexGlobalExceptionMiddleware(_next, _mockLogger.Object, _mockTelexLogger.Object);

            await middleware.Invoke(context);

            Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
            _mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
            _mockTelexLogger.Verify(x => x.SendInitialErrorReport(It.IsAny<ErrorDetail>()), Times.Once);
        }
    }
}

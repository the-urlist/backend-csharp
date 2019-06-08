using FakeItEasy;
using LinkyLink.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace LinkyLink.Tests
{
    public class ValidatePageTests : TestBase
    {
        [Fact]
        public async Task ValidatePage_Empty_Payload_Returns_BadRequest()
        {
            // Arrange
            HttpRequest req = this.AuthenticatedRequest;
            req.Body = this.GetHttpRequestBodyStream("");

            ILogger fakeLogger = A.Fake<ILogger>();

            // Act
            IActionResult result = await _linkOperations.ValidatePage(req, fakeLogger);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);

            BadRequestObjectResult badRequestResult = result as BadRequestObjectResult;
            Assert.IsType<ProblemDetails>(badRequestResult.Value);

            ProblemDetails problemDetails = badRequestResult.Value as ProblemDetails;

            Assert.Equal("Could not validate links", problemDetails.Title);
            Assert.Equal(problemDetails.Status, StatusCodes.Status400BadRequest);

            A.CallTo(fakeLogger)
              .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>("logLevel") == LogLevel.Error)
              .MustHaveHappened();
        }
    }
}

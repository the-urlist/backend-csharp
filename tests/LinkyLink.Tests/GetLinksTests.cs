using Xunit;
using LinkyLink.Tests.Helpers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using AutoFixture;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents;

namespace LinkyLink.Tests
{
    public class GetLinksTest : TestBase
    {
        [Fact]
        public void GetLinks_Emtpy_Collection_Should_Return_NotFound()
        {
            // Arrange
            IEnumerable<LinkBundle> docs = Enumerable.Empty<LinkBundle>();
            ILogger fakeLogger = A.Fake<ILogger>();

            // Act
            IActionResult result = LinkOperations.GetLinks(this.DefaultRequest, docs, "vanityUrl", fakeLogger);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            A.CallTo(fakeLogger)
                .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>("logLevel") == LogLevel.Information)
                .MustHaveHappened();
        }

        [Fact]
        public void GetLinks_Non_Emtpy_Collection_Should_Return_Single_Document()
        {
            // Arrange
            var docs = Fixture.CreateMany<LinkBundle>(1);

            // Act
            IActionResult result = LinkOperations.GetLinks(this.DefaultRequest, docs, string.Empty, A.Dummy<ILogger>());

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(docs.Single(), (result as OkObjectResult).Value);
        }

        [Fact]
        public void GetBundlesForUser_Request_Missing_Auth_Credentials_Should_Return_UnAuthorized()
        {
            // Arrange
            ILogger fakeLogger = A.Fake<ILogger>();

            // Act
            IActionResult result = LinkOperations.GetBundlesForUser(this.DefaultRequest, A.Dummy<IEnumerable<Document>>(), "userid", fakeLogger);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);

            A.CallTo(fakeLogger)
               .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>("logLevel") == LogLevel.Information)
               .MustHaveHappened();
        }

        [Fact]
        public void GetBundlesForUser_Authenticated_Request_With_Emtpy_Collection_Should_Return_NotFound()
        {
            // Arrange
            IEnumerable<Document> docs = Enumerable.Empty<Document>();
            ILogger fakeLogger = A.Fake<ILogger>();

            // Act
            IActionResult result = LinkOperations.GetBundlesForUser(this.AuthenticatedRequest, docs, "userid", fakeLogger);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            A.CallTo(fakeLogger)
                .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>("logLevel") == LogLevel.Information)
                .MustHaveHappened();
        }

        [Fact]
        public void GetBundlesForUser_Authenticated_Request_With_Collection_Should_Return_Formatted_Results()
        {
            // Arrange
            var docs = Fixture.CreateMany<Document>();

            // Act
            IActionResult result = LinkOperations.GetBundlesForUser(this.AuthenticatedRequest, docs, "userid", A.Dummy<ILogger>());

            //Assert
            Assert.IsType<OkObjectResult>(result);

            OkObjectResult okResult = result as OkObjectResult;
            IEnumerable<dynamic> resultData = okResult.Value as IEnumerable<dynamic>;

            Assert.Equal(docs.Count(), resultData.Count());

            foreach (dynamic item in resultData)
            {
                Assert.True(item.GetType().GetProperty("userId") != null);
                Assert.True(item.GetType().GetProperty("vanityUrl") != null);
                Assert.True(item.GetType().GetProperty("description") != null);
                Assert.True(item.GetType().GetProperty("linkCount") != null);
            }
        }
    }
}
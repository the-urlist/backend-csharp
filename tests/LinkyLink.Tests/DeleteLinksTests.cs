using AutoFixture;
using FakeItEasy;
using LinkyLink.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LinkyLink.Tests
{
    public class DeleteLinksTests : TestBase
    {
        [Fact]
        public async Task DeleteLink_Request_Missing_Auth_Credentials_Should_Return_UnAuthorized()
        {
            // Arrange
            IEnumerable<Document> docs = Fixture.CreateMany<Document>();
            ILogger dummyLogger = A.Dummy<ILogger>();
            Binder dummyBinder = A.Fake<Binder>();

            RemoveAuthFromContext();

            // Act
            IActionResult result = await _linkOperations.DeleteLink(this.DefaultRequest, docs, null, "vanityUrl", dummyBinder, dummyLogger);
            AddAuthToContext();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeleteLink_Authenticated_Request_With_Emtpy_Collection_Should_Return_NotFound()
        {
            // Arrange
            IEnumerable<Document> docs = Enumerable.Empty<Document>();
            ILogger fakeLogger = A.Fake<ILogger>();
            Binder fakeBinder = A.Fake<Binder>();

            // Act
            IActionResult result = await _linkOperations.DeleteLink(this.AuthenticatedRequest, docs, null, "userid", fakeBinder, fakeLogger);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            A.CallTo(fakeLogger)
                .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>("logLevel") == LogLevel.Information)
                .MustHaveHappened();
        }

        [Fact]
        public async Task DeleteLink_User_Cant_Remove_Document_Owned_By_Others_Should_Return_Forbidden()
        {
            // Arrange
            IEnumerable<Document> docs = Fixture.CreateMany<Document>(1);
            ILogger fakeLogger = A.Fake<ILogger>();
            Binder fakeBinder = A.Fake<Binder>();

            // Act
            IActionResult result = await _linkOperations.DeleteLink(this.AuthenticatedRequest, docs, null, "userid", fakeBinder, fakeLogger);

            // Assert
            Assert.IsType<StatusCodeResult>(result);

            StatusCodeResult statusResult = result as StatusCodeResult;
            Assert.Equal(statusResult.StatusCode, StatusCodes.Status403Forbidden);

            A.CallTo(fakeLogger)
                .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>("logLevel") == LogLevel.Warning)
                .MustHaveHappened();
        }
    }
}

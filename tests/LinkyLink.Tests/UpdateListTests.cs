using AutoFixture;
using FakeItEasy;
using FakeItEasy.Core;
using LinkyLink.Models;
using LinkyLink.Tests.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LinkyLink.Tests
{
    public class UpdateListTests : TestBase
    {
        [Fact]
        public async Task UpdateList_Request_Missing_Auth_Credentials_Should_Return_UnAuthorized()
        {
            // Arrange            
            var docs = Fixture.CreateMany<LinkBundle>();
            RemoveAuthFromContext();

            // Act
            IActionResult result = await _linkOperations.UpdateList(this.DefaultRequest, docs, null, "vanityUrl", A.Dummy<ILogger>());
            AddAuthToContext();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateList_Request_With_Emtpy_Collection_Should_Return_NotFound()
        {
            // Arrange
            IEnumerable<LinkBundle> docs = Enumerable.Empty<LinkBundle>();
            ILogger fakeLogger = A.Fake<ILogger>();

            // Act
            IActionResult result = await _linkOperations.UpdateList(this.AuthenticatedRequest, docs, null, "vanityUrl", fakeLogger);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            A.CallTo(fakeLogger)
                .Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>("logLevel") == LogLevel.Information)
                .MustHaveHappened();
        }

        [Fact]
        public async Task UpdateList_Applies_JsonPatch_To_Bundle()
        {
            // Arrange
            JsonPatchDocument<LinkBundle> patchReqDocument = new JsonPatchDocument<LinkBundle>();
            patchReqDocument.Replace(d => d.Description, "Description");
            patchReqDocument.Replace(d => d.Links, this.Fixture.CreateMany<IDictionary<string, string>>());

            HttpRequest req = this.AuthenticatedRequest;
            req.Body = this.GetHttpRequestBodyStream(JsonConvert.SerializeObject(patchReqDocument));

            IEnumerable<LinkBundle> docs = this.Fixture.CreateMany<LinkBundle>(1);
            IDocumentClient docClient = this.Fixture.Create<IDocumentClient>();

            LinkBundle captured = null;
            A.CallTo(() => docClient.UpsertDocumentAsync(A<Uri>.Ignored, A<LinkBundle>.Ignored, A<RequestOptions>.Ignored, false, default(CancellationToken)))
                .Invokes((IFakeObjectCall callOb) =>
                {
                    captured = callOb.Arguments[1] as LinkBundle;
                });
            string vanityUrl = "vanity";

            // Act
            IActionResult result = await _linkOperations.UpdateList(req, docs, docClient, vanityUrl, A.Dummy<ILogger>());

            // Assert
            Assert.Equal("Description", captured.Description);
            Assert.IsType<NoContentResult>(result);
        }

        [Theory]
        [InlineData("", typeof(BadRequestResult))]
        [InlineData("[]", typeof(NoContentResult))]
        [InlineData("{}", typeof(BadRequestResult))]
        public async Task UpdateList_Empty_Operation_Does_Not_Call_DocumentClient(string payload, Type returnType)
        {
            // Arrange
            HttpRequest req = this.AuthenticatedRequest;
            req.Body = this.GetHttpRequestBodyStream(payload);

            IEnumerable<LinkBundle> docs = this.Fixture.CreateMany<LinkBundle>(1);
            IDocumentClient docClient = this.Fixture.Create<IDocumentClient>();
            ILogger logger = this.Fixture.Create<ILogger>();
            string vanityUrl = "vanity";

            // Act
            IActionResult result = await _linkOperations.UpdateList(req, docs, docClient, vanityUrl, logger);

            // Assert
            Assert.IsType(returnType, result);

            A.CallTo(docClient)
                .Where((IFakeObjectCall call) => call.Method.Name == "UpsertDocumentAsync")
                .MustNotHaveHappened();

            A.CallTo(logger)
                .Where((IFakeObjectCall call) => call.Method.Name == "Log" && call.GetArgument<LogLevel>("logLevel") == LogLevel.Error)
                .MustHaveHappened();
        }
    }
}

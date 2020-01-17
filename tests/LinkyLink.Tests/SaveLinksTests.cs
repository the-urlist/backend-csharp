using AutoFixture;
using FakeItEasy;
using LinkyLink.Models;
using LinkyLink.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LinkyLink.Tests
{
    public class SaveLinksTests : TestBase
    {
        [Fact]
        public async Task SaveLinks_Empty_Payload_Returns_BadRequest()
        {
            // Arrange
            ILogger fakeLogger = A.Fake<ILogger>();
            HttpRequest req = this.DefaultRequest;
            req.Body = this.GetHttpRequestBodyStream("");
            IAsyncCollector<LinkBundle> collector = A.Fake<IAsyncCollector<LinkBundle>>();

            // Act
            IActionResult result = await _linkOperations.SaveLinks(req, collector, fakeLogger);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            A.CallTo(() => collector.AddAsync(A<LinkBundle>.Ignored, CancellationToken.None)).MustNotHaveHappened();
        }

        [Theory]
        [InlineData("new url")]
        [InlineData("url.com")]
        [InlineData("my@$(Surl@F(@LV((")]
        [InlineData("someurl/")]
        [InlineData(".com.com")]
        public async Task SaveLinks_Returns_BadRequest_If_Vanity_Url_Fails_Regex(string vanityUrl)
        {
            // Arrange
            ILogger fakeLogger = A.Fake<ILogger>();
            HttpRequest req = this.DefaultRequest;

            LinkBundle payload = this.Fixture.Create<LinkBundle>();
            payload.VanityUrl = vanityUrl;

            req.Body = this.GetHttpRequestBodyStream(JsonConvert.SerializeObject(payload));
            IAsyncCollector<LinkBundle> collector = A.Fake<IAsyncCollector<LinkBundle>>();

            // Act
            IActionResult result = await _linkOperations.SaveLinks(req, collector, fakeLogger);

            // Assert
            Assert.IsType<BadRequestResult>(result);
            A.CallTo(() => collector.AddAsync(A<LinkBundle>.Ignored, CancellationToken.None)).MustNotHaveHappened();
        }

        [Fact]
        public async Task SaveLinks_Valid_Payload_Returns_CreateRequest()
        {
            // Arrange
            ILogger fakeLogger = A.Fake<ILogger>();
            LinkBundle bundle = Fixture.Create<LinkBundle>();

            HttpRequest req = this.AuthenticatedRequest;
            req.Body = this.GetHttpRequestBodyStream(JsonConvert.SerializeObject(bundle));
            IAsyncCollector<LinkBundle> collector = A.Fake<IAsyncCollector<LinkBundle>>();

            // Act
            IActionResult result = await _linkOperations.SaveLinks(req, collector, fakeLogger);

            // Assert
            Assert.IsType<CreatedResult>(result);

            CreatedResult createdResult = result as CreatedResult;
            LinkBundle createdBundle = createdResult.Value as LinkBundle;
            Assert.Equal(_hasher.HashString("someone@linkylink.com"), createdBundle.UserId);

            A.CallTo(() => collector.AddAsync(A<LinkBundle>.That.Matches(b => b.UserId == _hasher.HashString("someone@linkylink.com")),
                default)).MustHaveHappened();
        }

        [Theory]
        [InlineData("lower")]
        [InlineData("UPPER")]
        [InlineData("MiXEd")]
        public async Task SaveLinks_Converts_VanityUrl_To_LowerCase(string vanityUrl)
        {
            // Arrange
            ILogger fakeLogger = A.Fake<ILogger>();
            LinkBundle bundle = Fixture.Create<LinkBundle>();
            bundle.VanityUrl = vanityUrl;

            HttpRequest req = this.AuthenticatedRequest;
            req.Body = this.GetHttpRequestBodyStream(JsonConvert.SerializeObject(bundle));
            IAsyncCollector<LinkBundle> collector = A.Fake<IAsyncCollector<LinkBundle>>();

            // Act
            IActionResult result = await _linkOperations.SaveLinks(req, collector, fakeLogger);

            // Assert
            Assert.IsType<CreatedResult>(result);

            CreatedResult createdResult = result as CreatedResult;
            LinkBundle createdBundle = createdResult.Value as LinkBundle;
            Assert.Equal(vanityUrl.ToLower(), createdBundle.VanityUrl);

            A.CallTo(() => collector.AddAsync(A<LinkBundle>.That.Matches(b => b.VanityUrl == vanityUrl.ToLower()),
                default)).MustHaveHappened();
        }

        [Fact]
        public async Task SaveLinks_Populates_VanityUrl_If_Not_Provided()
        {
            // Arrange
            ILogger fakeLogger = A.Fake<ILogger>();
            LinkBundle bundle = Fixture.Create<LinkBundle>();
            bundle.VanityUrl = string.Empty;

            HttpRequest req = this.AuthenticatedRequest;
            req.Body = this.GetHttpRequestBodyStream(JsonConvert.SerializeObject(bundle));
            IAsyncCollector<LinkBundle> collector = A.Fake<IAsyncCollector<LinkBundle>>();

            // Act
            IActionResult result = await _linkOperations.SaveLinks(req, collector, fakeLogger);

            // Assert
            Assert.IsType<CreatedResult>(result);

            CreatedResult createdResult = result as CreatedResult;
            LinkBundle createdBundle = createdResult.Value as LinkBundle;
            Assert.False(string.IsNullOrEmpty(createdBundle.VanityUrl));
            Assert.Equal(createdBundle.VanityUrl.ToLower(), createdBundle.VanityUrl);

            A.CallTo(() => collector.AddAsync(A<LinkBundle>.That.Matches(b => !string.IsNullOrEmpty(b.VanityUrl)),
                default)).MustHaveHappened();
        }
    }
}

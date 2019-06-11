using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using System.Security.Claims;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using Microsoft.Azure.Documents;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace LinkyLink.Tests.Helpers
{
    public abstract class TestBase
    {
        protected IFixture Fixture { get; set; }

        public TestBase()
        {
            this.Fixture = new Fixture()
                .Customize(new AutoFakeItEasyCustomization());

            Fixture.Register<Document>(() =>
            {
                Document doc = new Document();
                doc.SetPropertyValue("userId", Fixture.Create<string>());
                doc.SetPropertyValue("vanityUrl", Fixture.Create<string>());
                doc.SetPropertyValue("description", Fixture.Create<string>());
                doc.SetPropertyValue("linkCount", Fixture.Create<int>());
                return doc;
            });
        }

        private HttpRequest _defaultRequest;
        public HttpRequest DefaultRequest
        {
            get
            {
                if (_defaultRequest == null)
                {
                    ClaimsIdentity identity = new ClaimsIdentity("WebJobsAuthLevel");
                    identity.AddClaim(new Claim(Constants.FunctionsAuthLevelClaimType, "Function"));
                    identity.AddClaim(new Claim(Constants.FunctionsAuthLevelKeyNameClaimType, "default"));

                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                    var context = new DefaultHttpContext
                    {
                        User = principal
                    };

                    _defaultRequest = new DefaultHttpRequest(context);
                }
                return _defaultRequest;
            }
        }

        private HttpRequest _authenticatedRequest;
        public HttpRequest AuthenticatedRequest
        {
            get
            {
                if (_authenticatedRequest == null)
                {
                    ClaimsIdentity defaultIdentity = new ClaimsIdentity("WebJobsAuthLevel");
                    defaultIdentity.AddClaim(new Claim(Constants.FunctionsAuthLevelClaimType, "Function"));
                    defaultIdentity.AddClaim(new Claim(Constants.FunctionsAuthLevelKeyNameClaimType, "default"));

                    ClaimsPrincipal principal = new ClaimsPrincipal(defaultIdentity);

                    ClaimsIdentity twitterIdentity = new ClaimsIdentity("twitter");
                    twitterIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1111"));
                    twitterIdentity.AddClaim(new Claim(ClaimTypes.Name, "First Last"));
                    twitterIdentity.AddClaim(new Claim(ClaimTypes.Upn, "userid"));
                    principal.AddIdentity(twitterIdentity);

                    var context = new DefaultHttpContext
                    {
                        User = principal
                    };

                    _authenticatedRequest = new DefaultHttpRequest(context);
                }
                return _authenticatedRequest;
            }
        }

        public Stream GetHttpRequestBodyStream(object bodyContent)
        {
            byte[] bytes = default;
            if (bodyContent is string bodyString)
            {
                bytes = Encoding.UTF8.GetBytes(bodyString);
            }
            else if (bodyContent is byte[] bodyBytes)
            {
                bytes = bodyBytes;
            }
            else
            {
                string bodyJson = JsonConvert.SerializeObject(bodyContent);
                bytes = Encoding.UTF8.GetBytes(bodyJson);
            }
            return new MemoryStream(bytes);
        }

        private TelemetryConfiguration _defaultTestConfiguration;
        public TelemetryConfiguration DefaultTestConfiguration
        {
            get
            {
                if (_defaultTestConfiguration == null)
                {
                    _defaultTestConfiguration = new TelemetryConfiguration
                    {
                        TelemetryChannel = new StubTelemetryChannel(),
                        InstrumentationKey = Guid.NewGuid().ToString()
                    };
                }
                return _defaultTestConfiguration;
            }
        }
    }
}
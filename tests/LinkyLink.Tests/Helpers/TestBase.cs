using System;
using System.IO;
using System.Security.Claims;
using System.Text;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Azure.Documents;

using Newtonsoft.Json;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;

using LinkyLink.Infrastructure;

namespace LinkyLink.Tests.Helpers
{
    public abstract class TestBase
    {
        private IBlackListChecker _blackListChecker = new EnvironmentBlackListChecker();
        protected LinkOperations _linkOperations;
        protected IFixture Fixture { get; set; }

        public TestBase()
        {
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = CreateContext(true)
            };
            _linkOperations = new LinkOperations(httpContextAccessor, _blackListChecker);

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

        protected Stream GetHttpRequestBodyStream(object bodyContent)
        {
#pragma warning disable IDE0059
            byte[] bytes = default;
#pragma warning restore IDE0059

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

        private HttpRequest _defaultRequest;
        protected HttpRequest DefaultRequest
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
        protected HttpRequest AuthenticatedRequest
        {
            get
            {
                if (_authenticatedRequest == null)
                {
                    var context = CreateContext(true);
                    _authenticatedRequest = new DefaultHttpRequest(context);
                }
                return _authenticatedRequest;
            }
        }

        private TelemetryConfiguration _defaultTestConfiguration;
        protected TelemetryConfiguration DefaultTestConfiguration
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

        protected void RemoveAuthFromContext()
        {
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = CreateContext()
            };
            _linkOperations = new LinkOperations(httpContextAccessor, _blackListChecker);
        }

        protected void AddAuthToContext()
        {
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = CreateContext(true)
            };
            _linkOperations = new LinkOperations(httpContextAccessor, _blackListChecker);
        }

        private static HttpContext CreateContext(bool authenticated = false)
        {
            ClaimsIdentity defaultIdentity = new ClaimsIdentity("WebJobsAuthLevel");
            defaultIdentity.AddClaim(new Claim(Constants.FunctionsAuthLevelClaimType, "Function"));
            defaultIdentity.AddClaim(new Claim(Constants.FunctionsAuthLevelKeyNameClaimType, "default"));

            ClaimsPrincipal principal = new ClaimsPrincipal(defaultIdentity);

            if (authenticated)
            {
                ClaimsIdentity twitterIdentity = new ClaimsIdentity("twitter");
                twitterIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1111"));
                twitterIdentity.AddClaim(new Claim(ClaimTypes.Name, "First Last"));
                twitterIdentity.AddClaim(new Claim(ClaimTypes.Upn, "userid"));
                principal.AddIdentity(twitterIdentity);
            }

            var context = new DefaultHttpContext
            {
                User = principal
            };

            return context;
        }
    }
}
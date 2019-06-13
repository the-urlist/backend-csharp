using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using LinkyLink.Infrastructure;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace LinkyLink
{
    public partial class LinkOperations
    {
        protected IHttpContextAccessor _contextAccessor;
        protected IBlackListChecker _blackListChecker;
        protected TelemetryClient _telemetryClient;
        protected const string CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        protected const string VANITY_REGEX = @"^([\w\d-])+(/([\w\d-])+)*$";

        public LinkOperations(IHttpContextAccessor contextAccessor, IBlackListChecker blackListChecker)
        {
            _contextAccessor = contextAccessor;
            _blackListChecker = blackListChecker;
            TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.TelemetryInitializers.Add(new HeaderTelemetryInitializer(contextAccessor));
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        protected string GetTwitterHandle()
        {
            ClaimsIdentity twitterIdentity = _contextAccessor.HttpContext.User.Identities.SingleOrDefault(id => id.AuthenticationType.Equals("twitter", StringComparison.InvariantCultureIgnoreCase));
            if (twitterIdentity != null)
            {
                string handle = twitterIdentity.Claims.Single(c => c.Type == ClaimTypes.Upn).Value;
                return handle;
            }
            return string.Empty;
        }

        [ExcludeFromCodeCoverage]
        protected void TrackRequestHeaders(HttpRequest req, string requestName)
        {
            var reqTelemetry = new RequestTelemetry() { Name = requestName };
            foreach (var kvp in req.Headers)
            {
                reqTelemetry.Properties.Add($"header-{kvp.Key}", kvp.Value.ToString());
            }
            reqTelemetry.Properties.Add("IsAuthenticated", $"{req.HttpContext.User?.Identity.IsAuthenticated}");
            reqTelemetry.Properties.Add("IdentityCount", $"{req.HttpContext.User?.Identities.Count()}");

            if (req.HttpContext.User.Identities.Any())
            {
                foreach (ClaimsIdentity identity in req.HttpContext.User.Identities)
                {
                    foreach (var claim in identity.Claims)
                    {
                        reqTelemetry.Properties.Add($"{identity.AuthenticationType}-{claim.Type}", claim.Value);
                    }
                }

            }
            _telemetryClient.TrackRequest(reqTelemetry);
        }
    }
}
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
    public static partial class LinkOperations
    {
        public static TelemetryClient telemetryClient;
        public static IBlackListChecker BlackListChecker = new EnvironmentBlackListChecker();
        public const string CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        public const string VANITY_REGEX = @"^[\w\d-]+$";

        static LinkOperations()
        {
            TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.TelemetryInitializers.Add(new HeaderTelemetryInitializer());
            telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        public static string GetTwitterHandle(HttpRequest req)
        {
            ClaimsIdentity twitterIdentity = req.HttpContext.User.Identities.SingleOrDefault(id => id.AuthenticationType.Equals("twitter", StringComparison.InvariantCultureIgnoreCase));
            if (twitterIdentity != null)
            {
                string handle = twitterIdentity.Claims.Single(c => c.Type == ClaimTypes.Upn).Value;
                return handle;
            }
            return string.Empty;
        }

        [ExcludeFromCodeCoverage]
        public static void TrackRequestHeaders(HttpRequest req, string requestName)
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
            telemetryClient.TrackRequest(reqTelemetry);
        }
    }
}
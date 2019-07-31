using System.Linq;
using System.Security.Claims;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace LinkyLink.Infrastructure
{
    public class HeaderTelemetryInitializer : ITelemetryInitializer
    {
        private IHttpContextAccessor _contextAccessor;

        public HeaderTelemetryInitializer(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            var requestTelemetry = telemetry as RequestTelemetry;
            // Is this a TrackRequest() ?
            if (requestTelemetry == null) return;

            var context = _contextAccessor.HttpContext;

            foreach (var kvp in context.Request.Headers)
            {
                requestTelemetry.Properties.Add($"header-{kvp.Key}", kvp.Value.ToString());
            }

            requestTelemetry.Properties.Add("IsAuthenticated", $"{context.User?.Identity.IsAuthenticated}");
            requestTelemetry.Properties.Add("IdentityCount", $"{context.User?.Identities.Count()}");

            if (context.User.Identities.Any())
            {
                foreach (ClaimsIdentity identity in context.User.Identities)
                {
                    foreach (var claim in identity.Claims)
                    {
                        requestTelemetry.Properties.Add($"{identity.AuthenticationType}-{claim.Type}", claim.Value);
                    }
                }

            }
        }
    }
}
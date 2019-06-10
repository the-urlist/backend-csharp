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
        }
    }
}
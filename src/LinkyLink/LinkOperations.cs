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

        protected string GetAccountInfo()
        {
            ClaimsIdentity twitterIdentity = _contextAccessor.HttpContext.User.Identities.SingleOrDefault(id => id.AuthenticationType.Equals("twitter", StringComparison.InvariantCultureIgnoreCase));
            if (twitterIdentity != null)
            {
                string handle = twitterIdentity.Claims.Single(c => c.Type == ClaimTypes.Upn).Value;
                return handle;
            }

            ClaimsIdentity facebookIdentity = _contextAccessor.HttpContext.User.Identities.SingleOrDefault(id => id.AuthenticationType.Equals("facebook", StringComparison.InvariantCultureIgnoreCase));
            if (facebookIdentity != null)
            {
                string handle = facebookIdentity.Claims.Single(c => c.Type == ClaimTypes.Name).Value;
                return handle;
            }

            ClaimsIdentity googleIdentity = _contextAccessor.HttpContext.User.Identities.SingleOrDefault(id => id.AuthenticationType.Equals("google", StringComparison.InvariantCultureIgnoreCase));
            if (facebookIdentity != null)
            {
                string handle = googleIdentity.Claims.Single(c => c.Type == "name").Value;
                return handle;
            }

            ClaimsIdentity microsoftIdentity = _contextAccessor.HttpContext.User.Identities.SingleOrDefault(id => id.AuthenticationType.Equals("microsoftaccount", StringComparison.InvariantCultureIgnoreCase));
            if (facebookIdentity != null)
            {
                string handle = microsoftIdentity.Claims.Single(c => c.Type == ClaimTypes.Name).Value;
                return handle;
            }
            return string.Empty;
        }
    }
}
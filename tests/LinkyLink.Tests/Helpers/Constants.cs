namespace LinkyLink.Tests.Helpers
{
    public class Constants
    {
        // see https://github.com/Azure/azure-functions-host/blob/v2.0.12303/src/WebJobs.Script.WebHost/Security/Authentication/SecurityConstants.cs
        public const string FunctionsAuthLevelClaimType = "http://schemas.microsoft.com/2017/07/functions/claims/authlevel";
        public const string FunctionsAuthLevelKeyNameClaimType = "http://schemas.microsoft.com/2017/07/functions/claims/keyid";
    }
}
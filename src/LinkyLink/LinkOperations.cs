using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LinkyLink.Infrastructure;
using LinkyLink.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Primitives;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using QRCoder;
using static QRCoder.PayloadGenerator;

namespace LinkyLink
{
    public partial class LinkOperations
    {
        protected IHttpContextAccessor _contextAccessor;
        protected IBlackListChecker _blackListChecker;
        protected TelemetryClient _telemetryClient;
        protected const string CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        protected const string VANITY_REGEX = @"^([\w\d-])+(/([\w\d-])+)*$";
        protected const string QRCODECONTAINER = "qrcodes";

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

        private static async Task GenerateQRCodeAsync(LinkBundle linkDocument, HttpRequest req, Binder binder)
        {
            req.Headers.TryGetValue("Origin", out StringValues origin);
            Url generator = new Url($"{origin.ToString()}/{linkDocument.VanityUrl}");
            string payload = generator.ToString();

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

            var attributes = new Attribute[]
            {
                new BlobAttribute(blobPath: $"{QRCODECONTAINER}/{linkDocument.VanityUrl}.png", FileAccess.Write),
                new StorageAccountAttribute("AzureWebJobsStorage")
            };

            using (var writer = await binder.BindAsync<CloudBlobStream>(attributes).ConfigureAwait(false))

            {
                writer.Write(qrCodeAsPngByteArr);
            }
        }

        private static async Task DeleteQRCodeAsync(string vanityUrl, Binder binder)
        {
            StorageAccountAttribute storageAccountAttribute = new StorageAccountAttribute("AzureWebJobsStorage");
            CloudStorageAccount storageAccount = await binder.BindAsync<CloudStorageAccount>(storageAccountAttribute);

            CloudBlobClient client = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(QRCODECONTAINER);

            CloudBlockBlob blob = container.GetBlockBlobReference($"{vanityUrl}.png");
            await blob.DeleteIfExistsAsync();
        }
    }
}
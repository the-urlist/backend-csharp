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
        protected Hasher _hasher;
        protected TelemetryClient _telemetryClient;
        protected const string CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        protected const string VANITY_REGEX = @"^([\w\d-])+(/([\w\d-])+)*$";
        protected const string QRCODECONTAINER = "qrcodes";

        public LinkOperations(IHttpContextAccessor contextAccessor, IBlackListChecker blackListChecker, Hasher hasher)
        {
            _contextAccessor = contextAccessor;
            _blackListChecker = blackListChecker;
            _hasher = hasher;
            TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.TelemetryInitializers.Add(new HeaderTelemetryInitializer(contextAccessor));
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        protected UserInfo GetAccountInfo()
        {
            var socialIdentities = _contextAccessor.HttpContext.User
                  .Identities.Where(id => !id.AuthenticationType.Equals("WebJobsAuthLevel", StringComparison.InvariantCultureIgnoreCase));

            if (socialIdentities.Any())
            {
                var provider = _contextAccessor.HttpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"].FirstOrDefault();

                var primaryIdentity = socialIdentities.First();
                var email = primaryIdentity.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Email).Value;
                var userInfo = new UserInfo(provider, _hasher.HashString(email));

                var evt = new EventTelemetry("UserInfo Retrieved");
                evt.Properties.Add("Provider", provider);
                evt.Properties.Add("EmailAquired", (string.IsNullOrEmpty(email).ToString()));
                _telemetryClient.TrackEvent(evt);

                return userInfo;
            }

            return UserInfo.Empty; ;
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
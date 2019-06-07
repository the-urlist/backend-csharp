using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using System.Net;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.ApplicationInsights.DataContracts;
using System.Text.RegularExpressions;
using LinkyLink.Models;

namespace LinkyLink
{
    public static partial class LinkOperations
    {
        [FunctionName(nameof(SaveLinks))]
        public static async Task<IActionResult> SaveLinks(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "links")] HttpRequest req,
            [CosmosDB(
                databaseName: "linkylinkdb",
                collectionName: "linkbundles",
                ConnectionStringSetting = "LinkLinkConnection"
            )] IAsyncCollector<LinkBundle> documents,
            ILogger log)
        {
            TrackRequestHeaders(req, $"{nameof(GetLinks)}-HeaderData");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var linkDocument = JsonConvert.DeserializeObject<LinkBundle>(requestBody);

                if (!ValidatePayLoad(linkDocument, req, out ProblemDetails problems))
                {
                    log.LogError(problems.Detail);
                    return new BadRequestObjectResult(problems);
                }

                string handle = GetTwitterHandle(req);
                linkDocument.UserId = handle;
                EnsureVanityUrl(linkDocument);

                Match match = Regex.Match(linkDocument.VanityUrl, VANITY_REGEX, RegexOptions.IgnoreCase);

                if (!match.Success)
                {
                    // does not match
                    return new BadRequestResult();
                }

                if (!await BlackListChecker.Check(linkDocument.VanityUrl))
                {
                    ProblemDetails blacklistProblems = new ProblemDetails
                    {
                        Title = "Could not create link bundle",
                        Detail = "Vanity link is invalid",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "/linkylink/clientissue",
                        Instance = req.Path
                    };

                    log.LogError(problems.Detail);
                    return new BadRequestObjectResult(blacklistProblems);
                }

                await documents.AddAsync(linkDocument);
                return new CreatedResult($"/{linkDocument.VanityUrl}", linkDocument);
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                log.LogError(ex, ex.Message);

                ProblemDetails exceptionDetail = new ProblemDetails
                {
                    Title = "Could not create link bundle",
                    Detail = "Vanity link already in use",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "/linkylink/clientissue",
                    Instance = req.Path
                };
                return new BadRequestObjectResult(exceptionDetail);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private static void EnsureVanityUrl(LinkBundle linkDocument)
        {
            if (string.IsNullOrWhiteSpace(linkDocument.VanityUrl))
            {
                var code = new char[7];
                var rng = new RNGCryptoServiceProvider();

                var bytes = new byte[sizeof(uint)];
                for (int i = 0; i < code.Length; i++)
                {
                    rng.GetBytes(bytes);
                    uint num = BitConverter.ToUInt32(bytes, 0) % (uint)CHARACTERS.Length;
                    code[i] = CHARACTERS[(int)num];
                }

                linkDocument.VanityUrl = new String(code);

                telemetryClient.TrackEvent(new EventTelemetry { Name = "Custom Vanity Generated" });
            }

            // force lowercase
            linkDocument.VanityUrl = linkDocument.VanityUrl.ToLower();
        }

        private static bool ValidatePayLoad(LinkBundle linkDocument, HttpRequest req, out ProblemDetails problems)
        {
            bool isValid = (linkDocument != null) && linkDocument.Links.Count() > 0;
            problems = null;

            if (!isValid)
            {
                problems = new ProblemDetails()
                {
                    Title = "Payload is invalid",
                    Detail = "No links provided",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "/linkylink/clientissue",
                    Instance = req.Path
                };
            }
            return isValid;
        }
    }
}

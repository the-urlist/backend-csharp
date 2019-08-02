using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents;
using LinkyLink.Models;

namespace LinkyLink
{
    public partial class LinkOperations
    {
        [FunctionName(nameof(GetLinks))]
        public IActionResult GetLinks(
            [HttpTrigger(AuthorizationLevel.Function, "GET", Route = "links/{vanityUrl}")] HttpRequest req,
            [CosmosDB(
                databaseName: "linkylinkdb",
                collectionName: "linkbundles",
                ConnectionStringSetting = "LinkLinkConnection",
                SqlQuery = "SELECT * FROM linkbundles lb WHERE LOWER(lb.vanityUrl) = LOWER({vanityUrl})"
            )] IEnumerable<LinkBundle> documents,
            string vanityUrl,
            ILogger log)
        {
            if (!documents.Any())
            {
                log.LogInformation($"Bundle for {vanityUrl} not found.");
                return new NotFoundResult();
            }

            LinkBundle doc = documents.Single();
            return new OkObjectResult(doc);
        }

        [FunctionName(nameof(GetBundlesForUser))]
        public IActionResult GetBundlesForUser(
           [HttpTrigger(AuthorizationLevel.Function, "GET", Route = "links/user/{userId}")] HttpRequest req,
           [CosmosDB(
                databaseName: "linkylinkdb",
                collectionName: "linkbundles",
                ConnectionStringSetting = "LinkLinkConnection",
                SqlQuery = "SELECT c.userId, c.vanityUrl, c.description, ARRAY_LENGTH(c.links) as linkCount FROM c where c.userId = {userId}"
            )] IEnumerable<Document> documents,
           string userId,
           ILogger log)
        {
            string twitterHandle = GetAccountInfo().HashedID;
            if (string.IsNullOrEmpty(twitterHandle) || twitterHandle != userId)
            {
                log.LogInformation("Client is not authorized");
                return new UnauthorizedResult();
            }

            if (!documents.Any())
            {
                log.LogInformation($"No links for user: '{userId}'  found.");

                return new NotFoundResult();
            }
            var results = documents.Select(d => new
            {
                userId = d.GetPropertyValue<string>("userId"),
                vanityUrl = d.GetPropertyValue<string>("vanityUrl"),
                description = d.GetPropertyValue<string>("description"),
                linkCount = d.GetPropertyValue<string>("linkCount")
            });
            return new OkObjectResult(results);
        }
    }
}

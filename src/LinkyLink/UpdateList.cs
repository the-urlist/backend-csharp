using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinkyLink.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LinkyLink
{
    public static partial class LinkOperations
    {
        [FunctionName(nameof(UpdateList))]
        public static async Task<IActionResult> UpdateList(
            [HttpTrigger(AuthorizationLevel.Function, "PATCH", Route = "links/{vanityUrl}")] HttpRequest req,
            [CosmosDB(
                databaseName: "linkylinkdb",
                collectionName: "linkbundles",
                ConnectionStringSetting = "LinkLinkConnection",
                SqlQuery = "SELECT * FROM linkbundles lb WHERE LOWER(lb.vanityUrl) = LOWER({vanityUrl})"
            )] IEnumerable<LinkBundle> documents,
            [CosmosDB(ConnectionStringSetting = "LinkLinkConnection")] IDocumentClient docClient,
            string vanityUrl,
            ILogger log)
        {
            string handle = GetTwitterHandle(req);
            if (string.IsNullOrEmpty(handle)) return new UnauthorizedResult();

            if (!documents.Any())
            {
                log.LogInformation($"Bundle for {vanityUrl} not found.");
                return new NotFoundResult();
            }

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrEmpty(requestBody))
                {
                    log.LogError("Request body is empty.");
                    return new BadRequestResult();
                }

                JsonPatchDocument<LinkBundle> patchDocument = JsonConvert.DeserializeObject<JsonPatchDocument<LinkBundle>>(requestBody);

                if (!patchDocument.Operations.Any())
                {
                    log.LogError("Request body contained no operations.");
                    return new NoContentResult();
                }

                LinkBundle bundle = documents.Single();
                patchDocument.ApplyTo(bundle);

                Uri collUri = UriFactory.CreateDocumentCollectionUri("linkylinkdb", "linkbundles");
                RequestOptions reqOptions = new RequestOptions { PartitionKey = new PartitionKey(vanityUrl) };
                await docClient.UpsertDocumentAsync(collUri, bundle, reqOptions);
            }
            catch (JsonSerializationException ex)
            {
                log.LogError(ex, ex.Message);
                return new BadRequestResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return new NoContentResult();
        }
    }
}
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LinkyLink
{
    public class LinkBundle
    {
        public LinkBundle(string userId, string vanityUrl, string description, IDictionary<string, string>[] links)
        {
            this.UserId = userId;
            this.VanityUrl = vanityUrl;
            this.Description = description;
            this.Links = links;
        }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("userId", NullValueHandling = NullValueHandling.Ignore)]
        public string UserId { get; set; }

        [JsonProperty("vanityUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string VanityUrl { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("links", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string>[] Links { get; set; }
    }
}
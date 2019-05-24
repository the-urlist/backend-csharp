using System.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenGraphNet;

namespace LinkyLink
{
    public class OpenGraphResult
    {
        public OpenGraphResult() { }

        public OpenGraphResult(string id, OpenGraph graph, params HtmlNode[] nodes)
        {
            Id = id;
            nodes = nodes.Where(n => n != null).ToArray();
            //Use og:title else fallback to html title tag
            var title = nodes.SingleOrDefault(n => n.Name == "title")?.InnerText.Trim();
            Title = string.IsNullOrEmpty(graph.Title) ? title : HtmlEntity.DeEntitize(graph.Title);

            Image = graph.Metadata["og:image"].FirstOrDefault()?.Value;

            //Default to og:description else fallback to description meta tag
            string descriptionData = string.Empty;
            var descriptionNode = nodes.FirstOrDefault(n => n.Attributes.Contains("content")
                                              && n.Attributes.Contains("name")
                                              && n.Attributes["name"].Value == "description");

            Description = HtmlEntity.DeEntitize(graph.Metadata["og:description"].FirstOrDefault()?.Value) ?? descriptionNode?.Attributes["content"].Value;
        }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public string Image { get; set; }
    }
}
using Newtonsoft.Json;

namespace AutoHPMA.Models
{
    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("body")]
        public string Body { get; set; } = string.Empty;

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }

    public enum UpdateTrigger
    {
        Auto,
        Manual
    }

    public class UpdateOption
    {
        public UpdateTrigger Trigger { get; set; }
    }
}
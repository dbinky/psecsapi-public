using System.Text.Json.Serialization;

namespace psecsapi.api.models.Events
{
    public class CloudEventModel
    {
        [JsonPropertyName("specversion")]
        public string SpecVersion { get; set; } = "1.0";

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("datacontenttype")]
        public string DataContentType { get; set; } = "application/json";

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}

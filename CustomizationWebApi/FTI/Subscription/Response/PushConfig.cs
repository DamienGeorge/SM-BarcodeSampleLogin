using System.Text.Json.Serialization;

namespace CustomizationWebApi.FTI
{
    public class PushConfig
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; }

        [JsonPropertyName("exemptHandshake")]
        public bool ExemptHandshake { get; set; }

        [JsonPropertyName("onPremise")]
        public bool OnPremise { get; set; }

        [JsonPropertyName("cloudConnectorLocationId")]
        public string CloudConnectorLocationId { get; set; }
    }
} 
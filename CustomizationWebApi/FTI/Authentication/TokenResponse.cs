using System.Text.Json.Serialization;

namespace CustomizationWebApi.FTI
{
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
} 
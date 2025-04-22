using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CustomizationWebApi.FTI.Models
{
    public class SubscriptionRequest
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [Required]
        [JsonPropertyName("address")]
        public string Address { get; set; } 

        [Required]
        [Range(0, 1)]
        [JsonPropertyName("qos")]
        public int Qos { get; set; } = 1;

        [Required]
        [JsonPropertyName("pushConfig")]
        public PushConfig PushConfig { get; set; }

        public static SubscriptionRequest Create(
            string name, 
            string address,
            string endpoint, 
            SecuritySchemaBase securitySchema,
            int qos = 1)
        {
            return new SubscriptionRequest
            {
                Name = name,
                Address = address,
                Qos = qos,
                PushConfig = new PushConfig
                {
                    Type = PushConfigType.webhook,
                    Endpoint = endpoint,
                    SecuritySchema = securitySchema,
                    ExemptHandshake = true
                }
            };
        }

        // Helper methods for common scenarios
        public static SubscriptionRequest CreateBasicAuth(
            string name, 
            string address,
            string endpoint, 
            string user, 
            string password,
            int qos = 1)
        {
            var securitySchema = new BasicAuthSchema
            {
                User = user,
                Password = password
            };
            return Create(name, address, endpoint, securitySchema, qos);
        }


        public static SubscriptionRequest CreateOAuth2(
            string name, 
            string address,
            string endpoint, 
            string clientId, 
            string clientSecret, 
            string tokenUrl, 
            List<string> scopes = null,
            int qos = 1)
        {
            var securitySchema = new OAuth2Schema
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                TokenUrl = tokenUrl,
                Scopes = scopes
            };
            return Create(name, address, endpoint, securitySchema, qos);
        }


        public static SubscriptionRequest CreateCredentialRef(
            string name, 
            string address,
            string endpoint, 
            string credentialName,
            int qos = 1)
        {
            var securitySchema = new CredentialRefSchema
            {
                CredentialName = credentialName
            };
            return Create(name,address, endpoint, securitySchema, qos);
        }
    }

    public class PushConfig
    {
        [Required]
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PushConfigType Type { get; set; } = PushConfigType.webhook;

        [Required]
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; }

        [JsonPropertyName("exemptHandshake")]
        public bool ExemptHandshake { get; set; }

        [JsonPropertyName("securitySchema")]
        public SecuritySchemaBase SecuritySchema { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PushConfigType
    {
        webhook
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(BasicAuthSchema), typeDiscriminator: "basicAuth")]
    [JsonDerivedType(typeof(OAuth2Schema), typeDiscriminator: "oauth2")]
    [JsonDerivedType(typeof(CredentialRefSchema), typeDiscriminator: "credential-ref")]
    public abstract class SecuritySchemaBase
    {
        [Required]
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }

    public class BasicAuthSchema : SecuritySchemaBase
    {
        public override string Type => "basicAuth";

        [Required]
        [JsonPropertyName("user")]
        public string User { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

    public class OAuth2Schema : SecuritySchemaBase
    {
        public override string Type => "oauth2";

        [Required]
        [JsonPropertyName("grantType")]
        public string GrantType { get; set; } = "client_credentials";

        [Required]
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [Required]
        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; }

        [Required]
        [JsonPropertyName("tokenUrl")]
        public string TokenUrl { get; set; }

        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; }
    }

    public class CredentialRefSchema : SecuritySchemaBase
    {
        public override string Type => "credential-ref";

        [Required]
        [JsonPropertyName("credentialName")]
        public string CredentialName { get; set; }
    }
} 
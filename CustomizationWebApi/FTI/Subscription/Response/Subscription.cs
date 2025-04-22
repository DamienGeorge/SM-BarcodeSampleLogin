using System.Text.Json.Serialization;

namespace CustomizationWebApi.FTI
{
    public class Subscription
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("pushConfig")]
        public PushConfig PushConfig { get; set; }

        [JsonPropertyName("createdOn")]
        public string CreatedOn { get; set; }

        [JsonPropertyName("handshakeStatus")]
        public string HandshakeStatus { get; set; }

        [JsonPropertyName("subscriptionStatus")]
        public string SubscriptionStatus { get; set; }

        [JsonPropertyName("subscriptionStatusReason")]
        public string SubscriptionStatusReason { get; set; }

        [JsonPropertyName("lastSuccessfulDelivery")]
        public string LastSuccessfulDelivery { get; set; }

        [JsonPropertyName("lastFailedDelivery")]
        public string LastFailedDelivery { get; set; }

        [JsonPropertyName("lastFailedDeliveryReason")]
        public string LastFailedDeliveryReason { get; set; }

        [JsonPropertyName("qos")]
        public int Qos { get; set; }
    }
} 
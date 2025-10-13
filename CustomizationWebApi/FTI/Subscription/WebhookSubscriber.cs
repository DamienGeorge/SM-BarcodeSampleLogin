using System.Text;
using System.Net.Http.Headers;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Common.CommandLine;
using CustomizationWebApi.FTI.Authentication;
using CustomizationWebApi.FTI.Models;

namespace CustomizationWebApi.FTI
{
    [SampleManagerTask(nameof(WebhookSubscriber))]
    public class WebhookSubscriber : SampleManagerTask, IBackgroundTask
    {
        private HttpClient _httpClient;
        private string _webhookUrl;
        private string _subscriptionUrl;
        private TokenService _tokenService;

        //TODO - add all endpoints using a phrase list?
        private string orderDetailsEndPoint = "fti/OrderDetail";

        [CommandLineSwitch("subscription", "The name of the subscription to create")]
        public string SubscriptionName { get; set; } = "Test_OrderDetails"; // Default value

        public async Task SubscribeAsync(string subscriptionName, string endpointURL, string queueName)
        {
            Logger.Info($"Starting subscription process for {subscriptionName}");
            try
            {
                var accessToken = await _tokenService.GetAccessTokenAsync();

                if (await SubscriptionExistsAsync(subscriptionName, accessToken))
                {
                    Logger.Info($"Subscription '{subscriptionName}' already exists, skipping creation");
                    return;
                }

                var subscription = SubscriptionRequest.Create(
                    name: subscriptionName,
                    address: string.Join(":","queue",queueName),
                    endpoint: string.Join("/", _webhookUrl, endpointURL),
                    new BasicAuthSchema
                    {
                        User = Library.Environment.GetGlobalString("WEBHOOK_USER"),
                        Password = Library.Environment.GetGlobalString("WEBHOOK_PASSWORD")
                    });

                var request = new HttpRequestMessage(HttpMethod.Post, _subscriptionUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(subscription),
                    Encoding.UTF8,
                    "application/json");
                request.Content = content;

                Logger.Debug($"Sending subscription request to {_subscriptionUrl}");
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Failed to check subscription. Status: {response.StatusCode}, Response: {responseContent}");
                    throw new Exception($"Failed to create subscription: {responseContent}");
                }

                response.EnsureSuccessStatusCode();
                Logger.Info($"Successfully created subscription '{subscriptionName}'");
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while creating subscription: {ex}");
                throw;
            }
        }

        public async Task UnsubscribeAsync(string subscriptionId)
        {
            var response = await _httpClient.DeleteAsync($"{_webhookUrl}/{subscriptionId}");
            response.EnsureSuccessStatusCode();
        }

        private async Task<bool> SubscriptionExistsAsync(string subscriptionName, string accessToken)
        {
            Logger.Debug($"Checking if subscription '{subscriptionName}' exists");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _subscriptionUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Failed to check subscription. Status: {response.StatusCode}, Response: {responseContent}");
                    throw new Exception($"Failed to check subscription: {responseContent}");
                }

                var subscriptions = System.Text.Json.JsonSerializer.Deserialize<List<Subscription>>(responseContent);
                var exists = subscriptions.Any(s => s.Name == subscriptionName);
                Logger.Debug($"Subscription '{subscriptionName}' exists: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while checking subscription existence: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Entry point for the task
        /// </summary>
        public void Launch()
        {
            Initialize();

            Logger.Info($"Launching WebhookSubscriber task for subscription: {SubscriptionName}");
            try
            {
                SubscribeAsync(SubscriptionName, orderDetailsEndPoint, "de/cit/fti/serviceOrders").Wait();
                Logger.Info("WebhookSubscriber task completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"WebhookSubscriber task failed: {ex}");
                throw;
            }
        }

        private void Initialize()
        {
            _httpClient = new HttpClient();
            _webhookUrl = Library.Environment.GetGlobalString("WEBHOOK_URL");
            _subscriptionUrl = Library.Environment.GetGlobalString("SAP_SUBSCRIPTION_URL");
            _tokenService = new TokenService(Logger, Library);

            Logger.Info("Initialized WebhookSubscriber");
        }

        /// <summary>
        /// Entry Point to Debug task interactively
        /// </summary>
        protected override void SetupTask()
        {
            base.SetupTask();

            if (Library.Environment.IsBackground() == false)
            {
                Launch();
            }
        }
    }
}
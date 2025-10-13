//using CustomizationWebApi.FTI.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Threading.Tasks;

//namespace CustomizationWebApi.FTI.Authentication
//{
//    public class Utils
//    {
//        public async Task SubscribeAsync(string subscriptionName, string endpointURL, string queueName)
//        {
//            Logger.Info($"Starting subscription process for {subscriptionName}");
//            try
//            {
//                var accessToken = await _tokenService.GetAccessTokenAsync();

//                if (await SubscriptionExistsAsync(subscriptionName, accessToken))
//                {
//                    Logger.Info($"Subscription '{subscriptionName}' already exists, skipping creation");
//                    return;
//                }

//                var subscription = SubscriptionRequest.Create(
//                    name: subscriptionName,
//                    address: string.Join(":", "queue", queueName),
//                    endpoint: string.Join("/", _webhookUrl, endpointURL),
//                    new BasicAuthSchema
//                    {
//                        User = Library.Environment.GetGlobalString("WEBHOOK_USER"),
//                        Password = Library.Environment.GetGlobalString("WEBHOOK_PASSWORD")
//                    });

//                var request = new HttpRequestMessage(HttpMethod.Post, _subscriptionUrl);
//                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//                var content = new StringContent(
//                    System.Text.Json.JsonSerializer.Serialize(subscription),
//                    Encoding.UTF8,
//                    "application/json");
//                request.Content = content;

//                Logger.Debug($"Sending subscription request to {_subscriptionUrl}");
//                var response = await _httpClient.SendAsync(request);
//                var responseContent = await response.Content.ReadAsStringAsync();

//                if (!response.IsSuccessStatusCode)
//                {
//                    Logger.Error($"Failed to check subscription. Status: {response.StatusCode}, Response: {responseContent}");
//                    throw new Exception($"Failed to create subscription: {responseContent}");
//                }

//                response.EnsureSuccessStatusCode();
//                Logger.Info($"Successfully created subscription '{subscriptionName}'");
//            }
//            catch (Exception ex)
//            {
//                Logger.Error($"Exception while creating subscription: {ex}");
//                throw;
//            }
//        }

//        public async Task UnsubscribeAsync(string subscriptionId)
//        {
//            var response = await _httpClient.DeleteAsync($"{_webhookUrl}/{subscriptionId}");
//            response.EnsureSuccessStatusCode();
//        }

//        private async Task<bool> SubscriptionExistsAsync(string subscriptionName, string accessToken)
//        {
//            Logger.Debug($"Checking if subscription '{subscriptionName}' exists");
//            try
//            {
//                var request = new HttpRequestMessage(HttpMethod.Get, _subscriptionUrl);
//                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//                var response = await _httpClient.SendAsync(request);
//                var responseContent = await response.Content.ReadAsStringAsync();

//                if (!response.IsSuccessStatusCode)
//                {
//                    Logger.Error($"Failed to check subscription. Status: {response.StatusCode}, Response: {responseContent}");
//                    throw new Exception($"Failed to check subscription: {responseContent}");
//                }

//                var subscriptions = System.Text.Json.JsonSerializer.Deserialize<List<Subscription>>(responseContent);
//                var exists = subscriptions.Any(s => s.Name == subscriptionName);
//                Logger.Debug($"Subscription '{subscriptionName}' exists: {exists}");
//                return exists;
//            }
//            catch (Exception ex)
//            {
//                Logger.Error($"Exception while checking subscription existence: {ex}");
//                throw;
//            }
//        }
//    }
//}

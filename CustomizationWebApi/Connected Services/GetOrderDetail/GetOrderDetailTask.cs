using CustomizationWebApi.FTI;
using CustomizationWebApi.FTI.Authentication;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.DesignerRuntime;

namespace CustomizationWebApi.Connected_Services.GetOrderDetail
{
    /// <summary>
    /// Method to get order details from OrderDetail endpoint
    /// </summary>
    public class GetOrderDetail
    {
        public StandardLibrary StandardLibrary { get; }
        public IEntityManager EntityManager { get; }
        public Logger Logger { get; }

        public string OrderNumber { get; set; }

        private HttpClient _httpClient;
        private string accessToken;
        private TokenService _tokenService;

        public GetOrderDetail(StandardLibrary standardLibrary, IEntityManager entityManager, Logger logger, string orderNumber)
        {
            StandardLibrary = standardLibrary;
            EntityManager = entityManager;
            Logger = logger;
            OrderNumber = orderNumber;
            _httpClient = new HttpClient();

            accessToken = GetAccesstokenAsync().Result;
        }

        public async Task<string> GetRequestAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error($"Failed to get valid response. Status: {response.StatusCode}, Response: {responseContent}");
                throw new Exception($"Failed to send: {responseContent}");
            }

            return responseContent;
        }
        //public async Task PostRequestAsync<T>(T entity)
        //{
        //    var request = new HttpRequestMessage(HttpMethod.Post, _subscriptionUrl);
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //    var content = new StringContent(
        //        System.Text.Json.JsonSerializer.Serialize(subscription),
        //        Encoding.UTF8,
        //        "application/json");
        //    request.Content = content;

        //    Logger.Debug($"Sending subscription request to {_subscriptionUrl}");
        //    var response = await _httpClient.SendAsync(request);
        //    var responseContent = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        Logger.Error($"Failed to check subscription. Status: {response.StatusCode}, Response: {responseContent}");
        //        throw new Exception($"Failed to create subscription: {responseContent}");
        //    }

        //    response.EnsureSuccessStatusCode();
        //    Logger.Info($"Successfully created subscription '{subscriptionName}'");

        //}

        //Create a new access Token
        public async Task<string> GetAccesstokenAsync()
        {
            _tokenService = new TokenService(Logger, StandardLibrary);
            return await _tokenService.GetAccessTokenAsync();
        }
    }
}

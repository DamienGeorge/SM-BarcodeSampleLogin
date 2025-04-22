using System.Text;
using System.Net.Http.Headers;
using Thermo.Framework.Core;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.DesignerRuntime;

namespace CustomizationWebApi.FTI.Authentication
{
    public class TokenService
    {
        private readonly HttpClient _httpClient;
        private readonly string _tokenUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly StandardLibrary _library;
        private readonly Logger _logger;

        public TokenService(Logger logger, StandardLibrary standardLibrary)
        {
            _logger = logger;
            _library = standardLibrary;

            _httpClient = new HttpClient();
            _tokenUrl = _library.Environment.GetGlobalString("SAP_TOKEN_URL");
            _clientId = _library.Environment.GetGlobalString("SAP_CLIENT_ID");
            _clientSecret = _library.Environment.GetGlobalString("SAP_CLIENT_SECRET");
        }

        /// <summary>
        /// Retrieves an access token from SAP using client credentials
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            _logger.Debug($"Requesting access token from {_tokenUrl}");

            try
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));

                var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error($"Failed to get access token. Status: {response.StatusCode}, Response: {jsonResponse}");
                    throw new Exception($"Failed to get access token: {jsonResponse}");
                }

                _logger.Debug("Successfully obtained access token");
                var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
                return tokenResponse.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception while getting access token: {ex}");
                throw;
            }
        }
    }
}
using psecsapi.Console.Infrastructure.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace psecsapi.Console.Infrastructure.Http
{
    public class AuthenticatedHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly CliConfig _config;
        private bool _isRefreshing = false;

        public AuthenticatedHttpClient(HttpClient httpClient, CliConfig config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            UpdateAuthHeader();
        }

        private void UpdateAuthHeader()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrEmpty(_config.User.ApiKey))
            {
                // API key mode — send raw key as Bearer token
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _config.User.ApiKey);
            }
            else if (!string.IsNullOrEmpty(_config.User.AccessToken))
            {
                // JWT mode
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _config.User.AccessToken);
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            var response = await _httpClient.GetAsync(requestUri);
            return await HandleResponseAsync(response, () => _httpClient.GetAsync(requestUri));
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, object? data)
        {
            var response = await SendPostAsync(requestUri, data);
            return await HandleResponseAsync(response, () => SendPostAsync(requestUri, data));
        }

        public async Task<HttpResponseMessage> PostJsonAsync(string requestUri, string? jsonContent)
        {
            var response = await SendPostJsonAsync(requestUri, jsonContent);
            return await HandleResponseAsync(response, () => SendPostJsonAsync(requestUri, jsonContent));
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, object? data)
        {
            var response = await SendPutAsync(requestUri, data);
            return await HandleResponseAsync(response, () => SendPutAsync(requestUri, data));
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            var response = await _httpClient.DeleteAsync(requestUri);
            return await HandleResponseAsync(response, () => _httpClient.DeleteAsync(requestUri));
        }

        private async Task<HttpResponseMessage> SendPutAsync(string requestUri, object? data)
        {
            HttpContent? content = null;
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return await _httpClient.PutAsync(requestUri, content);
        }

        private async Task<HttpResponseMessage> SendPostAsync(string requestUri, object? data)
        {
            HttpContent? content = null;
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return await _httpClient.PostAsync(requestUri, content);
        }

        private async Task<HttpResponseMessage> SendPostJsonAsync(string requestUri, string? jsonContent)
        {
            HttpContent? content = null;
            if (jsonContent != null)
            {
                content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }
            return await _httpClient.PostAsync(requestUri, content);
        }

        private async Task<HttpResponseMessage> HandleResponseAsync(
            HttpResponseMessage response,
            Func<Task<HttpResponseMessage>> retryAction)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && !_isRefreshing)
            {
                if (!string.IsNullOrEmpty(_config.User.ApiKey))
                {
                    // API key mode — no refresh possible, key may be revoked
                    System.Console.WriteLine("API key authentication failed. Your key may have been revoked.");
                    System.Console.WriteLine("Use 'papi auth login' to re-authenticate, then 'papi auth create-api-key' to generate a new API key.");
                    return response;
                }

                // JWT mode — try refresh
                if (await TryRefreshTokenAsync())
                {
                    UpdateAuthHeader();
                    return await retryAction();
                }
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds;
                response.Headers.TryGetValues("X-RateLimit-Limit", out var limitValues);
                var limit = limitValues?.FirstOrDefault();

                var waitMsg = retryAfter.HasValue ? $"{(int)retryAfter.Value}s" : "a moment";
                System.Console.WriteLine($"Rate limited. Wait {waitMsg} before retrying.");
                if (!string.IsNullOrEmpty(limit))
                    System.Console.WriteLine($"Current limit: {limit} req/s. Use 'papi token stake' to increase it.");
                else
                    System.Console.WriteLine("Use 'papi token stake' to increase your rate limit.");
            }

            return response;
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_config.User.RefreshToken) || string.IsNullOrEmpty(_config.User.UserId))
            {
                System.Console.WriteLine("Session expired. Please login again.");
                return false;
            }

            _isRefreshing = true;
            try
            {
                var request = new
                {
                    userId = _config.User.UserId,
                    refreshToken = _config.User.RefreshToken
                };

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                // Remove auth header for refresh request (it's AllowAnonymous)
                var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
                _httpClient.DefaultRequestHeaders.Authorization = null;

                var response = await _httpClient.PostAsync("/api/auth/refresh", content);

                _httpClient.DefaultRequestHeaders.Authorization = originalAuth;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<RefreshResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (authResponse != null)
                    {
                        await _config.SetAuthTokens(authResponse.UserId, authResponse.AccessToken, authResponse.RefreshToken);
                        System.Console.WriteLine("Session refreshed.");
                        return true;
                    }
                }
                else
                {
                    // Refresh failed - clear tokens and prompt re-login
                    await _config.ClearAuthTokens();
                    System.Console.WriteLine("Session expired. Please login again.");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to refresh session: {ex.Message}");
            }
            finally
            {
                _isRefreshing = false;
            }

            return false;
        }

        private record RefreshResponse(string AccessToken, string RefreshToken, string UserId, string DisplayName);
    }
}

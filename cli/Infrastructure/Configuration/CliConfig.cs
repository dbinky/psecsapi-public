namespace psecsapi.Console.Infrastructure.Configuration
{
    public class CliConfig
    {
        public SystemCliConfig System { get; set; } = new SystemCliConfig();
        public UserCliConfig User { get; set; } = new UserCliConfig();

        private readonly ConfigRepository? _configRepository;

        public CliConfig(ConfigRepository configRepository)
        {
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            var result = _configRepository.GetConfig().Result;
            System = result.System;
            User = result.User;
        }

        public CliConfig()
        {
        }

        public async Task SetAuthTokens(string userId, string accessToken, string refreshToken)
        {
            User.SetAuthTokens(userId, accessToken, refreshToken);
            await _configRepository!.SaveConfig(this);
        }

        public async Task ClearAuthTokens()
        {
            User.ClearAuthTokens();
            await _configRepository!.SaveConfig(this);
        }

        public async Task SetDefaultCorpId(string defaultCorpId)
        {
            User.SetDefaultCorpId(defaultCorpId);
            await _configRepository!.SaveConfig(this);
        }

        public async Task SetDefaultCorpId(Guid defaultCorpId)
        {
            User.SetDefaultCorpId(defaultCorpId);
            await _configRepository!.SaveConfig(this);
        }

        public async Task SetApiKey(string? apiKey)
        {
            User.ApiKey = apiKey;
            await _configRepository!.SaveConfig(this);
        }

        public async Task SetAccessToken(string accessToken)
        {
            User.AccessToken = accessToken;
            await _configRepository!.SaveConfig(this);
        }

        public async Task SetBaseUrl(string baseUrl)
        {
            System.BaseUrl = baseUrl;
            await _configRepository!.SaveConfig(this);
        }
    }

    public class SystemCliConfig
    {
        public string? BaseUrl { get; set; }
    }

    public class UserCliConfig
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? ApiKey { get; set; }
        public Guid? DefaultCorpId { get; set; }

        internal void SetAuthTokens(string userId, string accessToken, string refreshToken)
        {
            UserId = userId;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        internal void ClearAuthTokens()
        {
            UserId = null;
            AccessToken = string.Empty;
            RefreshToken = string.Empty;
            ApiKey = null;
        }

        internal void SetDefaultCorpId(Guid defaultCorpId)
        {
            DefaultCorpId = defaultCorpId;
        }

        internal void SetDefaultCorpId(string defaultCorpId)
        {
            if (Guid.TryParse(defaultCorpId, out var companyId))
            {
                DefaultCorpId = companyId;
            }
            else
            {
                DefaultCorpId = null;
            }
        }
    }
}

using System.Text.Json;

namespace psecsapi.Console.Infrastructure.Configuration
{
    public class ConfigRepository
    {
        private const string DefaultBaseUrl = "https://api.psecsapi.com";

        private readonly string _configLocation;
        private readonly string _configFileLocation;
        private readonly JsonSerializerOptions _options;

        public ConfigRepository()
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _configLocation = Path.Combine(homeDir, ".psecsapi");
            _configFileLocation = Path.Combine(_configLocation, "config.json");

            _options = new JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<CliConfig> GetConfig()
        {
            CliConfig? cliConfig;

            if (!Directory.Exists(_configLocation))
                Directory.CreateDirectory(_configLocation);

            if (!File.Exists(_configFileLocation))
            {
                cliConfig = GetDefaultConfiguration();
                await SaveConfig(cliConfig);
            }
            else
            {
                string json = await File.ReadAllTextAsync(_configFileLocation);
                cliConfig = JsonSerializer.Deserialize<CliConfig>(json, _options);
            }

            return cliConfig!;
        }

        public async Task SaveConfig(CliConfig config)
        {
            string json = JsonSerializer.Serialize(config, _options);
            await File.WriteAllTextAsync(_configFileLocation, json);
        }

        public CliConfig GetDefaultConfiguration()
        {
            return new CliConfig()
            {
                System = new()
                {
                    BaseUrl = DefaultBaseUrl
                },
                User = new()
                {
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    UserId = null,
                    DefaultCorpId = null
                }
            };
        }
    }
}

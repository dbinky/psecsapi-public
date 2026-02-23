using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.User
{
    public class UserCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public UserCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("user", "User management commands");

            command.AddCommand(BuildGetCommand());
            command.AddCommand(BuildCreateCorpCommand());
            command.AddCommand(BuildMapCommand());
            command.AddCommand(BuildStakeApiCommand());
            command.AddCommand(BuildUnstakeApiCommand());
            command.AddCommand(BuildApiStakeInfoCommand());

            return command;
        }

        private Command BuildGetCommand()
        {
            var command = new Command("get", "Get your user profile");
            command.SetHandler(async () =>
            {
                var response = await _client.GetAsync("/api/User");
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error {(int)response.StatusCode}: {content}");
                    return;
                }
                System.Console.WriteLine(content);
            });
            return command;
        }

        private Command BuildCreateCorpCommand()
        {
            var nameOption = new Option<string>("--name", "Corporation name") { IsRequired = true };
            nameOption.AddAlias("-n");

            var command = new Command("create-corp", "Create a new corporation") { nameOption };
            command.SetHandler(async (name) =>
            {
                var response = await _client.PostAsync("/api/User/corp", new { name = name });
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error {(int)response.StatusCode}: {content}");
                    return;
                }
                System.Console.WriteLine(content);
            }, nameOption);

            return command;
        }

        private Command BuildMapCommand()
        {
            var command = new Command("map", "Get your user map data");
            command.SetHandler(async () =>
            {
                var response = await _client.GetAsync("/api/UserMap");
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error {(int)response.StatusCode}: {content}");
                    return;
                }
                System.Console.WriteLine(content);
            });
            return command;
        }

        private Command BuildStakeApiCommand()
        {
            var amountArg = new Argument<decimal>("amount", "Amount of tokens to stake (min 0.1, max 10)");
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON for automation");

            var command = new Command("stake-api",
                "Stake tokens to increase your API rate limit. " +
                "Each staked token increases requests per second. " +
                "Min: 0.1 tokens, Max: 10 tokens total.")
            {
                amountArg, jsonOption
            };
            command.AddAlias("stake");

            command.SetHandler(async (amount, json) =>
            {
                var request = new { amount };
                var response = await _client.PostAsync("/api/user/stake-api-tokens", request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<StakeResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine("Error parsing response");
                    return;
                }

                // Update stored access token
                if (!string.IsNullOrEmpty(result.AccessToken))
                {
                    await _config.SetAccessToken(result.AccessToken);
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Staked {amount} tokens successfully!");
                System.Console.WriteLine();
                System.Console.WriteLine($"Staked Tokens: {result.StakedTokens}");
                System.Console.WriteLine($"Available Tokens: {result.AvailableTokens}");
                System.Console.WriteLine($"New Rate Limit: {result.RateLimit} req/s");

            }, amountArg, jsonOption);

            return command;
        }

        private Command BuildUnstakeApiCommand()
        {
            var amountArg = new Argument<decimal>("amount", "Amount of tokens to unstake (min 0.1)");
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON for automation");

            var command = new Command("unstake-api",
                "Unstake tokens and return them to your available balance. " +
                "Triggers a 1-hour cooldown before next unstake. " +
                "Min: 0.1 tokens.")
            {
                amountArg, jsonOption
            };
            command.AddAlias("unstake");

            command.SetHandler(async (amount, json) =>
            {
                var request = new { amount };
                var response = await _client.PostAsync("/api/user/unstake-api-tokens", request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Check for cooldown error
                    if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        if (json)
                        {
                            System.Console.WriteLine(content);
                            return;
                        }

                        System.Console.WriteLine("Error: Unstake cooldown is active.");
                        // Try to parse cooldown time from response
                        try
                        {
                            using var doc = JsonDocument.Parse(content);
                            if (doc.RootElement.TryGetProperty("cooldownEndsAt", out var cooldownProp))
                            {
                                var cooldownEndsAt = cooldownProp.GetDateTime();
                                var remaining = cooldownEndsAt - DateTime.UtcNow;
                                System.Console.WriteLine($"Cooldown ends at: {cooldownEndsAt:HH:mm:ss} UTC ({remaining.TotalMinutes:F0} minutes remaining)");
                            }
                        }
                        catch { }
                        return;
                    }

                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<UnstakeResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine("Error parsing response");
                    return;
                }

                // Update stored access token
                if (!string.IsNullOrEmpty(result.AccessToken))
                {
                    await _config.SetAccessToken(result.AccessToken);
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Unstaked {amount} tokens successfully!");
                System.Console.WriteLine();
                System.Console.WriteLine($"Staked Tokens: {result.StakedTokens}");
                System.Console.WriteLine($"Available Tokens: {result.AvailableTokens}");
                System.Console.WriteLine($"New Rate Limit: {result.RateLimit} req/s");
                System.Console.WriteLine();
                System.Console.WriteLine($"Cooldown: Cannot unstake again until {result.CooldownEndsAt:HH:mm:ss} UTC");

            }, amountArg, jsonOption);

            return command;
        }

        private Command BuildApiStakeInfoCommand()
        {
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON for automation");

            var command = new Command("api-stake-info",
                "View your current API staking status including staked tokens, " +
                "rate limit, and any active cooldown.")
            {
                jsonOption
            };
            command.AddAlias("stake-info");

            command.SetHandler(async (json) =>
            {
                var response = await _client.GetAsync("/api/user/api-stake-info");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<ApiStakeInfoResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine("Error parsing response");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine("=== API Rate Limit Status ===");
                System.Console.WriteLine();
                System.Console.WriteLine($"Staked Tokens:    {result.StakedTokens}");
                System.Console.WriteLine($"Available Tokens: {result.AvailableTokens}");
                System.Console.WriteLine($"Rate Limit:       {result.RateLimit} req/s");
                System.Console.WriteLine();

                if (result.CooldownEndsAt.HasValue)
                {
                    var remaining = result.CooldownEndsAt.Value - DateTime.UtcNow;
                    if (remaining.TotalSeconds > 0)
                    {
                        System.Console.WriteLine($"Cooldown: Cannot unstake until {result.CooldownEndsAt.Value:HH:mm:ss} UTC ({remaining.TotalMinutes:F0} min remaining)");
                    }
                    else
                    {
                        System.Console.WriteLine("Cooldown: None (ready to unstake)");
                    }
                }
                else
                {
                    System.Console.WriteLine("Cooldown: None");
                }

            }, jsonOption);

            return command;
        }

        #region Staking Response Models

        private class StakeResponse
        {
            public decimal StakedTokens { get; set; }
            public decimal AvailableTokens { get; set; }
            public int RateLimit { get; set; }
            public string AccessToken { get; set; } = string.Empty;
        }

        private class UnstakeResponse
        {
            public decimal StakedTokens { get; set; }
            public decimal AvailableTokens { get; set; }
            public int RateLimit { get; set; }
            public DateTime CooldownEndsAt { get; set; }
            public string AccessToken { get; set; } = string.Empty;
        }

        private class ApiStakeInfoResponse
        {
            public decimal StakedTokens { get; set; }
            public decimal AvailableTokens { get; set; }
            public int RateLimit { get; set; }
            public DateTime? CooldownEndsAt { get; set; }
        }

        #endregion
    }
}

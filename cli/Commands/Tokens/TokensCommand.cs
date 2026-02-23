using System.CommandLine;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Tokens
{
    public class TokensCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;

        public TokensCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("tokens", "Token management commands (balance, buy)");

            command.AddCommand(BuildBalanceCommand());
            command.AddCommand(BuildBuyCommand());

            return command;
        }

        private Command BuildBalanceCommand()
        {
            var command = new Command("balance", "Check current token balance");
            command.SetHandler(async () =>
            {
                var response = await _client.GetAsync("/api/tokens/balance");
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error {(int)response.StatusCode}: {content}");
                    return;
                }

                var data = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var available = data.GetProperty("availableTokens").GetDecimal();
                var staked = data.GetProperty("stakedTokens").GetDecimal();
                var total = data.GetProperty("tokens").GetDecimal();

                System.Console.WriteLine();
                System.Console.WriteLine($"  Available: {available}");
                System.Console.WriteLine($"  Staked:    {staked}");
                System.Console.WriteLine($"  Total:     {total}");
                System.Console.WriteLine();
            });

            return command;
        }

        private Command BuildBuyCommand()
        {
            var quantityOption = new Option<int>("--quantity", "Number of tokens to buy");
            quantityOption.AddAlias("-q");
            quantityOption.IsRequired = true;

            var command = new Command("buy", "Buy tokens (opens browser for payment)");
            command.AddOption(quantityOption);

            command.SetHandler(async (int quantity) =>
            {
                if (quantity <= 0)
                {
                    System.Console.WriteLine("Quantity must be positive.");
                    return;
                }

                var response = await _client.PostAsync("/api/tokens/checkout", new { quantity });

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Failed to create checkout: {response.StatusCode}");
                    var body = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(body))
                        System.Console.WriteLine(body);
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var checkoutUrl = result.GetProperty("checkoutUrl").GetString()!;

                System.Console.WriteLine($"Opening payment page for {quantity} token(s)...");

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = checkoutUrl,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    System.Console.WriteLine($"Please open this URL in your browser: {checkoutUrl}");
                }
            }, quantityOption);

            return command;
        }
    }
}

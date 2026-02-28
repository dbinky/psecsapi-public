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
            var command = new Command("tokens", "Token management commands (balance, buy, purchases)");

            command.AddCommand(BuildBalanceCommand());
            command.AddCommand(BuildBuyCommand());
            command.AddCommand(BuildPurchasesCommand());

            return command;
        }

        private Command BuildBalanceCommand()
        {
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("balance", "Check current token balance");
            command.AddOption(jsonOption);

            command.SetHandler(async (bool json) =>
            {
                var response = await _client.GetAsync("/api/tokens/balance");
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error {(int)response.StatusCode}: {content}");
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
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
            }, jsonOption);

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

        private Command BuildPurchasesCommand()
        {
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("purchases", "View token purchase history");
            command.AddOption(jsonOption);

            command.SetHandler(async (bool json) =>
            {
                var response = await _client.GetAsync("/api/tokens/purchases");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"Error {(int)response.StatusCode}: {content}");
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var data = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!data.TryGetProperty("purchases", out var purchases) || purchases.ValueKind != JsonValueKind.Array)
                {
                    System.Console.WriteLine("Error: Unable to parse response.");
                    return;
                }

                var count = purchases.GetArrayLength();
                System.Console.WriteLine();

                if (count == 0)
                {
                    System.Console.WriteLine("No token purchases found.");
                    System.Console.WriteLine();
                    return;
                }

                System.Console.WriteLine($"Token Purchases ({count}):");
                System.Console.WriteLine();
                System.Console.WriteLine($"  {"Date",-24} {"Qty",4} {"Amount",8} {"State",-12} Session ID");
                System.Console.WriteLine($"  {new string('-', 72)}");

                foreach (var purchase in purchases.EnumerateArray())
                {
                    var created = purchase.TryGetProperty("createdTimestamp", out var ct) && ct.ValueKind != JsonValueKind.Null
                        ? ct.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                        : "(unknown)";
                    var qty = purchase.TryGetProperty("quantity", out var q) ? q.GetInt32() : 0;
                    var amount = purchase.TryGetProperty("amountPaid", out var ap) ? ap.GetDecimal() : 0;
                    var state = purchase.TryGetProperty("state", out var st) ? st.GetString() ?? "" : "";
                    var sessionId = purchase.TryGetProperty("sessionId", out var sid) ? sid.GetString() ?? "" : "";
                    var displaySession = sessionId.Length > 30 ? sessionId[..28] + ".." : sessionId;

                    System.Console.WriteLine($"  {created,-24} {qty,4} {amount,8:N2} {state,-12} {displaySession}");
                }

                System.Console.WriteLine();
            }, jsonOption);

            return command;
        }
    }
}

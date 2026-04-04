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
            var command = new Command("tokens", "Token management commands (balance, buy, invest, uninvest, purchases)");

            command.AddCommand(BuildBalanceCommand());
            command.AddCommand(BuildBuyCommand());
            command.AddCommand(BuildPurchasesCommand());
            command.AddCommand(BuildInvestCommand());
            command.AddCommand(BuildUninvestCommand());

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
                var invested = data.GetProperty("investedTokens").GetDecimal();
                var total = data.GetProperty("tokens").GetDecimal();

                System.Console.WriteLine();
                System.Console.WriteLine($"  Available:  {available}");
                System.Console.WriteLine($"  Staked:     {staked}");
                System.Console.WriteLine($"  Invested:   {invested}");
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

        private Command BuildInvestCommand()
        {
            var amountOption = new Option<decimal>("--amount", "Amount of tokens to invest (min 0.1)");
            amountOption.AddAlias("-a");
            amountOption.IsRequired = true;

            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("invest", "Invest tokens to earn 100 credits/day per token");
            command.AddOption(amountOption);
            command.AddOption(jsonOption);

            command.SetHandler(async (decimal amount, bool json) =>
            {
                if (amount < 0.1m)
                {
                    System.Console.WriteLine("Amount must be at least 0.1 tokens.");
                    return;
                }

                var response = await _client.PostAsync("/api/user/invest-tokens", new { amount });
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
                var newInvested = data.GetProperty("newInvestedTotal").GetDecimal();
                var newAvailable = data.GetProperty("newAvailableTokens").GetDecimal();
                var trancheCount = data.GetProperty("trancheCount").GetInt32();
                var dailyYield = newInvested * 100;

                System.Console.WriteLine();
                System.Console.WriteLine($"  Invested:       {newInvested} tokens");
                System.Console.WriteLine($"  Available:      {newAvailable} tokens");
                System.Console.WriteLine($"  Tranches:       {trancheCount}");
                System.Console.WriteLine($"  Est. daily yield: {dailyYield:N0} credits");
                System.Console.WriteLine();
            }, amountOption, jsonOption);

            return command;
        }

        private Command BuildUninvestCommand()
        {
            var amountOption = new Option<decimal>("--amount", "Amount of tokens to uninvest");
            amountOption.AddAlias("-a");
            amountOption.IsRequired = true;

            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("uninvest", "Return invested tokens to available balance (FIFO, oldest first)");
            command.AddOption(amountOption);
            command.AddOption(jsonOption);

            command.SetHandler(async (decimal amount, bool json) =>
            {
                if (amount <= 0)
                {
                    System.Console.WriteLine("Amount must be greater than zero.");
                    return;
                }

                var response = await _client.PostAsync("/api/user/uninvest-tokens", new { amount });
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
                var tokensUninvested = data.GetProperty("tokensUninvested").GetDecimal();
                var newInvested = data.GetProperty("newInvestedTotal").GetDecimal();
                var newAvailable = data.GetProperty("newAvailableTokens").GetDecimal();

                System.Console.WriteLine();
                System.Console.WriteLine($"  Uninvested: {tokensUninvested} tokens");
                System.Console.WriteLine($"  Invested:   {newInvested} tokens");
                System.Console.WriteLine($"  Available:  {newAvailable} tokens");
                System.Console.WriteLine();
            }, amountOption, jsonOption);

            return command;
        }
    }
}

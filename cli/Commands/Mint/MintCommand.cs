using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Mint
{
    public class MintCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;

        public MintCommand(AuthenticatedHttpClient client)
        {
            _client = client;
        }

        public Command Build()
        {
            var command = new Command("mint", "Credit Mint commands (check rate, burn tokens for credits)");

            command.AddCommand(BuildRateCommand());
            command.AddCommand(BuildBurnCommand());

            return command;
        }

        private Command BuildRateCommand()
        {
            var jsonOption = new Option<bool>("--json", "Output as raw JSON");

            var command = new Command("rate", "Check current token-to-credit exchange rate");
            command.AddOption(jsonOption);

            command.SetHandler(async (bool json) =>
            {
                var response = await _client.GetAsync("/api/mint/rate");
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
                var currentRate = data.GetProperty("currentRate").GetInt32();
                var baseRate = data.GetProperty("baseRate").GetInt32();
                var floorRate = data.GetProperty("floorRate").GetInt32();
                var recentVolume = data.GetProperty("recentBurnVolume").GetDecimal();
                var windowHours = data.GetProperty("windowHours").GetInt32();
                var ratePercent = baseRate > 0 ? (int)Math.Round(100.0 * currentRate / baseRate) : 0;

                System.Console.WriteLine();
                System.Console.WriteLine($"  Current Rate:  {currentRate:N0} credits/token ({ratePercent}% of max)");
                System.Console.WriteLine($"  Rate Range:    {floorRate:N0} — {baseRate:N0} credits/token");
                System.Console.WriteLine($"  Recent Burns:  {recentVolume} tokens in last {windowHours}h");
                System.Console.WriteLine($"  1 token yield: {currentRate:N0} credits");
                System.Console.WriteLine();

                if (ratePercent > 80)
                    System.Console.WriteLine("  Rate is high — good time to burn.");
                else if (ratePercent < 40)
                    System.Console.WriteLine("  Rate is depressed — consider waiting.");
                else
                    System.Console.WriteLine("  Rate is moderate.");

                System.Console.WriteLine();
            }, jsonOption);

            return command;
        }

        private Command BuildBurnCommand()
        {
            var amountOption = new Option<decimal>("--amount", "Number of tokens to burn (min 0.1, max 100, increments of 0.1)");
            amountOption.AddAlias("-a");
            amountOption.IsRequired = true;

            var jsonOption = new Option<bool>("--json", "Output as raw JSON");
            var yesOption = new Option<bool>("--yes", "Skip confirmation prompt");
            yesOption.AddAlias("-y");

            var command = new Command("burn", "Burn tokens to receive corp credits (irreversible)");
            command.AddOption(amountOption);
            command.AddOption(jsonOption);
            command.AddOption(yesOption);

            command.SetHandler(async (decimal amount, bool json, bool yes) =>
            {
                if (!yes)
                {
                    // Fetch current rate so players know what they're getting before confirming
                    var rateResponse = await _client.GetAsync("/api/mint/rate");
                    if (rateResponse.IsSuccessStatusCode)
                    {
                        var rateContent = await rateResponse.Content.ReadAsStringAsync();
                        var rateData = JsonSerializer.Deserialize<JsonElement>(rateContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var currentRate = rateData.GetProperty("currentRate").GetInt32();
                        var estimatedCredits = (long)Math.Round(amount * currentRate);
                        System.Console.Write($"Burn {amount} token(s) for ~{estimatedCredits:N0} credits? This is irreversible. (y/N): ");
                    }
                    else
                    {
                        System.Console.Write($"Burn {amount} token(s) for credits? This is irreversible. (y/N): ");
                    }
                    var confirm = System.Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (confirm != "y" && confirm != "yes")
                    {
                        System.Console.WriteLine("Cancelled.");
                        return;
                    }
                }

                var response = await _client.PostAsync("/api/mint/burn", new { amount });
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
                var tokensBurned = data.GetProperty("tokensBurned").GetDecimal();
                var rateApplied = data.GetProperty("rateApplied").GetInt32();
                var creditsReceived = data.GetProperty("creditsReceived").GetInt64();
                var newTokenBalance = data.GetProperty("newTokenBalance").GetDecimal();
                var newCorpCredits = data.GetProperty("newCorpCredits").GetInt64();

                System.Console.WriteLine();
                System.Console.WriteLine($"  Burned:         {tokensBurned} tokens");
                System.Console.WriteLine($"  Rate Applied:   {rateApplied:N0} credits/token");
                System.Console.WriteLine($"  Credits Minted: {creditsReceived:N0}");
                System.Console.WriteLine($"  Token Balance:  {newTokenBalance}");
                System.Console.WriteLine($"  Corp Credits:   {newCorpCredits:N0}");
                System.Console.WriteLine();
            }, amountOption, jsonOption, yesOption);

            return command;
        }
    }
}

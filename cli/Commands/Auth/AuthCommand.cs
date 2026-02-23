using System.CommandLine;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Auth;

public class AuthCommand : ICommand
{
    private readonly AuthenticatedHttpClient _authClient;
    private readonly CliConfig _config;

    public AuthCommand(AuthenticatedHttpClient authClient, CliConfig config)
    {
        _authClient = authClient;
        _config = config;
    }

    public Command Build()
    {
        var authCommand = new Command("auth", "Authentication management");

        authCommand.AddCommand(BuildLoginCommand());
        authCommand.AddCommand(BuildLogoutCommand());
        authCommand.AddCommand(BuildCreateApiKeyCommand());
        authCommand.AddCommand(BuildRevokeApiKeyCommand());
        authCommand.AddCommand(BuildStatusCommand());

        return authCommand;
    }

    private Command BuildLoginCommand()
    {
        var apiKeyOption = new Option<string?>("--api-key", "Store an API key for authentication");
        apiKeyOption.AddAlias("-k");

        var command = new Command("login", "Log in via browser or store an API key");
        command.AddOption(apiKeyOption);

        command.SetHandler(async (string? apiKey) =>
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                // API key mode — just store it
                await _config.SetApiKey(apiKey);
                System.Console.WriteLine("API key stored. All future requests will use this key.");
                return;
            }

            // TODO: Full PKCE flow with localhost callback is deferred.
            // For now, direct users to the web login page to get an API key.
            var baseUrl = _config.System.BaseUrl ?? "https://api.psecsapi.com";
            var webUrl = baseUrl.Replace("api.", "");

            System.Console.WriteLine("Opening browser for login...");
            System.Console.WriteLine($"If the browser doesn't open, visit: {webUrl}/account/login");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"{webUrl}/account/login",
                    UseShellExecute = true
                });
            }
            catch
            {
                System.Console.WriteLine($"Please open this URL in your browser: {webUrl}/account/login");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("After logging in, copy your API key from the dashboard and run:");
            System.Console.WriteLine("  papi auth login --api-key <your-key>");
        }, apiKeyOption);

        return command;
    }

    private Command BuildLogoutCommand()
    {
        var command = new Command("logout", "Log out and clear stored credentials");

        command.SetHandler(async () =>
        {
            try
            {
                await _authClient.PostAsync("/api/auth/logout", null);
            }
            catch
            {
                // Ignore errors — we're logging out anyway
            }

            await _config.ClearAuthTokens();
            System.Console.WriteLine("Logged out successfully.");
        });

        return command;
    }

    private Command BuildCreateApiKeyCommand()
    {
        var command = new Command("create-api-key",
            "Generate a new API key. Requires an active JWT session — log in via the web app first.");

        command.SetHandler(async () =>
        {
            var response = await _authClient.PostAsync("/api/auth/api-key", null);

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Failed to create API key: {response.StatusCode}");
                var body = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(body))
                    System.Console.WriteLine(body);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var apiKey = result.GetProperty("apiKey").GetString()!;

            System.Console.WriteLine("New API key generated:");
            System.Console.WriteLine();
            System.Console.WriteLine($"  {apiKey}");
            System.Console.WriteLine();
            System.Console.WriteLine("Copy this key now — it will not be shown again.");
            System.Console.WriteLine();
            System.Console.WriteLine("To use it with the CLI:");
            System.Console.WriteLine("  papi auth login --api-key <paste-key-here>");
        });

        return command;
    }

    private Command BuildRevokeApiKeyCommand()
    {
        var command = new Command("revoke-api-key", "Revoke your current API key");

        command.SetHandler(async () =>
        {
            var response = await _authClient.DeleteAsync("/api/auth/api-key");

            if (response.IsSuccessStatusCode)
            {
                await _config.SetApiKey(null);
                System.Console.WriteLine("API key revoked.");
            }
            else
            {
                System.Console.WriteLine($"Failed to revoke API key: {response.StatusCode}");
            }
        });

        return command;
    }

    private Command BuildStatusCommand()
    {
        var command = new Command("status", "Show current authentication status");

        command.SetHandler(() =>
        {
            System.Console.WriteLine("Authentication Status:");
            System.Console.WriteLine($"  User ID:      {_config.User.UserId ?? "Not logged in"}");
            System.Console.WriteLine($"  API Key:      {(string.IsNullOrEmpty(_config.User.ApiKey) ? "Not set" : "Active (stored)")}");
            System.Console.WriteLine($"  JWT Token:    {(string.IsNullOrEmpty(_config.User.AccessToken) ? "None" : "Present")}");
            System.Console.WriteLine($"  Default Corp: {_config.User.DefaultCorpId?.ToString() ?? "Not set"}");

            if (!string.IsNullOrEmpty(_config.User.ApiKey))
                System.Console.WriteLine("  Auth Mode:    API Key");
            else if (!string.IsNullOrEmpty(_config.User.AccessToken))
                System.Console.WriteLine("  Auth Mode:    JWT (interactive)");
            else
                System.Console.WriteLine("  Auth Mode:    Not authenticated");

            return Task.CompletedTask;
        });

        return command;
    }
}

using System.CommandLine;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Config
{
    public class ConfigCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;

        public ConfigCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("config", "Configuration commands");

            command.AddCommand(BuildSetCorpCommand());
            command.AddCommand(BuildGetCorpCommand());
            command.AddCommand(BuildSetUrlCommand());
            command.AddCommand(BuildShowCommand());

            return command;
        }

        private Command BuildSetCorpCommand()
        {
            var corpIdArg = new Argument<Guid>("corp-id", "Corporation ID to set as default");

            var command = new Command("set-corp", "Set default corporation ID") { corpIdArg };
            command.SetHandler(async (corpId) =>
            {
                await _config.SetDefaultCorpId(corpId);
                System.Console.WriteLine($"Default corporation ID set to: {corpId}");
            }, corpIdArg);

            return command;
        }

        private Command BuildGetCorpCommand()
        {
            var command = new Command("get-corp", "Get default corporation ID");
            command.SetHandler(() =>
            {
                if (_config.User.DefaultCorpId.HasValue)
                    System.Console.WriteLine($"Default corporation ID: {_config.User.DefaultCorpId}");
                else
                    System.Console.WriteLine("No default corporation ID set.");
            });
            return command;
        }

        private Command BuildSetUrlCommand()
        {
            var urlArg = new Argument<string>("url", "API base URL to use (e.g., https://api.psecsapi.com)");

            var command = new Command("set-url", "Set the API base URL") { urlArg };
            command.SetHandler(async (url) =>
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    System.Console.WriteLine($"Error: Invalid URL '{url}'. Must be a valid HTTP or HTTPS URL.");
                    return;
                }

                await _config.SetBaseUrl(url);
                System.Console.WriteLine($"API base URL set to: {url}");
                System.Console.WriteLine("Note: Restart the CLI for the new URL to take effect.");
            }, urlArg);

            return command;
        }

        private Command BuildShowCommand()
        {
            var command = new Command("show", "Show current configuration");
            command.SetHandler(() =>
            {
                System.Console.WriteLine($"Base URL:        {_config.System.BaseUrl}");
                System.Console.WriteLine($"User ID:         {_config.User.UserId ?? "(not logged in)"}");
                System.Console.WriteLine($"Default Corp ID: {_config.User.DefaultCorpId?.ToString() ?? "(not set)"}");
                System.Console.WriteLine($"API Key:         {(string.IsNullOrEmpty(_config.User.ApiKey) ? "Not set" : "Active (stored)")}");

                if (!string.IsNullOrEmpty(_config.User.ApiKey))
                    System.Console.WriteLine($"Auth Mode:       API Key");
                else if (!string.IsNullOrEmpty(_config.User.AccessToken))
                    System.Console.WriteLine($"Auth Mode:       JWT (interactive)");
                else
                    System.Console.WriteLine($"Auth Mode:       Not authenticated");
            });
            return command;
        }
    }
}

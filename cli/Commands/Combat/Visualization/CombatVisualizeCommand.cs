using System.CommandLine;
using System.Diagnostics;

namespace psecsapi.Console.Commands.Combat.Visualization;

/// <summary>
/// Implements the 'papi combat visualize' command.
/// Loads a replay binary file, starts a local HTTP server, and opens the browser
/// to display the combat visualizer.
/// </summary>
public static class CombatVisualizeCommand
{
    public static Command Build()
    {
        var replayFileArg = new Argument<string>(
            "replay-file",
            () => "combat-replay.bin",
            "Path to the replay binary file");

        var portOption = new Option<int>("--port", () => 0, "Port for local HTTP server (0 = auto)");
        portOption.AddAlias("-p");

        var noBrowserOption = new Option<bool>("--no-browser", "Do not open browser automatically");

        var cmd = new Command("visualize", "Visualize a combat replay in the browser")
        {
            replayFileArg, portOption, noBrowserOption
        };

        cmd.SetHandler(async (string replayFile, int port, bool noBrowser) =>
        {
            try
            {
                await RunVisualizer(replayFile, port, noBrowser);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, replayFileArg, portOption, noBrowserOption);

        return cmd;
    }

    private static async Task RunVisualizer(string replayFile, int port, bool noBrowser)
    {
        var fullPath = Path.GetFullPath(replayFile);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Replay file not found: {fullPath}");

        var replayBytes = await File.ReadAllBytesAsync(fullPath);
        if (replayBytes.Length == 0)
            throw new InvalidOperationException("Replay file is empty.");

        using var server = new ReplayServer(replayBytes, port);

        System.Console.Error.WriteLine($"Serving combat visualizer at: {server.Url}");
        System.Console.Error.WriteLine("Press Ctrl+C to stop.");

        if (!noBrowser)
        {
            OpenBrowser(server.Url);
        }

        // Run server until cancelled
        var cts = new CancellationTokenSource();
        System.Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            cts.Cancel();
            server.Stop();
        };

        try
        {
            await server.StartAsync().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }

        System.Console.Error.WriteLine("Server stopped.");
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }
        catch
        {
            System.Console.Error.WriteLine($"Could not open browser. Navigate to: {url}");
        }
    }
}

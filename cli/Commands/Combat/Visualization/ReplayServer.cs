using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using psecsapi.Combat.Events;

namespace psecsapi.Console.Commands.Combat.Visualization;

/// <summary>
/// Simple local HTTP server that serves the combat visualizer HTML page
/// and provides the replay data as JSON via the /replay endpoint.
/// Uses HttpListener for zero external dependencies.
/// </summary>
public class ReplayServer : IDisposable
{
    private readonly byte[] _replayBinary;
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts;
    private readonly int _port;

    public int Port => _port;
    public string Url => $"http://localhost:{_port}/";

    public ReplayServer(byte[] replayBinary, int port = 0)
    {
        _replayBinary = replayBinary;
        _cts = new CancellationTokenSource();

        // Find an available port if 0 was specified
        _port = port > 0 ? port : FindAvailablePort();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }

    public async Task StartAsync()
    {
        _listener.Start();

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(_cts.Token);
                _ = HandleRequestAsync(context).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        System.Console.Error.WriteLine($"ReplayServer: unhandled error: {t.Exception?.GetBaseException()}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener.Stop();
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";

            switch (path)
            {
                case "/":
                    await ServeHtml(context.Response);
                    break;
                case "/replay":
                    await ServeReplayJson(context.Response);
                    break;
                default:
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"ReplayServer: error handling {context.Request.Url}: {ex}");
            try { context.Response.Close(); } catch { /* ignore close errors */ }
        }
    }

    private async Task ServeHtml(HttpListenerResponse response)
    {
        var html = LoadVisualizerHtml();
        var bytes = Encoding.UTF8.GetBytes(html);

        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private async Task ServeReplayJson(HttpListenerResponse response)
    {
        // Deserialize the binary replay into event objects, then serialize to JSON.
        // We must serialize each event as its concrete type (not as base CombatEvent)
        // so that derived properties (gridWidth, shipLoadouts, etc.) are included.
        var events = CombatEventRecorder.Deserialize(_replayBinary);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Serialize each event using its runtime type to get polymorphic output
        var json = "[" + string.Join(",",
            events.Select(e => JsonSerializer.Serialize(e, e.GetType(), jsonOptions))
        ) + "]";
        var bytes = Encoding.UTF8.GetBytes(json);

        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.AppendHeader("Access-Control-Allow-Origin", "*");
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private static string LoadVisualizerHtml()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;
        var htmlPath = Path.Combine(assemblyDir, "Visualizer", "combat-visualizer.html");

        if (File.Exists(htmlPath))
            return File.ReadAllText(htmlPath);

        // Fallback if file not found
        return """
            <!DOCTYPE html>
            <html>
            <body>
                <h1>Combat Visualizer</h1>
                <p>Visualizer HTML not found. Expected at: Visualizer/combat-visualizer.html</p>
            </body>
            </html>
            """;
    }

    private static int FindAvailablePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
        (_listener as IDisposable)?.Dispose();
    }
}

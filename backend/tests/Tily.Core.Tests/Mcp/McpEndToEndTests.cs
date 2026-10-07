using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tily.Core.Mcp;
using Tily.Core.Tests.Terminal;
using Xunit;

namespace Tily.Core.Tests.Mcp;

[Collection(TerminalCollection.Name)]
public sealed class McpEndToEndTests : IClassFixture<McpEndToEndTests.HostFixture>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private const string Refused = "Demande MCP refusée";

    private readonly HostFixture _host;

    public McpEndToEndTests(HostFixture host)
    {
        _host = host;
    }

    [Fact]
    public async Task CallTool_WhenTilyMcpRunsInsidePane_ThenRequestReachesWebAndAnswerReturns()
    {
        await _host.CreatePaneAsync("pane-mcp");

        var result = await _host.CallLayoutFromPaneAsync("pane-mcp", "inside");

        Assert.False(result["isError"]?.GetValue<bool>() ?? false, $"Réponse en erreur : {TextOf(result)}");
        Assert.Contains("pane-mcp", TextOf(result));
        Assert.Contains(_host.Requests, request => request["pane"]?.GetValue<string>() == "pane-mcp");
    }

    [Fact]
    public async Task Send_WhenClientIsOutsidePane_ThenRefused()
    {
        await _host.CreatePaneAsync("pane-cible");
        var requestsBefore = _host.Requests.Count;

        var response = await McpPipeClient.SendAsync(McpEndpoint.PipeName(_host.DataDirectory), new McpPipeRequestModel("layout", "pane-cible", null), McpPipeClient.ConnectTimeout, CancellationToken.None);

        Assert.Contains(Refused, response.Error);
        Assert.Equal(requestsBefore, _host.Requests.Count);
    }

    [Fact]
    public async Task CallTool_WhenPaneSpoofedFromAnotherPane_ThenRefused()
    {
        await _host.CreatePaneAsync("pane-source");

        var result = await _host.CallLayoutFromPaneAsync("pane-source", "spoofed", "TILY_PANE_ID=inconnu ");

        Assert.True(result["isError"]?.GetValue<bool>(), $"La demande aurait dû être refusée : {TextOf(result)}");
        Assert.Contains(Refused, TextOf(result));
        Assert.DoesNotContain(_host.Requests, request => request["pane"]?.GetValue<string>() == "inconnu");
    }

    private static string TextOf(JsonObject result) =>
        string.Concat(result["content"]?.AsArray().Select(block => block?["text"]?.GetValue<string>()) ?? []);

    public sealed class HostFixture : IAsyncLifetime
    {
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);

        private readonly ConcurrentQueue<JsonObject> _messages = new();
        private readonly object _inputLock = new();
        private Process? _process;

        public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), $"tily-mcp-e2e-{Guid.NewGuid():N}");

        public ConcurrentQueue<JsonObject> Requests { get; } = new();

        public async Task InitializeAsync()
        {
            Directory.CreateDirectory(DataDirectory);
            var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Tily"))
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.Environment["HOME"] = DataDirectory;
            start.Environment["TILY_DATA_DIR"] = DataDirectory;
            _process = Process.Start(start) ?? throw new InvalidOperationException("Impossible de lancer l’hôte Tily.");
            _process.ErrorDataReceived += (_, _) => { };
            _process.BeginErrorReadLine();
            _ = Task.Run(ReadOutputAsync);

            Send(new JsonObject { ["type"] = "app.ready" });
            Send(new JsonObject { ["type"] = "mcp.install" });
            await WaitForAsync(() => _messages.Any(message => message["type"]?.GetValue<string>() == "settings.result" && message["mcp"]?["listening"]?.GetValue<bool>() == true));
        }

        public async Task CreatePaneAsync(string paneId)
        {
            Send(new JsonObject { ["type"] = "terminal.create", ["pane"] = paneId, ["shell"] = "zsh", ["cwd"] = DataDirectory, ["cols"] = 120, ["rows"] = 30 });
            await WaitForAsync(() => _messages.Any(message => message["type"]?.GetValue<string>() == "terminal.cwd" && message["pane"]?.GetValue<string>() == paneId));
        }

        public async Task<JsonObject> CallLayoutFromPaneAsync(string paneId, string name, string environmentPrefix = "")
        {
            var input = Path.Combine(DataDirectory, $"{name}.rpc.jsonl");
            var output = Path.Combine(DataDirectory, $"{name}.out.jsonl");
            await File.WriteAllLinesAsync(input,
            [
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"e2e","version":"1"}}}""",
                """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
                """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"tily_layout","arguments":{}}}"""
            ]);
            var mcp = Path.Combine(AppContext.BaseDirectory, ClaudeMcpInstaller.ExecutableName);
            Send(new JsonObject { ["type"] = "terminal.input", ["pane"] = paneId, ["data"] = $"(cat '{input}'; sleep 20) | {environmentPrefix}'{mcp}' > '{output}' 2>&1 &\r" });

            JsonObject? result = null;
            await WaitForAsync(() => (result = ToolResult(output)) is not null);
            return result!;
        }

        private static JsonObject? ToolResult(string output)
        {
            if (!File.Exists(output))
            {
                return null;
            }

            string[] lines;
            try
            {
                using var stream = new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                lines = reader.ReadToEnd().Split('\n');
            }
            catch (IOException)
            {
                return null;
            }

            foreach (var line in lines)
            {
                try
                {
                    if (JsonNode.Parse(line) is JsonObject message && message["id"]?.GetValue<int>() == 2)
                    {
                        return message["result"] as JsonObject ?? throw new InvalidOperationException($"Réponse JSON-RPC sans résultat : {line}");
                    }
                }
                catch (JsonException)
                {
                }
            }

            return null;
        }

        private async Task ReadOutputAsync()
        {
            while (await _process!.StandardOutput.ReadLineAsync() is { } line)
            {
                if (JsonNode.Parse(line) is not JsonObject message)
                {
                    continue;
                }

                _messages.Enqueue(message);
                if (message["type"]?.GetValue<string>() == "mcp.request")
                {
                    Requests.Enqueue(message);
                    Send(new JsonObject { ["type"] = "mcp.response", ["id"] = message["id"]?.GetValue<string>(), ["result"] = new JsonObject { ["pane"] = message["pane"]?.GetValue<string>() } });
                }
            }
        }

        private void Send(JsonObject message)
        {
            lock (_inputLock)
            {
                _process!.StandardInput.WriteLine(message.ToJsonString());
                _process.StandardInput.Flush();
            }
        }

        public async Task DisposeAsync()
        {
            if (_process is not null)
            {
                try
                {
                    Send(new JsonObject { ["type"] = "window.close" });
                    using var stop = new CancellationTokenSource(StopTimeout);
                    await _process.WaitForExitAsync(stop.Token);
                }
                catch (Exception exception) when (exception is OperationCanceledException or IOException)
                {
                    _process.Kill(true);
                }

                _process.Dispose();
            }

            if (Directory.Exists(DataDirectory))
            {
                Directory.Delete(DataDirectory, true);
            }
        }
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.Elapsed > Timeout)
            {
                throw new TimeoutException("Condition non remplie dans le délai imparti.");
            }

            await Task.Delay(100);
        }
    }
}

using System.IO.Pipes;
using Tily.Core.StatusLog;

namespace Tily.Core.Mcp;

public sealed class McpPipeServer : IDisposable
{
    private const int ReadBufferSize = 4096;
    private const int MaxRequestBytes = 1024 * 1024;
    private static readonly TimeSpan RequestReadTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CreateRetryDelay = TimeSpan.FromSeconds(1);

    private readonly string _pipeName;
    private readonly Func<McpPipeRequestModel, CancellationToken, Task<McpPipeResponseModel>> _handle;
    private readonly CancellationTokenSource _stop = new();
    private NamedPipeServerStream? _waiting;
    private NamedPipeServerStream? _listenerKeeper;

    public McpPipeServer(string pipeName, Func<McpPipeRequestModel, CancellationToken, Task<McpPipeResponseModel>> handle)
    {
        _pipeName = pipeName;
        _handle = handle;
    }

    public void Start()
    {
        NamedPipeServerStream first;
        try
        {
            first = Create(PipeOptions.FirstPipeInstance);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("Serveur MCP indisponible : une autre instance de Tily utilise déjà le même dossier de données.", exception);
        }

        _listenerKeeper = TryCreate();
        _ = AcceptAsync(first);
    }

    private NamedPipeServerStream Create(PipeOptions extra) =>
        new(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | extra);

    private async Task AcceptAsync(NamedPipeServerStream first)
    {
        var token = _stop.Token;
        NamedPipeServerStream? pending = first;
        while (pending is not null)
        {
            _waiting = pending;
            if (token.IsCancellationRequested)
            {
                await pending.DisposeAsync();
                return;
            }

            try
            {
                await pending.WaitForConnectionAsync(token);
                _ = ServeAsync(pending, token);
            }
            catch (Exception exception) when (exception is OperationCanceledException or IOException or ObjectDisposedException or UnauthorizedAccessException)
            {
                await pending.DisposeAsync();
            }

            pending = await NextListenerAsync(token);
        }
    }

    private async Task<NamedPipeServerStream?> NextListenerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (TryCreate() is { } listener)
            {
                return listener;
            }

            try
            {
                await Task.Delay(CreateRetryDelay, token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    private NamedPipeServerStream? TryCreate()
    {
        try
        {
            return Create(PipeOptions.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            return null;
        }
    }

    private async Task ServeAsync(NamedPipeServerStream stream, CancellationToken token)
    {
        await using (stream)
        {
            try
            {
                var line = await ReadRequestLineAsync(stream, token);
                var request = McpPipe.Parse<McpPipeRequestModel>(line);
                var response = await RespondAsync(request is null ? null : request with { PeerProcessId = McpPeer.ProcessIdOf(stream) }, token);
                await stream.WriteAsync(McpPipe.Line(response), token);
                await stream.FlushAsync(token);
                stream.WaitForPipeDrain();
            }
            catch (Exception exception) when (exception is IOException or OperationCanceledException or ObjectDisposedException)
            {
            }
        }
    }

    private static async Task<string?> ReadRequestLineAsync(Stream stream, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(RequestReadTimeout);
        using var line = new MemoryStream();
        var buffer = new byte[ReadBufferSize];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, timeout.Token);
            if (read == 0)
            {
                break;
            }

            var end = Array.IndexOf(buffer, (byte)'\n', 0, read);
            line.Write(buffer, 0, end < 0 ? read : end);
            if (line.Length > MaxRequestBytes)
            {
                return null;
            }

            if (end >= 0)
            {
                break;
            }
        }

        return line.Length == 0 ? null : McpPipe.Utf8.GetString(line.GetBuffer(), 0, (int)line.Length).TrimEnd('\r');
    }

    private async Task<McpPipeResponseModel> RespondAsync(McpPipeRequestModel? request, CancellationToken token)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Tool))
        {
            return McpPipeResponseModel.Failure(McpPipe.Unreadable);
        }

        try
        {
            return await _handle(request, token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return McpPipeResponseModel.Failure(UserErrorMessage.Of(exception));
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _waiting?.Dispose();
        _listenerKeeper?.Dispose();
    }
}

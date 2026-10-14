using Phoenix.AssetTool.Core.Network;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Phoenix.Framework.AssetImport;

/// <summary>
/// Client connecting to the Phoenix Asset Tool WebSocket IPC server.
/// Listens for asset reloads and notifications and dispatches them on the main game thread.
/// </summary>
public sealed class AssetIpcClient : IDisposable
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private readonly ConcurrentQueue<AssetReloadMessage> _pendingReloads = new();
    private readonly ConcurrentQueue<AssetLogMessage> _pendingLogs = new();
    private bool _disposed;

    public bool IsConnected => _ws?.State == WebSocketState.Open;
    public string? ServerEndpoint { get; private set; }

    public event Action<ModelReloadEventArgs>? ModelReloaded;
    public event Action<TextureReloadEventArgs>? TextureReloaded;
    public event Action<ShaderReloadEventArgs>? ShaderReloaded;
    public event Action<AssetReloadMessage>? AssetReloaded;
    public event Action<AssetLogMessage>? LogReceived;

    public void Connect(string host = "127.0.0.1", int port = 28473)
    {
        if (IsConnected)
            return;

        ServerEndpoint = $"{host}:{port}";
        _cts = new CancellationTokenSource();
        _ws = new ClientWebSocket();

        _ = Task.Run(async () =>
        {
            try
            {
                var uri = new Uri($"ws://{host}:{port}/ws/");
                await _ws.ConnectAsync(uri, _cts.Token);
                _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));
            }
            catch
            {
                // Asset server may not be running, soft failure
            }
        });
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        try
        {
            _ws?.Dispose();
        }
        catch
        {
        }
        _ws = null;
        _cts = null;
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        var buffer = new byte[8192];
        var ms = new MemoryStream();

        while (!token.IsCancellationRequested && _ws != null && _ws.State == WebSocketState.Open)
        {
            try
            {
                ms.SetLength(0);
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(ms.ToArray());
                    var msg = JsonSerializer.Deserialize<IpcMessage>(json);
                    if (msg != null)
                    {
                        if (msg.Type == IpcMessageType.AssetReload)
                        {
                            var reload = msg.DeserializePayload<AssetReloadMessage>();
                            if (reload != null)
                            {
                                _pendingReloads.Enqueue(reload);
                            }
                        }
                        else if (msg.Type == IpcMessageType.LogStream)
                        {
                            var log = msg.DeserializePayload<AssetLogMessage>();
                            if (log != null)
                            {
                                _pendingLogs.Enqueue(log);
                            }
                        }
                    }
                }
            }
            catch
            {
                break;
            }
        }
    }

    /// <summary>
    /// Processes all queued reload and log events on the caller's thread (game update thread).
    /// </summary>
    public void Update()
    {
        while (_pendingLogs.TryDequeue(out var log))
        {
            LogReceived?.Invoke(log);
        }

        while (_pendingReloads.TryDequeue(out var reload))
        {
            AssetReloaded?.Invoke(reload);

            string ext = Path.GetExtension(reload.RelativePath).ToLowerInvariant();
            if (ext is ".fbx" or ".obj" or ".gltf" or ".glb" or ".dae" or ".phxm")
            {
                var reloadedModel = AssetLoader.InvalidateAndReloadModel(reload.RelativePath);
                ModelReloaded?.Invoke(new ModelReloadEventArgs(reload.RelativePath, reloadedModel));
            }
            else if (ext is ".png" or ".jpg" or ".jpeg" or ".tga" or ".bmp" or ".ptex")
            {
                var reloadedTex = AssetLoader.InvalidateAndReloadTexture(reload.RelativePath);
                TextureReloaded?.Invoke(new TextureReloadEventArgs(reload.RelativePath, reloadedTex));
            }
            else if (ext is ".vert" or ".frag" or ".comp" or ".geom")
            {
                ShaderReloaded?.Invoke(new ShaderReloadEventArgs(reload.RelativePath));
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Disconnect();
        _disposed = true;
    }
}

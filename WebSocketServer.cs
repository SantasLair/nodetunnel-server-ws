using NodeTunnel.TCP;
using System.Net;

namespace NodeTunnel;

public class WebSocketServer {
    private readonly int _port;
    private readonly TCPHandler _tcpHandler;

    public WebSocketServer(int port, TCPHandler tcpHandler) {
        _port = port;
        _tcpHandler = tcpHandler;
    }

    public async Task StartAsync() {
        if (_tcpHandler == null)
        {
            throw new InvalidOperationException("TCPHandler is not initialized.");
        }

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{_port}/");
        listener.Start();
        Console.WriteLine($"WebSocket server started on port {_port}");

        while (true) {
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest) {
                var wsContext = await context.AcceptWebSocketAsync(null);
                var adapter = new WebSocketTCPAdapter(wsContext.WebSocket, _tcpHandler);
                _ = adapter.StartAsync(); // Fire and forget
            } else {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }
}

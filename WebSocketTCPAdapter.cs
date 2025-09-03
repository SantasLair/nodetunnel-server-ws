using NodeTunnel.TCP;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace NodeTunnel;

public class WebSocketTCPAdapter {
    private readonly WebSocket _webSocket;
    private readonly TCPHandler _tcpHandler;
    private TcpClient? _tcpClient;
    private NetworkStream? _tcpStream;

    public WebSocketTCPAdapter(WebSocket webSocket, TCPHandler tcpHandler) {
        _webSocket = webSocket;
        _tcpHandler = tcpHandler;
    }

    public async Task StartAsync() {
        try {
            // Connect to TCPHandler as a virtual client
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync("localhost", 8888); // Assuming TCPHandler port
            _tcpStream = _tcpClient.GetStream();

            // Relay tasks
            var wsToTcp = RelayWebSocketToTcp();
            var tcpToWs = RelayTcpToWebSocket();

            await Task.WhenAny(wsToTcp, tcpToWs);
        } catch (Exception ex) {
            Console.WriteLine($"WebSocket adapter error: {ex.Message}");
        } finally {
            _tcpClient?.Close();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
        }
    }

    private async Task RelayWebSocketToTcp() {
        var buffer = new byte[4096];
        while (_webSocket.State == WebSocketState.Open) {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close) break;
            await _tcpStream!.WriteAsync(buffer, 0, result.Count);
        }
    }

    private async Task RelayTcpToWebSocket() {
        var buffer = new byte[4096];
        while (_tcpClient!.Connected) {
            var bytesRead = await _tcpStream!.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}

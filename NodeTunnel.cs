using NodeTunnel.HTTP;
using NodeTunnel.TCP;
using NodeTunnel.UDP;

namespace NodeTunnel;

public class NodeTunnel {
    public static async Task Main() {
        var tcpHandler = new TCPHandler();
        var udpHandler = new UDPHandler(tcpHandler);
        var statusServer = new StatusServer(tcpHandler);
        var webSocketServer = new WebSocketServer(8081, tcpHandler); // New: WebSocket server

        tcpHandler.PeerDisconnected += udpHandler.HandlePeerDisconnected;
        tcpHandler.PeersDisconnected += udpHandler.HandlePeersDisconnected;
        
        try {
            var tcpTask = tcpHandler.StartTcpAsync();
            Console.WriteLine("TCP Started");
            var udpTask = udpHandler.StartUdpAsync();
            Console.WriteLine("UDP Started");
            var statusTask = statusServer.StartAsync();
            Console.WriteLine("HTTP Started");
            var wsTask = webSocketServer.StartAsync(); // New: Start WebSocket server
            Console.WriteLine("WebSocket Started");

            var completedTask = await Task.WhenAny(tcpTask, udpTask, statusTask, wsTask); // Updated: Include wsTask
            if (completedTask == tcpTask) {
                Console.WriteLine("TCP task completed");
                if (tcpTask.IsFaulted) Console.WriteLine($"TCP error: {tcpTask.Exception?.GetBaseException().Message}");
            }
            else if (completedTask == udpTask) {
                Console.WriteLine("UDP task completed");
                if (udpTask.IsFaulted) Console.WriteLine($"UDP error: {udpTask.Exception?.GetBaseException().Message}");
            }
            else if (completedTask == statusTask) {
                Console.WriteLine("HTTP task completed");
                if (statusTask.IsFaulted) Console.WriteLine($"HTTP error: {statusTask.Exception?.GetBaseException().Message}");
            }
            else if (completedTask == wsTask) { // New: Handle WebSocket task
                Console.WriteLine("WebSocket task completed");
                if (wsTask.IsFaulted) Console.WriteLine($"WebSocket error: {wsTask.Exception?.GetBaseException().Message}");
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Server error: {ex.Message}");
        }
        
        Console.WriteLine("Server stopped.");
    }
}

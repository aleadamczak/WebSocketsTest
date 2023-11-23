using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class WebSocketClient
{
    static async Task Main(string[] args)
    {
        // Set the WebSocket server URL
        Uri serverUri = new Uri("ws://localhost:8080/?topic=User1");

        using (ClientWebSocket clientWebSocket = new ClientWebSocket())
        {
            try
            {
                // Connect to the WebSocket server
                await clientWebSocket.ConnectAsync(serverUri, CancellationToken.None);
                Console.WriteLine("Connected to WebSocket server");

                // Subscribe to receive messages
                _ = ReceiveMessages(clientWebSocket);

                // Send messages to the server
                while (true)
                {
                    Console.Write("Enter message: ");
                    string message = Console.ReadLine();
                    await SendMessage(clientWebSocket, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    static async Task ReceiveMessages(ClientWebSocket clientWebSocket)
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (clientWebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message: {receivedMessage}");
                }
            }
        }
        catch (WebSocketException)
        {
            // WebSocket closed
            Console.WriteLine("WebSocket connection closed.");
        }
    }

    static async Task SendMessage(ClientWebSocket clientWebSocket, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        await clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

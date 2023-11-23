using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

class WebSocketServer
{
    private static Dictionary<string, List<WebSocket>> topics = new Dictionary<string, List<WebSocket>>();

    static async Task Main(string[] args)
    {
        // Set the URL for the server
        string serverUrl = "http://localhost:8080/";

        // Create and start the HTTP listener
        using (HttpListener httpListener = new HttpListener())
        {
            httpListener.Prefixes.Add(serverUrl);
            httpListener.Start();
            Console.WriteLine($"Server listening on {serverUrl}");

            // Handle incoming requests
            while (true)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    ProcessWebSocketRequest(context);
                }
                else
                {
                    // Handle other types of HTTP requests if needed
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
    }

    static async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
        WebSocket webSocket = webSocketContext.WebSocket;

        // Get the requested topic from the query string
        string topic = context.Request.QueryString["topic"];

        if (string.IsNullOrEmpty(topic))
        {
            // If no topic is specified, close the connection
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "No topic specified.", CancellationToken.None);
        }
        else
        {
            // Add the WebSocket to the specified topic
            AddToTopic(topic, webSocket);

            // Handle WebSocket connections here
            Console.WriteLine($"WebSocket connection established for topic: {topic}");

            // Example: Send a welcome message
            string welcomeMessage = $"WebSocket connection established for topic: {topic}. Welcome!";
            await SendWebSocketMessage(webSocket, welcomeMessage);

            // Example: Receive and process messages
            await ReceiveWebSocketMessages(webSocket, topic);
        }
    }

    static void AddToTopic(string topic, WebSocket webSocket)
    {
        lock (topics)
        {
            if (!topics.ContainsKey(topic))
            {
                topics[topic] = new List<WebSocket>();
            }

            topics[topic].Add(webSocket);
        }
    }

    static void RemoveFromTopic(string topic, WebSocket webSocket)
    {
        lock (topics)
        {
            if (topics.ContainsKey(topic))
            {
                topics[topic].Remove(webSocket);

                // Remove the topic if there are no more WebSocket connections
                if (topics[topic].Count == 0)
                {
                    topics.Remove(topic);
                }
            }
        }
    }

    static async Task SendWebSocketMessage(WebSocket webSocket, string message)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    static async Task ReceiveWebSocketMessages(WebSocket webSocket, string topic)
    {
        byte[] buffer = new byte[1024];
        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string receivedMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received message for topic {topic}: {receivedMessage}");

                // Broadcast the message to all clients in the same topic
                BroadcastMessage(topic, receivedMessage);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                // Remove the WebSocket from the topic when the connection is closed
                RemoveFromTopic(topic, webSocket);

                // Handle WebSocket close message
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }
    }

    static async Task BroadcastMessage(string topic, string message)
    {
        lock (topics)
        {
            if (topics.ContainsKey(topic))
            {
                // Broadcast the message to all clients in the same topic
                foreach (var client in topics[topic])
                {
                    SendWebSocketMessage(client, message);
                }
            }
        }
    }
}

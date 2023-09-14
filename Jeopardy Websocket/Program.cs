using Jeopardy_Websocket;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program {
    public static TaskFactory taskFactory = new TaskFactory();
    public readonly static string name = "Ratatui Server";
    public readonly static string version = "0.1";
    public readonly static string buildID = "0001";
    public static DataManager dataManager = new DataManager();
    public static async Task CLI() {
        Console.Write(">");
        string input = Console.ReadLine();
        while (true) {
            Console.Write('>');
            input = Console.ReadLine();
        }

    }

    private static async Task Main(string[] args) {
        Console.WriteLine($"{name} Version {version} id {buildID}");
        Console.WriteLine($"Loading at {Environment.CurrentDirectory}");
        Console.WriteLine($"For User {Environment.UserName} for Machine {Environment.MachineName}");
        Console.WriteLine("Initializing Files");
        dataManager.LoadUsers("User.json");
        dataManager.LoadQuestions("data.json");
        Console.WriteLine("Successfully decoded data");
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Console.WriteLine($"{name} WebSocket server started...");
        Console.WriteLine("Load Terminal cli..");
        await taskFactory.StartNew(CLI);
        while (true) {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest) {
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                Console.WriteLine($"Accepted a connection from {webSocketContext.Origin}");
                WebSocket webSocket = webSocketContext.WebSocket;
                HandleWebSocketConnection(webSocket);
            } else {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private static async void HandleWebSocketConnection(WebSocket webSocket) {

        try {
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue) {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                string response = ProcessMessage(message);

                buffer = Encoding.UTF8.GetBytes(response);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"WebSocket connection error: {ex.Message}");
        }
        finally {
            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None).Wait();
        }
    }

    private static string ProcessMessage(string message) {
        if (message == null) {
            return "NotFound";
        } else if (message == "getAllUsers") {
            var users = dataManager.GetAllUsers();

            if (users != null && users.Any()) {
                string userInformationString = string.Join(" END ", users.Select(user => $"{user.Name} {user.Points}"));
                Console.WriteLine(userInformationString);
                return "U " + userInformationString;
            } else {
                Console.WriteLine("No users found.");
                return "E You havent configured the User.json correctly!";
            }
        } else if (message.StartsWith("getQuestion(")) {
            var x = dataManager.GetQuestion(message.ElementAt(message.Length - 4).ToString(), Convert.ToInt32(message.ElementAt(message.Length - 2).ToString()));
            return "Q " + string.Join(" END ", $"{x.Category} {x.Number} '{x.Text}' '{x.Answer}' ");
        } else {
            return "NotFound";
        }
    }
}

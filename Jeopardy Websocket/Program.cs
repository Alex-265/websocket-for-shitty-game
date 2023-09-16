using Jeopardy_Websocket;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program {
    public static TaskFactory taskFactory = new TaskFactory();
    private static int MaxLogLines = Console.WindowHeight - 2; // One less than the height of the console
    private static List<string> logLines = new List<string>();
    private static string currentCommand = string.Empty;
    public readonly static string name = "Ratatui Server";
    public readonly static string version = "0.1";
    public readonly static string buildID = "0001";
    public static DataManager dataManager = new DataManager();
    private static List<string> commandHistory = new List<string>();
    private static int historyIndex = -1;
    public static string ProcessInput(string input) {
        string[] inputArray = input.ToLower().Split(" ");
        if (inputArray[0] == "points") {
            if (inputArray[1] == "add") {
                string username = inputArray[2];
                try {
                    int pointstoAdd = Convert.ToInt32(inputArray[3]);
                    dataManager.addPoints(username, pointstoAdd);
                    print($"Added {pointstoAdd} points to {username}, who has now {dataManager.getPoints(username)}", true);
                } catch {
                    return "Invalid Arguments!";
                }
                
            } else if (inputArray[1] == "rem") {
                string username = inputArray[2];
                try {
                    int pointstoAdd = Convert.ToInt32(inputArray[3]);
                    dataManager.removePoints(username, pointstoAdd);
                    print($"Removed {pointstoAdd} points from {username}, who has now {dataManager.getPoints(username)}", true);
                }
                catch {
                    return "Invalid Arguments!";
                }
            } else if (inputArray[1] == "get") {
                string username = inputArray[2];
                try {
                    print($"{username} has {dataManager.getPoints(username)} points", true);
                }
                catch {
                    return "Invalid Arguments!";
                }
            }
        } else if (inputArray[0] == "users") {
            List<User> users = dataManager.GetAllUsers();
            foreach (User user in users) {
                print($" {user.Name} ({user.Points}),");
            }
            print(" ");
        } else {
            print(" Command Not found!");
        }
        return "Error";
    }
    public static async Task print(string i, bool a = false) {
        if (a) {
            logLines.Add(" " + i);
        } else {
            logLines.Add(i);
        }
        if (logLines.Count > MaxLogLines) {
            logLines.RemoveAt(0);
        }
        DisplayLog();
    }

    private static async Task Main(string[] args) {
        Console.WriteLine("Loading Terminal cli..");
        Thread.Sleep(1000);
        taskFactory.StartNew(CliMain);
        Thread.Sleep(200);
        print($"{name} Version {version} id {buildID}");
        print($"Loading at {Environment.CurrentDirectory}");
        print($"For User {Environment.UserName} for Machine {Environment.MachineName}");
        print("Initializing Files");
        dataManager.LoadUsers("User.json");
        dataManager.LoadQuestions("data.json");
        print("Successfully decoded data");
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        print($"{name} WebSocket server started...");
        
        
        while (true) {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest) {
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
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
            print($"WebSocket connection error: {ex.Message}");
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
                return userInformationString;
            } else {
                return "E You havent configured the User.json correctly!";
            }
        } else if (message.StartsWith("getQuestion(")) {
            var x = dataManager.GetQuestion(message.ElementAt(message.Length - 4).ToString(), Convert.ToInt32(message.ElementAt(message.Length - 2).ToString()));
            return string.Join(" END ", $"{x.Category} {x.Number} '{x.Text}' '{x.Answer}' ");
        } else {
            return "NotFound";
        }
    }
    async static Task CliMain() {
        while (true) {
            MaxLogLines = Console.WindowHeight - 2;
            await DisplayLog();
            await DisplayCommand();

            var keyInfo = Console.ReadKey(intercept: true);

            if (char.IsControl(keyInfo.KeyChar)) // Handle control characters (e.g., Enter, Backspace)
            {
                switch (keyInfo.Key) {
                    case ConsoleKey.Enter:
                        await HandleCommand();
                        break;
                    case ConsoleKey.Backspace:
                        if (currentCommand.Length > 0) {
                            currentCommand = currentCommand.Substring(0, currentCommand.Length - 1);
                            Console.Write("\b \b");
                        }
                        break;
                    case ConsoleKey.Spacebar:
                        currentCommand += " ";
                        Console.Write(" ");
                        break;
                    case ConsoleKey.UpArrow:
                        if (historyIndex > 0) {
                            historyIndex--;
                            currentCommand = commandHistory[historyIndex];
                            ClearCurrentCommandLine();
                            Console.Write(currentCommand);
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (historyIndex < commandHistory.Count - 1) {
                            historyIndex++;
                            currentCommand = commandHistory[historyIndex];
                            ClearCurrentCommandLine();
                            Console.Write(currentCommand);
                        }
                        break;
                    default:
                        // Handle other control characters if necessary
                        break;
                }
            } else // Handle printable characters
              {
                currentCommand += keyInfo.KeyChar.ToString();
                Console.Write(keyInfo.KeyChar);
            }
        }
    }

    async static Task DisplayLog() {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Gray;

        // Determine the starting index for the log lines to display
        int startIndex = logLines.Count - MaxLogLines;
        startIndex = startIndex < 0 ? 0 : startIndex;

        for (int i = startIndex; i < logLines.Count; i++) {
            Console.WriteLine(logLines[i]);
        }

        // Add the line of "---------" at the bottom
        var pos = Console.GetCursorPosition();
        Console.SetCursorPosition(0, Console.WindowHeight - 2);
        Console.WriteLine(new string('-', Console.WindowWidth - 1));
        Console.SetCursorPosition(pos.Left, pos.Top);
        Console.ResetColor();
    }

    async static Task DisplayCommand() {
        Console.SetCursorPosition(0, Console.WindowHeight - 1);
        Console.Write(">");
        Console.SetCursorPosition(1, Console.WindowHeight - 1);
        if (currentCommand.Length > Console.WindowWidth - 3) {
            return;
        }
        Console.Write(currentCommand.PadRight(Console.WindowWidth - 1).ToLower());
        Console.SetCursorPosition(currentCommand.Length + 1, Console.WindowHeight - 1);
    }

    async static Task HandleCommand() {
        if (!string.IsNullOrWhiteSpace(currentCommand)) {
            logLines.Add(">" + currentCommand.ToLower());
            ProcessInput(currentCommand.ToLower());
            commandHistory.Add(currentCommand); // Add to command history
            historyIndex = commandHistory.Count; // Reset history index
            currentCommand = string.Empty;

            if (logLines.Count > MaxLogLines) {
                logLines.RemoveAt(0);
            }
        }
    }
    async static Task ClearCurrentCommandLine() {
        Console.SetCursorPosition(1, Console.WindowHeight - 1);
        Console.Write(new string(' ', Console.WindowWidth - 2));
        Console.SetCursorPosition(1, Console.WindowHeight - 1);
    }

}

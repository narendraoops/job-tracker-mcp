using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

namespace JobTrackerHost
{
    public class Program
    {
        // Path to the MCP server project. Change it if you move the folder.
        private const string ServerProject =
            @"E:\Tech\2026\Projects\AI\MyMCP\JobTrackerMcp\JobTrackerMcp.csproj";

        // The model may call tools at most this many times for one question.
        private const int MaxToolSteps = 5;

        public static async Task Main(string[] args)
        {
            string? connection = Environment.GetEnvironmentVariable("JOBTRACKER_CONNECTION");
            string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            string model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

            if (string.IsNullOrWhiteSpace(connection))
            {
                Console.WriteLine("JOBTRACKER_CONNECTION is not set.");
                return;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("OPENAI_API_KEY is not set.");
                return;
            }

            // 1. Start the MCP server and connect to it.
            StdioClientTransportOptions options = new StdioClientTransportOptions
            {
                Name = "JobTrackerMcp",
                Command = "dotnet",
                Arguments = new List<string> { "run", "--no-build", "--project", ServerProject },
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "JOBTRACKER_CONNECTION", connection }
                }
            };

            StdioClientTransport transport = new StdioClientTransport(options);

            await using (McpClient client = await McpClient.CreateAsync(transport))
            {
                // 2. Ask the server which tools it has.
                IList<McpClientTool> tools = await client.ListToolsAsync();
                Console.WriteLine("Connected to the MCP server. Tools: " + tools.Count);
                Console.WriteLine("Model: " + model);

                // 3. Build the chat client. It calls the OpenAI model and runs
                //    tool calls automatically, up to MaxToolSteps times per question.
                IChatClient openAiClient = new OpenAIClient(apiKey)
                    .GetChatClient(model)
                    .AsIChatClient();

                using (IChatClient chatClient = new ChatClientBuilder(openAiClient)
                    .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = MaxToolSteps)
                    .Build())
                {
                    ChatOptions chatOptions = new ChatOptions
                    {
                        Tools = tools.Cast<AITool>().ToList()
                    };

                    List<ChatMessage> messages = new List<ChatMessage>();
                    messages.Add(new ChatMessage(ChatRole.System, BuildSystemPrompt()));

                    // 4. Chat loop.
                    while (true)
                    {
                        Console.WriteLine();
                        Console.Write("You (empty line to quit): ");
                        string? question = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(question))
                        {
                            break;
                        }

                        messages.Add(new ChatMessage(ChatRole.User, question));

                        try
                        {
                            ChatResponse response = await chatClient.GetResponseAsync(messages, chatOptions);

                            PrintToolCalls(response);

                            Console.WriteLine();
                            Console.WriteLine("Assistant: " + response.Text);

                            // Keep the conversation so follow-up questions have context.
                            messages.AddRange(response.Messages);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                    }
                }
            }
        }

        private static string BuildSystemPrompt()
        {
            return "You help the user review their job applications. " +
                   "Use the tools to read the data. Never guess. " +
                   "If a tool returns nothing, say that no matching record was found. " +
                   "Today's date is " + DateTime.Today.ToString("yyyy-MM-dd") + ". " +
                   "Keep answers short and clear.";
        }

        // Shows which tools the model chose, so you can see the flow.
        private static void PrintToolCalls(ChatResponse response)
        {
            foreach (ChatMessage message in response.Messages)
            {
                foreach (AIContent content in message.Contents)
                {
                    FunctionCallContent? call = content as FunctionCallContent;

                    if (call != null)
                    {
                        string arguments = call.Arguments == null
                            ? ""
                            : string.Join(", ", call.Arguments.Select(a => a.Key + "=" + a.Value));

                        Console.WriteLine("  [tool call] " + call.Name + "(" + arguments + ")");
                    }
                }
            }
        }
    }
}

using JobTrackerApi.Models;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

namespace JobTrackerApi.Services
{
    // Owns the MCP client and the chat client. One instance is shared by all requests.
    public class ChatService : IAsyncDisposable
    {
        private const int MaxToolSteps = 5;

        // Only the most recent turns are sent to the model, to keep each request small.
        private const int MaxHistoryTurns = 10;

        private readonly McpClient _mcpClient;
        private readonly IChatClient _chatClient;
        private readonly ChatOptions _chatOptions;

        private ChatService(McpClient mcpClient, IChatClient chatClient, ChatOptions chatOptions)
        {
            _mcpClient = mcpClient;
            _chatClient = chatClient;
            _chatOptions = chatOptions;
        }

        public static async Task<ChatService> CreateAsync(
            string connection, string apiKey, string model, string serverProject)
        {
            // 1. Start the MCP server and connect to it.
            StdioClientTransportOptions options = new StdioClientTransportOptions
            {
                Name = "JobTrackerMcp",
                Command = "dotnet",
                Arguments = new List<string> { "run", "--no-build", "--project", serverProject },
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "JOBTRACKER_CONNECTION", connection }
                }
            };

            McpClient mcpClient = await McpClient.CreateAsync(new StdioClientTransport(options));

            // 2. Ask the server for its tools.
            IList<McpClientTool> tools = await mcpClient.ListToolsAsync();

            // 3. Build the chat client with the automatic tool loop.
            IChatClient openAiClient = new OpenAIClient(apiKey)
                .GetChatClient(model)
                .AsIChatClient();

            IChatClient chatClient = new ChatClientBuilder(openAiClient)
                .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = MaxToolSteps)
                .Build();

            ChatOptions chatOptions = new ChatOptions
            {
                Tools = tools.Cast<AITool>().ToList()
            };

            return new ChatService(mcpClient, chatClient, chatOptions);
        }

        public async Task<ChatReply> AskAsync(string question, List<ChatTurn>? history)
        {
            List<ChatMessage> messages = new List<ChatMessage>();
            messages.Add(new ChatMessage(ChatRole.System, BuildSystemPrompt()));

            // The browser sends the conversation so far. Replay the latest turns,
            // so the model can understand follow-ups such as "which of those?".
            if (history != null)
            {
                int skip = Math.Max(0, history.Count - MaxHistoryTurns);

                foreach (ChatTurn turn in history.Skip(skip))
                {
                    messages.Add(new ChatMessage(ChatRole.User, turn.Question));
                    messages.Add(new ChatMessage(ChatRole.Assistant, turn.Answer));
                }
            }

            messages.Add(new ChatMessage(ChatRole.User, question));

            ChatResponse response = await _chatClient.GetResponseAsync(messages, _chatOptions);

            ChatReply reply = new ChatReply();
            reply.Answer = response.Text;

            foreach (ChatMessage message in response.Messages)
            {
                foreach (AIContent content in message.Contents)
                {
                    FunctionCallContent? call = content as FunctionCallContent;

                    if (call != null)
                    {
                        reply.ToolCalls.Add(call.Name);
                    }
                }
            }

            return reply;
        }

        private static string BuildSystemPrompt()
        {
            return "You help the user review their job applications. " +
                   "Use the tools to read the data. Never guess. " +
                   "If a tool returns nothing, say that no matching record was found. " +
                   "Today's date is " + DateTime.Today.ToString("yyyy-MM-dd") + ". " +
                   "Keep answers short and clear.";
        }

        public async ValueTask DisposeAsync()
        {
            _chatClient.Dispose();
            await _mcpClient.DisposeAsync();
        }
    }
}

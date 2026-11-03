using ModelContextProtocol.Client;

namespace JobTrackerHost
{
    public class Program
    {
        // Path to the MCP server project. Change it if you move the folder.
        private const string ServerProject =
            @"E:\Tech\2026\Projects\AI\MyMCP\JobTrackerMcp\JobTrackerMcp.csproj";

        public static async Task Main(string[] args)
        {
            string? connection = Environment.GetEnvironmentVariable("JOBTRACKER_CONNECTION");

            if (string.IsNullOrWhiteSpace(connection))
            {
                Console.WriteLine("JOBTRACKER_CONNECTION is not set.");
                return;
            }

            // The host starts the server as a child process and talks to it over stdio.
            // Custom environment variables must be passed on explicitly.
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
                IList<McpClientTool> tools = await client.ListToolsAsync();

                Console.WriteLine("Connected. The server offers " + tools.Count + " tools:");
                Console.WriteLine();

                foreach (McpClientTool tool in tools)
                {
                    Console.WriteLine(tool.Name);
                    Console.WriteLine("    " + tool.Description);
                }
            }
        }
    }
}

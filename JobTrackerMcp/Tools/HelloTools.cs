using System.ComponentModel;
using ModelContextProtocol.Server;

namespace JobTrackerMcp.Tools
{
    [McpServerToolType]
    public static class HelloTools
    {
        [McpServerTool]
        [Description("Returns a greeting. Use it to check that the server is alive.")]
        public static string SayHello([Description("Name of the person to greet.")] string name)
        {
            return "Hello, " + name + "! This is the JobTracker MCP server.";
        }
    }
}
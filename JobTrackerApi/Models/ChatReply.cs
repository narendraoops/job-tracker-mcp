namespace JobTrackerApi.Models
{
    // What the API sends back: the answer and the tools the model used.
    public class ChatReply
    {
        public string Answer { get; set; } = "";
        public List<string> ToolCalls { get; set; } = new List<string>();
    }
}

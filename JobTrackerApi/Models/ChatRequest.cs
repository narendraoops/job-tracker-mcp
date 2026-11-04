namespace JobTrackerApi.Models
{
    // What the browser sends: the new question and the conversation so far.
    public class ChatRequest
    {
        public string Question { get; set; } = "";
        public List<ChatTurn> History { get; set; } = new List<ChatTurn>();
    }
}

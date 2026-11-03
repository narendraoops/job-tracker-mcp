namespace JobTrackerApi.Models
{
    // What the browser sends: one question.
    public class ChatRequest
    {
        public string Question { get; set; } = "";
    }
}

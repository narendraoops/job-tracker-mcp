using Microsoft.Extensions.AI;

namespace JobTrackerHost
{
    // One test question with the result we expect.
    public class EvalCase
    {
        public string Question { get; set; } = "";
        public string ExpectedTool { get; set; } = "";
        public string[] MustContainAll { get; set; } = Array.Empty<string>();
        public string[] MustContainAny { get; set; } = Array.Empty<string>();
    }

    public static class EvalRunner
    {
        public static async Task Run(IChatClient chatClient, ChatOptions chatOptions)
        {
            List<EvalCase> cases = BuildCases();
            int passed = 0;

            Console.WriteLine("Running " + cases.Count + " fixed questions...");
            Console.WriteLine();

            foreach (EvalCase testCase in cases)
            {
                // Every question starts a fresh conversation, so one answer
                // cannot help or hurt the next.
                List<ChatMessage> messages = new List<ChatMessage>();
                messages.Add(new ChatMessage(ChatRole.System, Program.BuildSystemPrompt()));
                messages.Add(new ChatMessage(ChatRole.User, testCase.Question));

                string failure;

                try
                {
                    ChatResponse response = await chatClient.GetResponseAsync(messages, chatOptions);
                    failure = Check(testCase, response);
                }
                catch (Exception ex)
                {
                    failure = "Error: " + ex.Message;
                }

                if (failure == "")
                {
                    passed++;
                    Console.WriteLine("PASS  " + testCase.Question);
                }
                else
                {
                    Console.WriteLine("FAIL  " + testCase.Question);
                    Console.WriteLine("      " + failure);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Result: " + passed + " of " + cases.Count + " passed.");
        }

        // Returns an empty string when the answer is good, otherwise the reason it failed.
        private static string Check(EvalCase testCase, ChatResponse response)
        {
            List<string> toolsCalled = new List<string>();

            foreach (ChatMessage message in response.Messages)
            {
                foreach (AIContent content in message.Contents)
                {
                    FunctionCallContent? call = content as FunctionCallContent;

                    if (call != null)
                    {
                        toolsCalled.Add(call.Name);
                    }
                }
            }

            if (!toolsCalled.Contains(testCase.ExpectedTool))
            {
                return "Expected tool " + testCase.ExpectedTool + " but the model called: " +
                       (toolsCalled.Count == 0 ? "no tool" : string.Join(", ", toolsCalled));
            }

            string answer = response.Text;

            foreach (string word in testCase.MustContainAll)
            {
                if (!answer.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    return "Answer is missing '" + word + "'. Answer was: " + answer;
                }
            }

            if (testCase.MustContainAny.Length > 0)
            {
                bool found = testCase.MustContainAny.Any(
                    word => answer.Contains(word, StringComparison.OrdinalIgnoreCase));

                if (!found)
                {
                    return "Answer has none of: " + string.Join(", ", testCase.MustContainAny) +
                           ". Answer was: " + answer;
                }
            }

            return "";
        }

        // Only questions whose answers do not change from day to day.
        private static List<EvalCase> BuildCases()
        {
            List<EvalCase> cases = new List<EvalCase>();

            cases.Add(new EvalCase
            {
                Question = "How many applications are in the Interview stage?",
                ExpectedTool = "list_applications",
                MustContainAny = new string[] { "3", "three" }
            });

            cases.Add(new EvalCase
            {
                Question = "Which company is application 1 with?",
                ExpectedTool = "get_application",
                MustContainAll = new string[] { "Contoso" }
            });

            cases.Add(new EvalCase
            {
                Question = "Tell me about application 999.",
                ExpectedTool = "get_application",
                MustContainAny = new string[] { "no ", "not found", "couldn't", "could not", "doesn't exist", "does not exist" }
            });

            cases.Add(new EvalCase
            {
                Question = "Which applications were rejected?",
                ExpectedTool = "list_applications",
                MustContainAll = new string[] { "Tailspin", "Coho" }
            });

            cases.Add(new EvalCase
            {
                Question = "Which applications need a follow-up today or are overdue?",
                ExpectedTool = "get_followups_due",
                MustContainAll = new string[] { "Northwind", "Woodgrove", "Trey" }
            });

            return cases;
        }
    }
}

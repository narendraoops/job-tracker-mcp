// What the page sends to the API. Matches ChatRequest in the .NET project.
export interface ChatRequest {
  question: string;
}

// What the API sends back. Matches ChatReply in the .NET project.
export interface ChatReply {
  answer: string;
  toolCalls: string[];
}

// What the page sends to the API. Matches ChatRequest in the .NET project.
export interface ChatRequest {
  question: string;
}

// What the API sends back. Matches ChatReply in the .NET project.
export interface ChatReply {
  answer: string;
  toolCalls: string[];
}

// One question and its answer, kept on the page as the conversation grows.
export interface ChatEntry {
  question: string;
  answer: string;
  toolCalls: string[];
}

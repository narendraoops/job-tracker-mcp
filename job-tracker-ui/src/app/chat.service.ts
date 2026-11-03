import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ChatReply, ChatRequest } from './chat.models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);

  // Address of the .NET Web API (started with: dotnet run --launch-profile http).
  private readonly apiUrl = 'http://localhost:5195/api/chat';

  // Sends one question and returns the reply as an Observable.
  ask(question: string): Observable<ChatReply> {
    const body: ChatRequest = { question };
    return this.http.post<ChatReply>(this.apiUrl, body);
  }
}

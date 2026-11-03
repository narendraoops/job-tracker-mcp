import { Component, inject, signal } from '@angular/core';
import { ChatService } from './chat.service';
import { ChatEntry, ChatReply } from './chat.models';

@Component({
  imports: [],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly chatService = inject(ChatService);

  protected readonly title = signal('Job Tracker Assistant');

  // State of the page. Each signal tells Angular to redraw what uses it.
  protected readonly question = signal('');
  protected readonly history = signal<ChatEntry[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal('');

  // Ready-made questions the user can click.
  protected readonly samples: string[] = [
    'Which follow-ups are due today or overdue?',
    'How many applications are in the Interview stage?',
    'Which applications were rejected?',
  ];

  // Runs on every keystroke and stores the text in the signal.
  protected onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.question.set(input.value);
  }

  protected useSample(text: string): void {
    this.question.set(text);
    this.send();
  }

  protected send(): void {
    const text = this.question().trim();

    if (!text || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.error.set('');

    // The request is sent only now, because subscribe() starts the Observable.
    this.chatService.ask(text).subscribe({
      next: (reply: ChatReply) => {
        this.history.update((list) => [
          ...list,
          { question: text, answer: reply.answer, toolCalls: reply.toolCalls },
        ]);
        this.question.set('');
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not reach the assistant. Is the API running?');
        this.loading.set(false);
      },
    });
  }
}

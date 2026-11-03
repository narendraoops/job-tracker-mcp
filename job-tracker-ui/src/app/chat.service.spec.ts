import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChatService } from './chat.service';
import { ChatReply } from './chat.models';

describe('ChatService', () => {
  let service: ChatService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ChatService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // Fails the test if a request was made that the test did not expect.
    http.verify();
  });

  it('should POST the question to the API and return the reply', () => {
    let received: ChatReply | undefined;

    service.ask('Which were rejected?').subscribe((reply) => (received = reply));

    const request = http.expectOne('http://localhost:5195/api/chat');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ question: 'Which were rejected?' });

    request.flush({ answer: 'Tailspin and Coho.', toolCalls: ['list_applications'] });

    expect(received?.answer).toBe('Tailspin and Coho.');
    expect(received?.toolCalls).toEqual(['list_applications']);
  });

  it('should not send anything until someone subscribes', () => {
    service.ask('Hello');

    http.expectNone('http://localhost:5195/api/chat');
  });
});

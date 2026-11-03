import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { App } from './app';
import { ChatService } from './chat.service';

describe('App', () => {
  // A fake service, so these tests never call the real API.
  const fakeChatService = { ask: vi.fn() };

  async function render(): Promise<ComponentFixture<App>> {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture;
  }

  beforeEach(async () => {
    fakeChatService.ask.mockReset();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: ChatService, useValue: fakeChatService }],
    }).compileComponents();
  });

  it('should show the heading', async () => {
    const fixture = await render();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('h1')?.textContent).toContain('Job Tracker Assistant');
  });

  it('should show a hint when there are no questions yet', async () => {
    const fixture = await render();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('.hint')).not.toBeNull();
    expect(page.querySelectorAll('.entry').length).toBe(0);
  });

  it('should disable the Send button when the box is empty', async () => {
    const fixture = await render();
    const page = fixture.nativeElement as HTMLElement;
    const sendButton = page.querySelector('.ask button') as HTMLButtonElement;

    expect(sendButton.disabled).toBe(true);
  });

  it('should send a sample question and show the answer and tools', async () => {
    fakeChatService.ask.mockReturnValue(
      of({ answer: 'There are 3 applications in Interview.', toolCalls: ['list_applications'] })
    );

    const fixture = await render();
    const page = fixture.nativeElement as HTMLElement;

    const firstSample = page.querySelector('.sample') as HTMLButtonElement;
    firstSample.click();
    await fixture.whenStable();

    expect(fakeChatService.ask).toHaveBeenCalledWith('Which follow-ups are due today or overdue?');
    expect(page.querySelector('.answer')?.textContent).toContain('3 applications');
    expect(page.querySelector('.tools')?.textContent).toContain('list_applications');
    expect(page.querySelector('.hint')).toBeNull();
  });

  it('should show an error message when the API call fails', async () => {
    fakeChatService.ask.mockReturnValue(throwError(() => new Error('offline')));

    const fixture = await render();
    const page = fixture.nativeElement as HTMLElement;

    (page.querySelector('.sample') as HTMLButtonElement).click();
    await fixture.whenStable();

    expect(page.querySelector('.error')?.textContent).toContain('Could not reach the assistant');
    expect(page.querySelectorAll('.entry').length).toBe(0);
  });
});

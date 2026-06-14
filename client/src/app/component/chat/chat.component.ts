import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ChatService } from '../../services/chat.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent {
  messages: { role: string; content: string }[] = [];
  input = '';
  loading = false;
  isOpen = false;

  // Suggestion state
  pendingSuggestion: string | null = null;

  constructor(private chatService: ChatService, private router: Router) {}

  send() {
    if (!this.input.trim() || this.loading) return;
    const msg = this.input;
    this.input = '';
    this.loading = true;
    this.messages.push({ role: 'user', content: msg });

    this.chatService.send(msg, this.messages.slice(0, -1)).subscribe({
      next: res => {
        this.messages.push({ role: 'assistant', content: res.reply });
        this.pendingSuggestion = res.suggestedSearch ?? null;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  onKey(e: KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.send(); }
  }

  acceptSuggestion() {
    if (!this.pendingSuggestion) return;
    const query = this.pendingSuggestion;
    this.pendingSuggestion = null;
    this.isOpen = false;
    this.router.navigate(['/products'], { queryParams: { search: query } });
  }

  declineSuggestion() {
    this.pendingSuggestion = null;
  }
}

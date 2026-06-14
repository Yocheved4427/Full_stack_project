import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ChatComponent } from './component/chat/chat.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ChatComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  standalone:true
})
export class App {
  protected readonly title = signal('client');
}

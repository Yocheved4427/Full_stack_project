import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ProductsPageComponent } from './component/products-page/products-page';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet,ProductsPageComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  standalone:true
})
export class App {
  protected readonly title = signal('client');
}

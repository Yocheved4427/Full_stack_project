import { Component, Input, Output, EventEmitter } from '@angular/core'; // הוספתי Output, EventEmitter
import { CommonModule } from '@angular/common';

// 👇 שימי לב: חייב להיות כתוב Module בסוף!
import { ButtonModule } from 'primeng/button'; 
import { CardModule } from 'primeng/card';
import { ImageUrlPipe } from '../../pipes/image-url.pipe';

import { Product } from '../../models/product.model';

@Component({
  selector: 'app-product-card',
  standalone: true,
  // 👇 גם כאן: חובה להשתמש במודולים
  imports: [CommonModule, ButtonModule, CardModule, ImageUrlPipe], 
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.scss',
})
export class ProductCard {
  @Input() product!: Product; 
  
  // 👇 זה מה שהוספנו: האירוע שמשדר החוצה
  @Output() addToCartClick = new EventEmitter<Product>();

  getSafeUrl(url: string): string {
    return encodeURI(url);
  }

  // 👇 הפונקציה שהכפתור ב-HTML יפעיל
  onAddToCart() {
    this.addToCartClick.emit(this.product);
  }
}
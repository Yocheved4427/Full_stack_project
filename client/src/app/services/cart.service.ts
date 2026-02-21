import { Injectable } from '@angular/core';
import { CartItem } from '../models/cart.model';

@Injectable({
  providedIn: 'root'
})
export class CartService {

  private cartItems: CartItem[] = [];

  constructor() {}

  getCart(): CartItem[] {
    const data = localStorage.getItem('cart');
    return data ? JSON.parse(data) : [];
  }

  addToCart(product: CartItem) {
    const cart = this.getCart();
    cart.push(product);
    localStorage.setItem('cart', JSON.stringify(cart));
  }

  removeItem(id: number) {
    const cart = this.getCart().filter(item => item.id !== id);
    localStorage.setItem('cart', JSON.stringify(cart));
  }

  clearCart() {
    localStorage.removeItem('cart');
  }
}

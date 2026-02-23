import { Injectable } from '@angular/core';
import { CartItem } from '../models/cart.model';

@Injectable({
  providedIn: 'root'
})
export class CartService {

  constructor() {}

  getCart(): CartItem[] {
    const data = localStorage.getItem('cart');
    return data ? JSON.parse(data) : [];
  }

  addToCart(product: CartItem) {
    const cart = this.getCart();
    const cartItem: CartItem = {
      ...product,
      cartItemId: product.cartItemId || this.generateCartItemId()
    };
    cart.push(cartItem);
    this.saveCart(cart);
  }

  removeItem(cartItemId: string) {
    const cart = this.getCart().filter(item => item.cartItemId !== cartItemId);
    this.saveCart(cart);
  }

  clearCart() {
    localStorage.removeItem('cart');
  }

  updateItem(updatedItem: CartItem) {
    const cart = this.getCart().map(item =>
      item.cartItemId === updatedItem.cartItemId ? { ...item, ...updatedItem } : item
    );
    this.saveCart(cart);
  }

  private saveCart(cart: CartItem[]) {
    localStorage.setItem('cart', JSON.stringify(cart));
  }

  private generateCartItemId(): string {
    return `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
  }

  // --- לוגיקת חישוב המחירים המרכזית ---

  /**
   * מחשבת מחיר ללילה בהתאם לעונת שיא
   */
  getEffectivePricePerNight(startDate: Date, basePrice: number, monthConfigs: any[]): number {
    if (!startDate || !monthConfigs || monthConfigs.length === 0) {
      return basePrice;
    }

    // חודשים ב-JS הם 0-11, לכן מוסיפים 1
    const startMonth = new Date(startDate).getMonth() + 1;
    const config = monthConfigs.find(mc =>
      mc.monthNumber === startMonth && mc.isAvailable !== false && mc.specialPrice > 0
    );

    return config ? config.specialPrice : basePrice;
  }

  /**
   * מחשבת את המחיר הכולל עבור הפריט בעגלה
   */
  calculateTotalAmount(startDate: Date, endDate: Date, basePrice: number, participants: number, monthConfigs: any[]): number {
    if (!startDate || !endDate) return 0;
    
    const start = new Date(startDate);
    const end = new Date(endDate);
    
    const diff = end.getTime() - start.getTime();
    const nights = Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
    
    const pricePerNight = this.getEffectivePricePerNight(startDate, basePrice, monthConfigs);
    
    return nights * pricePerNight * participants;
  }
}
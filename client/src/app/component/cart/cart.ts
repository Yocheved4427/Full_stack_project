
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DataViewModule } from 'primeng/dataview';
import { TagModule } from 'primeng/tag';
import { CartService } from '../../services/cart.service';
import { ApiService } from '../../services/api.service';
import { CommonModule } from '@angular/common';
import { CartItem } from '../../models/cart.model';
import { FormsModule } from '@angular/forms';


@Component({
  selector: 'app-cart',
  standalone: true, 
  imports: [ButtonModule, DataViewModule, TagModule, CommonModule, RouterLink, FormsModule],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',

})
export class Cart implements OnInit {

  cartItems: CartItem[] = [];  
  editingItemId: string | null = null;
  editStartDate = signal<string>('');
  editEndDate = signal<string>('');
  editParticipants = signal<number>(1);

  constructor(private cartService: CartService, private apiService: ApiService) {}

  ngOnInit() {
    this.cartItems = this.cartService.getCart();
  }

  removeItem(cartItemId: string) {
    this.cartService.removeItem(cartItemId);
    this.cartItems = this.cartService.getCart();
  }

  clearCart() {
    this.cartService.clearCart();
    this.cartItems = [];
  }

  startEdit(item: CartItem) {
    this.editingItemId = item.cartItemId;
    this.editStartDate.set(this.toDateInputValue(item.startDate));
    this.editEndDate.set(this.toDateInputValue(item.endDate));
    this.editParticipants.set(item.quantity ?? 1);
  }

  cancelEdit() {
    this.editingItemId = null;
  }

  saveEdit(item: CartItem) {
    const startDate = new Date(this.editStartDate());
    const endDate = new Date(this.editEndDate());
    const participants = Number(this.editParticipants());

    if (!this.editStartDate() || !this.editEndDate() || participants <= 0) {
      return;
    }

    const applyPricing = (basePrice: number, monthConfigs: any[]) => {
      const totalPrice = this.cartService.calculateTotalAmount(
        startDate,
        endDate,
        basePrice,
        participants,
        monthConfigs
      );

      item.startDate = startDate;
      item.endDate = endDate;
      item.quantity = participants;
      item.basePrice = basePrice;
      item.monthConfigs = monthConfigs;
      item.price = totalPrice;

      this.cartService.updateItem(item);
      this.cartItems = this.cartService.getCart();
      this.editingItemId = null;
    };

    if (!item.basePrice || !item.monthConfigs || item.monthConfigs.length === 0) {
      this.apiService.getVacationById(item.id).subscribe({
        next: (product: any) => {
          const basePrice = product?.price ?? item.basePrice ?? item.price;
          const monthConfigs = product?.monthConfigs ?? item.monthConfigs ?? [];
          applyPricing(basePrice, monthConfigs);
        },
        error: () => {
          const basePrice = item.basePrice ?? item.price;
          const monthConfigs = item.monthConfigs ?? [];
          applyPricing(basePrice, monthConfigs);
        }
      });
      return;
    }

    applyPricing(item.basePrice, item.monthConfigs);
  }

  private toDateInputValue(value: Date | string): string {
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }


  get totalPrice(): number {
    return this.cartItems.reduce((sum, item) => sum + item.price, 0);
  }
   
}



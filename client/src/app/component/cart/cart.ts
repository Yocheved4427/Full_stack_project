
import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DataViewModule } from 'primeng/dataview';
import { TagModule } from 'primeng/tag';
import { CartService } from '../../services/cart.service';
import { ApiService } from '../../services/api.service';
import { UserService } from '../../services/user.service';
import { CommonModule } from '@angular/common';
import { CartItem } from '../../models/cart.model';
import { FormsModule } from '@angular/forms';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { Tooltip } from 'primeng/tooltip';
import { CheckoutDialog } from '../checkout-dialog/checkout-dialog';


@Component({
  selector: 'app-cart',
  standalone: true, 
  imports: [ButtonModule, DataViewModule, TagModule, CommonModule, RouterLink, FormsModule, ToastModule, Tooltip],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
  providers: [DialogService, MessageService]
})
export class Cart implements OnInit, OnDestroy {

  cartItems: CartItem[] = [];  
  editingItemId: string | null = null;
  editStartDate = signal<string>('');
  editEndDate = signal<string>('');
  editParticipants = signal<number>(1);
  editDateError = signal<string>('');
  dialogRef: DynamicDialogRef | undefined;

  constructor(
    private cartService: CartService, 
    private apiService: ApiService,
    private dialogService: DialogService,
    private messageService: MessageService,
    private userService: UserService
  ) {}

  get isAdmin(): boolean {
    return this.userService.isAdmin();
  }

  ngOnInit() {
    this.cartItems = this.cartService.getCart();
  }

  ngOnDestroy() {
    if (this.dialogRef) {
      this.dialogRef.close();
    }
  }

  checkout() {
    if (this.cartItems.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Empty Cart',
        detail: 'Your cart is empty. Please add items before checkout.'
      });
      return;
    }

    const ref = this.dialogService.open(CheckoutDialog, {
      header: 'Complete Your Purchase',
      width: '700px',
      data: {
        cartItems: this.cartItems,
        totalPrice: this.totalPrice
      }
    });

    if (ref) {
      this.dialogRef = ref;
      ref.onClose.subscribe((result: any) => {
        if (result?.success) {
          // Clear cart - user will be redirected to thank you page
          this.clearCart();
        }
      });
    }
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
    this.editDateError.set('');
  }

  cancelEdit() {
    this.editingItemId = null;
    this.editDateError.set('');
  }

  onEditStartDateChange(value: string) {
    this.editStartDate.set(value);
    this.editDateError.set('');
    // Clear end date if it's no longer after the new start date
    if (this.editEndDate() && this.editEndDate() <= value) {
      this.editEndDate.set('');
    }
  }

  saveEdit(item: CartItem) {
    const startDate = new Date(this.editStartDate());
    const endDate = new Date(this.editEndDate());
    const participants = Number(this.editParticipants());
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (!this.editStartDate() || !this.editEndDate() || participants <= 0) {
      this.editDateError.set('Please fill in all fields.');
      return;
    }

    if (startDate < today) {
      this.editDateError.set('Start date cannot be in the past.');
      return;
    }

    if (endDate <= startDate) {
      this.editDateError.set('End date must be after start date (at least 1 night).');
      return;
    }

    this.editDateError.set('');

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

  get todayStr(): string {
    return this.toDateInputValue(new Date());
  }

  get editMinEndDateStr(): string {
    if (!this.editStartDate()) return this.todayStr;
    const next = new Date(this.editStartDate());
    next.setDate(next.getDate() + 1);
    return this.toDateInputValue(next);
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




import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DataViewModule } from 'primeng/dataview';
import { TagModule } from 'primeng/tag';
import { CartService } from '../../services/cart.service';
import { CommonModule } from '@angular/common';
import { CartItem } from '../../models/cart.model';


@Component({
  selector: 'app-cart',
  standalone: true, 
  imports: [ButtonModule, DataViewModule, TagModule, CommonModule, RouterLink],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',

})
export class Cart implements OnInit {

  cartItems: CartItem[] = [];  

  constructor(private cartService: CartService) {}

  ngOnInit() {
    this.cartItems = this.cartService.getCart();
  }

  removeItem(id: number) {
    this.cartService.removeItem(id);
    this.cartItems = this.cartService.getCart();
  }

  clearCart() {
    this.cartService.clearCart();
    this.cartItems = [];
  }

  get totalPrice(): number {
    return this.cartItems.reduce((sum, item) => sum + item.price, 0);
  }
   
}



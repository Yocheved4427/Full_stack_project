import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';

@Component({
  selector: 'app-thank-you',
  standalone: true,
  imports: [CommonModule, ButtonModule, CardModule, DividerModule],
  templateUrl: './thank-you.html',
  styleUrl: './thank-you.scss'
})
export class ThankYou implements OnInit {
  orderNumber: string = '';
  orderDate: Date = new Date();
  orderTotal: number = 0;
  customerEmail: string = '';
  orderItems: any[] = [];

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    // Get order details from navigation state
    const navigation = this.router.getCurrentNavigation();
    const state = navigation?.extras?.state || history.state;
    
    if (state) {
      this.orderNumber = state['orderNumber'] || this.generateOrderNumber();
      this.orderTotal = state['orderTotal'] || 0;
      this.customerEmail = state['customerEmail'] || '';
      this.orderItems = state['orderItems'] || [];
    }

    // If no state, try to get from route params
    this.route.queryParams.subscribe(params => {
      if (params['orderNumber']) {
        this.orderNumber = params['orderNumber'];
      }
      if (params['total']) {
        this.orderTotal = parseFloat(params['total']);
      }
    });

    // Generate order number if not provided
    if (!this.orderNumber) {
      this.orderNumber = this.generateOrderNumber();
    }
  }

  generateOrderNumber(): string {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000);
    return `ORD-${timestamp}-${random}`;
  }

  continueShopping() {
    this.router.navigate(['/products']);
  }

  viewOrders() {
    this.router.navigate(['/user-profile'], { 
      queryParams: { tab: 'orders' } 
    });
  }
}

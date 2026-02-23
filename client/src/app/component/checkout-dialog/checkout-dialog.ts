import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { UserService } from '../../services/user.service';
import { ApiService } from '../../services/api.service';
import { CartService } from '../../services/cart.service';
import { Router } from '@angular/router';

declare var google: any;

@Component({
  selector: 'app-checkout-dialog',
  standalone: true,
  imports: [CommonModule, ButtonModule, DividerModule, ProgressSpinnerModule],
  templateUrl: './checkout-dialog.html',
  styleUrl: './checkout-dialog.scss'
})
export class CheckoutDialog implements OnInit {
  cartItems: any[] = [];
  totalPrice: number = 0;
  isLoggedIn: boolean = false;
  googlePayClient: any = null;
  isGooglePayReady: boolean = false;
  isProcessing: boolean = false;

  private userService = inject(UserService);
  private apiService = inject(ApiService);
  private cartService = inject(CartService);
  private router = inject(Router);
  private ref = inject(DynamicDialogRef);
  private config = inject(DynamicDialogConfig);

  ngOnInit() {
    this.cartItems = this.config.data?.cartItems || [];
    this.totalPrice = this.config.data?.totalPrice || 0;
    this.isLoggedIn = this.userService.isLoggedIn();

    if (this.isLoggedIn) {
      this.initializeGooglePay();
    }
  }

  initializeGooglePay() {
    // Load Google Pay API
    if (typeof google === 'undefined' || !google.payments) {
      this.loadGooglePayScript();
    } else {
      this.configureGooglePay();
    }
  }

  loadGooglePayScript() {
    const script = document.createElement('script');
    script.src = 'https://pay.google.com/gp/p/js/pay.js';
    script.async = true;
    script.onload = () => {
      this.configureGooglePay();
    };
    document.body.appendChild(script);
  }

  configureGooglePay() {
    const baseRequest: any = {
      apiVersion: 2,
      apiVersionMinor: 0
    };

    const tokenizationSpecification = {
      type: 'PAYMENT_GATEWAY',
      parameters: {
        gateway: 'example',
        gatewayMerchantId: 'exampleGatewayMerchantId'
      }
    };

    const allowedCardNetworks = ['MASTERCARD', 'VISA', 'AMEX'];
    const allowedCardAuthMethods = ['PAN_ONLY', 'CRYPTOGRAM_3DS'];

    const baseCardPaymentMethod = {
      type: 'CARD',
      parameters: {
        allowedAuthMethods: allowedCardAuthMethods,
        allowedCardNetworks: allowedCardNetworks
      }
    };

    const cardPaymentMethod = Object.assign(
      {},
      baseCardPaymentMethod,
      {
        tokenizationSpecification: tokenizationSpecification
      }
    );

    this.googlePayClient = new google.payments.api.PaymentsClient({
      environment: 'TEST' // Change to 'PRODUCTION' for live
    });

    const isReadyToPayRequest: any = Object.assign({}, baseRequest);
    isReadyToPayRequest.allowedPaymentMethods = [baseCardPaymentMethod];

    this.googlePayClient.isReadyToPay(isReadyToPayRequest)
      .then((response: any) => {
        if (response.result) {
          this.isGooglePayReady = true;
          this.addGooglePayButton();
        }
      })
      .catch((err: any) => {
        console.error('Google Pay not available', err);
      });
  }

  addGooglePayButton() {
    const button = this.googlePayClient.createButton({
      onClick: () => this.onGooglePaymentButtonClicked(),
      buttonColor: 'black',
      buttonType: 'pay'
    });
    
    const container = document.getElementById('google-pay-button-container');
    if (container && container.children.length === 0) {
      container.appendChild(button);
    }
  }

  onGooglePaymentButtonClicked() {
    const paymentDataRequest = this.getGooglePaymentDataRequest();
    
    this.googlePayClient.loadPaymentData(paymentDataRequest)
      .then((paymentData: any) => {
        this.processPayment(paymentData);
      })
      .catch((err: any) => {
        console.error('Payment cancelled or failed', err);
      });
  }

  getGooglePaymentDataRequest() {
    const baseRequest: any = {
      apiVersion: 2,
      apiVersionMinor: 0
    };

    const tokenizationSpecification = {
      type: 'PAYMENT_GATEWAY',
      parameters: {
        gateway: 'example',
        gatewayMerchantId: 'exampleGatewayMerchantId'
      }
    };

    const allowedCardNetworks = ['MASTERCARD', 'VISA', 'AMEX'];
    const allowedCardAuthMethods = ['PAN_ONLY', 'CRYPTOGRAM_3DS'];

    const baseCardPaymentMethod = {
      type: 'CARD',
      parameters: {
        allowedAuthMethods: allowedCardAuthMethods,
        allowedCardNetworks: allowedCardNetworks
      }
    };

    const cardPaymentMethod = Object.assign(
      {},
      baseCardPaymentMethod,
      {
        tokenizationSpecification: tokenizationSpecification
      }
    );

    const paymentDataRequest: any = Object.assign({}, baseRequest);
    paymentDataRequest.allowedPaymentMethods = [cardPaymentMethod];
    paymentDataRequest.transactionInfo = {
      totalPriceStatus: 'FINAL',
      totalPrice: this.totalPrice.toString(),
      currencyCode: 'USD',
      countryCode: 'US'
    };

    paymentDataRequest.merchantInfo = {
      merchantName: 'Vacation Shop Demo',
      merchantId: '12345678901234567890'
    };

    return paymentDataRequest;
  }

  processPayment(paymentData: any) {
    this.isProcessing = true;
    
    const currentUser = this.userService.getCurrentUser();
    if (!currentUser) {
      this.isProcessing = false;
      return;
    }

    console.log('Current user data:', currentUser);
    console.log('User ID:', currentUser.id);
    console.log('User ID (capital):', currentUser.Id);
    console.log('User userId:', currentUser.userId);

    // Create order data
    const orderData = {
      userId: currentUser.id || currentUser.Id || currentUser.userId,
      orderDate: new Date().toISOString().split('T')[0], // YYYY-MM-DD format
      orderSum: this.totalPrice,
      status: 'Confirmed',
      orderItems: this.cartItems.map(item => ({
        productId: item.id,
        productName: item.name,
        imageUrl: item.image || '',
        quantity: item.quantity || 1,
        departureDate: this.formatDateToDateOnly(item.startDate),
        returnDate: this.formatDateToDateOnly(item.endDate),
        nightsCount: this.calculateNights(item.startDate, item.endDate),
        pricePerUnit: item.price / (item.quantity || 1)
      }))
    };

    console.log('Creating order with data:', orderData);

    // Submit order to API
    this.apiService.createOrder(orderData).subscribe({
      next: (order: any) => {
        console.log('Order created successfully:', order);
        
        // Send confirmation email
        const emailData = {
          to: currentUser.email,
          subject: 'Thank You for Your Purchase - Order Confirmation',
          orderNumber: order.orderId || this.generateOrderNumber(),
          customerName: `${currentUser.firstName} ${currentUser.lastName}`,
          orderDate: new Date(),
          orderTotal: this.totalPrice,
          orderItems: this.cartItems
        };

        console.log('Sending confirmation email to:', emailData.to);

        this.apiService.sendOrderConfirmationEmail(emailData).subscribe({
          next: (response) => {
            console.log('Confirmation email sent successfully:', response);
          },
          error: (err) => {
            console.error('Failed to send confirmation email:', err);
          }
        });

        // Navigate to thank you page
        this.isProcessing = false;
        this.ref.close({ success: true });
        
        this.router.navigate(['/thank-you'], {
          state: {
            orderNumber: order.orderId || emailData.orderNumber,
            orderTotal: this.totalPrice,
            customerEmail: currentUser.email,
            orderItems: this.cartItems
          }
        });
      },
      error: (err) => {
        console.error('Order creation failed - Full error:', err);
        console.error('Error status:', err.status);
        console.error('Error message:', err.message);
        console.error('Error details:', err.error);
        this.isProcessing = false;
        // Still navigate to thank you page with simulated order in demo mode
        const simulatedOrderNumber = this.generateOrderNumber();
        
        // Send email anyway for demo
        const emailData = {
          to: currentUser.email,
          subject: 'Thank You for Your Purchase - Order Confirmation',
          orderNumber: simulatedOrderNumber,
          customerName: `${currentUser.firstName} ${currentUser.lastName}`,
          orderDate: new Date(),
          orderTotal: this.totalPrice,
          orderItems: this.cartItems
        };
        
        this.apiService.sendOrderConfirmationEmail(emailData).subscribe();
        
        this.ref.close({ success: true });
        this.router.navigate(['/thank-you'], {
          state: {
            orderNumber: simulatedOrderNumber,
            orderTotal: this.totalPrice,
            customerEmail: currentUser.email,
            orderItems: this.cartItems
          }
        });
      }
    });
  }

  formatDateToDateOnly(date: Date | string): string {
    const d = new Date(date);
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  calculateNights(startDate: Date | string, endDate: Date | string): number {
    const start = new Date(startDate);
    const end = new Date(endDate);
    const diff = end.getTime() - start.getTime();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  generateOrderNumber(): string {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000);
    return `ORD-${timestamp}-${random}`;
  }

  goToLogin() {
    this.ref.close();
    this.router.navigate(['/sign-in']);
  }

  close() {
    this.ref.close();
  }
}

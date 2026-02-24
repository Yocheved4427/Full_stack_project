import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TabsModule } from 'primeng/tabs';
import { Tooltip } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { UserService } from '../../services/user.service';
import { ApiService } from '../../services/api.service';

interface Order {
  orderId: number;
  userId: number;
  orderDate: Date;
  beginDate: Date | null;
  endDate: Date | null;
  totalAmount: number;
  status: string;
  items: OrderItem[];
}

interface OrderItem {
  productId: number;
  productName: string;
  imageUrl: string;
  quantity: number;
  departureDate: Date | null;
  returnDate: Date | null;
  nightsCount: number;
  unitPrice: number;
  lineTotal: number;
}

interface ApiOrderItem {
  productId?: number;
  productName?: string;
  imageUrl?: string;
  quantity?: number;
  departureDate?: string;
  returnDate?: string;
  nightsCount?: number;
  pricePerUnit?: number;
}

interface ApiOrder {
  orderId?: number;
  userId?: number;
  orderDate?: string;
  orderSum?: number;
  status?: string;
  orderItems?: ApiOrderItem[];
}

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    CardModule,
    TableModule,
    ToastModule,
    Tooltip
  ],
  providers: [MessageService],
  templateUrl: './user-profile.html',
  styleUrl: './user-profile.scss'
})
export class UserProfile implements OnInit {
  // User details
  firstName = signal<string>('');
  lastName = signal<string>('');
  email = signal<string>('');
  currentPassword = signal<string>('');
  newPassword = signal<string>('');
  confirmPassword = signal<string>('');
  
  // UI state
  activeTab = signal<string>('profile');
  
  // Orders
  orders = signal<Order[]>([]);
  
  // Loading states
  isLoadingProfile = signal<boolean>(false);
  isLoadingOrders = signal<boolean>(false);
  isSavingProfile = signal<boolean>(false);

  constructor(
    private userService: UserService,
    private apiService: ApiService,
    private router: Router,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.loadUserProfile();
    this.loadOrderHistory();
  }

  loadUserProfile(): void {
    const user = this.userService.getCurrentUser();
    if (user) {
      this.firstName.set(user.firstName || '');
      this.lastName.set(user.lastName || '');
      this.email.set(user.email || '');
    } else {
      // Not logged in, redirect to sign-in
      this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/user-profile' } });
    }
  }

  loadOrderHistory(): void {
    this.isLoadingOrders.set(true);
    const user = this.userService.getCurrentUser();
    
    if (user?.id) {
      this.apiService.getUserOrders(user.id).subscribe({
        next: (orders: ApiOrder[]) => {
          this.orders.set(this.mapOrdersForView(orders));
          this.isLoadingOrders.set(false);
        },
        error: (error) => {
          console.error('Failed to load orders:', error);
          this.orders.set([]);
          this.isLoadingOrders.set(false);
        }
      });
    } else {
      this.isLoadingOrders.set(false);
    }
  }

  private mapOrdersForView(orders: ApiOrder[]): Order[] {
    return (orders || []).map((order) => {
      const mappedItems: OrderItem[] = (order.orderItems || []).map((item) => {
        const quantity = Number(item.quantity ?? 0);
        const unitPrice = Number(item.pricePerUnit ?? 0);
        return {
          productId: Number(item.productId ?? 0),
          productName: item.productName || 'Product',
          imageUrl: item.imageUrl || '',
          quantity,
          departureDate: item.departureDate ? new Date(item.departureDate) : null,
          returnDate: item.returnDate ? new Date(item.returnDate) : null,
          nightsCount: Number(item.nightsCount ?? 0),
          unitPrice,
          lineTotal: quantity * unitPrice
        };
      });

      return {
        orderId: Number(order.orderId ?? 0),
        userId: Number(order.userId ?? 0),
        orderDate: order.orderDate ? new Date(order.orderDate) : new Date(),
        beginDate: this.getBeginDate(mappedItems),
        endDate: this.getEndDate(mappedItems),
        totalAmount: Number(order.orderSum ?? 0),
        status: this.getDisplayStatus(order.status, mappedItems),
        items: mappedItems
      };
    });
  }

  private getDisplayStatus(rawStatus: string | undefined, items: OrderItem[]): string {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const beginDate = this.getBeginDate(items);
    const endDate = this.getEndDate(items);

    if (beginDate && endDate) {
      const begin = new Date(beginDate);
      const end = new Date(endDate);
      begin.setHours(0, 0, 0, 0);
      end.setHours(0, 0, 0, 0);

      if (today < begin) {
        return 'waiting...';
      }

      if (today >= begin && today <= end) {
        return 'In Vacation';
      }

      if (today > end) {
        return 'Completed';
      }
    }

    return rawStatus || 'waiting...';
  }

  private getBeginDate(items: OrderItem[]): Date | null {
    const dates = items
      .map(item => item.departureDate)
      .filter((date): date is Date => date !== null)
      .sort((a, b) => a.getTime() - b.getTime());

    return dates.length > 0 ? dates[0] : null;
  }

  private getEndDate(items: OrderItem[]): Date | null {
    const dates = items
      .map(item => item.returnDate)
      .filter((date): date is Date => date !== null)
      .sort((a, b) => b.getTime() - a.getTime());

    return dates.length > 0 ? dates[0] : null;
  }

  saveProfile(): void {
    if (!this.firstName() || !this.lastName() || !this.email()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields'
      });
      return;
    }

    // Email validation
    const emailRegex = /^[^@]+@[^@]+\.[^@]+$/;
    if (!emailRegex.test(this.email())) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please enter a valid email address'
      });
      return;
    }

    this.isSavingProfile.set(true);
    const user = this.userService.getCurrentUser();

    const updateData = {
      id: user?.id,
      firstName: this.firstName(),
      lastName: this.lastName(),
      email: this.email()
    };

    this.apiService.updateUser(updateData).subscribe({
      next: (response) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Profile updated successfully'
        });
        
        // Update user service with new data
        this.userService.updateUserData(response);
        this.isSavingProfile.set(false);
      },
      error: (error) => {
        console.error('Failed to update profile:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update profile. Please try again.'
        });
        this.isSavingProfile.set(false);
      }
    });
  }

  changePassword(): void {
    if (!this.currentPassword() || !this.newPassword() || !this.confirmPassword()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill in all password fields'
      });
      return;
    }

    if (this.newPassword() !== this.confirmPassword()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'New passwords do not match'
      });
      return;
    }

    if (this.newPassword().length < 6) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Password must be at least 6 characters long'
      });
      return;
    }

    const user = this.userService.getCurrentUser();
    const passwordData = {
      userId: user?.id,
      currentPassword: this.currentPassword(),
      newPassword: this.newPassword()
    };

    this.apiService.changePassword(passwordData).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Password changed successfully'
        });
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
      },
      error: (error) => {
        console.error('Failed to change password:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.message || 'Failed to change password'
        });
      }
    });
  }

  logout(): void {
    this.userService.logoutUser();
    this.router.navigate(['/products']);
  }

  goBack(): void {
    this.router.navigate(['/products']);
  }
}

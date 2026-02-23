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
  orderDate: Date;
  totalAmount: number;
  status: string;
  items: OrderItem[];
}

interface OrderItem {
  productName: string;
  quantity: number;
  price: number;
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
        next: (orders: any[]) => {
          this.orders.set(orders);
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

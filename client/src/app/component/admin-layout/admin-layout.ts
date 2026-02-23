import { Component } from '@angular/core';
import { RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MenuItem } from 'primeng/api';
import { MenuModule } from 'primeng/menu';
import { ButtonModule } from 'primeng/button';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, MenuModule, ButtonModule],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
})
export class AdminLayout {
  menuItems: MenuItem[] = [];

  constructor(
    private router: Router,
    private userService: UserService
  ) {
    this.initializeMenu();
  }

  initializeMenu(): void {
    this.menuItems = [
      {
        label: 'Dashboard',
        icon: 'pi pi-home',
        command: () => this.router.navigate(['/admin'])
      },
      {
        label: 'Products',
        icon: 'pi pi-box',
        command: () => this.router.navigate(['/admin/products'])
      },
      {
        label: 'Categories',
        icon: 'pi pi-tags',
        command: () => this.router.navigate(['/admin/categories'])
      },
      {
        label: 'Orders',
        icon: 'pi pi-shopping-cart',
        command: () => this.router.navigate(['/admin/orders'])
      }
    ];
  }

  logout(): void {
    // Use UserService to logout
    this.userService.logoutUser();
    // Navigate to home
    this.router.navigate(['/']);
  }
}

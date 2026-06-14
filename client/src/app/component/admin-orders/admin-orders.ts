import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { TableModule } from 'primeng/table';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, TableModule],
  templateUrl: './admin-orders.html',
  styleUrl: './admin-orders.scss'
})
export class AdminOrders implements OnInit {
  orders = signal<any[]>([]);
  isLoading = signal<boolean>(true);

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    this.apiService.getAllOrders().subscribe({
      next: (data) => {
        this.orders.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching orders:', err);
        this.isLoading.set(false);
      }
    });
  }

  getStatusClass(status: string): string {
    const s = (status || '').toLowerCase().trim();
    if (s === 'completed') return 'status-badge status-completed';
    if (s === 'in vacation') return 'status-badge status-in-vacation';
    if (s === 'cancelled') return 'status-badge status-cancelled';
    return 'status-badge status-waiting';
  }
}
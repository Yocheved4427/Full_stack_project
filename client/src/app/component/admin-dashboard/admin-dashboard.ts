import { Component, OnInit, ChangeDetectorRef } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ChartModule } from "primeng/chart";
import { CardModule } from "primeng/card";
import { ApiService } from "../../services/api.service";
import { catchError, of } from "rxjs";

const MONTHS = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];
const PALETTE = ["#667eea","#764ba2","#f59e0b","#10b981","#ef4444","#3b82f6"];
const NO_ANIMATION = { animation: false };

@Component({
  selector: "app-admin-dashboard",
  standalone: true,
  imports: [CommonModule, ChartModule, CardModule],
  templateUrl: "./admin-dashboard.html",
  styleUrl: "./admin-dashboard.scss"
})
export class AdminDashboard implements OnInit {

  totalOrders = 0;
  totalRevenue = 0;
  totalCustomers = 0;
  pendingOrders = 0;

  salesByMonthData: any = {};
  salesByMonthOptions: any = {};
  ordersByStatusData: any = {};
  ordersByStatusOptions: any = {};
  revenueData: any = {};
  revenueOptions: any = {};

  isLoading = true;

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.apiService.getDashboardStats().pipe(
      catchError(() => {
        // Fallback: backend not restarted yet — build stats from raw orders
        return this.apiService.getAllOrders();
      })
    ).subscribe({
      next: (data: any) => {
        if (data && 'totalOrders' in data) {
          // New endpoint response
          this.totalOrders    = data.totalOrders;
          this.totalRevenue   = data.totalRevenue;
          this.totalCustomers = data.totalCustomers;
          this.pendingOrders  = data.pendingOrders;
          this.buildSalesByMonth(data.salesByMonth ?? []);
          this.buildOrdersByStatus(data.ordersByStatus ?? []);
          this.buildRevenueChart(data.salesByMonth ?? []);
        } else {
          // Fallback: raw orders array
          const orders: any[] = Array.isArray(data) ? data : [];
          this.buildFromRawOrders(orders);
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.isLoading = false; this.cdr.detectChanges(); }
    });
  }

  private buildFromRawOrders(orders: any[]): void {
    this.totalOrders    = orders.length;
    this.totalRevenue   = orders.reduce((s, o) => s + (o.orderSum ?? 0), 0);
    this.totalCustomers = new Set(orders.map(o => o.userId)).size;
    this.pendingOrders  = orders.filter(o => (o.status ?? '').toLowerCase().includes('wait')).length;

    const revenue = new Array(12).fill(0);
    const count   = new Array(12).fill(0);
    const statusMap: Record<string, number> = {};
    orders.forEach(o => {
      if (o.orderDate) {
        const m = new Date(o.orderDate).getMonth();
        revenue[m] += o.orderSum ?? 0;
        count[m]++;
      }
      const s = o.status ?? 'Unknown';
      statusMap[s] = (statusMap[s] ?? 0) + 1;
    });

    const monthStats = MONTHS.map((_, i) => ({ month: i + 1, revenue: revenue[i], orderCount: count[i] }));
    const statusStats = Object.entries(statusMap).map(([status, count]) => ({ status, count }));
    this.buildSalesByMonth(monthStats);
    this.buildOrdersByStatus(statusStats);
    this.buildRevenueChart(monthStats);
  }

  private buildSalesByMonth(salesByMonth: any[]): void {
    const revenue = new Array(12).fill(0);
    const count   = new Array(12).fill(0);
    salesByMonth.forEach(s => {
      const idx = (s.month ?? 1) - 1;
      revenue[idx] = s.revenue ?? 0;
      count[idx]   = s.orderCount ?? 0;
    });

    this.salesByMonthData = {
      labels: MONTHS,
      datasets: [
        { label: "Revenue ($)", data: revenue, backgroundColor: "rgba(102,126,234,0.7)", borderColor: "#667eea", borderWidth: 2, borderRadius: 6, yAxisID: "y" },
        { label: "Orders",      data: count,   backgroundColor: "rgba(118,75,162,0.5)",  borderColor: "#764ba2", borderWidth: 2, borderRadius: 6, yAxisID: "y1" }
      ]
    };
    this.salesByMonthOptions = {
      ...NO_ANIMATION,
      responsive: true,
      plugins: { legend: { position: "top" } },
      scales: {
        y:  { type: "linear", position: "left",  ticks: { callback: (v: number) => `$${v}` } },
        y1: { type: "linear", position: "right", grid: { drawOnChartArea: false } }
      }
    };
  }

  private buildOrdersByStatus(ordersByStatus: any[]): void {
    this.ordersByStatusData = {
      labels: ordersByStatus.map(s => s.status),
      datasets: [{ data: ordersByStatus.map(s => s.count), backgroundColor: ordersByStatus.map((_: any, i: number) => PALETTE[i % PALETTE.length]), hoverOffset: 8 }]
    };
    this.ordersByStatusOptions = { ...NO_ANIMATION, responsive: true, plugins: { legend: { position: "right" } } };
  }

  private buildRevenueChart(salesByMonth: any[]): void {
    const monthly = new Array(12).fill(0);
    salesByMonth.forEach(s => { monthly[(s.month ?? 1) - 1] = s.revenue ?? 0; });

    let cum = 0;
    const cumData = monthly.map(v => { cum += v; return cum; });

    this.revenueData = {
      labels: MONTHS,
      datasets: [{ label: "Cumulative Revenue ($)", data: cumData, fill: true, borderColor: "#10b981", backgroundColor: "rgba(16,185,129,0.15)", tension: 0.4, pointRadius: 4 }]
    };
    this.revenueOptions = { ...NO_ANIMATION, responsive: true, plugins: { legend: { position: "top" } }, scales: { y: { ticks: { callback: (v: number) => `$${v}` } } } };
  }
}

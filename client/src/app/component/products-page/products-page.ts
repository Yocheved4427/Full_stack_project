import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ProductCard } from '../product-card/product-card.component';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';
import { ProductFilter } from '../../models/filter.model';
import { Filters } from '../filters/filters';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ProductQuickViewComponent } from '../product-quick-view/product-quick-view';
import { CartService } from '../../services/cart.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [CommonModule, ProductCard, Filters, ProductQuickViewComponent],
  templateUrl: './products-page.html',
  styleUrl: './products-page.scss'
})
export class ProductsPageComponent implements OnInit, OnDestroy {
  
  // משתנים עבור ה-Quick View
  displayDialog: boolean = false;
  selectedProduct: any = null;
  productKey: number = 0;

  // משתנים מקוריים
  allProducts: Product[] = [];
  displayProducts: Product[] = [];
  paginatedProducts: Product[] = [];
  totalItems: number = 0;
  hasNextPage: boolean = false;
  currentFilters: ProductFilter = { page: 0, pageSize: 6 };
  currentPage: number = 0;
  pageSize: number = 6;
  isLoading: boolean = false;
  cartItemCount: number = 0;
  
  private destroy$ = new Subject<void>();

  constructor(
    private productService: ProductService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private cartService: CartService,
    private userService: UserService
  ) {}

  ngOnInit() {
    // Load all products once
    this.loadProducts();
    this.updateCartCount();
  }
  
  loadProducts() {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.productService.getProducts({ page: 0, pageSize: 100 }).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response: any) => {
        console.log('Products response:', response);
        if (response && response.data && Array.isArray(response.data)) {
          // Sort products by name alphabetically
          this.allProducts = response.data.sort((a: any, b: any) => 
            a.productName.localeCompare(b.productName)
          );
          this.displayProducts = [...this.allProducts];
          this.totalItems = this.allProducts.length;
          this.currentPage = 0;
          this.updatePaginatedProducts();
          console.log('Loaded products:', this.displayProducts.length);
        } else {
          console.warn('No data in response or not an array');
          this.allProducts = [];
          this.displayProducts = [];
          this.paginatedProducts = [];
          this.totalItems = 0;
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.isLoading = false;
        this.allProducts = [];
        this.displayProducts = [];
        this.paginatedProducts = [];
        this.totalItems = 0;
        this.cdr.detectChanges();
      }
    });
  }
  
  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onFiltersChanged(filters: ProductFilter) {
    // Client-side filtering for instant response
    let filtered = [...this.allProducts];
    
    // Filter by categories
    if (filters.categoryIds && filters.categoryIds.length > 0) {
      filtered = filtered.filter(p => filters.categoryIds!.includes(p.categoryId));
    }
    
    // Filter by price range
    if (filters.minPrice !== undefined) {
      filtered = filtered.filter(p => p.price >= filters.minPrice!);
    }
    if (filters.maxPrice !== undefined) {
      filtered = filtered.filter(p => p.price <= filters.maxPrice!);
    }
    
    // Filter by search term
    if (filters.description) {
      const search = filters.description.toLowerCase();
      filtered = filtered.filter(p => 
        p.productName.toLowerCase().includes(search)
      );
    }
    
    // Sort filtered products by name alphabetically
    filtered.sort((a, b) => a.productName.localeCompare(b.productName));
    
    this.displayProducts = filtered;
    this.totalItems = filtered.length;
    this.currentPage = 0; // Reset to first page when filters change
    this.updatePaginatedProducts();
  }

  updatePaginatedProducts() {
    const startIndex = this.currentPage * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.paginatedProducts = this.displayProducts.slice(startIndex, endIndex);
  }

  onPageChange(newPage: number) {
    this.currentPage = newPage;
    this.updatePaginatedProducts();
  }

  // 👇 פונקציה לפתיחת ה-Quick View
  openQuickView(product: Product) {
    this.selectedProduct = product;
    this.productKey = product.productId;
    this.displayDialog = true;
  }

  navigateToCart() {
    this.router.navigate(['/cart']);
  }

  updateCartCount() {
    this.cartItemCount = this.cartService.getCart().length;
  }

  // Track by function for ngFor
  trackByProductId(index: number, product: Product): number {
    return product.productId;
  }
  
  goToUserArea() {
    const currentUser = this.userService.getCurrentUser();
    
    if (currentUser) {
      // User is logged in, go to user profile
      this.router.navigate(['/user-profile']);
    } else {
      // User is not logged in, go to sign-in page
      this.router.navigate(['/sign-in'], { queryParams: { returnUrl: '/user-profile' } });
    }
  }
}
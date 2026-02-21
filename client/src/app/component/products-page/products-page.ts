import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ProductCard } from '../product-card/product-card.component';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';
import { ProductFilter } from '../../models/filter.model';
import { Filters } from '../filters/filters';
import { Subject } from 'rxjs';
import { takeUntil, switchMap } from 'rxjs/operators';
import { ProductQuickViewComponent } from '../product-quick-view/product-quick-view';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [CommonModule, ProductCard, Filters, ProductQuickViewComponent], // ודאי שכל אלו כאן
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
  totalItems: number = 0;
  hasNextPage: boolean = false;
  currentFilters: ProductFilter = { page: 0, pageSize: 12 };
  isLoading: boolean = false;
  cartItemCount: number = 0;
  
  private destroy$ = new Subject<void>();

  constructor(
    private productService: ProductService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private cartService: CartService
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
          this.allProducts = response.data;
          this.displayProducts = response.data;
          console.log('Loaded products:', this.displayProducts.length);
        } else {
          console.warn('No data in response or not an array');
          this.allProducts = [];
          this.displayProducts = [];
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.isLoading = false;
        this.allProducts = [];
        this.displayProducts = [];
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
    
    this.displayProducts = filtered;
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
}
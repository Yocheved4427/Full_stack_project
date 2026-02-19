import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductCard } from '../product-card/product-card.component';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';
import { ProductFilter } from '../../models/filter.model';
import { Filters } from '../filters/filters';
import { Subject } from 'rxjs';
import { takeUntil, switchMap } from 'rxjs/operators';
import { ProductQuickViewComponent } from '../product-quick-view/product-quick-view';

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
  
  private destroy$ = new Subject<void>();

  constructor(private productService: ProductService) {}

  ngOnInit() {
    // Load all products once
    this.isLoading = true;
    this.productService.getProducts({ page: 0, pageSize: 1000 }).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response: any) => {
        if (response && response.data) {
          this.allProducts = response.data;
          this.displayProducts = response.data;
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.isLoading = false;
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

  // Track by function for ngFor
  trackByProductId(index: number, product: Product): number {
    return product.productId;
  }
}
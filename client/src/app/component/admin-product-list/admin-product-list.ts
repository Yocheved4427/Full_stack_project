import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

// PrimeNG Imports
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { Toolbar } from 'primeng/toolbar';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { Toast } from 'primeng/toast';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';

// Services & Models
import { ProductService } from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import { Product, Category } from '../../models/product.model';

// Components
import { AdminProduct } from '../admin-product/admin-product';

interface ProductWithStatus extends Product {
  isActive?: boolean;
}

@Component({
  selector: 'app-admin-product-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    TagModule,
    Toolbar,
    InputTextModule,
    DialogModule,
    Toast,
    ConfirmDialog,
    AdminProduct
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './admin-product-list.html',
  styleUrl: './admin-product-list.scss',
})
export class AdminProductList implements OnInit {
  products = signal<ProductWithStatus[]>([]);
  categories = signal<Category[]>([]);
  loading = signal<boolean>(false);
  globalFilterValue: string = '';
  
  // Dialog properties
  showProductDialog: boolean = false;
  selectedProduct: Product | null = null;
  
  // Pagination
  first: number = 0;
  rows: number = 10;

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.loadCategories();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.productService.getProducts({
      page: 0,
      pageSize: 100
    }).subscribe({
      next: (response) => {
        console.log('Products loaded:', response); // Debug log
        // Use the actual isActive value from the backend
        const productsWithStatus = (response.data || []).map((p: Product) => ({
          ...p,
          isActive: p.isActive ?? true // Use backend value, default to true if undefined
        }));
        this.products.set(productsWithStatus);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load products'
        });
        this.loading.set(false);
      }
    });
  }

  loadCategories(): void {
    this.categoryService.getAllCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }

  getCategoryName(categoryId: number): string {
    const category = this.categories().find(c => c.categoryId === categoryId);
    return category ? category.categoryName : 'Unknown';
  }

  getStatusSeverity(isActive?: boolean): 'success' | 'danger' {
    return isActive ? 'success' : 'danger';
  }

  getStatusLabel(isActive?: boolean): string {
    return isActive ? 'Active' : 'Hidden';
  }

  onGlobalFilter(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.globalFilterValue = input.value;
  }

  addNewProduct(): void {
    this.selectedProduct = null;
    this.showProductDialog = true;
  }

  editProduct(product: ProductWithStatus): void {
    this.selectedProduct = product;
    this.showProductDialog = true;
  }

  onProductSave(productData: any): void {
    const isEdit = productData.productId > 0;
    
    console.log('Saving product:', productData); // Debug log
    
    if (isEdit) {
      // Update existing product
      this.productService.updateProduct(productData.productId, productData).subscribe({
        next: () => {
          this.loadProducts();
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Product updated successfully'
          });
        },
        error: (error) => {
          console.error('Error updating product:', error);
          const errorMessage = error.error?.message || error.message || 'Failed to update product';
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: errorMessage
          });
        }
      });
    } else {
      // Add new product
      this.productService.addProduct(productData).subscribe({
        next: () => {
          this.loadProducts();
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Product added successfully'
          });
        },
        error: (error) => {
          console.error('Error adding product:', error);
          const errorMessage = error.error?.message || error.message || 'Unknown error occurred';
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: `Failed to add product: ${errorMessage}`
          });
        }
      });
    }
  }

  deleteProduct(product: ProductWithStatus): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete ${product.productName}?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.productService.deleteProduct(product.productId).subscribe({
          next: () => {
            this.loadProducts();
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Product deleted successfully'
            });
          },
          error: (error) => {
            console.error('Error deleting product:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete product'
            });
          }
        });
      }
    });
  }

  clear(): void {
    this.globalFilterValue = '';
  }
}

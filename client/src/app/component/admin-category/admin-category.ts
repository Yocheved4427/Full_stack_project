import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// PrimeNG Imports
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { Toolbar } from 'primeng/toolbar';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { Toast } from 'primeng/toast';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';

// Services & Models
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category.model';

@Component({
  selector: 'app-admin-category',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    Toolbar,
    InputTextModule,
    DialogModule,
    Toast,
    ConfirmDialog
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './admin-category.html',
  styleUrl: './admin-category.scss',
})
export class AdminCategory implements OnInit {
  categories = signal<Category[]>([]);
  loading = signal<boolean>(false);
  
  // Dialog properties
  showCategoryDialog: boolean = false;
  isEditMode: boolean = false;
  categoryForm: Partial<Category> = {};

  constructor(
    private categoryService: CategoryService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.categoryService.getAllCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load categories'
        });
        this.loading.set(false);
      }
    });
  }

  addNewCategory(): void {
    this.isEditMode = false;
    this.categoryForm = {};
    this.showCategoryDialog = true;
  }

  editCategory(category: Category): void {
    this.isEditMode = true;
    this.categoryForm = { ...category };
    this.showCategoryDialog = true;
  }

  saveCategory(): void {
    if (!this.categoryForm.categoryName?.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Category name is required'
      });
      return;
    }

    if (this.isEditMode && this.categoryForm.categoryId) {
      // Update existing category
      this.categoryService.updateCategory(this.categoryForm.categoryId, this.categoryForm).subscribe({
        next: () => {
          this.loadCategories();
          this.showCategoryDialog = false;
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Category updated successfully'
          });
        },
        error: (error) => {
          console.error('Error updating category:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update category'
          });
        }
      });
    } else {
      // Add new category
      this.categoryService.addCategory(this.categoryForm).subscribe({
        next: () => {
          this.loadCategories();
          this.showCategoryDialog = false;
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Category added successfully'
          });
        },
        error: (error) => {
          console.error('Error adding category:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to add category'
          });
        }
      });
    }
  }

  deleteCategory(category: Category): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${category.categoryName}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.categoryService.deleteCategory(category.categoryId).subscribe({
          next: () => {
            this.loadCategories();
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Category deleted successfully'
            });
          },
          error: (error) => {
            console.error('Error deleting category:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete category'
            });
          }
        });
      }
    });
  }

  hideDialog(): void {
    this.showCategoryDialog = false;
    this.categoryForm = {};
  }
}

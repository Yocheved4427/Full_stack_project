import { Component, OnInit, OnChanges, Input, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

// PrimeNG Imports
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { Checkbox } from 'primeng/checkbox';
import { Image } from 'primeng/image';
import { RadioButton } from 'primeng/radiobutton';
import { Tooltip } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';

// Services & Models
import { Product, Category, ProductMonthConfig } from '../../models/product.model';
import { ImageUrlPipe } from '../../pipes/image-url.pipe';

interface ImageFile {
  file?: File;
  url: string;
  isMain: boolean;
  isNew: boolean;
}

interface MonthConfig {
  monthNumber: number;
  monthName: string;
  isAvailable: boolean;
  specialPrice: number | null;
}

@Component({
  selector: 'app-admin-product',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    ButtonModule,
    InputTextModule,
    Textarea,
    InputNumber,
    Select,
    Checkbox,
    RadioButton,
    Tooltip,
    ImageUrlPipe
  ],
  providers: [MessageService],
  templateUrl: './admin-product.html',
  styleUrl: './admin-product.scss',
})
export class AdminProduct implements OnInit, OnChanges {
  @Input() visible: boolean = false;
  @Input() product: Product | null = null;
  @Input() categories: Category[] = [];
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() onSave = new EventEmitter<any>();

  // Form fields
  productName: string = '';
  description: string = '';
  categoryId: number | null = null;
  price: number = 0;
  images: ImageFile[] = [];
  monthConfigs: MonthConfig[] = [];

  monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];

  private uploadUrl = 'https://localhost:44386/api/ImageUpload/multiple';

  constructor(
    private messageService: MessageService,
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.initializeMonthConfigs();
  }

  ngOnChanges(): void {
    if (this.visible) {
      if (this.product) {
        // Edit mode - populate form with product data
        this.loadProduct();
      } else {
        // Add mode - reset form
        this.resetForm();
      }
    }
  }

  initializeMonthConfigs(): void {
    this.monthConfigs = this.monthNames.map((name, index) => ({
      monthNumber: index + 1,
      monthName: name,
      isAvailable: true,
      specialPrice: null
    }));
  }

  loadExistingImages(): void {
    if (!this.product || !this.categoryId) return;

    // Get category name from categories array
    const category = this.categories.find(c => c.categoryId === this.categoryId);
    if (!category) return;

    // Call API to get existing images from file system
    const listUrl = `${this.uploadUrl.replace('/multiple', '/list')}?categoryName=${encodeURIComponent(category.categoryName)}&productName=${encodeURIComponent(this.productName)}`;
    
    this.http.get<string[]>(listUrl).subscribe({
      next: (imageUrls) => {
        this.images = [];
        imageUrls.forEach((url, index) => {
          this.images.push({
            url: url,
            isMain: url === this.product?.mainImageUrl || (index === 0 && !this.product?.mainImageUrl),
            isNew: false
          });
        });
      },
      error: (error) => {
        console.error('Error loading existing images:', error);
        // Fallback to database URLs if file system load fails
        this.images = [];
        if (this.product?.mainImageUrl) {
          this.images.push({
            url: this.product.mainImageUrl,
            isMain: true,
            isNew: false
          });
        }
        if (this.product?.imageUrls && this.product.imageUrls.length > 0) {
          this.product.imageUrls.forEach(url => {
            if (url !== this.product!.mainImageUrl) {
              this.images.push({
                url: url,
                isMain: false,
                isNew: false
              });
            }
          });
        }
      }
    });
  }

  loadProduct(): void {
    if (!this.product) return;

    this.productName = this.product.productName;
    this.description = this.product.description;
    this.categoryId = this.product.categoryId;
    this.price = this.product.price;

    // Load existing images from file system
    this.loadExistingImages();

    // Load month configs
    if (this.product.monthConfigs && this.product.monthConfigs.length > 0) {
      this.monthConfigs = this.monthNames.map((name, index) => {
        const config = this.product!.monthConfigs?.find(c => c.monthNumber === index + 1);
        return {
          monthNumber: index + 1,
          monthName: name,
          isAvailable: config?.isAvailable ?? true,
          specialPrice: config?.specialPrice ?? null
        };
      });
    } else {
      this.initializeMonthConfigs();
    }
  }

  resetForm(): void {
    this.productName = '';
    this.description = '';
    this.categoryId = null;
    this.price = 0;
    this.images = [];
    this.initializeMonthConfigs();
  }

  onFileSelect(event: any): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    
    console.log('onFileSelect triggered', { files, fileCount: files?.length });
    
    if (files && files.length > 0) {
      // Validate category and product name
      if (!this.categoryId || !this.productName.trim()) {
        console.warn('Missing category or product name', { categoryId: this.categoryId, productName: this.productName });
        this.messageService.add({
          severity: 'warn',
          summary: 'Missing Information',
          detail: 'Please select a category and enter a product name before uploading images.'
        });
        input.value = ''; // Clear the input
        return;
      }

      // Get category name from categories array
      const selectedCategory = this.categories.find(c => c.categoryId === this.categoryId);
      if (!selectedCategory) {
        console.error('Category not found', { categoryId: this.categoryId, categories: this.categories });
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Selected category not found.'
        });
        input.value = ''; // Clear the input
        return;
      }

      console.log('Starting upload', { 
        categoryName: selectedCategory.categoryName, 
        productName: this.productName,
        fileCount: files.length,
        uploadUrl: this.uploadUrl
      });

      // Upload files to server
      const formData = new FormData();
      for (let i = 0; i < files.length; i++) {
        formData.append('files', files[i]);
        console.log(`Added file ${i}:`, files[i].name);
      }
      formData.append('categoryName', selectedCategory.categoryName);
      formData.append('productName', this.productName);

      this.http.post<string[]>(this.uploadUrl, formData).subscribe({
        next: (uploadedUrls) => {
          console.log('Upload successful', { uploadedUrls });
          // Add uploaded images with server URLs, but avoid duplicates
          let addedCount = 0;
          uploadedUrls.forEach((url, index) => {
            // Check if this URL already exists in the images array
            const exists = this.images.some(img => img.url === url);
            if (!exists) {
              this.images.push({
                url: url,
                isMain: this.images.length === 0 && index === 0, // First image is main
                isNew: true
              });
              addedCount++;
            }
          });
          
          console.log('Images added', { addedCount, totalImages: this.images.length });
          
          // Trigger change detection to avoid NG0100 error
          this.cdr.detectChanges();
          
          if (addedCount > 0) {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `${addedCount} image(s) uploaded successfully`
            });
          } else {
            this.messageService.add({
              severity: 'info',
              summary: 'Info',
              detail: 'All selected images already exist'
            });
          }
          input.value = ''; // Clear the input after successful upload
        },
        error: (error) => {
          console.error('Upload error:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to upload images. Please try again.'
          });
          input.value = ''; // Clear the input even on error
        }
      });
    } else {
      console.warn('No files selected');
    }
  }

  getMainImageIndex(): number {
    return this.images.findIndex(img => img.isMain);
  }

  setMainImage(index: number): void {
    this.images.forEach((img, i) => {
      img.isMain = i === index;
    });
  }

  removeImage(index: number): void {
    const imageToRemove = this.images[index];
    const wasMain = imageToRemove.isMain;
    
    // For existing images (not newly uploaded), optionally delete from file system
    if (!imageToRemove.isNew && imageToRemove.url) {
      // Delete from file system
      const deleteUrl = this.uploadUrl.replace('/multiple', '') + `?imagePath=${encodeURIComponent(imageToRemove.url)}`;
      this.http.delete(deleteUrl).subscribe({
        next: () => {
          console.log('Image deleted from file system');
        },
        error: (error) => {
          console.warn('Could not delete image from file system:', error);
        }
      });
    }
    
    this.images.splice(index, 1);
    
    // If removed image was main, set first image as main
    if (wasMain && this.images.length > 0) {
      this.images[0].isMain = true;
    }
  }

  getImageUrl(url: string): string {
    if (!url) return '';
    
    // If URL starts with http/https, return as-is (absolute URL)
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    
    // If it's an old GUID-based path saved in server's wwwroot
    if (url.includes('images/products/')) {
      return `https://localhost:44386/${url}`;
    }
    
    // If path starts with "images/", it's from client/public folder
    // Add leading slash and ensure it's served from Angular dev server
    if (url.startsWith('images/')) {
      // Angular serves public folder contents at root level
      // Add leading slash for absolute path from root
      return '/' + url;
    }
    
    // If it's just a filename without path, it's from old data - try to construct path
    // This handles cases like "London England1.png"
    // We'll need category name and product name to construct full path
    // For now, return error image placeholder or the url as-is and let browser 404
    console.warn('Invalid image path format:', url);
    return url;
  }

  hideDialog(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  saveProduct(): void {
    // Validate form
    if (!this.productName || !this.categoryId || !this.price) {
      this.messageService.add({
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields'
      });
      return;
    }

    if (this.images.length === 0) {
      this.messageService.add({
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please upload at least one image'
      });
      return;
    }

    // Prepare product data
    const mainImage = this.images.find(img => img.isMain);
    const productData = {
      productId: this.product?.productId || 0,
      productName: this.productName,
      description: this.description,
      categoryId: this.categoryId!,
      price: this.price,
      isActive: true, // New/edited products are active by default
      mainImageUrl: mainImage?.url || this.images[0].url,
      imageUrls: this.images.map(img => img.url),
      monthConfigs: this.monthConfigs
        .filter(m => m.specialPrice !== null && m.specialPrice > 0)
        .map(m => ({
          configId: 0,
          monthNumber: m.monthNumber,
          isAvailable: m.isAvailable,
          specialPrice: m.specialPrice!
        }))
    };

    this.onSave.emit(productData);
    this.hideDialog();
  }
}

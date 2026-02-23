import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { GalleriaModule } from 'primeng/galleria';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ProductMonthConfig } from '../../models/product.model';
import { CartService } from '../../services/cart.service';
import { CartItem } from '../../models/cart.model';
import { ImageUrlPipe } from '../../pipes/image-url.pipe';

@Component({
  selector: 'app-product-quick-view',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ButtonModule,
    DatePickerModule,
    InputNumberModule,
    GalleriaModule,
    Tooltip,
    FormsModule
  ],
  templateUrl: './product-quick-view.html',
  styleUrls: ['./product-quick-view.scss']
})
export class ProductQuickViewComponent implements OnChanges {
  @Input() product: any;
  @Input() visible: boolean = false;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() cartUpdated = new EventEmitter<void>();

  startDate: Date | null = null;
  endDate: Date | null = null;
  participants: number = 1;
  minDate: Date = new Date();
  images: any[] = [];
  activeIndex: number = 0;
  galleryKey: number = 0;
  
  monthConfigs: ProductMonthConfig[] = [];
  peakPeriodMessage: string = '';
  priceAdjustmentPercent: number = 0;
  disabledDates: Date[] = [];
  dateValidationError: string = '';
  successMessage: string = '';
  
  private imageUrlPipe = new ImageUrlPipe();
  
  constructor(
    private cdr: ChangeDetectorRef, 
    private apiService: ApiService, 
    private cartService: CartService,
    private router: Router
  ) {}  ngOnChanges(changes: SimpleChanges) {
    // Rebuild images when product changes OR when dialog becomes visible
    if ((changes['product'] && changes['product'].currentValue) || 
        (changes['visible'] && changes['visible'].currentValue && this.product)) {
      
      // IMPORTANT: Reset active index to 0 to show main image first
      this.activeIndex = 0;
      
      // IMPORTANT: Create a NEW array (don't just clear it)
      this.images = [];
      
      // Load full product details with month configs when dialog opens
      if (changes['visible']?.currentValue && this.product?.productId) {
        this.loadProductDetails();
      }
      
      // imageUrls contains ALL images from the database
      // mainImageUrl is the one where IsMain = 1
      // We want to show mainImageUrl first, then the other images
      
      if (this.product.imageUrls && this.product.imageUrls.length > 0) {
        // First, add the main image
        if (this.product.mainImageUrl) {
          const transformedUrl = this.imageUrlPipe.transform(this.product.mainImageUrl);
          this.images.push({
            itemImageSrc: transformedUrl,
            thumbnailImageSrc: transformedUrl,
            alt: this.product.productName
          });
        }
        
        // Then add all other images (excluding the main one)
        this.product.imageUrls.forEach((url: string) => {
          if (url !== this.product.mainImageUrl) {
            const transformedUrl = this.imageUrlPipe.transform(url);
            this.images.push({
              itemImageSrc: transformedUrl,
              thumbnailImageSrc: transformedUrl,
              alt: this.product.productName
            });
          }
        });
      } else if (this.product.mainImageUrl) {
        // Fallback: if no imageUrls array, just show the main image
        const transformedUrl = this.imageUrlPipe.transform(this.product.mainImageUrl);
        this.images.push({
          itemImageSrc: transformedUrl,
          thumbnailImageSrc: transformedUrl,
          alt: this.product.productName
        });
      }
    }
  }

  loadProductDetails() {
    this.apiService.getVacationById(this.product.productId).subscribe({
      next: (productDetails: any) => {
        if (productDetails.monthConfigs) {
          this.monthConfigs = productDetails.monthConfigs;
          this.buildDisabledDates();
          this.updatePeakPeriodMessage();
        }
      },
      error: (error: any) => {
        console.error('Error loading product details:', error);
      }
    });
  }

  buildDisabledDates() {
    this.disabledDates = [];
    
    if (!this.monthConfigs || this.monthConfigs.length === 0) {
      return;
    }
    
    // Build array of disabled dates for unavailable months
    const currentYear = new Date().getFullYear();
    const nextYear = currentYear + 1;
    
    this.monthConfigs.forEach(config => {
      if (config.isAvailable === false) {
        const monthIndex = config.monthNumber - 1; // JavaScript months are 0-based
        
        // Disable all days in this month for current and next year
        [currentYear, nextYear].forEach(year => {
          const daysInMonth = new Date(year, monthIndex + 1, 0).getDate();
          for (let day = 1; day <= daysInMonth; day++) {
            this.disabledDates.push(new Date(year, monthIndex, day));
          }
        });
      }
    });
  }

  updatePeakPeriodMessage() {
    this.peakPeriodMessage = '';
    this.priceAdjustmentPercent = 0;
    
    if (!this.startDate || !this.endDate || !this.monthConfigs || this.monthConfigs.length === 0) {
      return;
    }

    // Check if the selected dates fall within any peak period months
    const startMonth = this.startDate.getMonth() + 1;
    const endMonth = this.endDate.getMonth() + 1;
    
    const config = this.monthConfigs.find(mc => 
      mc.monthNumber === startMonth && mc.isAvailable !== false && mc.specialPrice > 0
    );
    
    if (config && this.product?.price) {
      const regularPrice = this.product.price;
      const percentIncrease = ((config.specialPrice - regularPrice) / regularPrice * 100).toFixed(0);
      this.priceAdjustmentPercent = parseFloat(percentIncrease);
      this.peakPeriodMessage = `The vacation you selected is in peak period, the price is ${percentIncrease}% higher than the regular price`;
    }
  }


  get totalNights(): number {
    if (!this.startDate || !this.endDate) return 0;
    const diff = this.endDate.getTime() - this.startDate.getTime();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  get totalPrice(): number {
    if (!this.startDate || !this.endDate) return 0;
    
    const basePrice = this.product?.price || 0;
    let effectivePrice = basePrice;
    
    // Check if start date falls in a peak period month
    const startMonth = this.startDate.getMonth() + 1;
    const config = this.monthConfigs.find(mc => 
      mc.monthNumber === startMonth && mc.isAvailable !== false && mc.specialPrice > 0
    );
    
    if (config) {
      effectivePrice = config.specialPrice;
    }
    
    return this.totalNights * effectivePrice * this.participants;
  }

  onDateChange() {
    this.dateValidationError = '';
    
    // If start date is after end date, show error and clear the end date
    if (this.startDate && this.endDate && this.startDate > this.endDate) {
      this.dateValidationError = 'End date must be after start date. Please select a valid end date.';
      this.endDate = null;
    }
    this.updatePeakPeriodMessage();
  }

  isFormValid(): boolean {
    if (!this.startDate || !this.endDate || this.participants <= 0) {
      return false;
    }
    
    // Ensure start date is before or equal to end date
    return this.startDate <= this.endDate;
  }

  nextImage() {
    if (this.activeIndex < this.images.length - 1) {
      this.activeIndex++;
    } else {
      this.activeIndex = 0; // Loop back to first
    }
  }

  previousImage() {
    if (this.activeIndex > 0) {
      this.activeIndex--;
    } else {
      this.activeIndex = this.images.length - 1; // Loop to last
    }
  }

  addToCart() {
    if (!this.isFormValid()) {
      return;
    }

    const cartItem: CartItem = {
      id: this.product.productId,
      name: this.product.productName,
      price: this.totalPrice,
      image: this.product.mainImageUrl || '',
      startDate: this.startDate!,
      endDate: this.endDate!,
      quantity: this.participants
    };

    this.cartService.addToCart(cartItem);
    this.cartUpdated.emit();
    this.successMessage = 'Product added to cart successfully!';
    
    // Clear the success message after 3 seconds
    setTimeout(() => {
      this.successMessage = '';
      this.cdr.detectChanges();
    }, 3000);
  }

  goToCart() {
    this.close();
    this.router.navigate(['/cart']);
  }

  close() {
    this.visibleChange.emit(false);
  }
}
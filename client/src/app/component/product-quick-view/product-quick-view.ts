import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { GalleriaModule } from 'primeng/galleria';
import { FormsModule } from '@angular/forms';

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
    FormsModule
  ],
  templateUrl: './product-quick-view.html',
  styleUrls: ['./product-quick-view.scss']
})
export class ProductQuickViewComponent implements OnChanges {
  @Input() product: any;
  @Input() visible: boolean = false;
  @Output() visibleChange = new EventEmitter<boolean>();

  startDate: Date | null = null;
  endDate: Date | null = null;
  participants: number = 1;
  minDate: Date = new Date();
  images: any[] = [];
  activeIndex: number = 0;
  galleryKey: number = 0;
  
  constructor(private cdr: ChangeDetectorRef) {}  ngOnChanges(changes: SimpleChanges) {
    // Rebuild images when product changes OR when dialog becomes visible
    if ((changes['product'] && changes['product'].currentValue) || 
        (changes['visible'] && changes['visible'].currentValue && this.product)) {
      
      // IMPORTANT: Reset active index to 0 to show main image first
      this.activeIndex = 0;
      
      // IMPORTANT: Create a NEW array (don't just clear it)
      this.images = [];
      
      // imageUrls contains ALL images from the database
      // mainImageUrl is the one where IsMain = 1
      // We want to show mainImageUrl first, then the other images
      
      if (this.product.imageUrls && this.product.imageUrls.length > 0) {
        // First, add the main image
        if (this.product.mainImageUrl) {
          this.images.push({
            itemImageSrc: this.product.mainImageUrl,
            thumbnailImageSrc: this.product.mainImageUrl,
            alt: this.product.productName
          });
        }
        
        // Then add all other images (excluding the main one)
        this.product.imageUrls.forEach((url: string) => {
          if (url !== this.product.mainImageUrl) {
            this.images.push({
              itemImageSrc: url,
              thumbnailImageSrc: url,
              alt: this.product.productName
            });
          }
        });
      } else if (this.product.mainImageUrl) {
        // Fallback: if no imageUrls array, just show the main image
        this.images.push({
          itemImageSrc: this.product.mainImageUrl,
          thumbnailImageSrc: this.product.mainImageUrl,
          alt: this.product.productName
        });
      }
    }
  }

  get totalNights(): number {
    if (!this.startDate || !this.endDate) return 0;
    const diff = this.endDate.getTime() - this.startDate.getTime();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  get totalPrice(): number {
    return this.totalNights * (this.product?.price || 0) * this.participants;   
  }

  isFormValid(): boolean {
    return !!(this.startDate && this.endDate && this.participants > 0);
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

  close() {
    this.visibleChange.emit(false);
  }
}
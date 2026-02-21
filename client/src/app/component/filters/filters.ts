import { Component, OnInit, Output, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category.model';
import { ProductFilter } from '../../models/filter.model';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

// PrimeNG Modules
import { PanelModule } from 'primeng/panel';
import { ListboxModule } from 'primeng/listbox';
import { SliderModule } from 'primeng/slider';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext'; 

@Component({
  selector: 'app-filters',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    PanelModule, 
    ListboxModule, 
    SliderModule, 
    ButtonModule,
    InputTextModule
  ],
  templateUrl: './filters.html',
  styleUrls: ['./filters.scss']
})
export class Filters implements OnInit, OnDestroy {
  @Output() filtersChanged = new EventEmitter<ProductFilter>();

  categories: Category[] = [];
  selectedCategories: any[] = [];
  rangeValues: number[] = [0, 3000];
  searchTerm: string = '';
  
  private searchSubject = new Subject<string>();

  constructor(
    private categoryService: CategoryService,
    private cd: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.categoryService.getAllCategories().subscribe(data => {
      this.categories = data;
      this.cd.detectChanges(); 
    });
    
    // No debounce for instant response - switchMap in parent handles cancellation
    this.searchSubject.pipe(
      distinctUntilChanged()
    ).subscribe(() => {
      this.updateFilters();
    });
  }
  
  ngOnDestroy(): void {
    this.searchSubject.complete();
  }
  
  onSearchChange(): void {
    this.searchSubject.next(this.searchTerm);
  }

updateFilters() {
    const categoryIds = this.selectedCategories.length > 0 
      ? this.selectedCategories.map(c => c.categoryId)
      : undefined;
    
    const filters: ProductFilter = {
      page: 0,
      pageSize: 12,
      categoryIds: categoryIds,
      minPrice: this.rangeValues[0] > 0 ? this.rangeValues[0] : undefined,
      maxPrice: this.rangeValues[1] < 3000 ? this.rangeValues[1] : undefined,
      description: this.searchTerm.trim() || undefined
    };

    console.log('✅ שולח פילטרים לשרת:', filters);
    this.filtersChanged.emit(filters);
}

  reset() {
    this.selectedCategories = [];
    this.rangeValues = [0, 3000];
    this.searchTerm = '';
    this.updateFilters();
  }
}
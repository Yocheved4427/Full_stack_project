export interface ProductFilter {
  categoryIds?: number[];
  minPrice?: number;
  maxPrice?: number;
  description?: string; 
  page: number;         
  pageSize: number;     
}
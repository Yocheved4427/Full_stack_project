import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/product.model';
import { ProductFilter } from '../models/filter.model';

export interface PageResponse<T> {
  data: T[];
  totalItems: number;
  currentPage: number;
  pageSize: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {

  private apiUrl = 'https://localhost:44386/api/Products'; 

  constructor(private http: HttpClient) { }

getProducts(filter: ProductFilter): Observable<any> {
    let params = new HttpParams();

    
    if (filter.page !== undefined && filter.page !== null) {
      
      params = params.set('position', (filter.page + 1).toString());
    } else {
      params = params.set('position', '1');
    }

    
    if (filter.pageSize && filter.pageSize > 0) {
     
      params = params.set('skip', filter.pageSize.toString());
    } else {
      
      params = params.set('skip', '10');
    }


    if (filter.description) {
      params = params.set('description', filter.description);
    }

    if (filter.minPrice) {
      params = params.set('minPrice', filter.minPrice.toString());
    }
    if (filter.maxPrice) {
      params = params.set('maxPrice', filter.maxPrice.toString());
    }

    if (filter.categoryIds && filter.categoryIds.length > 0) {
      filter.categoryIds.forEach(id => {
        params = params.append('categoryIds', id.toString());
      });
    }

  
    return this.http.get<any>(this.apiUrl, { params });
  }
  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  addProduct(product: Partial<Product>): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product);
  }

  updateProduct(id: number, product: Partial<Product>): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, product);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
export interface ProductFilter {
  categoryIds?: number[];
  minPrice?: number;
  maxPrice?: number;
  description?: string; // לחיפוש החופשי למעלה
  page: number;         // לדפדוף
  pageSize: number;     // כמה להציג בדף (למשל 12)
}
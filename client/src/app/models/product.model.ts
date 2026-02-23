export interface ProductMonthConfig {
    configId: number;
    monthNumber: number;
    isAvailable?: boolean;
    specialPrice: number;
}

export interface Product {
    productId: number;
    productName: string;
    description: string;
    categoryId: number;
    price: number;
    isActive?: boolean;
    mainImageUrl: string; 
    imageUrls?: string[];
    monthConfigs?: ProductMonthConfig[];
}

export interface Category {
    categoryId: number;
    categoryName: string;
}


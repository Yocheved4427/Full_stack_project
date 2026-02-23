export interface CartItem {
  cartItemId: string;
  id: number;
  name: string;
  price: number;
  basePrice?: number;
  monthConfigs?: any[];
  image: string;
  startDate: Date;
  endDate: Date;
  quantity: number;
}
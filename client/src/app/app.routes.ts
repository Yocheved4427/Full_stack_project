import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { ProductsPageComponent } from './component/products-page/products-page';
import { Cart } from './component/cart/cart';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'products', component: ProductsPageComponent },
  { path: 'cart', component: Cart },
  { path: '**', redirectTo: '' }
];

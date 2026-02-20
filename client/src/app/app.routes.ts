import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { ProductsPageComponent } from './component/products-page/products-page';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'products', component: ProductsPageComponent },
  { path: '**', redirectTo: '' }
];

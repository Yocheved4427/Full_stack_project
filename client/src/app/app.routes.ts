import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home';
import { ProductsPageComponent } from './component/products-page/products-page';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'products', component: ProductsPageComponent },
  { path: '**', redirectTo: '' }
];

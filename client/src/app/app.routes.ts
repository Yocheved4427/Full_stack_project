import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { ProductsPageComponent } from './component/products-page/products-page';
import { Cart } from './component/cart/cart';
import { SignIn } from './component/sign-in/sign-in';
import { SignUp } from './component/sign-up/sign-up';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'products', component: ProductsPageComponent },
  { path: 'cart', component: Cart },
  {path: 'sign-in', component: SignIn},
  {path: 'sign-up', component: SignUp},
  { path: '**', redirectTo: '' }
];

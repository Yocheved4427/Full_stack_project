import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { ProductsPageComponent } from './component/products-page/products-page';
import { Cart } from './component/cart/cart';
import { SignIn } from './component/sign-in/sign-in';
import { SignUp } from './component/sign-up/sign-up';
import { AdminLayout } from './component/admin-layout/admin-layout';
import { AdminProductList } from './component/admin-product-list/admin-product-list';
import { AdminCategory } from './component/admin-category/admin-category';
import { adminGuard } from './guards/admin.guard';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'products', component: ProductsPageComponent },
  { path: 'cart', component: Cart },
  { path: 'sign-in', component: SignIn },
  { path: 'sign-up', component: SignUp },
  {
    path: 'admin',
    component: AdminLayout,
    canActivate: [adminGuard],
    children: [
      { path: '', redirectTo: 'products', pathMatch: 'full' },
      { path: 'products', component: AdminProductList },
      { path: 'categories', component: AdminCategory },
      // Add more admin routes here (orders, etc.)
    ]
  },
  { path: '**', redirectTo: '' }
];

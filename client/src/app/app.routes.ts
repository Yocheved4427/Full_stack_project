import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { ProductsPageComponent } from './component/products-page/products-page';
import { Cart } from './component/cart/cart';
import { SignIn } from './component/sign-in/sign-in';
import { SignUp } from './component/sign-up/sign-up';
import { UserProfile } from './component/user-profile/user-profile';
import { AdminLayout } from './component/admin-layout/admin-layout';
import { AdminDashboard } from './component/admin-dashboard/admin-dashboard';
import { AdminProductList } from './component/admin-product-list/admin-product-list';
import { AdminCategory } from './component/admin-category/admin-category';
import { AdminOrders } from './component/admin-orders/admin-orders';
import { adminGuard } from './guards/admin.guard';
import { ThankYou } from './component/thank-you/thank-you';
import { DreamVacationComponent } from './component/dream-vacation/dream-vacation.component';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'products', component: ProductsPageComponent },
  { path: 'cart', component: Cart },
  { path: 'sign-in', component: SignIn },
  { path: 'sign-up', component: SignUp },
  { path: 'user-profile', component: UserProfile },
  { path: 'thank-you', component: ThankYou },
  { path: 'dream-vacation', component: DreamVacationComponent },
  {
    path: 'admin',
    component: AdminLayout,
    canActivate: [adminGuard],
    children: [
      { path: '', component: AdminDashboard },
      { path: 'products', component: AdminProductList },
      { path: 'categories', component: AdminCategory },
      { path: 'orders', component: AdminOrders }
    ]
  },
  { path: '**', redirectTo: '' }
];

import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { UserService } from '../services/user.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const userService = inject(UserService);
  const router = inject(Router);

  // Check if user is logged in and is admin
  if (userService.isLoggedIn() && userService.isAdmin()) {
    return true;
  }

  // If not admin, redirect to home or sign-in
  if (!userService.isLoggedIn()) {
    // Not logged in, redirect to sign-in with return URL
    router.navigate(['/sign-in'], { 
      queryParams: { returnUrl: state.url } 
    });
  } else {
    // Logged in but not admin, redirect to home
    router.navigate(['/']);
  }
  
  return false;
};

import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { ApiService } from '../../services/api.service';
import { UserService } from '../../services/user.service';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-sign-in',
  imports: [CommonModule, FormsModule, ButtonModule, CheckboxModule, InputTextModule, CardModule, RouterModule],
  standalone: true,
  templateUrl: './sign-in.html',
  styleUrls: ['./sign-in.scss'],
})
export class SignIn {
  returnUrl: string = '/';
  checked1 = signal<boolean>(true);
  email = signal<string>('');
  password = signal<string>('');
  isLoading = signal<boolean>(false);
  errorMessage = signal<string>('');

  constructor(
    private apiService: ApiService, 
    private userService: UserService,
    private router: Router, 
    private route: ActivatedRoute
  ) {}
  
  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }
  
  login(): void {
    
    this.errorMessage.set('');
    
    if (!this.email() || !this.password()) {
      this.errorMessage.set('Please enter email and password');
      return;
    }

    this.isLoading.set(true);
    this.apiService.login(this.email(), this.password()).subscribe({
      next: (response: any) => {
        console.log('Login successful:', response);
        
        // Save token to localStorage
        if (response.token) {
          localStorage.setItem('auth_token', response.token);
        }
        
        // Save user data
        if (response.user || response) {
          const userData = response.user || response;
          this.userService.loginUser(userData);
          
          // Check if user is admin and route accordingly
          if (userData.isAdmin === true) {
            console.log('Admin user detected, routing to admin panel');
            this.router.navigate(['/admin']);
          } else {
            // Regular user, navigate to return URL or home
            this.router.navigate([this.returnUrl]);
          }
        } else {
          // Fallback if no user data
          this.router.navigate([this.returnUrl]);
        }
      },
      error: (error: any) => {
        console.error('Login failed:', error);
        console.error('Status:', error.status);
        console.error('Response body:', error.error);
        const msg = this.extractErrorMessage(error, 'Invalid email or password');
        this.errorMessage.set(msg);
        this.isLoading.set(false);
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  private extractErrorMessage(error: any, defaultMessage: string): string {
    if (typeof error?.error === 'string' && error.error.trim()) {
      return error.error;
    }

    if (error?.error?.message) {
      return error.error.message;
    }

    if (error?.message && !error.message.includes('Http failure response')) {
      return error.message;
    }

    return defaultMessage;
  }
}

import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { ApiService } from '../../services/api.service';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-sign-in',
  imports: [CommonModule, FormsModule, ButtonModule, CheckboxModule, InputTextModule, CardModule, RouterModule],
  standalone: true,
  templateUrl: './sign-in.html',
  styleUrls: ['./sign-in.scss'],
})
export class SignIn {
  checked1 = signal<boolean>(true);
  email = signal<string>('');
  password = signal<string>('');
  isLoading = signal<boolean>(false);
  errorMessage = signal<string>('');

  constructor(private apiService: ApiService, private router: Router) {}

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
        // Navigate to home or dashboard
        this.router.navigate(['/home']);
      },
      error: (error: any) => {
        console.error('Login failed:', error);
        console.error('Status:', error.status);
        console.error('Response body:', error.error);
        const msg = error.error?.message || error.statusText || 'Login failed. Please try again.';
        this.errorMessage.set(msg);
        this.isLoading.set(false);
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }
}

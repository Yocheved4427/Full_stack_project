import { Component, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { ButtonModule } from "primeng/button";
import { CheckboxModule } from "primeng/checkbox";
import { InputTextModule } from "primeng/inputtext";
import { CardModule } from "primeng/card";
import { RouterModule } from "@angular/router";
import { ApiService } from "../../services/api.service";
import { Router } from "@angular/router";

@Component({
  selector: "app-sign-up",
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CheckboxModule,
    InputTextModule,
    CardModule,
    RouterModule,
  ],
  standalone: true,
  templateUrl: "./sign-up.html",
  styleUrls: ["./sign-up.scss"],
})
export class SignUp {
  firstName = signal<string>("");
  lastName = signal<string>("");
  email = signal<string>("");
  password = signal<string>("");
  confirmPassword = signal<string>("");
  agreedToTerms = signal<boolean>(false);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string>("");

  constructor(private apiService: ApiService, private router: Router) {}

  signup(): void {
    this.errorMessage.set("");

    // Validation
    if (
      !this.firstName() ||
      !this.lastName() ||
      !this.email() ||
      !this.password()
    ) {
      this.errorMessage.set("Please fill in all fields");
      return;
    }

    if (this.password() !== this.confirmPassword()) {
      this.errorMessage.set("Passwords do not match");
      return;
    }

    if (!this.agreedToTerms()) {
      this.errorMessage.set(
        "You must agree to the Terms and Conditions"
      );
      return;
    }

    // Email validation
    const emailRegex = /^[^@]+@[^@]+\.[^@]+$/;
    if (!emailRegex.test(this.email())) {
      this.errorMessage.set("Please enter a valid email address");
      return;
    }

    // Password validation
    if (this.password().length < 6) {
      this.errorMessage.set("Password must be at least 6 characters long");
      return;
    }

    this.isLoading.set(true);
    const signupData = {
      firstName: this.firstName(),
      lastName: this.lastName(),
      email: this.email(),
      password: this.password(),
    };

    // Call your API registration endpoint
    this.apiService.register(signupData).subscribe({
      next: (response: any) => {
        console.log("Signup successful:", response);
        // Navigate to sign-in page after successful signup
        this.router.navigate(["/sign-in"]);
      },
      error: (error: any) => {
        console.error("Signup failed:", error);
        this.errorMessage.set(this.extractErrorMessage(error));
        this.isLoading.set(false);
      },
      complete: () => {
        this.isLoading.set(false);
      },
    });
  }

  private extractErrorMessage(error: any): string {
    if (typeof error?.error === 'string' && error.error.trim()) {
      return error.error;
    }

    if (error?.error?.message) {
      return error.error.message;
    }

    if (error?.message && !error.message.includes('Http failure response')) {
      return error.message;
    }

    return 'Signup failed. Please check your password and try again.';
  }
}

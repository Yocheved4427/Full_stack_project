import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  // 1. פונקציה לבדיקה אם מחובר
  isLoggedIn(): boolean {
    // בודק אם יש נתוני משתמש שמורים בדפדפן
    return localStorage.getItem('currentUser') !== null;
  }

  // 2. פונקציה לשמירת המשתמש (נשתמש בה אחרי שהשרת יאשר הרשמה/התחברות)
  loginUser(userData: any) {
    localStorage.setItem('currentUser', JSON.stringify(userData));
  }

  // 3. פונקציה לניתוק - פשוט מוחקת את הנתונים מהזיכרון!
  logoutUser() {
    localStorage.removeItem('currentUser');
    localStorage.removeItem('auth_token');
  }

  // 4. Get current user data
  getCurrentUser(): any {
    const userData = localStorage.getItem('currentUser');
    return userData ? JSON.parse(userData) : null;
  }

  // 5. Check if user is admin
  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.isAdmin === true;
  }

  // 6. Get user role
  getUserRole(): string {
    const user = this.getCurrentUser();
    return user?.isAdmin ? 'admin' : 'user';
  }
}
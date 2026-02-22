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
  }
}
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  isLoggedIn(): boolean {
 
    return localStorage.getItem('currentUser') !== null;
  }

  loginUser(userData: any) {
    localStorage.setItem('currentUser', JSON.stringify(userData));
  }

  
  logoutUser() {
    localStorage.removeItem('currentUser');
    localStorage.removeItem('auth_token');
  }

 
  getCurrentUser(): any {
    const userData = localStorage.getItem('currentUser');
    return userData ? JSON.parse(userData) : null;
  }

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.isAdmin === true;
  }

  getUserRole(): string {
    const user = this.getCurrentUser();
    return user?.isAdmin ? 'admin' : 'user';
  }

  updateUserData(userData: any) {
    const currentUser = this.getCurrentUser();
    if (currentUser) {
      const updatedUser = { ...currentUser, ...userData };
      localStorage.setItem('currentUser', JSON.stringify(updatedUser));
    }
  }
}
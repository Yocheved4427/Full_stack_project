import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {


  private apiUrl = 'https://localhost:44386/api';

  constructor(private http: HttpClient) { }

  
  getVacations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Products`);
  }
  
  
  getVacationById(id: number): Observable<any> {
      return this.http.get<any>(`${this.apiUrl}/Products/${id}`);
  }

  login(email: string, password: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/Users/login`, { email, password });
  }

  register(signupData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/Users`, signupData);
  }

  getUserOrders(userId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Orders/user/${userId}`);
  }

  createOrder(orderData: any): Observable<any> {
    console.log('API Service: Creating order', orderData);
    return this.http.post<any>(`${this.apiUrl}/Orders`, orderData);
  }

  sendOrderConfirmationEmail(emailData: any): Observable<any> {
    console.log('API Service: Sending email', emailData);
    // Call the real backend email endpoint
    return this.http.post<any>(`${this.apiUrl}/Email/send-order-confirmation`, {
      to: emailData.to,
      customerName: emailData.customerName,
      orderNumber: emailData.orderNumber,
      orderTotal: emailData.orderTotal,
      orderItems: emailData.orderItems.map((item: any) => 
        `${item.name} (${item.quantity} participants) - $${item.price}`
      ).join(', ')
    });
  }

  updateUser(userData: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/Users/${userData.id}`, userData);
  }

  changePassword(passwordData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/Users/change-password`, passwordData);
  }
}
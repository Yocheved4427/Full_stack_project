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
}
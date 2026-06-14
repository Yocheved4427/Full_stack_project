import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  VacationBuildResponse,
  SimulatorRequest,
  SimulatorResponse,
} from '../models/vacation.model';

@Injectable({ providedIn: 'root' })
export class VacationService {
  private readonly base = '/api/vacation';

  constructor(private http: HttpClient) {}

  /** Input: free text */
  buildFromText(text: string): Observable<VacationBuildResponse> {
    const form = new FormData();
    form.append('text', text);
    return this.http.post<VacationBuildResponse>(`${this.base}/build`, form);
  }

  /** Input: voice recording file */
  buildFromAudio(file: File): Observable<VacationBuildResponse> {
    const form = new FormData();
    form.append('audio', file, file.name);
    return this.http.post<VacationBuildResponse>(`${this.base}/build`, form);
  }

  /** Input: inspiration image file */
  buildFromImage(file: File): Observable<VacationBuildResponse> {
    const form = new FormData();
    form.append('image', file, file.name);
    return this.http.post<VacationBuildResponse>(`${this.base}/build`, form);
  }

  /** What-If Simulator: re-rank without any AI call */
  simulate(req: SimulatorRequest): Observable<SimulatorResponse> {
    return this.http.post<SimulatorResponse>(`${this.base}/simulator`, req);
  }
}

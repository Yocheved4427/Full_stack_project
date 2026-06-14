import { Product } from './product.model';

// ── Build endpoint ────────────────────────────────────────────────────────────

export interface VacationAnalysis {
  detectedVibe: string;
  requestedWeather: string;
  pace: string;
  estimatedBudgetLevel: 'low' | 'medium' | 'high';
}

export type TravelTwin =
  | 'Explorer'
  | 'Luxury Traveler'
  | 'Nature Escapist'
  | 'Urban Discoverer'
  | 'Adrenaline Hunter';

export interface VacationBuildResponse {
  analysis: VacationAnalysis;
  travelTwin: TravelTwin;
  searchQuery: string;
  recommendedPackages: Product[];
  whyRecommended: string[];
  similarityScores: number[];
}

// ── Simulator endpoint ────────────────────────────────────────────────────────

export interface SimulatorRequest {
  luxuryLevel: number;      // 1–5
  natureVibe: number;       // 1–5
  budgetLimit: number;      // 0 = no limit
  attractionCount: number;  // ≥ 0
}

export interface SimulatorResultItem {
  product: Product;
  score: number;
  matchReason: string;
}

export interface SimulatorResponse {
  results: SimulatorResultItem[];
  totalConsidered: number;
  totalExcludedByBudget: number;
}

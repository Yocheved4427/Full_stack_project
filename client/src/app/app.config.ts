import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router'; // הייבוא שהיה חסר
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';
import { provideHttpClient } from '@angular/common/http';
import { routes } from './app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes), 
    provideHttpClient(),
    provideAnimations(),
    providePrimeNG({
        theme: {
            preset: Aura
        }
    })
  ]
};
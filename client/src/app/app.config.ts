import { ApplicationConfig } from '@angular/core';
import { provideRouter, withRouterConfig } from '@angular/router'; // הייבוא שהיה חסר
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';
import { authInterceptor } from './auth-interceptor';
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withRouterConfig({ onSameUrlNavigation: 'reload' })), 
    provideHttpClient(),
    provideAnimations(),
    providePrimeNG({
        theme: {
            preset: Aura
        },
        zIndex: {
            modal: 1100,
            overlay: 10000,
            menu: 10000,
            tooltip: 10000
        }
    }),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
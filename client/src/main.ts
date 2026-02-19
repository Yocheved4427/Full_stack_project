import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app'; // ודאי שזה השם הנכון של הקומפוננטה הראשית
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

bootstrapApplication(App, {
  providers: [
    provideHttpClient(),      // הזרקה ישירה כאן עוקפת את כל הבעיות
    provideRouter(routes),
    providePrimeNG({
        theme: {
            preset: Aura
        }
    })
  ]
}).catch((err) => console.error(err));
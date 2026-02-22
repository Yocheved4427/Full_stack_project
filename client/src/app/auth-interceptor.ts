import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // 1. שליפת הטוקן מהאחסון
  const token = localStorage.getItem('auth_token');

  // 2. אם יש טוקן, נשכפל את הבקשה ונוסיף לה את ה-Header
  if (token) {
    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(cloned);
  }

  // 3. אם אין טוקן, נשלח את הבקשה המקורית (כמו שהיא)
  return next(req);
};
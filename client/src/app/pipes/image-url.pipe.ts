import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'imageUrl',
  standalone: true
})
export class ImageUrlPipe implements PipeTransform {
  transform(url: string | undefined | null): string {
    if (!url) return '';
    
    // If URL starts with http/https, return as-is (absolute URL)
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    
    // If it's an old GUID-based path saved in server's wwwroot
    if (url.includes('images/products/')) {
      return `https://localhost:44386/${url}`;
    }
    
    // If path starts with "images/", serve it through the backend API
    if (url.startsWith('images/')) {
      return `https://localhost:44386/api/ImageUpload/image?path=${encodeURIComponent(url)}`;
    }
    
    // Fallback: return as-is and let browser handle it
    return url;
  }
}

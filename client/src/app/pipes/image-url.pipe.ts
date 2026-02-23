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
    
    // If path starts with "images/", it's from client/public folder
    // Split the path, encode each segment, then rejoin
    if (url.startsWith('images/')) {
      const parts = url.split('/');
      const encodedParts = parts.map(part => encodeURIComponent(part));
      return '/' + encodedParts.join('/');
    }
    
    // Fallback: return as-is and let browser handle it
    return url;
  }
}

import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'imageUrl',
  standalone: true
})
export class ImageUrlPipe implements PipeTransform {
  transform(url: string | undefined | null): string {
    if (!url) return '';
    const normalizedUrl = url.replace(/\\/g, '/').trim();

    // If URL starts with http/https, return as-is (absolute URL)
    if (normalizedUrl.startsWith('http://') || normalizedUrl.startsWith('https://')) {
      return normalizedUrl;
    }

    // If path is already absolute from app root
    if (normalizedUrl.startsWith('/images/')) {
      return encodeURI(normalizedUrl);
    }

    // If it's an old GUID-based path saved in server's wwwroot
    if (normalizedUrl.includes('images/products/')) {
      return `https://localhost:44386/${normalizedUrl}`;
    }

    // Product/category images are served by Angular from client/public/images
    if (normalizedUrl.startsWith('images/')) {
      return encodeURI(`/${normalizedUrl}`);
    }

    // Fallback: return as-is and let browser handle it
    return encodeURI(normalizedUrl);
  }
}

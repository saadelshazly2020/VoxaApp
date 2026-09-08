import { authService } from './auth.service';

export interface UploadResult {
  fileName: string;
  originalName: string;
  url: string;
  size: number;
}

class UploadService {
  private getHeaders(): Record<string, string> {
    const token = authService.getToken();
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  async uploadImages(files: File[]): Promise<UploadResult[]> {
    const formData = new FormData();
    for (const file of files) {
      formData.append('files', file);
    }

    const response = await fetch('/api/upload/image', {
      method: 'POST',
      headers: this.getHeaders(),
      body: formData
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Upload failed' }));
      throw new Error(error.message || 'Upload failed');
    }

    const data = await response.json();
    return data.files;
  }

  async uploadAttachment(file: File): Promise<UploadResult> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/api/upload/attachment', {
      method: 'POST',
      headers: this.getHeaders(),
      body: formData
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Upload failed' }));
      throw new Error(error.message || 'Upload failed');
    }

    return await response.json();
  }
}

export const uploadService = new UploadService();

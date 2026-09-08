import { authService } from './auth.service';

export interface ProfileData {
  id: number;
  username: string;
  displayName?: string;
  profilePictureUrl?: string;
  bio?: string;
  email?: string;
  isOnline?: boolean;
  lastSeen?: string;
  createdAt?: string;
  isFriend?: boolean;
  postCount?: number;
}

class ProfileService {
  private getHeaders(): Record<string, string> {
    const token = authService.getToken();
    return {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    };
  }

  async getMyProfile(): Promise<ProfileData | null> {
    try {
      const response = await fetch('/api/profile/me', {
        headers: this.getHeaders()
      });
      if (!response.ok) return null;
      return await response.json();
    } catch {
      return null;
    }
  }

  async updateMyProfile(data: {
    displayName?: string;
    bio?: string;
    profilePictureUrl?: string;
  }): Promise<{ success: boolean; message: string; user?: ProfileData }> {
    try {
      const response = await fetch('/api/profile/me', {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify(data)
      });
      const result = await response.json();
      if (!response.ok) return { success: false, message: result.message };
      return { success: true, message: result.message, user: result.user };
    } catch {
      return { success: false, message: 'Failed to update profile' };
    }
  }

  async getUserProfile(userId: number): Promise<ProfileData | null> {
    try {
      const response = await fetch(`/api/profile/${userId}`, {
        headers: this.getHeaders()
      });
      if (!response.ok) return null;
      return await response.json();
    } catch {
      return null;
    }
  }
}

export const profileService = new ProfileService();

import { authService } from './auth.service';

export interface Friend {
  id: number;
  username: string;
  displayName: string;
  isOnline: boolean;
  profilePictureUrl?: string;
  lastSeen?: string;
}

export interface FriendRequest {
  id: number;
  sender: {
    id: number;
    username: string;
    displayName: string;
    profilePictureUrl?: string;
  };
  message?: string;
  createdAt: string;
}

export interface SentRequest {
  id: number;
  receiver: {
    id: number;
    username: string;
    displayName: string;
    profilePictureUrl?: string;
  };
  message?: string;
  createdAt: string;
}

export interface SearchUserResult {
  id: number;
  username: string;
  displayName: string;
  isOnline: boolean;
  profilePictureUrl?: string;
  isFriend: boolean;
  hasPendingRequest: boolean;
}

export class FriendshipService {
  private token = authService.getToken();

  private getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${this.token || authService.getToken()}`
    };
  }

  async sendFriendRequest(receiverId: number, message?: string): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch('/api/friendship/send-request', {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify({ receiverId, message })
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message };
    } catch (error) {
      console.error('Error sending friend request:', error);
      return { success: false, message: 'Failed to send friend request' };
    }
  }

  async acceptFriendRequest(requestId: number): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(`/api/friendship/accept-request/${requestId}`, {
        method: 'POST',
        headers: this.getHeaders()
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message };
    } catch (error) {
      console.error('Error accepting friend request:', error);
      return { success: false, message: 'Failed to accept request' };
    }
  }

  async rejectFriendRequest(requestId: number): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(`/api/friendship/reject-request/${requestId}`, {
        method: 'POST',
        headers: this.getHeaders()
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message };
    } catch (error) {
      console.error('Error rejecting friend request:', error);
      return { success: false, message: 'Failed to reject request' };
    }
  }

  async getFriends(): Promise<Friend[]> {
    try {
      const response = await fetch('/api/friendship/list', {
        headers: this.getHeaders()
      });

      if (!response.ok) {
        console.error('Failed to get friends');
        return [];
      }

      const data = await response.json();
      return data.friends || [];
    } catch (error) {
      console.error('Error getting friends:', error);
      return [];
    }
  }

  async getPendingRequests(): Promise<FriendRequest[]> {
    try {
      const response = await fetch('/api/friendship/pending-requests', {
        headers: this.getHeaders()
      });

      if (!response.ok) {
        console.error('Failed to get pending requests');
        return [];
      }

      const data = await response.json();
      return data.requests || [];
    } catch (error) {
      console.error('Error getting pending requests:', error);
      return [];
    }
  }

  async getSentRequests(): Promise<SentRequest[]> {
    try {
      const response = await fetch('/api/friendship/sent-requests', {
        headers: this.getHeaders()
      });

      if (!response.ok) {
        console.error('Failed to get sent requests');
        return [];
      }

      const data = await response.json();
      return data.requests || [];
    } catch (error) {
      console.error('Error getting sent requests:', error);
      return [];
    }
  }

  async removeFriend(friendId: number): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(`/api/friendship/remove/${friendId}`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message };
    } catch (error) {
      console.error('Error removing friend:', error);
      return { success: false, message: 'Failed to remove friend' };
    }
  }

  async searchUsers(query: string): Promise<SearchUserResult[]> {
    try {
      const response = await fetch(`/api/friendship/search/${encodeURIComponent(query)}`, {
        headers: this.getHeaders()
      });

      if (!response.ok) {
        return [];
      }

      const data = await response.json();
      return data.users || [];
    } catch (error) {
      console.error('Error searching users:', error);
      return [];
    }
  }
}

export const friendshipService = new FriendshipService();

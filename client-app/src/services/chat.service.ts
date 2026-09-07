import { authService } from './auth.service';

export interface ChatMessage {
  id: number;
  senderId: number;
  sender: {
    id: number;
    username: string;
    displayName: string;
    profilePictureUrl?: string;
  };
  receiverId: number;
  content: string;
  attachmentUrl?: string;
  attachmentType?: string;
  sentAt: string;
  isRead: boolean;
  readAt?: string;
  isDeleted: boolean;
  isSentByMe: boolean;
}

export interface Conversation {
  id: number;
  otherUser: {
    id: number;
    username: string;
    displayName: string;
    profilePictureUrl?: string;
    isOnline: boolean;
    lastSeen?: string;
  };
  lastMessage?: {
    content: string;
    sentAt: string;
    isRead: boolean;
    attachmentUrl?: string;
    isSentByMe: boolean;
  };
  unreadCount: number;
  lastMessageAt: string;
}

export class ChatService {
  private getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${authService.getToken()}`
    };
  }

  async sendMessage(
    receiverId: number, 
    content: string, 
    attachmentUrl?: string, 
    attachmentType?: string
  ): Promise<{ success: boolean; message: string; chatMessage?: ChatMessage }> {
    try {
      const response = await fetch('/api/chat/send', {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify({ receiverId, content, attachmentUrl, attachmentType })
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message, chatMessage: data.chatMessage };
    } catch (error) {
      console.error('Error sending message:', error);
      return { success: false, message: 'Failed to send message' };
    }
  }

  async getConversationMessages(
    otherUserId: number, 
    skip: number = 0, 
    take: number = 50
  ): Promise<ChatMessage[]> {
    try {
      const response = await fetch(
        `/api/chat/conversation/${otherUserId}?skip=${skip}&take=${take}`,
        {
          headers: this.getHeaders()
        }
      );

      if (!response.ok) {
        console.error('Failed to get messages');
        return [];
      }

      const data = await response.json();
      return data.messages || [];
    } catch (error) {
      console.error('Error getting messages:', error);
      return [];
    }
  }

  async getConversations(): Promise<Conversation[]> {
    try {
      const response = await fetch('/api/chat/conversations', {
        headers: this.getHeaders()
      });

      if (!response.ok) {
        console.error('Failed to get conversations');
        return [];
      }

      const data = await response.json();
      return data.conversations || [];
    } catch (error) {
      console.error('Error getting conversations:', error);
      return [];
    }
  }

  async markAsRead(messageId: number): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(`/api/chat/mark-read/${messageId}`, {
        method: 'POST',
        headers: this.getHeaders()
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message };
    } catch (error) {
      console.error('Error marking as read:', error);
      return { success: false, message: 'Failed to mark as read' };
    }
  }

  async markConversationAsRead(otherUserId: number): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(`/api/chat/mark-conversation-read/${otherUserId}`, {
        method: 'POST',
        headers: this.getHeaders()
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message };
    } catch (error) {
      console.error('Error marking conversation as read:', error);
      return { success: false, message: 'Failed to mark conversation as read' };
    }
  }

  async deleteMessage(messageId: number): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(`/api/chat/delete/${messageId}`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message };
    } catch (error) {
      console.error('Error deleting message:', error);
      return { success: false, message: 'Failed to delete message' };
    }
  }

  async getTotalUnreadCount(): Promise<number> {
    try {
      const response = await fetch('/api/chat/unread-count', {
        headers: this.getHeaders()
      });

      if (!response.ok) {
        return 0;
      }

      const data = await response.json();
      return data.unreadCount || 0;
    } catch (error) {
      console.error('Error getting unread count:', error);
      return 0;
    }
  }

  async getUnreadCount(otherUserId: number): Promise<number> {
    try {
      const response = await fetch(`/api/chat/unread-count/${otherUserId}`, {
        headers: this.getHeaders()
      });

      if (!response.ok) {
        return 0;
      }

      const data = await response.json();
      return data.unreadCount || 0;
    } catch (error) {
      console.error('Error getting unread count:', error);
      return 0;
    }
  }
}

export const chatService = new ChatService();

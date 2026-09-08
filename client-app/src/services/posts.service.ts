import { authService } from './auth.service';

export interface PostAuthor {
  id: number;
  username: string;
  displayName: string;
  profilePictureUrl?: string;
}

export interface PostComment {
  id: number;
  postId: number;
  authorId: number;
  author: PostAuthor;
  content: string;
  createdAt: string;
  updatedAt?: string;
  isOwnComment: boolean;
}

export interface Post {
  id: number;
  authorId: number;
  author: PostAuthor;
  content: string;
  imageUrl?: string;
  createdAt: string;
  updatedAt?: string;
  isOwnPost: boolean;
  myReaction: string | null;
  reactionCounts: Record<string, number>;
  totalReactions: number;
  comments: PostComment[];
  totalComments: number;
}

export const REACTION_EMOJIS: Record<string, string> = {
  Like:  '\u{1F44D}',
  Love:  '\u{2764}\u{FE0F}',
  Haha:  '\u{1F602}',
  Wow:   '\u{1F62E}',
  Sad:   '\u{1F622}',
  Angry: '\u{1F621}'
};

export class PostsService {
  private getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${authService.getToken()}`
    };
  }

  async getFeed(skip = 0, take = 20): Promise<Post[]> {
    try {
      const res = await fetch(`/api/posts/feed?skip=${skip}&take=${take}`, {
        headers: this.getHeaders()
      });
      if (!res.ok) return [];
      const data = await res.json();
      return data.posts ?? [];
    } catch {
      return [];
    }
  }

  async createPost(content: string, imageUrls?: string[]): Promise<{ success: boolean; message: string; post?: Post }> {
    try {
      const res = await fetch('/api/posts', {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify({ content, imageUrl: imageUrls?.[0] })
      });
      const data = await res.json();
      if (!res.ok) return { success: false, message: data.message };
      return { success: true, message: data.message, post: data.post };
    } catch {
      return { success: false, message: 'Failed to create post' };
    }
  }

  async editPost(postId: number, content: string): Promise<{ success: boolean; message: string; post?: Post }> {
    try {
      const res = await fetch(`/api/posts/${postId}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify({ content })
      });
      const data = await res.json();
      if (!res.ok) return { success: false, message: data.message };
      return { success: true, message: data.message, post: data.post };
    } catch {
      return { success: false, message: 'Failed to update post' };
    }
  }

  async deletePost(postId: number): Promise<{ success: boolean; message: string }> {
    try {
      const res = await fetch(`/api/posts/${postId}`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });
      const data = await res.json();
      if (!res.ok) return { success: false, message: data.message };
      return { success: true, message: data.message };
    } catch {
      return { success: false, message: 'Failed to delete post' };
    }
  }

  async reactToPost(postId: number, type: string): Promise<{ success: boolean; message: string }> {
    try {
      const res = await fetch(`/api/posts/${postId}/react`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify({ type })
      });
      const data = await res.json();
      if (!res.ok) return { success: false, message: data.message };
      return { success: true, message: data.message };
    } catch {
      return { success: false, message: 'Failed to react' };
    }
  }

  async removeReaction(postId: number): Promise<{ success: boolean; message: string }> {
    try {
      const res = await fetch(`/api/posts/${postId}/react`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });
      const data = await res.json();
      if (!res.ok) return { success: false, message: data.message };
      return { success: true, message: data.message };
    } catch {
      return { success: false, message: 'Failed to remove reaction' };
    }
  }

  async addComment(postId: number, content: string): Promise<{ success: boolean; message: string; comment?: PostComment }> {
    try {
      const res = await fetch(`/api/posts/${postId}/comments`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify({ content })
      });
      const data = await res.json();
      if (!res.ok) return { success: false, message: data.message };
      return { success: true, message: data.message, comment: data.comment };
    } catch {
      return { success: false, message: 'Failed to add comment' };
    }
  }

  async getComments(postId: number, skip = 0, take = 20): Promise<PostComment[]> {
    try {
      const res = await fetch(`/api/posts/${postId}/comments?skip=${skip}&take=${take}`, {
        headers: this.getHeaders()
      });
      if (!res.ok) return [];
      const data = await res.json();
      return data.comments ?? [];
    } catch {
      return [];
    }
  }

  async editComment(commentId: number, content: string): Promise<{ success: boolean; message: string }> {
    try {
      const res = await fetch(`/api/posts/comments/${commentId}`, {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify({ content })
      });
      const data = await res.json();
      if (!res.ok) return { success: false, message: data.message };
      return { success: true, message: data.message };
    } catch {
      return { success: false, message: 'Failed to update comment' };
    }
  }

  async deleteComment(commentId: number): Promise<{ success: boolean; message: string }> {
    try {
      const res = await fetch(`/api/posts/comments/${commentId}`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });
      const data = await res.json();
      if (!res.ok) return { success: false, message: data.message };
      return { success: true, message: data.message };
    } catch {
      return { success: false, message: 'Failed to delete comment' };
    }
  }
}

export const postsService = new PostsService();

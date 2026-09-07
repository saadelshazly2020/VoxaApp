import { ref } from 'vue';

export interface AuthUser {
  id: number;
  username: string;
  email: string;
  displayName: string;
  isOnline: boolean;
}

export interface LoginResponse {
  token: string;
  user: AuthUser;
  message: string;
}

export class AuthService {
  private token = ref<string | null>(localStorage.getItem('auth_token'));
  private user = ref<AuthUser | null>(JSON.parse(localStorage.getItem('current_user') || 'null'));

  async register(username: string, email: string, password: string): Promise<{ success: boolean; message: string; userId?: number }> {
    try {
      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, email, password })
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      return { success: true, message: data.message, userId: data.userId };
    } catch (error) {
      console.error('Registration error:', error);
      return { success: false, message: 'Registration failed' };
    }
  }

  async login(email: string, password: string): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });

      const data = await response.json();

      if (!response.ok) {
        return { success: false, message: data.message };
      }

      // Store token and user
      this.token.value = data.token;
      this.user.value = data.user;
      localStorage.setItem('auth_token', data.token);
      localStorage.setItem('current_user', JSON.stringify(data.user));

      return { success: true, message: data.message };
    } catch (error) {
      console.error('Login error:', error);
      return { success: false, message: 'Login failed' };
    }
  }

  async logout(): Promise<void> {
    const user = this.user.value;

    if (user) {
      try {
        await fetch('/api/auth/logout', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${this.token.value}`
          },
          body: JSON.stringify({ userId: user.id })
        });
      } catch (error) {
        console.error('Logout error:', error);
      }
    }

    this.token.value = null;
    this.user.value = null;
    localStorage.removeItem('auth_token');
    localStorage.removeItem('current_user');
  }

  getToken(): string | null {
    return this.token.value;
  }

  getUser(): AuthUser | null {
    return this.user.value;
  }

  isAuthenticated(): boolean {
    return !!this.token.value && !!this.user.value;
  }

  isLoggedIn() {
    return this.token.value;
  }
}

export const authService = new AuthService();

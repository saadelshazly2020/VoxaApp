import { SignalRService } from './signalr.service';
import { getHubUrl } from '@/utils/config';
import { authService } from './auth.service';

/**
 * Shared SignalR connection manager.
 * Ensures only one WebSocket connection is open across the entire dashboard.
 * Components register/unregister their handlers; the connection stays alive
 * as long as at least one component is listening.
 */
class SignalRManager {
  private service: SignalRService | null = null;
  private refCount = 0;
  private registering = false;

  async acquire(): Promise<SignalRService> {
    this.refCount++;

    if (this.service && this.service.getConnectionState() === 'Connected') {
      return this.service;
    }

    if (!this.service) {
      this.service = new SignalRService(getHubUrl());

      this.service.on('reconnected', () => {
        this.registerCurrentUser();
      });
    }

    if (this.service.getConnectionState() !== 'Connected') {
      await this.service.start();
      await this.registerCurrentUser();
    }

    return this.service;
  }

  release(): void {
    this.refCount = Math.max(0, this.refCount - 1);
    // Keep connection alive; only tear down when page unloads or explicitly disposed
  }

  getService(): SignalRService | null {
    return this.service;
  }

  private async registerCurrentUser(): Promise<void> {
    if (this.registering || !this.service) return;
    this.registering = true;
    try {
      const user = authService.getUser();
      if (user) {
        await this.service.registerChatUser(user.id);
      }
    } catch (err) {
      console.warn('SignalRManager: failed to register user', err);
    } finally {
      this.registering = false;
    }
  }

  async dispose(): Promise<void> {
    this.refCount = 0;
    if (this.service) {
      await this.service.stop();
      this.service = null;
    }
  }
}

export const signalRManager = new SignalRManager();

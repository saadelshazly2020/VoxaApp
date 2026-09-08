import { authService } from './auth.service';

class PushNotificationService {
  private swRegistration: ServiceWorkerRegistration | null = null;
  private vapidPublicKey = '';

  async init(): Promise<void> {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
      console.log('Push notifications not supported');
      return;
    }

    try {
      this.swRegistration = await navigator.serviceWorker.register('/sw.js');
      console.log('Service Worker registered');

      // Fetch VAPID public key from server
      const response = await fetch('/api/pushnotification/vapid-public-key');
      const data = await response.json();

      if (data.enabled && data.publicKey) {
        this.vapidPublicKey = data.publicKey;
      }
    } catch (error) {
      console.error('Failed to register service worker:', error);
    }
  }

  async requestPermission(): Promise<NotificationPermission> {
    if (!('Notification' in window)) return 'denied';

    if (Notification.permission === 'granted') return 'granted';
    if (Notification.permission === 'denied') return 'denied';

    return await Notification.requestPermission();
  }

  async subscribe(): Promise<boolean> {
    if (!this.swRegistration || !this.vapidPublicKey) return false;

    const permission = await this.requestPermission();
    if (permission !== 'granted') return false;

    try {
      const subscription = await this.swRegistration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: this.urlBase64ToUint8Array(this.vapidPublicKey) as BufferSource
      });

      const sub = subscription.toJSON();
      if (!sub.endpoint) return false;

      // Send subscription to server
      const token = authService.getToken();
      await fetch('/api/pushnotification/subscribe', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {})
        },
        body: JSON.stringify({
          endpoint: sub.endpoint,
          p256dh: sub.keys?.p256dh || '',
          auth: sub.keys?.auth || ''
        })
      });

      console.log('Push subscription saved');
      return true;
    } catch (error) {
      console.error('Failed to subscribe to push:', error);
      return false;
    }
  }

  async unsubscribe(): Promise<void> {
    if (!this.swRegistration) return;

    try {
      const subscription = await this.swRegistration.pushManager.getSubscription();
      if (subscription) {
        const endpoint = subscription.endpoint;
        await subscription.unsubscribe();

        const token = authService.getToken();
        await fetch('/api/pushnotification/unsubscribe', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {})
          },
          body: JSON.stringify({ endpoint })
        });
      }
    } catch (error) {
      console.error('Failed to unsubscribe from push:', error);
    }
  }

  async isSubscribed(): Promise<boolean> {
    if (!this.swRegistration) return false;

    const subscription = await this.swRegistration.pushManager.getSubscription();
    return subscription !== null;
  }

  private urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; i++) {
      outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
  }
}

export const pushNotificationService = new PushNotificationService();

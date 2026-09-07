import * as signalR from '@microsoft/signalr';
import { User } from '../models/user.model';

export class SignalRService {
  private connection: signalR.HubConnection;
  private eventHandlers: Map<string, Function[]> = new Map();

  constructor(hubUrl: string) {
      const url = hubUrl || '/videocallhub';

      this.connection = new signalR.HubConnectionBuilder()
          .withUrl(url, {
              withCredentials: true,
              transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
          })
          .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
          .configureLogging(signalR.LogLevel.Information)
          .build();

    this.setupConnectionEvents();
  }

  private setupConnectionEvents(): void {
    this.connection.onclose(() => {
      this.emit('disconnected');
    });

    this.connection.onreconnecting(() => {
      this.emit('reconnecting');
    });

    this.connection.onreconnected(() => {
      this.emit('reconnected');
    });

    // Setup server event handlers
    this.connection.on('Registered', (userId: string) => {
      this.emit('Registered', userId);
    });

    this.connection.on('UserListUpdated', (users: User[]) => {
      this.emit('UserListUpdated', users);
    });

    this.connection.on('RoomCreated', (roomId: string) => {
      this.emit('RoomCreated', roomId);
    });

    this.connection.on('RoomJoined', (roomId: string, participants: string[]) => {
      this.emit('RoomJoined', roomId, participants);
    });

    this.connection.on('RoomLeft', (roomId: string) => {
      this.emit('RoomLeft', roomId);
    });

    this.connection.on('ReceiveOffer', (senderId: string, offer: string) => {
      this.emit('ReceiveOffer', senderId, offer);
    });

    this.connection.on('ReceiveAnswer', (senderId: string, answer: string) => {
      this.emit('ReceiveAnswer', senderId, answer);
    });

    this.connection.on('ReceiveIceCandidate', (senderId: string, candidate: any) => {
      this.emit('ReceiveIceCandidate', senderId, candidate);
    });

    this.connection.on('IncomingCall', (fromUserId: string) => {
      this.emit('IncomingCall', fromUserId);
    });

    this.connection.on('CallAccepted', (fromUserId: string) => {
      this.emit('CallAccepted', fromUserId);
    });

    this.connection.on('CallRejected', (fromUserId: string, reason: string) => {
      this.emit('CallRejected', fromUserId, reason);
    });

    this.connection.on('CallCancelled', (fromUserId: string) => {
      this.emit('CallCancelled', fromUserId);
    });

    this.connection.on('CallEnded', (fromUserId: string) => {
      this.emit('CallEnded', fromUserId);
    });

    this.connection.on('Error', (message: string) => {
      this.emit('Error', message);
    });
  }

  async start(): Promise<void> {
    try {
      await this.connection.start();
      this.emit('connected');
    } catch (error) {
      console.error('SignalR connection failed:', error);
      throw error;
    }
  }

  async stop(): Promise<void> {
    await this.connection.stop();
  }

  async registerUser(userId: string): Promise<void> {
    await this.connection.invoke('RegisterUser', userId);
  }

  async createRoom(roomId: string): Promise<void> {
    await this.connection.invoke('CreateRoom', roomId);
  }

  async joinRoom(roomId: string): Promise<void> {
    await this.connection.invoke('JoinRoom', roomId);
  }

  async leaveRoom(roomId: string): Promise<void> {
    await this.connection.invoke('LeaveRoom', roomId);
  }

  async sendOffer(receiverId: string, offer: string): Promise<void> {
    await this.connection.invoke('CallUser', receiverId, offer);
  }

  async sendAnswer(receiverId: string, answer: string): Promise<void> {
    await this.connection.invoke('AnswerCall', receiverId, answer);
  }

  async sendIceCandidate(receiverId: string, candidate: any): Promise<void> {
    await this.connection.invoke('SendIceCandidate', receiverId, candidate);
  }

  async acceptCall(callerId: string): Promise<void> {
    await this.connection.invoke('AcceptCall', callerId);
  }

  async rejectCall(callerId: string, reason?: string): Promise<void> {
    await this.connection.invoke('RejectCall', callerId, reason || 'Call rejected');
  }

  async cancelCall(targetUserId: string): Promise<void> {
    await this.connection.invoke('CancelCall', targetUserId);
  }

  async endCall(otherUserId: string): Promise<void> {
    await this.connection.invoke('EndCall', otherUserId);
  }

  on(event: string, handler: Function): void {
    if (!this.eventHandlers.has(event)) {
      this.eventHandlers.set(event, []);
    }
    this.eventHandlers.get(event)!.push(handler);
  }

  off(event: string, handler: Function): void {
    const handlers = this.eventHandlers.get(event);
    if (handlers) {
      const index = handlers.indexOf(handler);
      if (index > -1) {
        handlers.splice(index, 1);
      }
    }
  }

  private emit(event: string, ...args: any[]): void {
    const handlers = this.eventHandlers.get(event);
    if (handlers) {
      handlers.forEach(handler => handler(...args));
    }
  }

  getConnectionState(): signalR.HubConnectionState {
    return this.connection.state;
  }
}

import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export type SignalRCallback = (...args: any[]) => void;

export class SignalRService {
    private connection: HubConnection | null = null;
    private readonly hubUrl: string;
    private reconnectAttempts: number = 0;
    private readonly maxReconnectAttempts: number = 5;
    private callbacks: Map<string, SignalRCallback[]> = new Map();

    constructor(hubUrl: string = '/videocallhub') {
        this.hubUrl = hubUrl;
    }

    public async start(): Promise<void> {
        try {
            this.connection = new HubConnectionBuilder()
                .withUrl(this.hubUrl)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (retryContext) => {
                        if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
                            return null;
                        }
                        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                    }
                })
                .configureLogging(LogLevel.Information)
                .build();

            this.setupEventHandlers();
            await this.connection.start();
            console.log('SignalR connected successfully');
            this.reconnectAttempts = 0;
            this.triggerCallback('connected');
        } catch (error) {
            console.error('Error starting SignalR connection:', error);
            this.reconnectAttempts++;
            
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
                console.log(`Retrying connection in ${delay}ms...`);
                setTimeout(() => this.start(), delay);
            } else {
                this.triggerCallback('error', 'Failed to connect after multiple attempts');
            }
            throw error;
        }
    }

    public async stop(): Promise<void> {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
            console.log('SignalR connection stopped');
            this.triggerCallback('disconnected');
        }
    }

    public isConnected(): boolean {
        return this.connection?.state === 'Connected';
    }

    public getConnectionState(): string {
        return this.connection?.state || 'Disconnected';
    }

    private setupEventHandlers(): void {
        if (!this.connection) return;

        this.connection.onclose((error) => {
            console.log('SignalR connection closed', error);
            this.triggerCallback('disconnected');
        });

        this.connection.onreconnecting((error) => {
            console.log('SignalR reconnecting...', error);
            this.triggerCallback('reconnecting');
        });

        this.connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected', connectionId);
            this.triggerCallback('reconnected');
        });

        // Register server message handlers
        this.connection.on('Registered', (userId: string) => {
            this.triggerCallback('Registered', userId);
        });

        this.connection.on('UserListUpdated', (users: any[]) => {
            this.triggerCallback('UserListUpdated', users);
        });

        this.connection.on('ReceiveOffer', (fromUserId: string, offer: any) => {
            this.triggerCallback('ReceiveOffer', fromUserId, offer);
        });

        this.connection.on('ReceiveAnswer', (fromUserId: string, answer: any) => {
            this.triggerCallback('ReceiveAnswer', fromUserId, answer);
        });

        this.connection.on('ReceiveIceCandidate', (fromUserId: string, candidate: any) => {
            this.triggerCallback('ReceiveIceCandidate', fromUserId, candidate);
        });

        this.connection.on('UserJoinedRoom', (userId: string, roomId: string) => {
            this.triggerCallback('UserJoinedRoom', userId, roomId);
        });

        this.connection.on('UserLeftRoom', (userId: string, roomId: string) => {
            this.triggerCallback('UserLeftRoom', userId, roomId);
        });

        this.connection.on('RoomCreated', (roomId: string) => {
            this.triggerCallback('RoomCreated', roomId);
        });

        this.connection.on('RoomJoined', (roomId: string, participants: string[]) => {
            this.triggerCallback('RoomJoined', roomId, participants);
        });

        this.connection.on('RoomLeft', (roomId: string) => {
            this.triggerCallback('RoomLeft', roomId);
        });

        this.connection.on('UserDisconnected', (userId: string) => {
            this.triggerCallback('UserDisconnected', userId);
        });

        this.connection.on('Error', (message: string) => {
            this.triggerCallback('Error', message);
        });
    }

    public on(eventName: string, callback: SignalRCallback): void {
        if (!this.callbacks.has(eventName)) {
            this.callbacks.set(eventName, []);
        }
        this.callbacks.get(eventName)!.push(callback);
    }

    public off(eventName: string, callback?: SignalRCallback): void {
        if (!callback) {
            this.callbacks.delete(eventName);
            return;
        }

        const callbacks = this.callbacks.get(eventName);
        if (callbacks) {
            const index = callbacks.indexOf(callback);
            if (index > -1) {
                callbacks.splice(index, 1);
            }
        }
    }

    private triggerCallback(eventName: string, ...args: any[]): void {
        const callbacks = this.callbacks.get(eventName);
        if (callbacks) {
            callbacks.forEach(callback => {
                try {
                    callback(...args);
                } catch (error) {
                    console.error(`Error in ${eventName} callback:`, error);
                }
            });
        }
    }

    // SignalR Hub Methods
    public async registerUser(userId: string): Promise<void> {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        await this.connection.invoke('RegisterUser', userId);
    }

    public async callUser(targetUserId: string, offer: RTCSessionDescriptionInit): Promise<void> {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        await this.connection.invoke('CallUser', targetUserId, offer);
    }

    public async answerCall(callerUserId: string, answer: RTCSessionDescriptionInit): Promise<void> {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        await this.connection.invoke('AnswerCall', callerUserId, answer);
    }

    public async sendIceCandidate(targetUserId: string, candidate: RTCIceCandidateInit): Promise<void> {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        await this.connection.invoke('SendIceCandidate', targetUserId, candidate);
    }

    public async createRoom(roomId: string): Promise<string> {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        return await this.connection.invoke<string>('CreateRoom', roomId);
    }

    public async joinRoom(roomId: string): Promise<void> {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        await this.connection.invoke('JoinRoom', roomId);
    }

    public async leaveRoom(roomId: string): Promise<void> {
        if (!this.connection) {
            throw new Error('SignalR connection not established');
        }
        await this.connection.invoke('LeaveRoom', roomId);
    }
}

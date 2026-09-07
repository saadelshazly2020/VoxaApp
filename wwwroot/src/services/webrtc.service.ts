import { SignalRService } from './signalr.service';

export interface PeerConnectionInfo {
    connection: RTCPeerConnection;
    userId: string;
    stream?: MediaStream;
}

export class WebRTCService {
    private localStream: MediaStream | null = null;
    private peerConnections: Record<string, PeerConnectionInfo> = {};
    private readonly configuration: RTCConfiguration;
    private signalRService: SignalRService;
    private readonly localUserId: string;
    private callbacks: Map<string, Function[]> = new Map();

    constructor(signalRService: SignalRService, localUserId: string) {
        this.signalRService = signalRService;
        this.localUserId = localUserId;

        // Configure STUN servers for remote connections
        // Use public STUN servers and allow all ICE candidates (including host, srflx, and relay)
        this.configuration = {
            iceServers: [
                { urls: 'stun:stun.l.google.com:19302' },
                { urls: 'stun:stun1.l.google.com:19302' },
                { urls: 'stun:stun2.l.google.com:19302' }
            ]
            // Removed iceTransportPolicy to allow all connection types (host, srflx, relay)
            // This enables direct connections when possible, falling back to relay when needed
        };

        this.setupSignalRHandlers();
    }

    public on(eventName: string, callback: Function): void {
        if (!this.callbacks.has(eventName)) {
            this.callbacks.set(eventName, []);
        }
        this.callbacks.get(eventName)!.push(callback);
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

    private setupSignalRHandlers(): void {
        this.signalRService.on('ReceiveOffer', async (fromUserId: string, offer: RTCSessionDescriptionInit) => {
            console.log(`Received offer from ${fromUserId}`);
            await this.handleOffer(fromUserId, offer);
        });

        this.signalRService.on('ReceiveAnswer', async (fromUserId: string, answer: RTCSessionDescriptionInit) => {
            console.log(`Received answer from ${fromUserId}`);
            await this.handleAnswer(fromUserId, answer);
        });

        this.signalRService.on('ReceiveIceCandidate', async (fromUserId: string, candidate: RTCIceCandidateInit) => {
            console.log(`Received ICE candidate from ${fromUserId}`);
            await this.handleIceCandidate(fromUserId, candidate);
        });

        this.signalRService.on('UserJoinedRoom', async (userId: string, roomId: string) => {
            console.log(`User ${userId} joined room ${roomId}`);
            // Create offer for new user
            await this.createOfferForUser(userId);
        });

        this.signalRService.on('UserLeftRoom', (userId: string, roomId: string) => {
            console.log(`User ${userId} left room ${roomId}`);
            this.closeConnection(userId);
        });

        this.signalRService.on('UserDisconnected', (userId: string) => {
            console.log(`User ${userId} disconnected`);
            this.closeConnection(userId);
        });
    }

    public async initLocalMedia(constraints: MediaStreamConstraints = { video: true, audio: true }): Promise<MediaStream> {
        try {
            console.log('Requesting local media with constraints:', constraints);
            this.localStream = await navigator.mediaDevices.getUserMedia(constraints);
            console.log('Local media initialized successfully');
            this.triggerCallback('localStreamReady', this.localStream);
            return this.localStream;
        } catch (error) {
            console.error('Error accessing media devices:', error);
            
            // Handle common errors
            if (error instanceof DOMException) {
                if (error.name === 'NotFoundError') {
                    throw new Error('No camera or microphone found');
                } else if (error.name === 'NotAllowedError' || error.name === 'PermissionDeniedError') {
                    throw new Error('Permission denied. Please allow camera and microphone access');
                } else if (error.name === 'NotReadableError') {
                    throw new Error('Camera or microphone is already in use');
                }
            }
            
            throw new Error('Failed to access camera/microphone: ' + (error as Error).message);
        }
    }

    public getLocalStream(): MediaStream | null {
        return this.localStream;
    }

    public createPeerConnection(userId: string): RTCPeerConnection {
        if (this.peerConnections[userId]) {
            console.log(`Peer connection for ${userId} already exists`);
            return this.peerConnections[userId].connection;
        }

        console.log(`Creating peer connection for ${userId}`);
        const peerConnection = new RTCPeerConnection(this.configuration);

        // Add local stream tracks to peer connection
        if (this.localStream) {
            this.localStream.getTracks().forEach(track => {
                peerConnection.addTrack(track, this.localStream!);
                console.log(`Added ${track.kind} track to peer connection for ${userId}`);
            });
        }

        // Handle ICE candidates
        peerConnection.onicecandidate = async (event: RTCPeerConnectionIceEvent) => {
            if (event.candidate) {
                console.log(`Sending ICE candidate to ${userId}`);
                try {
                    await this.signalRService.sendIceCandidate(userId, event.candidate.toJSON());
                } catch (error) {
                    console.error(`Error sending ICE candidate to ${userId}:`, error);
                }
            }
        };

        // Handle incoming tracks
        peerConnection.ontrack = (event: RTCTrackEvent) => {
            console.log(`Received ${event.track.kind} track from ${userId}`);
            const remoteStream = event.streams[0];
            
            if (!this.peerConnections[userId].stream) {
                this.peerConnections[userId].stream = remoteStream;
                this.triggerCallback('remoteStream', userId, remoteStream);
            }
        };

        // Handle connection state changes
        peerConnection.onconnectionstatechange = () => {
            const state = peerConnection.connectionState;
            console.log(`Peer connection state with ${userId}: ${state}`);
            
            this.triggerCallback('connectionStateChange', userId, state);

            if (state === 'failed' || state === 'closed' || state === 'disconnected') {
                console.log(`Peer connection with ${userId} ${state}`);
                if (state === 'failed') {
                    // Attempt to restart ICE
                    console.log(`Attempting to restart ICE for ${userId}`);
                    peerConnection.restartIce();
                }
            } else if (state === 'connected') {
                console.log(`Successfully connected to ${userId}`);
            }
        };

        // Handle ICE connection state changes
        peerConnection.oniceconnectionstatechange = () => {
            console.log(`ICE connection state with ${userId}: ${peerConnection.iceConnectionState}`);
        };

        // Handle ICE gathering state changes
        peerConnection.onicegatheringstatechange = () => {
            console.log(`ICE gathering state with ${userId}: ${peerConnection.iceGatheringState}`);
        };

        // Handle signaling state changes
        peerConnection.onsignalingstatechange = () => {
            console.log(`Signaling state with ${userId}: ${peerConnection.signalingState}`);
        };

        this.peerConnections[userId] = {
            connection: peerConnection,
            userId: userId
        };

        return peerConnection;
    }

    public async createOfferForUser(userId: string): Promise<void> {
        try {
            const peerConnection = this.createPeerConnection(userId);
            
            console.log(`Creating offer for ${userId}`);
            const offer = await peerConnection.createOffer({
                offerToReceiveAudio: true,
                offerToReceiveVideo: true
            });

            await peerConnection.setLocalDescription(offer);
            console.log(`Sending offer to ${userId}`);
            
            await this.signalRService.callUser(userId, offer);
        } catch (error) {
            console.error(`Error creating offer for ${userId}:`, error);
            throw error;
        }
    }

    public async handleOffer(fromUserId: string, offer: RTCSessionDescriptionInit): Promise<void> {
        try {
            const peerConnection = this.createPeerConnection(fromUserId);

            console.log(`Setting remote description from ${fromUserId}`);
            await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));

            console.log(`Creating answer for ${fromUserId}`);
            const answer = await peerConnection.createAnswer();
            await peerConnection.setLocalDescription(answer);

            console.log(`Sending answer to ${fromUserId}`);
            await this.signalRService.answerCall(fromUserId, answer);
        } catch (error) {
            console.error(`Error handling offer from ${fromUserId}:`, error);
            throw error;
        }
    }

    public async handleAnswer(fromUserId: string, answer: RTCSessionDescriptionInit): Promise<void> {
        try {
            const peerInfo = this.peerConnections[fromUserId];
            if (!peerInfo) {
                console.error(`No peer connection found for ${fromUserId}`);
                return;
            }

            console.log(`Setting remote description (answer) from ${fromUserId}`);
            await peerInfo.connection.setRemoteDescription(new RTCSessionDescription(answer));
        } catch (error) {
            console.error(`Error handling answer from ${fromUserId}:`, error);
            throw error;
        }
    }

    public async handleIceCandidate(fromUserId: string, candidate: RTCIceCandidateInit): Promise<void> {
        try {
            const peerInfo = this.peerConnections[fromUserId];
            if (!peerInfo) {
                console.error(`No peer connection found for ${fromUserId}`);
                return;
            }

            if (candidate) {
                console.log(`Adding ICE candidate from ${fromUserId}`);
                await peerInfo.connection.addIceCandidate(new RTCIceCandidate(candidate));
            }
        } catch (error) {
            console.error(`Error handling ICE candidate from ${fromUserId}:`, error);
        }
    }

    public closeConnection(userId: string): void {
        const peerInfo = this.peerConnections[userId];
        if (peerInfo) {
            console.log(`Closing connection with ${userId}`);
            peerInfo.connection.close();
            delete this.peerConnections[userId];
            this.triggerCallback('connectionClosed', userId);
        }
    }

    public closeAllConnections(): void {
        console.log('Closing all peer connections');
        Object.keys(this.peerConnections).forEach(userId => {
            this.closeConnection(userId);
        });
    }

    public stopLocalStream(): void {
        if (this.localStream) {
            console.log('Stopping local stream');
            this.localStream.getTracks().forEach(track => {
                track.stop();
            });
            this.localStream = null;
        }
    }

    public getPeerConnection(userId: string): RTCPeerConnection | null {
        return this.peerConnections[userId]?.connection || null;
    }

    public getAllPeerConnections(): Record<string, PeerConnectionInfo> {
        return { ...this.peerConnections };
    }

    public getRemoteStream(userId: string): MediaStream | null {
        return this.peerConnections[userId]?.stream || null;
    }

    public async toggleAudio(enabled: boolean): Promise<void> {
        if (this.localStream) {
            this.localStream.getAudioTracks().forEach(track => {
                track.enabled = enabled;
            });
            console.log(`Audio ${enabled ? 'enabled' : 'disabled'}`);
        }
    }

    public async toggleVideo(enabled: boolean): Promise<void> {
        if (this.localStream) {
            this.localStream.getVideoTracks().forEach(track => {
                track.enabled = enabled;
            });
            console.log(`Video ${enabled ? 'enabled' : 'disabled'}`);
        }
    }

    public isAudioEnabled(): boolean {
        if (!this.localStream) return false;
        const audioTrack = this.localStream.getAudioTracks()[0];
        return audioTrack ? audioTrack.enabled : false;
    }

    public isVideoEnabled(): boolean {
        if (!this.localStream) return false;
        const videoTrack = this.localStream.getVideoTracks()[0];
        return videoTrack ? videoTrack.enabled : false;
    }

    public cleanup(): void {
        console.log('Cleaning up WebRTC service');
        this.closeAllConnections();
        this.stopLocalStream();
    }
}

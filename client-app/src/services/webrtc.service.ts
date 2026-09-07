import { SignalRService } from './signalr.service';
import { MediaConstraints } from '../models/webrtc.model';

export class WebRTCService {
private localStream: MediaStream | null = null;
private peerConnections: Map<string, RTCPeerConnection> = new Map();
private pendingOffers: Map<string, RTCSessionDescriptionInit> = new Map();
private activeCalls: Set<string> = new Set();
private eventHandlers: Map<string, Function[]> = new Map();
private signalRService: SignalRService;
private pendingCandidates: Map<string, RTCIceCandidateInit[]> = new Map();
private remoteDescriptionSet: Map<string, boolean> = new Map();

  private rtcConfiguration: RTCConfiguration = {
    iceServers: [
      {
        urls: [
          'stun:stun.l.google.com:19302',
          'stun:stun1.l.google.com:19302',
          'stun:stun2.l.google.com:19302',
          'stun:stun3.l.google.com:19302',
          'stun:stun4.l.google.com:19302'
        ]
      },
      {
        urls: [
          'stun:stun.services.mozilla.com:3478'
        ]
      }
    ]
  };

  constructor(signalRService: SignalRService, _userId: string) {
    this.signalRService = signalRService;
    this.setupSignalRHandlers();
  }

  private setupSignalRHandlers(): void {
    // Handle incoming call notification
    this.signalRService.on('IncomingCall', (fromUserId: string) => {
      console.log(`Incoming call from ${fromUserId}`);
      this.emit('incomingCall', fromUserId);
    });

    // Handle call accepted
    this.signalRService.on('CallAccepted', (fromUserId: string) => {
      console.log(`Call accepted by ${fromUserId}`);
      this.emit('callAccepted', fromUserId);
    });

    // Handle call rejected
    this.signalRService.on('CallRejected', (fromUserId: string, reason: string) => {
      console.log(`Call rejected by ${fromUserId}: ${reason}`);
      this.emit('callRejected', fromUserId, reason);
      this.closePeerConnection(fromUserId);
    });

    // Handle call cancelled
    this.signalRService.on('CallCancelled', (fromUserId: string) => {
      console.log(`Call cancelled by ${fromUserId}`);
      this.emit('callCancelled', fromUserId);
      this.pendingOffers.delete(fromUserId);
      this.activeCalls.delete(fromUserId);
      this.closePeerConnection(fromUserId);
    });

    this.signalRService.on('CallEnded', (fromUserId: string) => {
      console.log(`Call ended by ${fromUserId}`);
      this.emit('callEnded', fromUserId);
      this.activeCalls.delete(fromUserId);
      this.closePeerConnection(fromUserId);
    });

    this.signalRService.on('ReceiveOffer', async (senderId: string, offer: string) => {
      console.log(`Received offer from ${senderId}, storing for user acceptance`);
      const offerDesc: RTCSessionDescriptionInit = { type: 'offer', sdp: offer };
      this.pendingOffers.set(senderId, offerDesc);
    });

    this.signalRService.on('ReceiveAnswer', async (senderId: string, answer: string) => {
      await this.handleAnswer(senderId, answer);
    });

    this.signalRService.on('ReceiveIceCandidate', async (senderId: string, candidate: any) => {
      await this.handleIceCandidate(senderId, candidate);
    });

    this.signalRService.on('UserDisconnected', (userId: string) => {
      console.log(`User ${userId} disconnected`);
      this.closePeerConnection(userId);
      this.emit('userDisconnected', userId);
    });
  }

  async initLocalMedia(constraints: MediaConstraints): Promise<MediaStream> {
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia(constraints);
      this.emit('localStreamReady', this.localStream);
      return this.localStream;
    } catch (error) {
      console.error('Failed to get local media:', error);
      throw new Error('Could not access camera/microphone');
    }
  }

  async createOfferForUser(userId: string): Promise<void> {
    try {
      const peerConnection = this.createPeerConnection(userId);
      
      if (this.localStream) {
        this.localStream.getTracks().forEach(track => {
          try {
            peerConnection.addTrack(track, this.localStream!);
          } catch (e) {
            console.warn(`Track already added or error adding track:`, e);
          }
        });
      }

      const offer = await peerConnection.createOffer();
      await peerConnection.setLocalDescription(offer);
      console.log(`Offer created and set for ${userId}`);
      
      // Send offer with retry logic
      let retries = 0;
      const maxRetries = 3;
      
      while (retries < maxRetries) {
        try {
          await this.signalRService.sendOffer(userId, offer.sdp!);
          console.log(`Offer sent for ${userId}`);
          break;
        } catch (error) {
          retries++;
          if (retries >= maxRetries) {
            throw error;
          }
          console.warn(`Failed to send offer (attempt ${retries}/${maxRetries}), retrying...`);
          await new Promise(resolve => setTimeout(resolve, 100 * retries));
        }
      }
    } catch (error) {
      console.error(`Failed to create offer for ${userId}:`, error);
    }
  }

  async acceptCall(senderId: string): Promise<void> {
    const offer = this.pendingOffers.get(senderId);
    if (!offer) {
      console.error(`No pending offer from ${senderId}`);
      return;
    }

    await this.signalRService.acceptCall(senderId);
    await this.handleOffer(senderId, offer.sdp!);
    this.pendingOffers.delete(senderId);
    this.activeCalls.add(senderId);
  }

  async rejectCall(senderId: string, reason?: string): Promise<void> {
    await this.signalRService.rejectCall(senderId, reason);
    this.pendingOffers.delete(senderId);
    this.activeCalls.delete(senderId);
    this.closePeerConnection(senderId);
  }

  async cancelCall(userId: string): Promise<void> {
    await this.signalRService.cancelCall(userId);
    this.activeCalls.delete(userId);
    this.closePeerConnection(userId);
  }

  async endCall(userId: string): Promise<void> {
    await this.signalRService.endCall(userId);
    this.activeCalls.delete(userId);
    this.closePeerConnection(userId);
  }

  getActiveCalls(): string[] {
    return Array.from(this.activeCalls);
  }

  private async handleOffer(senderId: string, offerSdp: string): Promise<void> {
    try {
      // Ensure peer connection exists
      let peerConnection = this.peerConnections.get(senderId);
      if (!peerConnection) {
        peerConnection = this.createPeerConnection(senderId);
      }

      // Add local tracks
      if (this.localStream) {
        const pc = peerConnection; // Capture in local variable for closure
        this.localStream.getTracks().forEach(track => {
          try {
            pc.addTrack(track, this.localStream!);
          } catch (e) {
            console.warn(`Track already added or error adding track:`, e);
          }
        });
      }

      // Set remote description (offer)
      const offer: RTCSessionDescriptionInit = { type: 'offer', sdp: offerSdp };
      
      if (peerConnection.remoteDescription === null) {
        await peerConnection.setRemoteDescription(offer);
        this.remoteDescriptionSet.set(senderId, true);
        console.log(`Offer set for ${senderId}`);
        
        // Flush any pending candidates that arrived before offer
        await this.flushPendingCandidates(senderId);
      } else {
        console.warn(`Remote description already set for ${senderId}, skipping`);
        return;
      }

      // Create and send answer
      const answer = await peerConnection.createAnswer();
      await peerConnection.setLocalDescription(answer);
      console.log(`Answer created and set for ${senderId}`);
      
      // Send answer with retry logic
      let retries = 0;
      const maxRetries = 3;
      
      while (retries < maxRetries) {
        try {
          await this.signalRService.sendAnswer(senderId, answer.sdp!);
          console.log(`Answer sent for ${senderId}`);
          break;
        } catch (error) {
          retries++;
          if (retries >= maxRetries) {
            throw error;
          }
          console.warn(`Failed to send answer (attempt ${retries}/${maxRetries}), retrying...`);
          await new Promise(resolve => setTimeout(resolve, 100 * retries));
        }
      }
    } catch (error) {
      console.error(`Failed to handle offer from ${senderId}:`, error);
      this.closePeerConnection(senderId);
    }
  }

  private async handleAnswer(senderId: string, answerSdp: string): Promise<void> {
    const peerConnection = this.peerConnections.get(senderId);
    if (!peerConnection) {
      console.error(`No peer connection found for ${senderId} when handling answer`);
      return;
    }

    try {
      const answer: RTCSessionDescriptionInit = { type: 'answer', sdp: answerSdp };
      if (peerConnection.remoteDescription === null) {
        await peerConnection.setRemoteDescription(answer);
        this.remoteDescriptionSet.set(senderId, true);
        console.log(`Answer set for ${senderId}`);
        
        // Flush any pending candidates
        await this.flushPendingCandidates(senderId);
      } else {
        console.warn(`Remote description already set for ${senderId}, skipping`);
      }
    } catch (error) {
      console.error(`Failed to set remote description (answer) for ${senderId}:`, error);
    }
  }

  private async handleIceCandidate(senderId: string, candidateData: any): Promise<void> {
    const peerConnection = this.peerConnections.get(senderId);
    if (!peerConnection) {
      console.warn(`No peer connection found for ${senderId}, queueing candidate`);
      if (!this.pendingCandidates.has(senderId)) {
        this.pendingCandidates.set(senderId, []);
      }
      this.pendingCandidates.get(senderId)!.push(candidateData);
      return;
    }

    // Check if remote description is set
    const remoteDescSet = this.remoteDescriptionSet.get(senderId) || false;
    if (!remoteDescSet) {
      console.warn(`Remote description not set for ${senderId}, queueing candidate`);
      if (!this.pendingCandidates.has(senderId)) {
        this.pendingCandidates.set(senderId, []);
      }
      this.pendingCandidates.get(senderId)!.push(candidateData);
      return;
    }

    try {
      if (candidateData && candidateData.candidate) {
        await peerConnection.addIceCandidate(new RTCIceCandidate(candidateData));
        console.log(`Added ICE candidate for ${senderId}`);
      }
    } catch (error) {
      console.error(`Failed to add ICE candidate for ${senderId}:`, error);
    }
  }

  private async flushPendingCandidates(senderId: string): Promise<void> {
    const candidates = this.pendingCandidates.get(senderId);
    if (!candidates || candidates.length === 0) {
      return;
    }

    const peerConnection = this.peerConnections.get(senderId);
    if (!peerConnection) {
      return;
    }

    console.log(`Flushing ${candidates.length} pending candidates for ${senderId}`);
    
    for (const candidateData of candidates) {
      try {
        if (candidateData && candidateData.candidate) {
          await peerConnection.addIceCandidate(new RTCIceCandidate(candidateData));
          console.log(`Added queued ICE candidate for ${senderId}`);
        }
      } catch (error) {
        console.error(`Failed to add queued ICE candidate for ${senderId}:`, error);
      }
    }

    this.pendingCandidates.delete(senderId);
  }

  private createPeerConnection(userId: string): RTCPeerConnection {
    if (this.peerConnections.has(userId)) {
      return this.peerConnections.get(userId)!;
    }

    const peerConnection = new RTCPeerConnection(this.rtcConfiguration);

    peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        console.log('New ICE candidate:', event.candidate.candidate);
        this.signalRService.sendIceCandidate(userId, event.candidate.toJSON());
      } else {
        console.log('ICE candidate gathering complete');
      }
    };

    peerConnection.ontrack = (event) => {
      console.log('Received remote track from:', userId);
      this.emit('remoteStream', userId, event.streams[0]);
    };

    peerConnection.onconnectionstatechange = () => {
      console.log(`Connection state with ${userId}:`, peerConnection.connectionState);
      this.emit('connectionStateChange', userId, peerConnection.connectionState);

      if (peerConnection.connectionState === 'disconnected' || 
          peerConnection.connectionState === 'failed' ||
          peerConnection.connectionState === 'closed') {
        this.closePeerConnection(userId);
      }
    };

    peerConnection.oniceconnectionstatechange = () => {
      console.log(`ICE connection state with ${userId}:`, peerConnection.iceConnectionState);
      console.log(`Connection state with ${userId}:`, peerConnection.connectionState);
      console.log(`Signaling state with ${userId}:`, peerConnection.signalingState);
      
      // Log detailed connection info
      if (peerConnection.iceConnectionState === 'failed') {
        console.error(`ICE connection FAILED with ${userId} - checking logs above`);
        console.error('Current connection state:', peerConnection.connectionState);
        console.error('Signaling state:', peerConnection.signalingState);
      }
    };

    this.peerConnections.set(userId, peerConnection);
    return peerConnection;
  }

  closePeerConnection(userId: string): void {
    const peerConnection = this.peerConnections.get(userId);
    if (peerConnection) {
      peerConnection.close();
      this.peerConnections.delete(userId);
      this.pendingCandidates.delete(userId);
      this.remoteDescriptionSet.delete(userId);
      this.emit('connectionClosed', userId);
    }
  }

  closeAllConnections(): void {
    this.peerConnections.forEach((_, userId) => {
      this.closePeerConnection(userId);
    });
    this.pendingCandidates.clear();
    this.remoteDescriptionSet.clear();
  }

  async toggleAudio(enabled: boolean): Promise<void> {
    if (this.localStream) {
      this.localStream.getAudioTracks().forEach(track => {
        track.enabled = enabled;
      });
    }
  }

  async toggleVideo(enabled: boolean): Promise<void> {
    if (this.localStream) {
      this.localStream.getVideoTracks().forEach(track => {
        track.enabled = enabled;
      });
    }
  }

  cleanup(): void {
    this.closeAllConnections();
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = null;
    }
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

  getLocalStream(): MediaStream | null {
    return this.localStream;
  }

  getPeerConnections(): Map<string, RTCPeerConnection> {
    return this.peerConnections;
  }
}


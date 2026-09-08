import { reactive, ref } from 'vue';
import { WebRTCService } from '@/services/webrtc.service';
import { authService } from '@/services/auth.service';
import { signalRManager } from '@/services/signalr-manager';
import type { SignalRService } from '@/services/signalr.service';

export type CallState = 'idle' | 'ringing-out' | 'ringing-in' | 'in-call';

const RING_TIMEOUT_MS = 45000;

const state = ref<CallState>('idle');
const peerId = ref<string | null>(null);
const peerName = ref<string>('');
const localStream = ref<MediaStream | null>(null);
const remoteStreams = reactive<Record<string, MediaStream>>({});
const isAudioEnabled = ref(true);
const isVideoEnabled = ref(true);
const statusMessage = ref('');
const errorMessage = ref('');

let webRTCService: WebRTCService | null = null;
let ringTimeout: number | null = null;
let messageTimeout: number | null = null;

function showMessage(target: typeof statusMessage, message: string) {
  statusMessage.value = '';
  errorMessage.value = '';
  target.value = message;

  if (messageTimeout) window.clearTimeout(messageTimeout);
  messageTimeout = window.setTimeout(() => {
    target.value = '';
  }, 5000);
}

function showError(message: string) {
  showMessage(errorMessage, message);
}

function showStatus(message: string) {
  showMessage(statusMessage, message);
}

function clearRingTimeout() {
  if (ringTimeout) {
    window.clearTimeout(ringTimeout);
    ringTimeout = null;
  }
}

function releaseMedia() {
  webRTCService?.cleanup();
  localStream.value = null;
}

function reset() {
  clearRingTimeout();
  state.value = 'idle';
  peerId.value = null;
  peerName.value = '';
  isAudioEnabled.value = true;
  isVideoEnabled.value = true;
  Object.keys(remoteStreams).forEach(key => delete remoteStreams[key]);
}

async function ensureLocalMedia() {
  if (!webRTCService) throw new Error('Call service is not ready');

  if (!webRTCService.getLocalStream()) {
    await webRTCService.initLocalMedia({ video: true, audio: true });
  }
}

function attachWebRTCHandlers(service: WebRTCService) {
  service.on('localStreamReady', (stream: MediaStream) => {
    localStream.value = stream;
  });

  service.on('remoteStream', (userId: string, stream: MediaStream) => {
    remoteStreams[userId] = stream;
    state.value = 'in-call';
  });

  service.on('incomingCall', (fromUserId: string) => {
    if (state.value !== 'idle') return;
    peerId.value = fromUserId;
    state.value = 'ringing-in';
  });

  service.on('callAccepted', () => {
    clearRingTimeout();
    state.value = 'in-call';
  });

  service.on('callRejected', (_fromUserId: string, reason: string) => {
    showError(reason ? `Call declined (${reason})` : 'Call declined');
    reset();
  });

  service.on('callCancelled', () => {
    showStatus('Call cancelled');
    reset();
  });

  service.on('callEnded', () => {
    showStatus('Call ended');
    reset();
    releaseMedia();
  });

  service.on('connectionClosed', (userId: string) => {
    delete remoteStreams[userId];
    if (state.value === 'in-call' && Object.keys(remoteStreams).length === 0) {
      reset();
      releaseMedia();
    }
  });
}

async function connect() {
  if (webRTCService) return;

  const currentUser = authService.getUser();
  if (!currentUser) return;

  try {
    const signalRService: SignalRService = await signalRManager.acquire();

    signalRService.on('Error', (message: string) => {
      showError(String(message));
      if (state.value === 'ringing-out') {
        void cancelCall();
      }
    });

    webRTCService = new WebRTCService(signalRService, String(currentUser.id));
    attachWebRTCHandlers(webRTCService);
  } catch (error) {
    console.error('Failed to start the call service:', error);
    showError('Could not connect to the call service');
  }
}

async function startCall(userId: number, name?: string) {
  if (state.value !== 'idle') return;

  await connect();
  if (!webRTCService) return;

  try {
    await ensureLocalMedia();
  } catch (error) {
    showError((error as Error).message || 'Could not access camera/microphone');
    return;
  }

  peerId.value = String(userId);
  peerName.value = name ?? '';
  state.value = 'ringing-out';

  await webRTCService.createOfferForUser(String(userId));

  ringTimeout = window.setTimeout(() => {
    if (state.value === 'ringing-out') {
      showStatus('No answer');
      void cancelCall();
    }
  }, RING_TIMEOUT_MS);
}

async function acceptCall() {
  if (!webRTCService || !peerId.value) return;

  try {
    await ensureLocalMedia();
    await webRTCService.acceptCall(peerId.value);
    state.value = 'in-call';
  } catch (error) {
    console.error('Failed to accept the call:', error);
    showError('Could not access camera/microphone');
    await rejectCall();
  }
}

async function rejectCall() {
  const targetId = peerId.value;
  reset();

  if (!webRTCService || !targetId) return;

  try {
    await webRTCService.rejectCall(targetId, 'Call declined');
  } catch (error) {
    console.error('Failed to reject the call:', error);
  }
}

async function cancelCall() {
  const targetId = peerId.value;
  reset();

  if (!webRTCService || !targetId) return;

  try {
    await webRTCService.cancelCall(targetId);
  } catch (error) {
    console.error('Failed to cancel the call:', error);
  }
}

async function endCall() {
  const targetId = peerId.value;

  reset();
  releaseMedia();

  if (!webRTCService || !targetId) return;

  try {
    await webRTCService.endCall(targetId);
  } catch (error) {
    console.error('Failed to end the call:', error);
  }
}

async function toggleAudio() {
  if (!webRTCService) return;
  isAudioEnabled.value = !isAudioEnabled.value;
  await webRTCService.toggleAudio(isAudioEnabled.value);
}

async function toggleVideo() {
  if (!webRTCService) return;
  isVideoEnabled.value = !isVideoEnabled.value;
  await webRTCService.toggleVideo(isVideoEnabled.value);
}

async function dispose() {
  clearRingTimeout();
  if (messageTimeout) {
    window.clearTimeout(messageTimeout);
    messageTimeout = null;
  }

  if (webRTCService) {
    webRTCService.cleanup();
    webRTCService = null;
  }

  signalRManager.release();
  reset();
}

export function useCall() {
  return {
    state,
    peerId,
    peerName,
    localStream,
    remoteStreams,
    isAudioEnabled,
    isVideoEnabled,
    statusMessage,
    errorMessage,
    connect,
    startCall,
    acceptCall,
    rejectCall,
    cancelCall,
    endCall,
    toggleAudio,
    toggleVideo,
    dispose
  };
}

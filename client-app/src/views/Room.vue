<template>
  <div class="min-h-screen bg-gray-50">
    <AppHeader :isConnected="isConnected" :userId="userId" />

    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div class="card mb-6">
        <div class="flex items-center justify-between">
          <div>
            <h1 class="text-2xl font-bold text-gray-900">Room: {{ roomId }}</h1>
            <p class="text-gray-600">{{ participants.length }} participant(s)</p>
          </div>
          <router-link to="/" class="btn btn-outline">
            ? Back to Home
          </router-link>
        </div>
      </div>

      <!-- Join Room Card -->
      <div v-if="!isRegistered" class="max-w-md mx-auto">
        <div class="card">
          <h2 class="text-xl font-semibold text-gray-800 mb-4">Join Room</h2>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">
                Your User ID
              </label>
              <input
                v-model="userId"
                type="text"
                placeholder="Enter your name"
                class="input"
                @keyup.enter="joinRoomWithUserId"
              />
            </div>
            <button
              @click="joinRoomWithUserId"
              :disabled="!isConnected || !userId.trim()"
              class="btn btn-primary w-full"
            >
              Join Room
            </button>
          </div>
        </div>
      </div>

      <!-- Room Content -->
      <div v-else>
        <div class="card">
          <VideoGrid
            :localStream="localStream"
            :remoteStreams="remoteStreams"
            :localUserId="userId"
          />

          <!-- Video Controls -->
          <div v-if="localStream" class="mt-6 flex justify-center items-center space-x-3">
            <button
              @click="toggleAudio"
              :class="isAudioEnabled ? 'btn-secondary' : 'btn-danger'"
              class="btn flex items-center space-x-2"
            >
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path v-if="isAudioEnabled" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z" />
                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z" />
              </svg>
              <span>{{ isAudioEnabled ? 'Mute' : 'Unmute' }}</span>
            </button>

            <button
              @click="toggleVideo"
              :class="isVideoEnabled ? 'btn-secondary' : 'btn-danger'"
              class="btn flex items-center space-x-2"
            >
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path v-if="isVideoEnabled" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
              </svg>
              <span>{{ isVideoEnabled ? 'Stop Video' : 'Start Video' }}</span>
            </button>

            <button
              @click="leaveRoomAndGoHome"
              class="btn btn-danger flex items-center space-x-2"
            >
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
              <span>Leave Room</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { SignalRService } from '../services/signalr.service';
import { WebRTCService } from '../services/webrtc.service';
import AppHeader from '../components/AppHeader.vue';
import VideoGrid from '../components/VideoGrid.vue';

interface Props {
  roomId: string;
}

const props = defineProps<Props>();
const router = useRouter();

// State
const userId = ref<string>('');
const isRegistered = ref<boolean>(false);
const isConnected = ref<boolean>(false);
const localStream = ref<MediaStream | null>(null);
const remoteStreams = reactive<Record<string, MediaStream>>({});
const participants = ref<string[]>([]);
const isAudioEnabled = ref<boolean>(true);
const isVideoEnabled = ref<boolean>(true);

// Services
let signalRService: SignalRService;
let webRTCService: WebRTCService | null = null;

// Initialize
const initializeSignalR = async () => {
  try {
    signalRService = new SignalRService('/videocallhub');
    
    signalRService.on('connected', () => {
      isConnected.value = true;
    });

    signalRService.on('disconnected', () => {
      isConnected.value = false;
    });

    signalRService.on('Registered', () => {
      isRegistered.value = true;
    });

    signalRService.on('RoomJoined', async (_roomId: string, roomParticipants: string[]) => {
      participants.value = roomParticipants;

      for (const participantId of roomParticipants) {
        if (participantId !== userId.value) {
          await webRTCService?.createOfferForUser(participantId);
        }
      }
    });

    await signalRService.start();
  } catch (error) {
    console.error('Failed to initialize SignalR:', error);
  }
};

const joinRoomWithUserId = async () => {
  if (!userId.value.trim()) return;

  try {
    await signalRService.registerUser(userId.value);
    
    webRTCService = new WebRTCService(signalRService, userId.value);
    
    webRTCService.on('localStreamReady', (stream: MediaStream) => {
      localStream.value = stream;
    });

    webRTCService.on('remoteStream', (remoteUserId: string, stream: MediaStream) => {
      remoteStreams[remoteUserId] = stream;
    });

    webRTCService.on('connectionClosed', (remoteUserId: string) => {
      delete remoteStreams[remoteUserId];
    });

    await webRTCService.initLocalMedia({
      video: true,
      audio: true
    });

    await signalRService.joinRoom(props.roomId);
  } catch (error) {
    console.error('Failed to join room:', error);
  }
};

const toggleAudio = async () => {
  if (!webRTCService) return;
  isAudioEnabled.value = !isAudioEnabled.value;
  await webRTCService.toggleAudio(isAudioEnabled.value);
};

const toggleVideo = async () => {
  if (!webRTCService) return;
  isVideoEnabled.value = !isVideoEnabled.value;
  await webRTCService.toggleVideo(isVideoEnabled.value);
};

const leaveRoomAndGoHome = async () => {
  try {
    await signalRService.leaveRoom(props.roomId);
    webRTCService?.cleanup();
    await signalRService.stop();
    router.push('/');
  } catch (error) {
    console.error('Failed to leave room:', error);
    router.push('/');
  }
};

onMounted(async () => {
  await initializeSignalR();
});

onUnmounted(() => {
  webRTCService?.cleanup();
  signalRService?.stop();
});
</script>

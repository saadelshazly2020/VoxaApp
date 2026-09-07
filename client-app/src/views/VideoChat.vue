<template>
<div class="min-h-screen bg-gray-50">
  <AppHeader :isConnected="isConnected" :userId="userId" />

  <!-- Incoming Call Dialog -->
  <IncomingCallDialog
    :show="showIncomingCall"
    :callerName="incomingCallerId"
    @accept="acceptIncomingCall"
    @reject="rejectIncomingCall"
  />

  <!-- Outgoing Call Dialog -->
  <OutgoingCallDialog
    :show="showOutgoingCall"
    :calleeName="outgoingCalleeId"
    @cancel="cancelOutgoingCall"
  />

  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- Alert Messages -->
      <transition name="slide-down">
        <div v-if="errorMessage" class="mb-4 p-4 bg-red-50 border-l-4 border-red-500 rounded-lg">
          <div class="flex items-center">
            <svg class="h-5 w-5 text-red-500 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span class="text-red-800">{{ errorMessage }}</span>
          </div>
        </div>
      </transition>

      <transition name="slide-down">
        <div v-if="successMessage" class="mb-4 p-4 bg-green-50 border-l-4 border-green-500 rounded-lg">
          <div class="flex items-center">
            <svg class="h-5 w-5 text-green-500 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span class="text-green-800">{{ successMessage }}</span>
          </div>
        </div>
      </transition>

      <!-- Main Content -->
      <div class="grid grid-cols-1 lg:grid-cols-4 gap-6">
        <!-- Sidebar -->
        <div class="lg:col-span-1">
          <div class="card sticky top-4">
            <!-- Registration Form -->
            <div v-if="!isRegistered" class="space-y-4">
              <h2 class="text-xl font-semibold text-gray-800">Join Chat</h2>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">
                  Your User ID
                </label>
                <input
                  v-model="userId"
                  type="text"
                  placeholder="Enter your name"
                  class="input"
                  @keyup.enter="registerUser"
                />
              </div>
              <button
                @click="registerUser"
                :disabled="!isConnected || !userId.trim()"
                class="btn btn-primary w-full"
              >
                Register
              </button>
            </div>

            <!-- User Info & Room Controls -->
            <div v-else class="space-y-6">
              <!-- Current User Info -->
              <div class="bg-gradient-to-br from-primary-50 to-primary-100 rounded-lg p-4">
                <div class="text-sm font-medium text-primary-800 mb-1">Logged in as</div>
                <div class="text-lg font-bold text-primary-900">{{ userId }}</div>
              </div>

              <!-- Room Controls -->
              <div v-if="!currentRoomId" class="space-y-4">
                <h3 class="text-sm font-semibold text-gray-700 uppercase tracking-wide">
                  Room Options
                </h3>
                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-2">
                    Room ID
                  </label>
                  <input
                    v-model="roomInput"
                    type="text"
                    placeholder="Enter room ID"
                    class="input"
                  />
                </div>
                <div class="grid grid-cols-2 gap-2">
                  <button
                    @click="createRoom"
                    :disabled="!roomInput.trim()"
                    class="btn btn-success text-sm"
                  >
                    Create
                  </button>
                  <button
                    @click="joinRoom"
                    :disabled="!roomInput.trim()"
                    class="btn btn-primary text-sm"
                  >
                    Join
                  </button>
                </div>
              </div>

              <!-- Connected Users -->
              <div class="pt-4 border-t border-gray-200">
                <UserList
                  :users="users"
                  :currentUserId="userId"
                  @call-user="callUser"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- Main Video Area -->
        <div class="lg:col-span-3">
          <div class="card">
            <!-- Current Room Info -->
            <div v-if="currentRoomId" class="mb-6 p-4 bg-blue-50 rounded-lg border border-blue-200">
              <div class="flex items-center justify-between">
                <div>
                  <div class="text-sm font-medium text-blue-800">Current Room</div>
                  <div class="text-lg font-bold text-blue-900">{{ currentRoomId }}</div>
                </div>
                <div class="badge bg-blue-500 text-white">
                  Active
                </div>
              </div>
            </div>

            <!-- Video Grid -->
            <div v-if="isRegistered">
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
                  v-if="currentRoomId"
                  @click="leaveRoom"
                  class="btn btn-danger flex items-center space-x-2"
                >
                  <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                  </svg>
                  <span>Leave Room</span>
                </button>

                <button
                  v-else-if="isInCall"
                  @click="endCall"
                  class="btn btn-danger flex items-center space-x-2"
                >
                  <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 8l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2M5 3a2 2 0 00-2 2v1c0 8.284 6.716 15 15 15h1a2 2 0 002-2v-3.28a1 1 0 00-.684-.948l-4.493-1.498a1 1 0 00-1.21.502l-1.13 2.257a11.042 11.042 0 01-5.516-5.517l2.257-1.128a1 1 0 00.502-1.21L9.228 3.683A1 1 0 008.279 3H5z" />
                  </svg>
                  <span>End Call</span>
                </button>
              </div>
            </div>

            <!-- Welcome State -->
            <div v-else class="text-center py-20">
              <svg class="mx-auto h-20 w-20 text-gray-400 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
              </svg>
              <h2 class="text-2xl font-semibold text-gray-700 mb-2">Welcome to Video Chat</h2>
              <p class="text-gray-500">Please register with a user ID to start</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import { SignalRService } from '../services/signalr.service';
import { WebRTCService } from '../services/webrtc.service';
import { authService } from '../services/auth.service';
import { User } from '../models/user.model';
import { getHubUrl } from '../utils/config';
import AppHeader from '../components/AppHeader.vue';
import UserList from '../components/UserList.vue';
import VideoGrid from '../components/VideoGrid.vue';
import IncomingCallDialog from '../components/IncomingCallDialog.vue';
import OutgoingCallDialog from '../components/OutgoingCallDialog.vue';

// State
const userId = ref<string>('');
const isRegistered = ref<boolean>(false);
const isConnected = ref<boolean>(false);
const users = ref<User[]>([]);
const localStream = ref<MediaStream | null>(null);
const remoteStreams = reactive<Record<string, MediaStream>>({});
const currentRoomId = ref<string | null>(null);
const roomInput = ref<string>('');
const errorMessage = ref<string>('');
const successMessage = ref<string>('');
const isAudioEnabled = ref<boolean>(true);
const isVideoEnabled = ref<boolean>(true);
const isInCall = ref<boolean>(false);

// Call notification states
const showIncomingCall = ref<boolean>(false);
const incomingCallerId = ref<string>('');
const showOutgoingCall = ref<boolean>(false);
const outgoingCalleeId = ref<string>('');

// Friend to dial automatically (comes from ?callUser= on the chat tab)
const route = useRoute();
const pendingCallTarget = ref<string>('');

// Services
let signalRService: SignalRService;
let webRTCService: WebRTCService | null = null;

// Initialize SignalR
const initializeSignalR = async () => {
  try {
      signalRService = new SignalRService(getHubUrl());;
    
    signalRService.on('connected', () => {
      isConnected.value = true;
      clearMessages();
    });

    signalRService.on('disconnected', () => {
      isConnected.value = false;
      showError('Disconnected from server');
    });

    signalRService.on('reconnecting', () => {
      showError('Connection lost. Reconnecting...');
    });

    signalRService.on('reconnected', () => {
      showSuccess('Reconnected successfully');
      if (isRegistered.value && userId.value) {
        registerUser();
      }
    });

    signalRService.on('Registered', (registeredUserId: string) => {
      isRegistered.value = true;
      showSuccess(`Registered as ${registeredUserId}`);
    });

    signalRService.on('UserListUpdated', (updatedUsers: User[]) => {
      users.value = updatedUsers;
    });

    signalRService.on('RoomCreated', (roomId: string) => {
      currentRoomId.value = roomId;
      showSuccess(`Room ${roomId} created`);
    });

    signalRService.on('RoomJoined', async (roomId: string, participants: string[]) => {
      currentRoomId.value = roomId;
      showSuccess(`Joined room ${roomId}`);

      for (const participantId of participants) {
        if (participantId !== userId.value) {
          await webRTCService?.createOfferForUser(participantId);
        }
      }
    });

    signalRService.on('RoomLeft', (roomId: string) => {
      currentRoomId.value = null;
      showSuccess(`Left room ${roomId}`);
    });

    signalRService.on('Error', (message: string) => {
      showError(message);
    });

    await signalRService.start();
  } catch (error) {
    console.error('Failed to initialize SignalR:', error);
    showError('Failed to connect to server');
  }
};

// Register user
const registerUser = async () => {
  if (!userId.value.trim()) {
    showError('Please enter a user ID');
    return;
  }

  try {
    await signalRService.registerUser(userId.value);
    
    webRTCService = new WebRTCService(signalRService, userId.value);
    
    webRTCService.on('localStreamReady', (stream: MediaStream) => {
      localStream.value = stream;
    });

    webRTCService.on('remoteStream', (remoteUserId: string, stream: MediaStream) => {
      remoteStreams[remoteUserId] = stream;
      isInCall.value = true;
    });

    webRTCService.on('connectionClosed', (remoteUserId: string) => {
      delete remoteStreams[remoteUserId];
      if (Object.keys(remoteStreams).length === 0) {
        isInCall.value = false;
      }
    });

    webRTCService.on('connectionStateChange', (remoteUserId: string, state: string) => {
      if (state === 'failed') {
        showError(`Connection failed with ${remoteUserId}`);
      }
    });

    // Call notification events
    webRTCService.on('incomingCall', (fromUserId: string) => {
      incomingCallerId.value = fromUserId;
      showIncomingCall.value = true;
    });

    webRTCService.on('callAccepted', (fromUserId: string) => {
      showOutgoingCall.value = false;
      showSuccess(`${fromUserId} accepted your call`);
      isInCall.value = true;
    });

    webRTCService.on('callRejected', (fromUserId: string, reason: string) => {
      showOutgoingCall.value = false;
      showError(`${fromUserId} rejected your call: ${reason}`);
    });

    webRTCService.on('callCancelled', (fromUserId: string) => {
      showIncomingCall.value = false;
      showError(`${fromUserId} cancelled the call`);
    });

    webRTCService.on('callEnded', (fromUserId: string) => {
      delete remoteStreams[fromUserId];
      if (Object.keys(remoteStreams).length === 0) {
        isInCall.value = false;
      }
      showError(`${fromUserId} ended the call`);
    });

    await webRTCService.initLocalMedia({
      video: true,
      audio: true
    });

    // Dial automatically when coming from a chat "Call" button
    if (pendingCallTarget.value) {
      const targetId = pendingCallTarget.value;
      pendingCallTarget.value = '';
      await callUser(targetId);
    }

  } catch (error) {
    console.error('Registration failed:', error);
    showError((error as Error).message || 'Registration failed');
  }
};

// Call user
const callUser = async (targetUserId: string) => {
  if (!webRTCService) {
    showError('WebRTC not initialized');
    return;
  }

  try {
    outgoingCalleeId.value = targetUserId;
    showOutgoingCall.value = true;
    await webRTCService.createOfferForUser(targetUserId);
    // isInCall will be set when call is accepted, not when calling
  } catch (error) {
    console.error('Call failed:', error);
    showOutgoingCall.value = false;
    showError(`Failed to call ${targetUserId}`);
  }
};

// Accept incoming call
const acceptIncomingCall = async () => {
  if (!webRTCService || !incomingCallerId.value) return;

  try {
    const callerId = incomingCallerId.value;
    await webRTCService.acceptCall(callerId);
    showIncomingCall.value = false;
    showSuccess(`Accepted call from ${callerId}`);
    isInCall.value = true;
    incomingCallerId.value = '';
  } catch (error) {
    console.error('Failed to accept call:', error);
    showError('Failed to accept call');
  }
};

// Reject incoming call
const rejectIncomingCall = async () => {
  if (!webRTCService || !incomingCallerId.value) return;

  try {
    await webRTCService.rejectCall(incomingCallerId.value, 'User declined');
    showIncomingCall.value = false;
    showSuccess(`Rejected call from ${incomingCallerId.value}`);
    incomingCallerId.value = '';
  } catch (error) {
    console.error('Failed to reject call:', error);
  }
};

// Cancel outgoing call
const cancelOutgoingCall = async () => {
  if (!webRTCService || !outgoingCalleeId.value) return;

  try {
    await webRTCService.cancelCall(outgoingCalleeId.value);
    showOutgoingCall.value = false;
    showSuccess(`Cancelled call to ${outgoingCalleeId.value}`);
    outgoingCalleeId.value = '';
  } catch (error) {
    console.error('Failed to cancel call:', error);
  }
};

// Room operations
const createRoom = async () => {
  if (!roomInput.value.trim()) {
    showError('Please enter a room ID');
    return;
  }

  try {
    await signalRService.createRoom(roomInput.value);
    roomInput.value = '';
  } catch (error) {
    showError('Failed to create room');
  }
};

const joinRoom = async () => {
  if (!roomInput.value.trim()) {
    showError('Please enter a room ID');
    return;
  }

  try {
    await signalRService.joinRoom(roomInput.value);
    roomInput.value = '';
    isInCall.value = true;
  } catch (error) {
    showError('Failed to join room');
  }
};

const leaveRoom = async () => {
  if (!currentRoomId.value) return;

  try {
    await signalRService.leaveRoom(currentRoomId.value);
    webRTCService?.closeAllConnections();
    Object.keys(remoteStreams).forEach(key => {
      delete remoteStreams[key];
    });
    isInCall.value = false;
  } catch (error) {
    showError('Failed to leave room');
  }
};

const endCall = async () => {
  if (!webRTCService) return;

  // Get all active calls and notify each peer
  const activeCalls = webRTCService.getActiveCalls();
  
  for (const userId of activeCalls) {
    try {
      await webRTCService.endCall(userId);
    } catch (error) {
      console.error(`Failed to notify ${userId} about call end:`, error);
    }
  }

  // Clean up all connections
  webRTCService.closeAllConnections();
  
  // Clear remote streams
  Object.keys(remoteStreams).forEach(key => {
    delete remoteStreams[key];
  });
  
  isInCall.value = false;
  showSuccess('Call ended');
};

// Media controls
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

// Message utilities
const showError = (message: string) => {
  errorMessage.value = message;
  successMessage.value = '';
  setTimeout(() => {
    errorMessage.value = '';
  }, 5000);
};

const showSuccess = (message: string) => {
  successMessage.value = message;
  errorMessage.value = '';
  setTimeout(() => {
    successMessage.value = '';
  }, 3000);
};

const clearMessages = () => {
  errorMessage.value = '';
  successMessage.value = '';
};

// Lifecycle
onMounted(async () => {
  await initializeSignalR();

  // Signed-in users are registered automatically so calls can be routed to them
  const currentUser = authService.getUser();
  if (!currentUser) return;

  userId.value = String(currentUser.id);

  const callUserParam = route.query.callUser;
  if (callUserParam && String(callUserParam) !== userId.value) {
    pendingCallTarget.value = String(callUserParam);
  }

  await registerUser();
});

onUnmounted(() => {
  webRTCService?.cleanup();
  signalRService?.stop();
});
</script>

<style scoped>
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
}

.slide-down-enter-from {
  transform: translateY(-20px);
  opacity: 0;
}

.slide-down-leave-to {
  opacity: 0;
}
</style>

<template>
  <div>
    <!-- Incoming call -->
    <IncomingCallDialog
      :show="state === 'ringing-in'"
      :caller-name="peerDisplayName"
      @accept="acceptCall"
      @reject="rejectCall"
    />

    <!-- Outgoing call -->
    <OutgoingCallDialog
      :show="state === 'ringing-out'"
      :callee-name="peerDisplayName"
      @cancel="cancelCall"
    />

    <!-- Active call -->
    <div v-if="state === 'in-call'" class="fixed inset-0 z-[60] bg-gray-900 flex flex-col">
      <!-- Call header -->
      <div class="flex items-center justify-between px-4 py-3 bg-black/60 flex-shrink-0">
        <div class="min-w-0">
          <p class="text-white font-semibold truncate">{{ peerDisplayName }}</p>
          <p class="text-xs text-gray-300">{{ formattedDuration }}</p>
        </div>
        <button
          @click="endCall"
          class="p-2 rounded-lg bg-red-500/90 hover:bg-red-600 text-white transition flex-shrink-0"
          title="Minimise"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Videos -->
      <div class="flex-1 min-h-0 grid gap-3 p-3 grid-rows-2 md:grid-rows-1 md:grid-cols-2">
        <div class="relative min-h-0 bg-black rounded-xl overflow-hidden">
          <video ref="localVideoRef" autoplay muted playsinline class="w-full h-full object-cover"></video>
          <span class="absolute bottom-2 left-2 text-xs text-white bg-black/60 px-2 py-1 rounded">You</span>
        </div>

        <div
          v-for="(_stream, id) in remoteStreams"
          :key="id"
          class="relative min-h-0 bg-black rounded-xl overflow-hidden"
        >
          <video
            :ref="el => setRemoteVideoRef(id, el)"
            autoplay
            playsinline
            class="w-full h-full object-cover"
          ></video>
          <span class="absolute bottom-2 left-2 text-xs text-white bg-black/60 px-2 py-1 rounded">
            {{ peerDisplayName }}
          </span>
        </div>

        <!-- Waiting for the remote stream -->
        <div
          v-if="Object.keys(remoteStreams).length === 0"
          class="relative min-h-0 bg-gray-800 rounded-xl overflow-hidden flex items-center justify-center"
        >
          <div class="text-center px-4">
            <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-white mx-auto mb-3"></div>
            <p class="text-sm text-gray-300">Connecting to {{ peerDisplayName }}...</p>
          </div>
        </div>
      </div>

      <!-- Controls -->
      <div class="p-4 flex items-center justify-center gap-4 bg-black/60 flex-shrink-0 pb-[max(1rem,env(safe-area-inset-bottom))]">
        <button
          @click="toggleAudio"
          :class="isAudioEnabled ? 'bg-gray-700 hover:bg-gray-600' : 'bg-red-600 hover:bg-red-700'"
          class="w-14 h-14 rounded-full flex items-center justify-center text-white transition"
          :title="isAudioEnabled ? 'Mute' : 'Unmute'"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path v-if="isAudioEnabled" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z" />
            <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z" />
          </svg>
        </button>

        <button
          @click="toggleVideo"
          :class="isVideoEnabled ? 'bg-gray-700 hover:bg-gray-600' : 'bg-red-600 hover:bg-red-700'"
          class="w-14 h-14 rounded-full flex items-center justify-center text-white transition"
          :title="isVideoEnabled ? 'Stop video' : 'Start video'"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path v-if="isVideoEnabled" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
            <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
          </svg>
        </button>

        <button
          @click="endCall"
          class="w-14 h-14 rounded-full flex items-center justify-center text-white bg-red-600 hover:bg-red-700 transition"
          title="End call"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 8l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2M5 3a2 2 0 00-2 2v1c0 8.284 6.716 15 15 15h1a2 2 0 002-2v-3.28a1 1 0 00-.684-.948l-4.493-1.498a1 1 0 00-1.21.502l-1.13 2.257a11.042 11.042 0 01-5.516-5.517l2.257-1.128a1 1 0 00.502-1.21L9.228 3.683A1 1 0 008.279 3H5z" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Feedback messages -->
    <div
      v-if="errorMessage || statusMessage"
      class="fixed bottom-4 left-1/2 -translate-x-1/2 z-[70] px-4 w-full max-w-sm"
    >
      <div
        :class="[
          'px-4 py-3 rounded-lg shadow-lg text-sm font-medium text-center',
          errorMessage ? 'bg-red-600 text-white' : 'bg-gray-800 text-white'
        ]"
      >
        {{ errorMessage || statusMessage }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useCall } from '@/composables/useCall';
import { friendshipService } from '@/services/friendship.service';
import IncomingCallDialog from './IncomingCallDialog.vue';
import OutgoingCallDialog from './OutgoingCallDialog.vue';

const {
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
  acceptCall,
  rejectCall,
  cancelCall,
  endCall,
  toggleAudio,
  toggleVideo,
  dispose
} = useCall();

const friends = ref<{ id: number; username: string; displayName: string }[]>([]);
const callSeconds = ref(0);
let durationTimer: number | null = null;

const localVideoRef = ref<HTMLVideoElement | null>(null);
const remoteVideoRefs = new Map<string, HTMLVideoElement>();

const peerDisplayName = computed(() => {
  if (peerName.value) return peerName.value;

  const id = Number(peerId.value);
  const friend = friends.value.find(f => f.id === id);
  if (friend) return friend.displayName || friend.username;

  return peerId.value ? `User ${peerId.value}` : '';
});

const formattedDuration = computed(() => {
  const minutes = Math.floor(callSeconds.value / 60);
  const seconds = callSeconds.value % 60;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
});

const setRemoteVideoRef = (id: string, el: any) => {
  if (el) {
    remoteVideoRefs.set(id, el as HTMLVideoElement);
  }
};

watch(localStream, async (stream) => {
  await nextTick();
  if (localVideoRef.value && localVideoRef.value.srcObject !== stream) {
    localVideoRef.value.srcObject = stream ?? null;
  }
}, { immediate: true });

watch(remoteStreams, async (streams) => {
  await nextTick();
  Object.entries(streams).forEach(([id, stream]) => {
    const element = remoteVideoRefs.get(id);
    if (element && element.srcObject !== stream) {
      element.srcObject = stream;
    }
  });
}, { deep: true, immediate: true });

watch(state, (value) => {
  if (durationTimer) {
    window.clearInterval(durationTimer);
    durationTimer = null;
  }

  if (value === 'in-call') {
    callSeconds.value = 0;
    durationTimer = window.setInterval(() => {
      callSeconds.value++;
    }, 1000);
  }
});

onMounted(async () => {
  await connect();

  try {
    friends.value = await friendshipService.getFriends();
  } catch (error) {
    console.error('Failed to load friends for call names:', error);
  }
});

onUnmounted(async () => {
  if (durationTimer) window.clearInterval(durationTimer);
  await dispose();
});
</script>

<template>
  <div class="video-grid-container">
    <div class="grid-wrapper" :class="gridClass">
      <!-- Local Video -->
      <div v-if="localStream" class="video-item">
        <div class=" rounded-xl overflow-hidden shadow-xl bg-gray-900">
          <video
            ref="localVideoRef"
            autoplay
            muted
            playsinline
            class="w-full h-full object-cover"
          />
          <div class="absolute inset-0 flex flex-col justify-end bg-gradient-to-t from-black/80 via-transparent to-transparent pointer-events-none">
            <div class="p-3">
              <div class="flex items-center justify-between">
                <span class="text-white text-sm font-semibold">
                  {{ localUserId }} (You)
                </span>
                <span class="badge bg-green-500 text-white text-xs font-medium px-2 py-1 rounded">
                  Local
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Remote Videos -->
      <div
        v-for="(_stream, userId) in remoteStreams"
        :key="userId"
        class="video-item"
      >
        <div class=" rounded-xl overflow-hidden shadow-xl bg-gray-900">
          <video
            :ref="el => setRemoteVideoRef(userId, el)"
            autoplay
            playsinline
            class="w-full h-full object-cover"
          />
          <div class="absolute inset-0 flex flex-col justify-end bg-gradient-to-t from-black/80 via-transparent to-transparent pointer-events-none">
            <div class="p-3">
              <div class="flex items-center justify-between">
                <span class="text-white text-sm font-semibold">
                  {{ userId }}
                </span>
                <span class="badge bg-blue-500 text-white text-xs font-medium px-2 py-1 rounded">
                  Remote
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- No video state -->
    <div v-if="!localStream && Object.keys(remoteStreams).length === 0" class="no-video-state">
      <svg class="mx-auto h-16 w-16 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
      </svg>
      <p class="mt-4 text-gray-600">No active video streams</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted, nextTick } from 'vue';

interface Props {
  localStream: MediaStream | null;
  remoteStreams: Record<string, MediaStream>;
  localUserId: string;
}

const props = defineProps<Props>();

const localVideoRef = ref<HTMLVideoElement | null>(null);
const remoteVideoRefs = new Map<string, HTMLVideoElement>();

const gridClass = computed(() => {
  const totalVideos = (props.localStream ? 1 : 0) + Object.keys(props.remoteStreams).length;
  
  if (totalVideos === 1) return 'grid-cols-1';
  if (totalVideos === 2) return 'grid-cols-1 md:grid-cols-2';
  if (totalVideos <= 4) return 'grid-cols-1 md:grid-cols-2';
  if (totalVideos <= 6) return 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3';
  return 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4';
});

const setRemoteVideoRef = (userId: string, el: any) => {
  if (el) {
    remoteVideoRefs.set(userId, el as HTMLVideoElement);
  }
};

watch(() => props.localStream, async (newStream) => {
  await nextTick();
  if (newStream && localVideoRef.value) {
    localVideoRef.value.srcObject = newStream;
  }
}, { immediate: true });

watch(() => props.remoteStreams, async (newStreams) => {
  await nextTick();
  Object.entries(newStreams).forEach(([userId, stream]) => {
    const videoElement = remoteVideoRefs.get(userId);
    if (videoElement && videoElement.srcObject !== stream) {
      videoElement.srcObject = stream;
    }
  });
}, { deep: true, immediate: true });

onMounted(async () => {
  await nextTick();
  if (props.localStream && localVideoRef.value) {
    localVideoRef.value.srcObject = props.localStream;
  }
});
</script>

<style scoped>
.video-grid-container {
  @apply w-full h-full min-h-[400px];
}

.grid-wrapper {
  @apply grid gap-4 p-4;
}

.video-item {
  @apply aspect-video bg-gray-900 rounded-xl overflow-hidden;
  min-height: 200px;
  position: relative;
}

.video-item video {
  @apply w-full h-full object-cover;
  display: block;
}

.no-video-state {
  @apply flex flex-col items-center justify-center h-96 bg-white rounded-xl shadow-inner;
}

video {
  @apply w-full h-full;
}

.badge {
  @apply inline-block whitespace-nowrap;
  z-index: 10;
}
</style>

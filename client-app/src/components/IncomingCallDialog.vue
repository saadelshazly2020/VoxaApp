<template>
  <div v-if="show" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
    <div class="bg-white rounded-lg shadow-2xl p-8 max-w-md w-full mx-4 animate-bounce-in">
      <div class="text-center">
        <!-- Phone Icon with Animation -->
        <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-green-100 mb-4 animate-pulse">
          <svg class="w-10 h-10 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
          </svg>
        </div>

        <!-- Caller Info -->
        <h2 class="text-2xl font-bold text-gray-800 mb-2">Incoming Call</h2>
        <p class="text-lg text-gray-600 mb-6">
          <span class="font-semibold text-gray-800">{{ callerName }}</span> is calling you
        </p>

        <!-- Ringing Animation -->
        <div class="flex justify-center items-center space-x-2 mb-8">
          <div class="w-3 h-3 bg-green-500 rounded-full animate-ping"></div>
          <div class="w-3 h-3 bg-green-500 rounded-full animate-ping" style="animation-delay: 0.2s"></div>
          <div class="w-3 h-3 bg-green-500 rounded-full animate-ping" style="animation-delay: 0.4s"></div>
        </div>

        <!-- Action Buttons -->
        <div class="flex space-x-4">
          <button
            @click="handleReject"
            class="flex-1 bg-red-500 hover:bg-red-600 text-white font-semibold py-4 px-6 rounded-lg transition duration-200 flex items-center justify-center space-x-2 shadow-lg hover:shadow-xl transform hover:scale-105"
          >
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
            <span>Decline</span>
          </button>
          
          <button
            @click="handleAccept"
            class="flex-1 bg-green-500 hover:bg-green-600 text-white font-semibold py-4 px-6 rounded-lg transition duration-200 flex items-center justify-center space-x-2 shadow-lg hover:shadow-xl transform hover:scale-105"
          >
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
            </svg>
            <span>Accept</span>
          </button>
        </div>

        <!-- Timer -->
        <div class="mt-4 text-sm text-gray-500">
          Ringing for {{ elapsedTime }}s
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';

interface Props {
  show: boolean;
  callerName: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  accept: [];
  reject: [];
}>();

const elapsedTime = ref(0);
let timer: number | null = null;
let audioElement: HTMLAudioElement | null = null;

const handleAccept = () => {
  stopRinging();
  emit('accept');
};

const handleReject = () => {
  stopRinging();
  emit('reject');
};

const startRinging = () => {
  // Start timer
  elapsedTime.value = 0;
  timer = window.setInterval(() => {
    elapsedTime.value++;
  }, 1000);

  // Play ringing sound using Web Audio API
  try {
    // Create an audio context
    const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    
    // Create oscillator for ringtone
    const oscillator = audioContext.createOscillator();
    const gainNode = audioContext.createGain();
    
    oscillator.connect(gainNode);
    gainNode.connect(audioContext.destination);
    
    // Set ringtone frequency (typical phone ring is around 440Hz)
    oscillator.frequency.value = 440;
    oscillator.type = 'sine';
    
    // Set volume
    gainNode.gain.value = 0.3;
    
    // Start the oscillator
    oscillator.start();
    
    // Create a repeating ring pattern
    const ringPattern = setInterval(() => {
      gainNode.gain.value = 0.3;
      setTimeout(() => {
        gainNode.gain.value = 0;
      }, 300);
    }, 1000);
    
    // Store references for cleanup
    (audioElement as any) = { oscillator, gainNode, audioContext, ringPattern };
    
  } catch (err) {
    console.log('Could not play ringtone:', err);
  }
};

const stopRinging = () => {
  if (timer !== null) {
    clearInterval(timer);
    timer = null;
  }
  if (audioElement) {
    try {
      const audio = audioElement as any;
      if (audio.ringPattern) {
        clearInterval(audio.ringPattern);
      }
      if (audio.oscillator) {
        audio.oscillator.stop();
        audio.oscillator.disconnect();
      }
      if (audio.gainNode) {
        audio.gainNode.disconnect();
      }
      if (audio.audioContext) {
        audio.audioContext.close();
      }
    } catch (err) {
      console.log('Error stopping ringtone:', err);
    }
    audioElement = null;
  }
  elapsedTime.value = 0;
};

watch(() => props.show, (newValue) => {
  if (newValue) {
    startRinging();
  } else {
    stopRinging();
  }
});

onMounted(() => {
  if (props.show) {
    startRinging();
  }
});

onUnmounted(() => {
  stopRinging();
});
</script>

<style scoped>
@keyframes bounce-in {
  0% {
    transform: scale(0.3);
    opacity: 0;
  }
  50% {
    transform: scale(1.05);
  }
  70% {
    transform: scale(0.9);
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
}

.animate-bounce-in {
  animation: bounce-in 0.5s ease-out;
}
</style>

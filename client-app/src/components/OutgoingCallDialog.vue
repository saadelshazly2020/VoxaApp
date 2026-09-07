<template>
  <div v-if="show" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
    <div class="bg-white rounded-lg shadow-2xl p-8 max-w-md w-full mx-4">
      <div class="text-center">
        <!-- Phone Icon with Animation -->
        <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-blue-100 mb-4">
          <svg class="w-10 h-10 text-blue-600 animate-pulse" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
          </svg>
        </div>

        <!-- Status -->
        <h2 class="text-2xl font-bold text-gray-800 mb-2">Calling...</h2>
        <p class="text-lg text-gray-600 mb-6">
          Calling <span class="font-semibold text-gray-800">{{ calleeName }}</span>
        </p>

        <!-- Loading Animation -->
        <div class="flex justify-center items-center space-x-2 mb-8">
          <div class="w-3 h-3 bg-blue-500 rounded-full animate-bounce"></div>
          <div class="w-3 h-3 bg-blue-500 rounded-full animate-bounce" style="animation-delay: 0.1s"></div>
          <div class="w-3 h-3 bg-blue-500 rounded-full animate-bounce" style="animation-delay: 0.2s"></div>
        </div>

        <!-- Status Message -->
        <p class="text-sm text-gray-500 mb-6">Waiting for {{ calleeName }} to answer</p>

        <!-- Cancel Button -->
        <button
          @click="handleCancel"
          class="bg-red-500 hover:bg-red-600 text-white font-semibold py-3 px-8 rounded-lg transition duration-200 flex items-center justify-center space-x-2 mx-auto shadow-lg hover:shadow-xl transform hover:scale-105"
        >
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
          <span>Cancel Call</span>
        </button>

        <!-- Timer -->
        <div class="mt-4 text-sm text-gray-500">
          {{ elapsedTime }}s
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';

interface Props {
  show: boolean;
  calleeName: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  cancel: [];
}>();

const elapsedTime = ref(0);
let timer: number | null = null;

const handleCancel = () => {
  stopTimer();
  emit('cancel');
};

const startTimer = () => {
  elapsedTime.value = 0;
  timer = window.setInterval(() => {
    elapsedTime.value++;
  }, 1000);
};

const stopTimer = () => {
  if (timer !== null) {
    clearInterval(timer);
    timer = null;
  }
  elapsedTime.value = 0;
};

watch(() => props.show, (newValue) => {
  if (newValue) {
    startTimer();
  } else {
    stopTimer();
  }
});

onMounted(() => {
  if (props.show) {
    startTimer();
  }
});

onUnmounted(() => {
  stopTimer();
});
</script>

<template>
  <nav class="bg-white shadow-sm border-b border-gray-200">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex justify-between items-center h-16">
        <div class="flex items-center space-x-3">
          <router-link to="/" class="flex items-center space-x-2">
            <svg class="h-8 w-8 text-primary-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
            </svg>
            <span class="text-xl font-bold text-gray-900">VideoChat</span>
          </router-link>
        </div>

        <div class="flex items-center space-x-4">
          <div v-if="isConnected" class="flex items-center space-x-2">
            <span class="relative flex h-3 w-3">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
              <span class="relative inline-flex rounded-full h-3 w-3 bg-green-500"></span>
            </span>
            <span class="text-sm text-gray-600">Connected</span>
          </div>
          <div v-else class="flex items-center space-x-2">
            <span class="relative flex h-3 w-3 bg-red-500 rounded-full"></span>
            <span class="text-sm text-gray-600">Disconnected</span>
          </div>

          <div v-if="userId" class="flex items-center space-x-2">
            <div class="h-8 w-8 rounded-full bg-primary-100 flex items-center justify-center">
              <span class="text-primary-700 font-semibold text-sm">
                {{ userId.substring(0, 2).toUpperCase() }}
              </span>
            </div>
            <span class="text-sm font-medium text-gray-700">{{ userId }}</span>
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
interface Props {
  isConnected: boolean;
  userId?: string;
}

defineProps<Props>();
</script>

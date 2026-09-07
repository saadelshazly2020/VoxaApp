<template>
  <div class="min-h-screen flex items-center justify-center p-4">
    <div class="max-w-4xl w-full">
      <div class="text-center mb-12">
        <div class="flex justify-center mb-6">
          <div class="h-20 w-20 bg-primary-600 rounded-2xl flex items-center justify-center shadow-2xl">
            <svg class="h-12 w-12 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
            </svg>
          </div>
        </div>
        <h1 class="text-5xl font-bold text-gray-900 mb-4">
          Video Chat Application
        </h1>
        <p class="text-xl text-gray-600">
          Connect face-to-face with anyone, anywhere
        </p>
      </div>

      <div class="grid md:grid-cols-2 gap-6">
        <!-- Start Chat Card -->
        <div class="card hover:shadow-2xl transition-shadow">
          <div class="text-center">
            <div class="h-16 w-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg class="h-8 w-8 text-primary-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8h2a2 2 0 012 2v6a2 2 0 01-2 2h-2v4l-4-4H9a1.994 1.994 0 01-1.414-.586m0 0L11 14h4a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2v4l.586-.586z" />
              </svg>
            </div>
            <h3 class="text-2xl font-semibold text-gray-900 mb-2">
              Start Video Chat
            </h3>
            <p class="text-gray-600 mb-6">
              Join the main video chat room and connect with others
            </p>
            <router-link to="/video-chat" class="btn btn-primary w-full">
              Get Started
            </router-link>
          </div>
        </div>

        <!-- Join Room Card -->
        <div class="card hover:shadow-2xl transition-shadow">
          <div class="text-center">
            <div class="h-16 w-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg class="h-8 w-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <h3 class="text-2xl font-semibold text-gray-900 mb-2">
              Join a Room
            </h3>
            <p class="text-gray-600 mb-6">
              Enter a room ID to join a specific video room
            </p>
            <div class="space-y-3">
              <input
                v-model="roomIdInput"
                type="text"
                placeholder="Enter Room ID"
                class="input"
                @keyup.enter="joinSpecificRoom"
              />
              <button
                @click="joinSpecificRoom"
                :disabled="!roomIdInput.trim()"
                class="btn btn-success w-full"
              >
                Join Room
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Features Section -->
      <div class="mt-16 grid md:grid-cols-3 gap-8">
        <div class="text-center">
          <div class="h-12 w-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-3">
            <svg class="h-6 w-6 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
          </div>
          <h4 class="font-semibold text-gray-900 mb-1">Secure & Private</h4>
          <p class="text-sm text-gray-600">End-to-end encrypted connections</p>
        </div>

        <div class="text-center">
          <div class="h-12 w-12 bg-green-100 rounded-lg flex items-center justify-center mx-auto mb-3">
            <svg class="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </div>
          <h4 class="font-semibold text-gray-900 mb-1">Lightning Fast</h4>
          <p class="text-sm text-gray-600">Real-time video with low latency</p>
        </div>

        <div class="text-center">
          <div class="h-12 w-12 bg-purple-100 rounded-lg flex items-center justify-center mx-auto mb-3">
            <svg class="h-6 w-6 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
          </div>
          <h4 class="font-semibold text-gray-900 mb-1">Multi-User Support</h4>
          <p class="text-sm text-gray-600">Connect with multiple participants</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const roomIdInput = ref('');

const joinSpecificRoom = () => {
  if (roomIdInput.value.trim()) {
    router.push(`/room/${roomIdInput.value.trim()}`);
  }
};
</script>

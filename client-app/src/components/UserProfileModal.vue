<template>
  <div v-if="show && profile" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="$emit('close')">
    <div class="absolute inset-0 bg-black/50" />
    <div class="relative bg-white rounded-2xl shadow-2xl w-full max-w-sm overflow-hidden">
      <!-- Header with gradient -->
      <div class="h-24 bg-gradient-to-r from-blue-500 to-purple-600 relative">
        <button @click="$emit('close')" class="absolute top-3 right-3 p-1.5 bg-white/20 hover:bg-white/30 rounded-lg transition">
          <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Avatar -->
      <div class="flex justify-center -mt-12">
        <div class="w-20 h-20 rounded-full border-4 border-white overflow-hidden bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white text-2xl font-bold shadow-lg">
          <img v-if="profile.profilePictureUrl" :src="profile.profilePictureUrl" class="w-full h-full object-cover" />
          <span v-else>{{ profile.displayName?.[0]?.toUpperCase() || profile.username[0].toUpperCase() }}</span>
        </div>
      </div>

      <!-- Info -->
      <div class="text-center px-6 pt-3 pb-6">
        <h3 class="text-xl font-bold text-gray-800">{{ profile.displayName || profile.username }}</h3>
        <p class="text-sm text-gray-500">@{{ profile.username }}</p>

        <p v-if="profile.bio" class="mt-3 text-sm text-gray-600 leading-relaxed">{{ profile.bio }}</p>

        <!-- Stats -->
        <div class="flex justify-center gap-6 mt-4">
          <div class="text-center">
            <p class="text-lg font-bold text-gray-800">{{ profile.postCount || 0 }}</p>
            <p class="text-xs text-gray-400">Posts</p>
          </div>
          <div class="text-center">
            <div class="flex items-center gap-1 justify-center">
              <span :class="profile.isOnline ? 'bg-green-400' : 'bg-gray-300'" class="w-2 h-2 rounded-full" />
              <p class="text-sm font-semibold text-gray-700">{{ profile.isOnline ? 'Online' : 'Offline' }}</p>
            </div>
            <p class="text-xs text-gray-400">Status</p>
          </div>
        </div>

        <!-- Member since -->
        <p v-if="profile.createdAt" class="text-xs text-gray-400 mt-4">
          Member since {{ new Date(profile.createdAt).toLocaleDateString() }}
        </p>

        <!-- Actions -->
        <div class="flex gap-3 mt-5">
          <button
            v-if="profile.isFriend"
            @click="openChat"
            class="flex-1 py-2.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold rounded-xl hover:shadow-lg transition text-sm"
          >
            Message
          </button>
          <button
            v-else
            @click="sendFriendRequest"
            :disabled="requestSent"
            class="flex-1 py-2.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold rounded-xl hover:shadow-lg transition text-sm disabled:opacity-50"
          >
            {{ requestSent ? 'Request Sent' : 'Add Friend' }}
          </button>
          <button
            @click="$emit('call', profile.id, profile.displayName || profile.username)"
            :disabled="!profile.isOnline"
            class="py-2.5 px-4 border border-gray-200 text-gray-700 font-semibold rounded-xl hover:bg-gray-50 transition text-sm disabled:opacity-50"
            title="Video call"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { profileService, type ProfileData } from '@/services/profile.service';
import { friendshipService } from '@/services/friendship.service';

const props = defineProps<{ show: boolean; userId: number }>();
const emit = defineEmits<{
  close: [];
  openChat: [userId: number, userName: string, isOnline: boolean];
  call: [userId: number, name: string];
}>();

const profile = ref<ProfileData | null>(null);
const requestSent = ref(false);

watch(() => props.show, async (visible) => {
  if (visible && props.userId) {
    profile.value = await profileService.getUserProfile(props.userId);
    requestSent.value = false;
  }
});

const sendFriendRequest = async () => {
  if (!profile.value) return;
  const result = await friendshipService.sendFriendRequest(profile.value.id);
  if (result.success) {
    requestSent.value = true;
  }
};

const openChat = () => {
  if (!profile.value) return;
  emit('openChat', profile.value.id, profile.value.displayName || profile.value.username, profile.value.isOnline ?? false);
  emit('close');
};
</script>

<template>
  <div class="bg-white rounded-xl shadow-lg p-6">
    <div class="flex items-center justify-between mb-6">
      <h2 class="text-2xl font-bold text-gray-800">Friends</h2>
      <button
        @click="refreshFriends"
        class="p-2 hover:bg-gray-100 rounded-lg transition"
        title="Refresh friends list"
      >
        <svg class="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
        </svg>
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex justify-center items-center py-8">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="friends.length === 0" class="text-center py-8">
      <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z" />
      </svg>
      <p class="text-gray-500 mt-4">No friends yet</p>
      <p class="text-gray-400 text-sm">Search for users to add friends</p>
    </div>

    <!-- Friends List -->
    <div v-else class="space-y-3">
      <div
        v-for="friend in friends"
        :key="friend.id"
        class="flex items-center justify-between p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition"
      >
        <div class="flex items-center gap-3 flex-1">
          <!-- Avatar -->
          <button @click="emit('viewProfile', friend.id)" class="relative flex-shrink-0">
            <div class="w-10 h-10 rounded-full overflow-hidden bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white font-bold">
              <img v-if="(friend as any).profilePictureUrl" :src="(friend as any).profilePictureUrl" class="w-full h-full object-cover" />
              <span v-else>{{ friend.username[0].toUpperCase() }}</span>
            </div>
            <!-- Online Status -->
            <span
              :class="[
                'absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-white',
                friend.isOnline ? 'bg-green-500' : 'bg-gray-400'
              ]"
              :title="friend.isOnline ? 'Online' : 'Offline'"
            />
          </button>

          <!-- User Info -->
          <button @click="emit('viewProfile', friend.id)" class="flex-1 text-left">
            <p class="font-semibold text-gray-800 hover:text-blue-600 transition">{{ friend.displayName || friend.username }}</p>
            <p class="text-xs text-gray-500">
              {{ friend.isOnline ? 'Online' : `Last seen ${formatTime(friend.lastSeen)}` }}
            </p>
          </button>
        </div>

        <!-- Actions -->
        <div class="flex gap-2">
          <!-- Chat Button -->
          <button
            @click="chatFriend(friend.id, friend.displayName || friend.username, friend.isOnline)"
            class="p-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg transition"
            title="Send message"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
          </button>

          <!-- Call Button (only if online) -->
          <button
            v-if="friend.isOnline"
            @click="callFriend(friend.id, friend.displayName || friend.username)"
            class="p-2 bg-green-500 hover:bg-green-600 text-white rounded-lg transition"
            title="Call friend"
          >
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path d="M2 3a1 1 0 011-1h2.153a1 1 0 01.986.836l.74 4.435a1 1 0 01-.54 1.06l-1.548.773c.032.088.064.165.068.236.3.88.032 2.903.966 5.022.45 1.054.95 1.856 1.234 2.077l1.548.773a1 1 0 01.54 1.06l-.74 4.435A1 1 0 018.153 20H6a1 1 0 01-1-1v-2.868a1 1 0 00-1-1H2a1 1 0 01-1-1V3z" />
            </svg>
          </button>

          <!-- Remove Friend Button -->
          <button
            @click="confirmRemoveFriend(friend.id, friend.displayName || friend.username)"
            class="p-2 bg-red-500 hover:bg-red-600 text-white rounded-lg transition"
            title="Remove friend"
          >
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Confirmation Dialog -->
    <div
      v-if="showConfirmDialog"
      class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 rounded-lg"
    >
      <div class="bg-white rounded-lg p-6 max-w-sm">
        <h3 class="text-lg font-semibold text-gray-800 mb-4">
          Remove friend?
        </h3>
        <p class="text-gray-600 mb-6">
          Are you sure you want to remove <strong>{{ confirmDialogName }}</strong> from your friends?
        </p>
        <div class="flex gap-3">
          <button
            @click="showConfirmDialog = false"
            class="flex-1 px-4 py-2 bg-gray-300 text-gray-800 rounded-lg hover:bg-gray-400 transition"
          >
            Cancel
          </button>
          <button
            @click="removeFriend"
            :disabled="removing"
            class="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition disabled:opacity-50"
          >
            {{ removing ? 'Removing...' : 'Remove' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { friendshipService, type Friend } from '@/services/friendship.service';

const emit = defineEmits<{
  callFriend: [friendId: number, friendName: string];
  chatFriend: [friendId: number, friendName: string, isOnline: boolean];
  viewProfile: [userId: number];
}>();

const friends = ref<Friend[]>([]);
const loading = ref(true);
const showConfirmDialog = ref(false);
const removing = ref(false);
const confirmDialogName = ref('');
const friendToRemoveId = ref<number | null>(null);

const refreshFriends = async () => {
  loading.value = true;
  friends.value = await friendshipService.getFriends();
  loading.value = false;
};

const callFriend = (friendId: number, friendName: string) => {
  emit('callFriend', friendId, friendName);
};

const chatFriend = (friendId: number, friendName: string, isOnline: boolean) => {
  emit('chatFriend', friendId, friendName, isOnline);
};

const confirmRemoveFriend = (friendId: number, friendName: string) => {
  friendToRemoveId.value = friendId;
  confirmDialogName.value = friendName;
  showConfirmDialog.value = true;
};

const removeFriend = async () => {
  if (!friendToRemoveId.value) return;

  removing.value = true;
  const result = await friendshipService.removeFriend(friendToRemoveId.value);

  if (result.success) {
    friends.value = friends.value.filter(f => f.id !== friendToRemoveId.value);
    showConfirmDialog.value = false;
  } else {
    alert('Failed to remove friend: ' + result.message);
  }

  removing.value = false;
};

const formatTime = (dateString?: string) => {
  if (!dateString) return 'never';
  const date = new Date(dateString);
  const now = new Date();
  const diff = now.getTime() - date.getTime();
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);
  const days = Math.floor(diff / 86400000);

  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes}m ago`;
  if (hours < 24) return `${hours}h ago`;
  if (days < 7) return `${days}d ago`;
  return date.toLocaleDateString();
};

onMounted(async () => {
  await refreshFriends();
});
</script>

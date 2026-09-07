<template>
  <div class="space-y-6">
    <!-- Incoming Requests -->
    <div class="bg-white rounded-xl shadow-lg p-6">
      <div class="flex items-center gap-2 mb-4">
        <h2 class="text-xl font-bold text-gray-800">Friend Requests</h2>
        <span v-if="pendingRequests.length > 0" class="bg-red-500 text-white text-xs font-bold px-2 py-1 rounded-full">
          {{ pendingRequests.length }}
        </span>
      </div>

      <div v-if="loadingIncoming" class="flex justify-center items-center py-8">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>

      <div v-else-if="pendingRequests.length === 0" class="text-center py-8">
        <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 5.636l-3.536 3.536m0 5.656l3.536 3.536M9.172 9.172L5.636 5.636m3.536 9.192l-3.536 3.536M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-5-4a2 2 0 11-4 0 2 2 0 014 0z" />
        </svg>
        <p class="text-gray-500 mt-4">No friend requests</p>
      </div>

      <div v-else class="space-y-3">
        <div
          v-for="request in pendingRequests"
          :key="request.id"
          class="flex items-center justify-between p-4 bg-gradient-to-r from-blue-50 to-purple-50 rounded-lg"
        >
          <div class="flex items-center gap-3 flex-1">
            <!-- Avatar -->
            <div class="w-10 h-10 bg-gradient-to-br from-blue-400 to-purple-500 rounded-full flex items-center justify-center text-white font-bold">
              {{ request.sender.username[0].toUpperCase() }}
            </div>

            <!-- Request Info -->
            <div class="flex-1">
              <p class="font-semibold text-gray-800">
                {{ request.sender.displayName || request.sender.username }}
              </p>
              <p v-if="request.message" class="text-sm text-gray-600 italic">
                "{{ request.message }}"
              </p>
              <p class="text-xs text-gray-500">{{ formatTime(request.createdAt) }}</p>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex gap-2">
            <button
              @click="acceptRequest(request.id)"
              :disabled="processingRequestId === request.id"
              class="px-3 py-1 bg-green-500 hover:bg-green-600 text-white text-sm rounded-lg transition disabled:opacity-50"
            >
              {{ processingRequestId === request.id ? '...' : 'Accept' }}
            </button>
            <button
              @click="rejectRequest(request.id)"
              :disabled="processingRequestId === request.id"
              class="px-3 py-1 bg-gray-500 hover:bg-gray-600 text-white text-sm rounded-lg transition disabled:opacity-50"
            >
              {{ processingRequestId === request.id ? '...' : 'Reject' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Sent Requests -->
    <div class="bg-white rounded-xl shadow-lg p-6">
      <h2 class="text-xl font-bold text-gray-800 mb-4">Sent Requests</h2>

      <div v-if="loadingOutgoing" class="flex justify-center items-center py-8">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>

      <div v-else-if="sentRequests.length === 0" class="text-center py-8">
        <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2m0 0v-8m0 8a9 9 0 018.354-8.646" />
        </svg>
        <p class="text-gray-500 mt-4">No sent requests</p>
      </div>

      <div v-else class="space-y-3">
        <div
          v-for="request in sentRequests"
          :key="request.id"
          class="flex items-center justify-between p-4 bg-gradient-to-r from-yellow-50 to-orange-50 rounded-lg"
        >
          <div class="flex items-center gap-3 flex-1">
            <!-- Avatar -->
            <div class="w-10 h-10 bg-gradient-to-br from-yellow-400 to-orange-500 rounded-full flex items-center justify-center text-white font-bold">
              {{ request.receiver.username[0].toUpperCase() }}
            </div>

            <!-- Request Info -->
            <div class="flex-1">
              <p class="font-semibold text-gray-800">
                {{ request.receiver.displayName || request.receiver.username }}
              </p>
              <p v-if="request.message" class="text-sm text-gray-600 italic">
                "{{ request.message }}"
              </p>
              <p class="text-xs text-gray-500">Sent {{ formatTime(request.createdAt) }}</p>
            </div>
          </div>

          <!-- Pending Badge -->
          <span class="text-xs font-semibold text-yellow-700 bg-yellow-200 px-3 py-1 rounded-full">
            Pending
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { friendshipService, type FriendRequest, type SentRequest } from '@/services/friendship.service';

const pendingRequests = ref<FriendRequest[]>([]);
const sentRequests = ref<SentRequest[]>([]);
const loadingIncoming = ref(true);
const loadingOutgoing = ref(true);
const processingRequestId = ref<number | null>(null);

const refreshRequests = async () => {
  loadingIncoming.value = true;
  loadingOutgoing.value = true;

  const [pending, sent] = await Promise.all([
    friendshipService.getPendingRequests(),
    friendshipService.getSentRequests()
  ]);

  pendingRequests.value = pending;
  sentRequests.value = sent;

  loadingIncoming.value = false;
  loadingOutgoing.value = false;
};

const acceptRequest = async (requestId: number) => {
  processingRequestId.value = requestId;
  const result = await friendshipService.acceptFriendRequest(requestId);

  if (result.success) {
    pendingRequests.value = pendingRequests.value.filter(r => r.id !== requestId);
  } else {
    alert('Failed to accept request: ' + result.message);
  }

  processingRequestId.value = null;
};

const rejectRequest = async (requestId: number) => {
  processingRequestId.value = requestId;
  const result = await friendshipService.rejectFriendRequest(requestId);

  if (result.success) {
    pendingRequests.value = pendingRequests.value.filter(r => r.id !== requestId);
  } else {
    alert('Failed to reject request: ' + result.message);
  }

  processingRequestId.value = null;
};

const formatTime = (dateString: string) => {
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
  await refreshRequests();
});
</script>

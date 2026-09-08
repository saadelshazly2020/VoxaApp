<template>
  <div class="bg-white rounded-xl shadow-lg p-6">
    <h2 class="text-xl font-bold text-gray-800 mb-4">Search Users</h2>

    <!-- Search Input -->
    <div class="mb-6">
      <input
        v-model="searchQuery"
        @keyup.enter="performSearch"
        type="text"
        placeholder="Search by username..."
        class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
      <button
        @click="performSearch"
        :disabled="searching || !searchQuery"
        class="mt-2 w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition disabled:opacity-50"
      >
        {{ searching ? 'Searching...' : 'Search' }}
      </button>
    </div>

    <!-- Search Results -->
    <div v-if="searched && !searching">
      <!-- Results Found -->
      <div v-if="searchResults.length > 0" class="space-y-3">
        <div
          v-for="user in searchResults"
          :key="user.id"
          class="p-4 bg-gradient-to-r from-green-50 to-emerald-50 rounded-lg border border-green-200"
        >
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3 flex-1">
              <!-- Avatar -->
              <div class="w-10 h-10 bg-gradient-to-br from-green-400 to-emerald-500 rounded-full flex items-center justify-center text-white font-bold">
                {{ user.username[0].toUpperCase() }}
              </div>

              <!-- User Info -->
              <div class="flex-1 min-w-0">
                <p class="font-semibold text-gray-800 truncate">{{ user.displayName || user.username }}</p>
                <p class="text-sm text-gray-600 truncate">@{{ user.username }}</p>
                <div class="flex gap-2 mt-1">
                  <span
                    v-if="user.isFriend"
                    class="text-xs bg-green-200 text-green-800 px-2 py-0.5 rounded font-semibold"
                  >
                    Friend
                  </span>
                  <span
                    v-else-if="user.hasPendingRequest"
                    class="text-xs bg-yellow-200 text-yellow-800 px-2 py-0.5 rounded font-semibold"
                  >
                    Pending
                  </span>
                </div>
              </div>
            </div>

            <!-- Action Button -->
            <div class="ml-3 flex-shrink-0">
              <button
                v-if="!user.isFriend && !user.hasPendingRequest"
                @click="sendFriendRequest(user)"
                :disabled="sendingRequestId === user.id"
                class="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50"
              >
                {{ sendingRequestId === user.id ? 'Sending...' : 'Add' }}
              </button>
              <button
                v-else-if="user.hasPendingRequest"
                disabled
                class="px-3 py-1.5 bg-gray-400 text-white text-sm font-semibold rounded-lg cursor-not-allowed"
              >
                Sent
              </button>
              <button
                v-else
                disabled
                class="px-3 py-1.5 bg-gray-400 text-white text-sm font-semibold rounded-lg cursor-not-allowed"
              >
                Friends
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Not Found -->
      <div v-else class="text-center py-8">
        <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <p class="text-gray-500 mt-4">No users found</p>
        <p class="text-gray-400 text-sm">Try a different username</p>
      </div>

      <!-- Success Message -->
      <div v-if="successMessage" class="mt-4 p-3 bg-green-100 border border-green-400 text-green-700 rounded-lg text-sm">
        {{ successMessage }}
      </div>

      <!-- Error Message -->
      <div v-if="errorMessage" class="mt-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded-lg text-sm">
        {{ errorMessage }}
      </div>
    </div>

    <!-- Initial State -->
    <div v-else-if="!searched" class="text-center py-8">
      <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 10l-2 1m0 0l-2-1m2 1v2.5M20 7l-2 1m2-1l-2-1m2 1v2.5M14 4l-2 1m2-1l-2-1m2 1v2.5" />
      </svg>
      <p class="text-gray-500 mt-4">Search for users to add as friends</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { friendshipService, type SearchUserResult } from '@/services/friendship.service';

const searchQuery = ref('');
const searchResults = ref<SearchUserResult[]>([]);
const searched = ref(false);
const searching = ref(false);
const sendingRequestId = ref<number | null>(null);
const successMessage = ref('');
const errorMessage = ref('');

const performSearch = async () => {
  if (!searchQuery.value.trim()) return;

  searching.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    searchResults.value = await friendshipService.searchUsers(searchQuery.value);
    searched.value = true;
  } catch (error) {
    errorMessage.value = 'Error searching for users';
    console.error(error);
  } finally {
    searching.value = false;
  }
};

const sendFriendRequest = async (user: SearchUserResult) => {
  sendingRequestId.value = user.id;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    const result = await friendshipService.sendFriendRequest(user.id);

    if (result.success) {
      successMessage.value = `Friend request sent to ${user.username}!`;
      user.hasPendingRequest = true;
      setTimeout(() => {
        successMessage.value = '';
      }, 3000);
    } else {
      errorMessage.value = result.message;
    }
  } catch (error) {
    errorMessage.value = 'Error sending friend request';
    console.error(error);
  } finally {
    sendingRequestId.value = null;
  }
};
</script>

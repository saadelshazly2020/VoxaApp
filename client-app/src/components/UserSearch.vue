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
      <!-- Found User -->
      <div v-if="searchResult" class="p-4 bg-gradient-to-r from-green-50 to-emerald-50 rounded-lg border border-green-200">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-3 flex-1">
            <!-- Avatar -->
            <div class="w-12 h-12 bg-gradient-to-br from-green-400 to-emerald-500 rounded-full flex items-center justify-center text-white font-bold text-lg">
              {{ searchResult.username[0].toUpperCase() }}
            </div>

            <!-- User Info -->
            <div class="flex-1">
              <p class="font-semibold text-gray-800">{{ searchResult.displayName || searchResult.username }}</p>
              <p class="text-sm text-gray-600">@{{ searchResult.username }}</p>
              <div class="flex gap-2 mt-2">
                <span
                  v-if="searchResult.isFriend"
                  class="text-xs bg-green-200 text-green-800 px-2 py-1 rounded font-semibold"
                >
                  ? Friend
                </span>
                <span
                  v-else-if="searchResult.hasPendingRequest"
                  class="text-xs bg-yellow-200 text-yellow-800 px-2 py-1 rounded font-semibold"
                >
                  ? Pending Request
                </span>
              </div>
            </div>
          </div>

          <!-- Action Button -->
          <div class="ml-4">
            <button
              v-if="!searchResult.isFriend && !searchResult.hasPendingRequest"
              @click="sendFriendRequest"
              :disabled="sendingRequest"
              class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition disabled:opacity-50"
            >
              {{ sendingRequest ? 'Sending...' : 'Add Friend' }}
            </button>
            <button
              v-else-if="searchResult.hasPendingRequest"
              disabled
              class="px-4 py-2 bg-gray-400 text-white font-semibold rounded-lg cursor-not-allowed"
            >
              Request Sent
            </button>
            <button
              v-else
              disabled
              class="px-4 py-2 bg-gray-400 text-white font-semibold rounded-lg cursor-not-allowed"
            >
              Already Friends
            </button>
          </div>
        </div>

        <!-- Optional Message -->
        <div v-if="!searchResult.isFriend && !searchResult.hasPendingRequest" class="mt-4">
          <input
            v-model="requestMessage"
            type="text"
            placeholder="Add a message (optional)"
            class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>
      </div>

      <!-- Not Found -->
      <div v-else class="text-center py-8">
        <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <p class="text-gray-500 mt-4">No user found</p>
        <p class="text-gray-400 text-sm">Try searching for another username</p>
      </div>

      <!-- Success Message -->
      <div v-if="successMessage" class="mt-4 p-4 bg-green-100 border border-green-400 text-green-700 rounded-lg">
        {{ successMessage }}
      </div>

      <!-- Error Message -->
      <div v-if="errorMessage" class="mt-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded-lg">
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
import { friendshipService } from '@/services/friendship.service';

const searchQuery = ref('');
const searchResult = ref<any>(null);
const searched = ref(false);
const searching = ref(false);
const sendingRequest = ref(false);
const requestMessage = ref('');
const successMessage = ref('');
const errorMessage = ref('');

const performSearch = async () => {
  if (!searchQuery.value.trim()) return;

  searching.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    searchResult.value = await friendshipService.searchUser(searchQuery.value);
    searched.value = true;
  } catch (error) {
    errorMessage.value = 'Error searching for user';
    console.error(error);
  } finally {
    searching.value = false;
  }
};

const sendFriendRequest = async () => {
  if (!searchResult.value) return;

  sendingRequest.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    const result = await friendshipService.sendFriendRequest(
      searchResult.value.id,
      requestMessage.value || undefined
    );

    if (result.success) {
      successMessage.value = 'Friend request sent successfully!';
      searchResult.value.hasPendingRequest = true;
      requestMessage.value = '';
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
    sendingRequest.value = false;
  }
};
</script>

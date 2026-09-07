<template>
  <div class="user-list-container">
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-lg font-semibold text-gray-800">
        Connected Users
      </h3>
      <span class="badge badge-success">
        {{ users.length }}
      </span>
    </div>

    <div v-if="users.length === 0" class="text-center py-8">
      <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
      </svg>
      <p class="mt-2 text-sm text-gray-500">No users connected</p>
    </div>

    <div v-else class="space-y-2">
      <div
        v-for="user in filteredUsers"
        :key="user.userId"
        class="bg-white border border-gray-200 rounded-lg p-3 hover:shadow-md transition-shadow"
      >
        <div class="flex items-center justify-between">
          <div class="flex items-center space-x-3">
            <div class="flex-shrink-0">
              <div class="h-10 w-10 rounded-full bg-primary-100 flex items-center justify-center">
                <span class="text-primary-700 font-semibold text-sm">
                  {{ getUserInitials(user.userId) }}
                </span>
              </div>
            </div>
            <div>
              <p class="text-sm font-medium text-gray-900">
                {{ user.userId }}
              </p>
              <p v-if="user.currentRoomId" class="text-xs text-gray-500">
                ?? {{ user.currentRoomId }}
              </p>
              <p v-else class="text-xs text-green-600">
                ? Available
              </p>
            </div>
          </div>
          <button
            @click="$emit('call-user', user.userId)"
            class="btn btn-primary text-sm py-1 px-3"
          >
            <svg class="w-4 h-4 inline-block mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
            </svg>
            Call
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { User } from '../models/user.model';

interface Props {
  users: User[];
  currentUserId: string;
}

const props = defineProps<Props>();

defineEmits<{
  'call-user': [userId: string];
}>();

const filteredUsers = computed(() => {
  return props.users.filter(user => user.userId !== props.currentUserId);
});

const getUserInitials = (userId: string): string => {
  return userId.substring(0, 2).toUpperCase();
};
</script>

<style scoped>
.user-list-container {
  @apply bg-gray-50 rounded-xl p-4;
}
</style>

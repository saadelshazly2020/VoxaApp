<template>
  <div class="bg-white rounded-xl shadow-lg p-6">
    <div class="flex items-center justify-between mb-6">
      <h2 class="text-2xl font-bold text-gray-800">Messages</h2>
      <div class="flex items-center gap-2">
        <span 
          v-if="totalUnreadCount > 0" 
          class="bg-red-500 text-white text-xs font-bold px-2 py-1 rounded-full"
        >
          {{ totalUnreadCount }}
        </span>
        <button
          @click="refreshConversations"
          class="p-2 hover:bg-gray-100 rounded-lg transition"
          title="Refresh conversations"
        >
          <svg class="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex justify-center items-center py-8">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="conversations.length === 0" class="text-center py-8">
      <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
      </svg>
      <p class="text-gray-500 mt-4">No conversations yet</p>
      <p class="text-gray-400 text-sm">Start chatting with your friends</p>
    </div>

    <!-- Conversations List -->
    <div v-else class="space-y-2">
      <div
        v-for="conversation in conversations"
        :key="conversation.id"
        @click="openChat(conversation.otherUser.id, conversation.otherUser.displayName || conversation.otherUser.username)"
        class="flex items-center gap-3 p-3 rounded-lg hover:bg-gray-50 cursor-pointer transition"
        :class="{ 'bg-blue-50': selectedUserId === conversation.otherUser.id }"
      >
        <!-- Avatar -->
        <div class="relative flex-shrink-0">
          <div class="w-12 h-12 bg-gradient-to-br from-blue-400 to-purple-500 rounded-full flex items-center justify-center text-white font-bold">
            {{ conversation.otherUser.username[0].toUpperCase() }}
          </div>
          <!-- Online Status -->
          <span
            :class="[
              'absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-white',
              isBusy(conversation.otherUser.id)
                ? 'bg-red-500'
                : (conversation.otherUser.isOnline ? 'bg-green-500' : 'bg-gray-400')
            ]"
            :title="isBusy(conversation.otherUser.id)
              ? 'Busy - in a call'
              : (conversation.otherUser.isOnline ? 'Online' : 'Offline')"
          />
        </div>

        <!-- Conversation Info -->
        <div class="flex-1 min-w-0">
          <div class="flex items-center justify-between mb-1">
            <p class="font-semibold text-gray-800 truncate">
              {{ conversation.otherUser.displayName || conversation.otherUser.username }}
            </p>
            <span class="text-xs text-gray-500 flex-shrink-0 ml-2">
              {{ formatTime(conversation.lastMessageAt) }}
            </span>
          </div>
          <div class="flex items-center justify-between">
            <p 
              class="text-sm text-gray-600 truncate"
              :class="{ 'font-semibold text-gray-900': conversation.unreadCount > 0 }"
            >
              <span v-if="conversation.lastMessage">
                <span v-if="conversation.lastMessage.isSentByMe" class="text-gray-500">You: </span>
                {{ conversation.lastMessage.content }}
              </span>
              <span v-else class="text-gray-400">No messages yet</span>
            </p>
            <span
              v-if="isBusy(conversation.otherUser.id)"
              class="ml-2 bg-red-500 text-white text-xs font-bold px-2 py-0.5 rounded-full flex-shrink-0"
              title="In a call"
            >
              Busy
            </span>
            <span
              v-if="conversation.unreadCount > 0"
              class="ml-2 bg-blue-600 text-white text-xs font-bold px-2 py-0.5 rounded-full flex-shrink-0"
            >
              {{ conversation.unreadCount }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { chatService, type Conversation } from '@/services/chat.service';
import { authService } from '@/services/auth.service';

const emit = defineEmits<{
  openChat: [userId: number, userName: string, isOnline: boolean];
}>();

defineProps<{
  selectedUserId?: number;
}>();

const conversations = ref<Conversation[]>([]);
const loading = ref(true);
const totalUnreadCount = ref(0);
const busyUserIds = ref<Set<number>>(new Set());
let signalRConnection: any = null;

const isBusy = (userId: number): boolean => busyUserIds.value.has(userId);

const refreshConversations = async () => {
  loading.value = true;
  const [convos, unreadCount] = await Promise.all([
    chatService.getConversations(),
    chatService.getTotalUnreadCount()
  ]);
  conversations.value = convos;
  totalUnreadCount.value = unreadCount;
  loading.value = false;
  await refreshBusyStatuses();
};

const refreshBusyStatuses = async () => {
  if (!signalRConnection) return;

  const busy = new Set<number>();
  for (const conversation of conversations.value) {
    try {
      if (await signalRConnection.invoke('GetCallStatus', conversation.otherUser.id)) {
        busy.add(conversation.otherUser.id);
      }
    } catch (error) {
      console.error('Failed to get call status:', error);
    }
  }
  busyUserIds.value = busy;
};

const handleUserBusyStatusChanged = (userId: number, isBusyFlag: boolean) => {
  const next = new Set(busyUserIds.value);
  if (isBusyFlag) next.add(userId);
  else next.delete(userId);
  busyUserIds.value = next;
};

const openChat = (userId: number, userName: string) => {
  const conversation = conversations.value.find(c => c.otherUser.id === userId);
  emit('openChat', userId, userName, conversation?.otherUser.isOnline ?? false);
};

const formatTime = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diff = now.getTime() - date.getTime();
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);
  const days = Math.floor(diff / 86400000);

  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes}m`;
  if (hours < 24) return `${hours}h`;
  if (days < 7) return `${days}d`;
  return date.toLocaleDateString();
};

// Handle user online status changes from SignalR
const handleUserOnlineStatusChanged = (userId: number, isOnline: boolean) => {
  const conversation = conversations.value.find(c => c.otherUser.id === userId);
  if (conversation) {
    conversation.otherUser.isOnline = isOnline;
  }
};

// Handle new messages from SignalR
const handleReceiveMessage = (_message: any) => {
  // Refresh conversations to update last message and unread count
  refreshConversations();
};

// Setup SignalR connection for real-time updates
const setupSignalR = async () => {
  try {
    const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
    
    signalRConnection = new HubConnectionBuilder()
      .withUrl('/videocallhub', {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    signalRConnection.on('UserOnlineStatusChanged', handleUserOnlineStatusChanged);
    signalRConnection.on('UserBusyStatusChanged', handleUserBusyStatusChanged);
    signalRConnection.on('ReceiveMessage', handleReceiveMessage);

    await signalRConnection.start();
    console.log('SignalR connected for conversations list');
    
    // Register chat user (re-register after every reconnect, connection id changes)
    const register = async () => {
      const user = authService.getUser();
      if (user) {
        await signalRConnection.invoke('RegisterChatUser', user.id);
      }
    };

    signalRConnection.onreconnected(register);
    await register();
  } catch (error) {
    console.error('Failed to setup SignalR:', error);
  }
};

onMounted(async () => {
  await refreshConversations();
  await setupSignalR();
  
  // Refresh periodically for other updates
  setInterval(refreshConversations, 30000); // Every 30 seconds instead of 10
});

onUnmounted(async () => {
  if (signalRConnection) {
    signalRConnection.off('UserOnlineStatusChanged', handleUserOnlineStatusChanged);
    signalRConnection.off('UserBusyStatusChanged', handleUserBusyStatusChanged);
    signalRConnection.off('ReceiveMessage', handleReceiveMessage);
    await signalRConnection.stop();
  }
});

// Expose refresh method for parent
defineExpose({
  refreshConversations
});
</script>

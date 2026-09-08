<template>
  <div class="flex flex-col h-full bg-white rounded-xl shadow-lg overflow-hidden">
    <!-- Chat Header -->
    <div class="bg-gradient-to-r from-blue-600 to-purple-600 text-white p-3 sm:p-4 flex items-center justify-between gap-2 flex-shrink-0">
      <div class="flex items-center gap-2 sm:gap-3 min-w-0">
        <button
          @click="$emit('close')"
          class="lg:hidden p-2 -ml-1 hover:bg-white/20 rounded-lg transition flex-shrink-0"
          title="Back to conversations"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </button>

        <div class="relative flex-shrink-0">
          <div class="w-10 h-10 bg-white/20 rounded-full flex items-center justify-center font-bold">
            {{ userName[0].toUpperCase() }}
          </div>
          <span
            :class="[
              'absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-blue-600',
              peerBusy ? 'bg-red-400' : (peerOnline ? 'bg-green-400' : 'bg-gray-400')
            ]"
          />
        </div>
        
        <div class="min-w-0">
          <p class="font-semibold truncate">{{ userName }}</p>
          <p class="text-xs text-blue-100">
            <span :class="{ 'text-red-200 font-semibold': peerBusy }">
              {{ peerBusy ? 'Busy - in a call' : (peerOnline ? 'Online' : 'Offline') }}
            </span>
          </p>
        </div>
      </div>

      <div class="flex items-center gap-1 sm:gap-2 flex-shrink-0">
        <!-- Call Button (peer to peer) -->
        <button
          @click="startCall"
          :disabled="!canCall"
          :title="callButtonTitle"
          :class="[
            'flex items-center gap-1.5 px-2.5 sm:px-3 py-2 rounded-lg text-sm font-semibold transition',
            canCall
              ? 'bg-white/20 hover:bg-white/30 text-white'
              : 'bg-white/5 text-white/40 cursor-not-allowed'
          ]"
        >
          <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path d="M2 6a2 2 0 012-2h6a2 2 0 012 2v8a2 2 0 01-2 2H4a2 2 0 01-2-2V6zM14.553 7.106A1 1 0 0014 8v4a1 1 0 00.553.894l2 1A1 1 0 0018 13V7a1 1 0 00-1.447-.894l-2 1z" />
          </svg>
          <span class="hidden sm:inline">{{ peerBusy ? 'Busy' : 'Call' }}</span>
        </button>


        <!-- More Options -->
        <button
          class="p-2 hover:bg-white/20 rounded-lg transition"
          title="More options"
        >
          <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Messages Area -->
    <div 
      ref="messagesContainer"
      class="flex-1 min-h-0 overflow-y-auto overscroll-contain p-3 sm:p-4 space-y-3 bg-gray-50"
      @scroll="handleScroll"
    >
      <!-- Loading More Messages -->
      <div v-if="loadingMore" class="text-center py-2">
        <div class="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
      </div>

      <!-- Loading Initial Messages -->
      <div v-if="loading" class="flex justify-center items-center py-8">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>

      <!-- Messages -->
      <div v-else-if="messages.length > 0">
        <div
          v-for="message in messages"
          :key="message.id"
          :class="[
            'flex',
            message.isSentByMe ? 'justify-end' : 'justify-start'
          ]"
        >
          <div
            :class="[
              'max-w-[80%] sm:max-w-xs lg:max-w-md px-4 py-2 rounded-2xl break-words',
              message.isSentByMe
                ? 'bg-gradient-to-r from-blue-600 to-purple-600 text-white rounded-br-none'
                : 'bg-white text-gray-800 border border-gray-200 rounded-bl-none'
            ]"
          >
            <!-- Message Content -->
            <p :class="message.isDeleted ? 'italic text-gray-400' : ''">
              {{ message.content }}
            </p>

            <!-- Attachment (if any) -->
            <div v-if="message.attachmentUrl && !message.isDeleted" class="mt-2">
              <a
                :href="message.attachmentUrl"
                target="_blank"
                class="text-sm underline"
                :class="message.isSentByMe ? 'text-blue-200' : 'text-blue-600'"
              >
                &#128206; Attachment
              </a>
            </div>

            <!-- Message Info -->
            <div class="flex items-center gap-2 mt-1 text-xs" :class="message.isSentByMe ? 'text-blue-200' : 'text-gray-500'">
              <span>{{ formatMessageTime(message.sentAt) }}</span>
              <span v-if="message.isSentByMe">
                <span v-if="message.isRead" title="Read">&check;&check;</span>
                <span v-else title="Sent">&check;</span>
              </span>
            </div>

            <!-- Delete Button (only for sent messages) -->
            <button
              v-if="message.isSentByMe && !message.isDeleted"
              @click="confirmDelete(message.id)"
              class="mt-1 text-xs underline opacity-70 hover:opacity-100"
              :class="message.isSentByMe ? 'text-blue-200' : 'text-gray-500'"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else class="text-center py-8">
        <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
        </svg>
        <p class="text-gray-500 mt-4">No messages yet</p>
        <p class="text-gray-400 text-sm">Start the conversation!</p>
      </div>
    </div>

    <!-- Message Input -->
    <div class="border-t border-gray-200 p-3 sm:p-4 bg-white flex-shrink-0 pb-[max(0.75rem,env(safe-area-inset-bottom))] sm:pb-4">
      <div class="flex gap-2">
        <input
          v-model="newMessage"
          type="text"
          placeholder="Type a message..."
          enterkeyhint="send"
          class="flex-1 min-w-0 px-4 py-2 border border-gray-300 rounded-full focus:outline-none focus:ring-2 focus:ring-blue-500"
          @keyup.enter="sendMessage"
          :disabled="sending"
        />
        <button
          @click="sendMessage"
          :disabled="!newMessage.trim() || sending"
          class="px-4 sm:px-6 py-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold rounded-full hover:shadow-lg transition disabled:opacity-50 flex-shrink-0"
        >
          {{ sending ? '...' : 'Send' }}
        </button>
      </div>
    </div>

    <!-- Delete Confirmation Dialog -->
    <div
      v-if="showDeleteDialog"
      class="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
      @click.self="showDeleteDialog = false"
    >
      <div class="bg-white rounded-lg p-6 max-w-sm mx-4">
        <h3 class="text-lg font-semibold text-gray-800 mb-4">
          Delete Message?
        </h3>
        <p class="text-gray-600 mb-6">
          This message will be replaced with "This message was deleted"
        </p>
        <div class="flex gap-3">
          <button
            @click="showDeleteDialog = false"
            class="flex-1 px-4 py-2 bg-gray-300 text-gray-800 rounded-lg hover:bg-gray-400 transition"
          >
            Cancel
          </button>
          <button
            @click="deleteMessage"
            :disabled="deleting"
            class="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition disabled:opacity-50"
          >
            {{ deleting ? 'Deleting...' : 'Delete' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue';
import { chatService, type ChatMessage } from '@/services/chat.service';
import { authService } from '@/services/auth.service';
import { signalRManager } from '@/services/signalr-manager';
import type { SignalRService } from '@/services/signalr.service';

const props = defineProps<{
  userId: number;
  userName: string;
  isOnline?: boolean;
}>();

const emit = defineEmits<{
  close: [];
  videoCall: [userId: number];
  updateUnreadCount: [];
  onlineStatusChanged: [userId: number, isOnline: boolean];
  busyStatusChanged: [userId: number, isBusy: boolean];
}>();

const messages = ref<ChatMessage[]>([]);
const peerOnline = ref(props.isOnline ?? false);
const peerBusy = ref(false);
const newMessage = ref('');
const loading = ref(false);
const sending = ref(false);
const loadingMore = ref(false);
const messagesContainer = ref<HTMLElement>();
const showDeleteDialog = ref(false);
const messageToDelete = ref<number | null>(null);
const deleting = ref(false);
const scrolledToBottom = ref(true);
let signalRService: SignalRService | null = null;

const loadMessages = async (append: boolean = false) => {
  if (append) {
    loadingMore.value = true;
  } else {
    loading.value = true;
  }

  const skip = append ? messages.value.length : 0;
  const newMessages = await chatService.getConversationMessages(props.userId, skip, 50);

  if (append) {
    // When loading more (scrolling up), prepend to beginning
    messages.value = [...newMessages, ...messages.value];
  } else {
    // Initial load: messages come in reverse order from API, keep them that way (oldest first)
    messages.value = newMessages;
    await nextTick();
    scrollToBottom();
  }

  loading.value = false;
  loadingMore.value = false;

  // Mark conversation as read
  await chatService.markConversationAsRead(props.userId);
  emit('updateUnreadCount');
};

const sendMessage = async () => {
  if (!newMessage.value.trim() || sending.value) return;

  sending.value = true;
  const messageContent = newMessage.value.trim();
  newMessage.value = '';

  const result = await chatService.sendMessage(props.userId, messageContent);

  if (result.success && result.chatMessage) {
    // Add sent message with isSentByMe flag
    const sentMessage = {
      ...result.chatMessage,
      isSentByMe: true
    };
    messages.value.push(sentMessage);
    await nextTick();
    scrollToBottom();
  } else {
    alert('Failed to send message: ' + result.message);
    newMessage.value = messageContent; // Restore message
  }

  sending.value = false;
};

const confirmDelete = (messageId: number) => {
  messageToDelete.value = messageId;
  showDeleteDialog.value = true;
};

const deleteMessage = async () => {
  if (!messageToDelete.value) return;

  deleting.value = true;
  const result = await chatService.deleteMessage(messageToDelete.value);

  if (result.success) {
    const message = messages.value.find(m => m.id === messageToDelete.value);
    if (message) {
      message.isDeleted = true;
      message.content = 'This message was deleted';
    }
    showDeleteDialog.value = false;
  } else {
    alert('Failed to delete message: ' + result.message);
  }

  deleting.value = false;
  messageToDelete.value = null;
};

const scrollToBottom = (smooth: boolean = false) => {
  if (messagesContainer.value) {
    messagesContainer.value.scrollTo({
      top: messagesContainer.value.scrollHeight,
      behavior: smooth ? 'smooth' : 'auto'
    });
    scrolledToBottom.value = true;
  }
};

const handleScroll = () => {
  if (!messagesContainer.value || loadingMore.value) return;

  const container = messagesContainer.value;
  const isAtBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 50;
  scrolledToBottom.value = isAtBottom;

  // Load more when scrolled to top
  if (container.scrollTop === 0 && messages.value.length >= 50) {
    const previousHeight = container.scrollHeight;
    loadMessages(true).then(() => {
      // Maintain scroll position after loading more messages
      nextTick(() => {
        if (container) {
          container.scrollTop = container.scrollHeight - previousHeight;
        }
      });
    });
  }
};

const formatMessageTime = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diffInHours = (now.getTime() - date.getTime()) / (1000 * 60 * 60);

  if (diffInHours < 24) {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  } else if (diffInHours < 48) {
    return 'Yesterday ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  } else {
    return date.toLocaleDateString([], { month: 'short', day: 'numeric' }) + ' ' +
           date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
};

// Handle real-time message updates
const handleNewMessage = (message: any) => {
  // Only add if message is part of this conversation
  if ((message.senderId === props.userId && message.receiverId === getCurrentUserId()) ||
      (message.receiverId === props.userId && message.senderId === getCurrentUserId())) {
    
    // Check if message already exists (avoid duplicates)
    const exists = messages.value.some(m => m.id === message.id);
    if (!exists) {
      messages.value.push(message);
      
      // Only scroll if user is already at bottom
      if (scrolledToBottom.value) {
        nextTick(() => scrollToBottom(true));
      }
      
      // Mark as read if from the current chat partner
      if (message.senderId === props.userId) {
        chatService.markAsRead(message.id);
        emit('updateUnreadCount');
      }
    }
  }
};

const canCall = computed(() => peerOnline.value && !peerBusy.value);

const callButtonTitle = computed(() => {
  if (peerBusy.value) return `${props.userName} is in another call`;
  if (!peerOnline.value) return `${props.userName} is offline`;
  return `Call ${props.userName}`;
});

const startCall = () => {
  if (!canCall.value) return;
  emit('videoCall', props.userId);
};

const refreshPeerBusyStatus = async () => {
  try {
    peerBusy.value = await signalRService?.getCallStatus(props.userId) ?? false;
  } catch (error) {
    console.error('Failed to get call status:', error);
  }
};

const handleMessagesRead = (payload: { readerId: number; messageIds?: number[] }) => {
  if (!payload || payload.readerId !== props.userId) return;

  const ids = payload.messageIds ?? [];
  for (const message of messages.value) {
    if (ids.length === 0) {
      if (message.senderId === getCurrentUserId()) message.isRead = true;
    } else if (ids.includes(message.id)) {
      message.isRead = true;
    }
  }
};

const getCurrentUserId = (): number => {
  return authService.getUser()?.id ?? 0;
};

// Setup SignalR connection for real-time messages
const setupSignalR = async () => {
  try {
    signalRService = await signalRManager.acquire();

    signalRService.on('ReceiveMessage', handleNewMessage);
    signalRService.on('MessagesRead', handleMessagesRead);

    signalRService.on('UserOnlineStatusChanged', (userId: number, isOnline: boolean) => {
      if (userId === props.userId) {
        peerOnline.value = isOnline;
        emit('onlineStatusChanged', userId, isOnline);
      }
    });

    signalRService.on('UserBusyStatusChanged', (userId: number, isBusy: boolean) => {
      if (userId === props.userId) {
        peerBusy.value = isBusy;
        emit('busyStatusChanged', userId, isBusy);
      }
    });

    await refreshPeerBusyStatus();
    console.log('SignalR connected for chat');
  } catch (error) {
    console.error('Failed to setup SignalR:', error);
  }
};

// Watch for user changes
watch(() => props.userId, async () => {
  await loadMessages();
});

watch(() => props.isOnline, (value) => {
  peerOnline.value = value ?? false;
});

watch(() => props.userId, () => {
  peerBusy.value = false;
  if (signalRService) refreshPeerBusyStatus();
});

onMounted(async () => {
  await loadMessages();
  await setupSignalR();
});

onUnmounted(async () => {
  if (signalRService) {
    signalRManager.release();
  }
});
</script>

<style scoped>
/* Custom scrollbar */
.overflow-y-auto::-webkit-scrollbar {
  width: 6px;
}

.overflow-y-auto::-webkit-scrollbar-track {
  background: #f1f1f1;
}

.overflow-y-auto::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 3px;
}

.overflow-y-auto::-webkit-scrollbar-thumb:hover {
  background: #94a3b8;
}
</style>

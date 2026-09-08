<template>
  <div class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
    <!-- Header/Navigation -->
    <nav class="bg-white shadow-md">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16 gap-3">
          <!-- Logo -->
          <div class="flex items-center gap-3 min-w-0">
            <div class="h-10 w-10 flex-shrink-0 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex items-center justify-center">
              <svg class="h-6 w-6 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
              </svg>
            </div>
            <h1 class="text-xl font-bold text-gray-800 truncate">VideoChat</h1>
          </div>

          <!-- User Info & Actions -->
          <div class="flex items-center gap-3 sm:gap-4 flex-shrink-0">
            <div class="text-right hidden sm:block">
              <p class="text-sm font-semibold text-gray-800">{{ currentUser?.displayName || currentUser?.username }}</p>
              <p class="text-xs text-gray-500">@{{ currentUser?.username }}</p>
            </div>

            <button @click="showProfileEdit = true" class="relative group">
              <div class="w-10 h-10 rounded-full overflow-hidden bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white font-bold">
                <img v-if="currentUser?.profilePictureUrl" :src="currentUser.profilePictureUrl" class="w-full h-full object-cover" />
                <span v-else>{{ currentUser?.username?.[0]?.toUpperCase() }}</span>
              </div>
              <span class="absolute bottom-0 right-0 w-3 h-3 bg-green-500 rounded-full border-2 border-white"></span>
              <span class="absolute inset-0 bg-black/30 rounded-full opacity-0 group-hover:opacity-100 flex items-center justify-center transition">
                <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                </svg>
              </span>
            </button>

            <button
              @click="handleLogout"
              class="p-2 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded-lg transition"
              title="Logout"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </nav>

    <!-- Main Content -->
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6 lg:py-8">
      <!-- Tabs: scrolls horizontally on small screens, equal columns from lg up -->
      <div class="bg-white rounded-xl shadow-md p-2 mb-4 sm:mb-8">
        <div class="flex gap-2 overflow-x-auto no-scrollbar lg:grid lg:grid-cols-6 lg:overflow-visible">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            @click="activeTab = tab.id"
            :class="[
              'flex-shrink-0 min-w-[8.5rem] lg:min-w-0 whitespace-nowrap py-3 px-4 rounded-lg font-semibold transition-all flex items-center justify-center gap-2',
              activeTab === tab.id
                ? 'bg-gradient-to-r from-blue-600 to-purple-600 text-white shadow-lg'
                : 'text-gray-700 hover:bg-gray-100'
            ]"
          >
            <component :is="tab.icon" class="w-5 h-5" />
            {{ tab.label }}
            <span
              v-if="tab.badge && tab.badge > 0"
              class="ml-1 px-2 py-0.5 bg-red-500 text-white text-xs font-bold rounded-full"
            >
              {{ tab.badge }}
            </span>
          </button>
        </div>
      </div>

      <!-- Tab Content -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Main Content Area -->
        <div class="lg:col-span-2">
          <!-- Posts Tab -->
          <div v-if="activeTab === 'posts'">
            <PostFeed />
          </div>

          <!-- Friends Tab -->
          <div v-if="activeTab === 'friends'">
            <FriendsList 
              @call-friend="handleCallFriend"
              @chat-friend="openChatWithFriend"
              @view-profile="viewUserProfile"
            />
          </div>

          <!-- Chat Tab: one pane at a time on mobile (list -> conversation), split view from lg up -->
          <div v-if="activeTab === 'chat'" class="flex flex-col lg:grid lg:grid-cols-2 gap-4 h-[75vh] min-h-[380px] lg:h-[600px]">
            <!-- Conversations List -->
            <div class="h-full min-h-0 overflow-hidden lg:block" :class="selectedChatUserId ? 'hidden' : 'block'">
              <ConversationsList
                ref="conversationsListRef"
                :selected-user-id="selectedChatUserId || undefined"
                @open-chat="handleChatOpen"
              />
            </div>

            <!-- Chat Window -->
            <div class="h-full min-h-0 overflow-hidden" v-if="selectedChatUserId">
              <ChatWindow
                :user-id="selectedChatUserId"
                :user-name="selectedChatUserName"
                :is-online="selectedChatUserOnline"
                @close="handleCloseChat"
                @video-call="handleVideoCallFromChat"
                @update-unread-count="handleUpdateUnreadCount"
                @online-status-changed="handlePeerOnlineStatusChanged"
              />
            </div>

            <!-- Empty State -->
            <div v-else class="h-full hidden lg:flex items-center justify-center bg-gray-50 rounded-xl">
              <div class="text-center">
                <svg class="mx-auto h-16 w-16 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                </svg>
                <p class="text-gray-500 font-semibold">Select a conversation</p>
                <p class="text-gray-400 text-sm mt-2">Choose a friend to start chatting</p>
              </div>
            </div>
          </div>

          <!-- Requests Tab -->
          <div v-if="activeTab === 'requests'">
            <FriendRequests />
          </div>

          <!-- Search Tab -->
          <div v-if="activeTab === 'search'">
            <UserSearch />
          </div>

          <!-- Video Chat Tab -->
          <div v-if="activeTab === 'video'">
            <div class="bg-white rounded-xl shadow-lg p-8 text-center">
              <div class="h-20 w-20 bg-gradient-to-br from-green-400 to-emerald-500 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <svg class="h-12 w-12 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
                </svg>
              </div>
              <h2 class="text-2xl font-bold text-gray-800 mb-4">Start Video Chat</h2>
              <p class="text-gray-600 mb-6">Join the main video chat room or create a new room</p>
              <div class="space-y-3">
                <router-link
                  to="/video-chat"
                  class="block w-full py-3 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold rounded-lg hover:shadow-xl transition"
                >
                  Join Main Room
                </router-link>
                <div class="relative">
                  <div class="absolute inset-0 flex items-center">
                    <div class="w-full border-t border-gray-300"></div>
                  </div>
                  <div class="relative flex justify-center text-sm">
                    <span class="px-2 bg-white text-gray-500">or</span>
                  </div>
                </div>
                <input
                  v-model="roomIdInput"
                  type="text"
                  placeholder="Enter Room ID"
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  @keyup.enter="joinRoom"
                />
                <button
                  @click="joinRoom"
                  :disabled="!roomIdInput.trim()"
                  class="w-full py-3 bg-green-600 hover:bg-green-700 text-white font-semibold rounded-lg transition disabled:opacity-50"
                >
                  Join Specific Room
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Sidebar -->
        <div class="lg:col-span-1">
          <!-- Quick Stats -->
          <div class="bg-white rounded-xl shadow-lg p-6 mb-6">
            <h3 class="text-lg font-bold text-gray-800 mb-4">Quick Stats</h3>
            <div class="space-y-3">
              <div class="flex items-center justify-between p-3 bg-blue-50 rounded-lg">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 bg-blue-500 rounded-lg flex items-center justify-center">
                    <svg class="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                      <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z" />
                    </svg>
                  </div>
                  <span class="font-semibold text-gray-700">Friends</span>
                </div>
                <span class="text-2xl font-bold text-blue-600">{{ friendsCount }}</span>
              </div>

              <div class="flex items-center justify-between p-3 bg-yellow-50 rounded-lg">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 bg-yellow-500 rounded-lg flex items-center justify-center">
                    <svg class="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                      <path d="M2.003 5.884L10 9.882l7.997-3.998A2 2 0 0016 4H4a2 2 0 00-1.997 1.884z" />
                      <path d="M18 8.118l-8 4-8-4V14a2 2 0 002 2h12a2 2 0 002-2V8.118z" />
                    </svg>
                  </div>
                  <span class="font-semibold text-gray-700">Requests</span>
                </div>
                <span class="text-2xl font-bold text-yellow-600">{{ pendingRequestsCount }}</span>
              </div>

              <div class="flex items-center justify-between p-3 bg-green-50 rounded-lg">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 bg-green-500 rounded-lg flex items-center justify-center">
                    <svg class="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                      <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                    </svg>
                  </div>
                  <span class="font-semibold text-gray-700">Online</span>
                </div>
                <span class="text-2xl font-bold text-green-600">{{ onlineFriendsCount }}</span>
              </div>

              <div class="flex items-center justify-between p-3 bg-purple-50 rounded-lg">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 bg-purple-500 rounded-lg flex items-center justify-center">
                    <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                    </svg>
                  </div>
                  <span class="font-semibold text-gray-700">Unread</span>
                </div>
                <span class="text-2xl font-bold text-purple-600">{{ totalUnreadCount }}</span>
              </div>
            </div>
          </div>

          <!-- Quick Actions -->
          <div class="bg-white rounded-xl shadow-lg p-6">
            <h3 class="text-lg font-bold text-gray-800 mb-4">Quick Actions</h3>
            <div class="space-y-2">
              <button
                @click="activeTab = 'posts'"
                class="w-full p-3 text-left bg-gradient-to-r from-blue-50 to-indigo-50 hover:from-blue-100 hover:to-indigo-100 rounded-lg transition flex items-center gap-3"
              >
                <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z" />
                </svg>
                <span class="font-semibold text-gray-700">View Posts Feed</span>
              </button>

              <button
                @click="activeTab = 'chat'"
                class="w-full p-3 text-left bg-gradient-to-r from-purple-50 to-pink-50 hover:from-purple-100 hover:to-pink-100 rounded-lg transition flex items-center gap-3"
              >
                <svg class="w-5 h-5 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                </svg>
                <span class="font-semibold text-gray-700">Chat with Friends</span>
                <span v-if="totalUnreadCount > 0" class="ml-auto bg-purple-600 text-white text-xs font-bold px-2 py-0.5 rounded-full">
                  {{ totalUnreadCount }}
                </span>
              </button>

              <button
                @click="activeTab = 'search'"
                class="w-full p-3 text-left bg-gradient-to-r from-blue-50 to-purple-50 hover:from-blue-100 hover:to-purple-100 rounded-lg transition flex items-center gap-3"
              >
                <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
                <span class="font-semibold text-gray-700">Find Friends</span>
              </button>

              <button
                @click="activeTab = 'video'"
                class="w-full p-3 text-left bg-gradient-to-r from-green-50 to-emerald-50 hover:from-green-100 hover:to-emerald-100 rounded-lg transition flex items-center gap-3"
              >
                <svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
                </svg>
                <span class="font-semibold text-gray-700">Start Video Call</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Incoming / outgoing / active call UI -->
    <CallOverlay />

    <!-- Profile Modals -->
    <ProfileEditModal
      :show="showProfileEdit"
      @close="showProfileEdit = false"
      @updated="onProfileUpdated"
    />
    <UserProfileModal
      :show="showUserProfile"
      :user-id="viewedUserId"
      @close="showUserProfile = false"
      @open-chat="openChatWithFriend"
      @call="(id, name) => handleCallFriend(id, name)"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, h } from 'vue';
import { useRouter } from 'vue-router';
import { authService } from '@/services/auth.service';
import { friendshipService } from '@/services/friendship.service';
import { chatService } from '@/services/chat.service';
import FriendsList from '@/components/FriendsList.vue';
import FriendRequests from '@/components/FriendRequests.vue';
import UserSearch from '@/components/UserSearch.vue';
import ConversationsList from '@/components/ConversationsList.vue';
import ChatWindow from '@/components/ChatWindow.vue';
import PostFeed from '@/components/PostFeed.vue';
import CallOverlay from '@/components/CallOverlay.vue';
import ProfileEditModal from '@/components/ProfileEditModal.vue';
import UserProfileModal from '@/components/UserProfileModal.vue';
import { useCall } from '@/composables/useCall';
import { pushNotificationService } from '@/services/push-notification.service';

const router = useRouter();
const { startCall } = useCall();
type TabId = 'friends' | 'chat' | 'requests' | 'search' | 'video' | 'posts';

const activeTab = ref<TabId>('posts');
const roomIdInput = ref('');
const showProfileEdit = ref(false);
const showUserProfile = ref(false);
const viewedUserId = ref(0);

const currentUser = computed(() => authService.getUser());
const friendsCount = ref(0);
const pendingRequestsCount = ref(0);
const onlineFriendsCount = ref(0);
const totalUnreadCount = ref(0);

// Chat state
const selectedChatUserId = ref<number | null>(null);
const selectedChatUserName = ref<string>('');
const selectedChatUserOnline = ref<boolean>(false);
const conversationsListRef = ref();

// Icons as components
const FriendsIcon = () => h('svg', {
  class: 'w-5 h-5',
  fill: 'currentColor',
  viewBox: '0 0 20 20'
}, h('path', {
  d: 'M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z'
}));

const ChatIcon = () => h('svg', {
  class: 'w-5 h-5',
  fill: 'none',
  stroke: 'currentColor',
  viewBox: '0 0 24 24'
}, h('path', {
  'stroke-linecap': 'round',
  'stroke-linejoin': 'round',
  'stroke-width': '2',
  d: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z'
}));

const RequestsIcon = () => h('svg', {
  class: 'w-5 h-5',
  fill: 'none',
  stroke: 'currentColor',
  viewBox: '0 0 24 24'
}, h('path', {
  'stroke-linecap': 'round',
  'stroke-linejoin': 'round',
  'stroke-width': '2',
  d: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z'
}));

const SearchIcon = () => h('svg', {
  class: 'w-5 h-5',
  fill: 'none',
  stroke: 'currentColor',
  viewBox: '0 0 24 24'
}, h('path', {
  'stroke-linecap': 'round',
  'stroke-linejoin': 'round',
  'stroke-width': '2',
  d: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z'
}));

const VideoIcon = () => h('svg', {
  class: 'w-5 h-5',
  fill: 'none',
  stroke: 'currentColor',
  viewBox: '0 0 24 24'
}, h('path', {
  'stroke-linecap': 'round',
  'stroke-linejoin': 'round',
  'stroke-width': '2',
  d: 'M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z'
}));

const PostsIcon = () => h('svg', {
  class: 'w-5 h-5',
  fill: 'none',
  stroke: 'currentColor',
  viewBox: '0 0 24 24'
}, h('path', {
  'stroke-linecap': 'round',
  'stroke-linejoin': 'round',
  'stroke-width': '2',
  d: 'M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z'
}));

const tabs = computed<{ id: TabId; label: string; icon: any; badge: number }[]>(() => [
  { id: 'posts', label: 'Posts', icon: PostsIcon, badge: 0 },
  { id: 'friends', label: 'Friends', icon: FriendsIcon, badge: 0 },
  { id: 'chat', label: 'Chat', icon: ChatIcon, badge: totalUnreadCount.value },
  { id: 'requests', label: 'Requests', icon: RequestsIcon, badge: pendingRequestsCount.value },
  { id: 'search', label: 'Search', icon: SearchIcon, badge: 0 },
  { id: 'video', label: 'Video Chat', icon: VideoIcon, badge: 0 }
]);

const loadStats = async () => {
  const [friends, requests, unreadCount] = await Promise.all([
    friendshipService.getFriends(),
    friendshipService.getPendingRequests(),
    chatService.getTotalUnreadCount()
  ]);

  friendsCount.value = friends.length;
  pendingRequestsCount.value = requests.length;
  onlineFriendsCount.value = friends.filter(f => f.isOnline).length;
  totalUnreadCount.value = unreadCount;
};

const handleLogout = async () => {
  await authService.logout();
  router.push('/login');
};

const handleCallFriend = (friendId: number, friendName: string) => {
  // Calls are handled in-app so the other person gets the ring wherever they are
  void startCall(friendId, friendName);
};

const openChatWithFriend = (friendId: number, friendName: string, isOnline: boolean) => {
  selectedChatUserId.value = friendId;
  selectedChatUserName.value = friendName;
  selectedChatUserOnline.value = isOnline;
  activeTab.value = 'chat';
};

const handleChatOpen = (userId: number, userName: string, isOnline: boolean) => {
  selectedChatUserId.value = userId;
  selectedChatUserName.value = userName;
  selectedChatUserOnline.value = isOnline;
};

const handlePeerOnlineStatusChanged = (userId: number, isOnline: boolean) => {
  if (userId === selectedChatUserId.value) {
    selectedChatUserOnline.value = isOnline;
  }
};

const handleCloseChat = () => {
  selectedChatUserId.value = null;
  selectedChatUserName.value = '';
};

const handleUpdateUnreadCount = async () => {
  totalUnreadCount.value = await chatService.getTotalUnreadCount();
  if (conversationsListRef.value) {
    conversationsListRef.value.refreshConversations();
  }
};

const handleVideoCallFromChat = (userId: number) => {
  void startCall(userId, selectedChatUserName.value);
};

const joinRoom = () => {
  if (roomIdInput.value.trim()) {
    router.push(`/room/${roomIdInput.value.trim()}`);
  }
};

const onProfileUpdated = () => {
  // Refresh current user data from localStorage (already updated by ProfileEditModal)
};

const viewUserProfile = (userId: number) => {
  viewedUserId.value = userId;
  showUserProfile.value = true;
};

onMounted(async () => {
  // Check if user is authenticated
  if (!authService.isAuthenticated()) {
    router.push('/login');
    return;
  }

  await loadStats();

  // Initialize push notifications
  await pushNotificationService.init();
  if (await pushNotificationService.requestPermission() === 'granted') {
    await pushNotificationService.subscribe();
  }

  // Refresh stats every 30 seconds
  setInterval(loadStats, 30000);
});
</script>

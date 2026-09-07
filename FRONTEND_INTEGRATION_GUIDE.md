# Frontend Integration Guide

## Overview
The backend friendship API is complete and ready. This guide helps you integrate it with the Vue.js frontend components.

## Existing Frontend Components

Based on your open files, you have these Vue components ready:
- ? `client-app/src/components/UserSearch.vue`
- ? `client-app/src/components/FriendRequests.vue`
- ? `client-app/src/components/FriendsList.vue`
- ? `client-app/src/components/Login.vue`

And these services:
- ? `client-app/src/services/auth.service.ts`
- ? `client-app/src/services/friendship.service.ts`

## Backend API Endpoints Available

### Authentication Endpoints
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/register` | Register new user | No |
| POST | `/api/auth/login` | Login and get JWT | No |
| POST | `/api/auth/logout` | Logout user | No |

### Friendship Endpoints
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/friendship/send-request` | Send friend request | Yes |
| POST | `/api/friendship/accept-request/{id}` | Accept friend request | Yes |
| POST | `/api/friendship/reject-request/{id}` | Reject friend request | Yes |
| DELETE | `/api/friendship/remove/{friendId}` | Remove friend | Yes |
| GET | `/api/friendship/list` | Get friends list | Yes |
| GET | `/api/friendship/pending-requests` | Get pending requests | Yes |
| GET | `/api/friendship/sent-requests` | Get sent requests | Yes |
| GET | `/api/friendship/search/{username}` | Search for users | Yes |

## Frontend Service Implementation

### 1. Update `auth.service.ts`

```typescript
import axios from 'axios';
import { config } from '../utils/config';

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface User {
  id: number;
  username: string;
  email: string;
  displayName: string;
  isOnline: boolean;
  profilePictureUrl?: string;
  lastSeen?: string;
}

export interface AuthResponse {
  message: string;
  token: string;
  user: User;
}

class AuthService {
  private readonly apiUrl = `${config.API_BASE_URL}/api/auth`;

  async register(data: RegisterRequest): Promise<{ message: string; userId: number }> {
    const response = await axios.post(`${this.apiUrl}/register`, data);
    return response.data;
  }

  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await axios.post(`${this.apiUrl}/login`, data);
    
    // Store token and user in localStorage
    if (response.data.token) {
      localStorage.setItem('token', response.data.token);
      localStorage.setItem('user', JSON.stringify(response.data.user));
    }
    
    return response.data;
  }

  async logout(): Promise<void> {
    const user = this.getCurrentUser();
    if (user) {
      await axios.post(`${this.apiUrl}/logout`, { userId: user.id });
    }
    
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getCurrentUser(): User | null {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getAuthHeader() {
    const token = this.getToken();
    return token ? { Authorization: `Bearer ${token}` } : {};
  }
}

export default new AuthService();
```

### 2. Update `friendship.service.ts`

```typescript
import axios from 'axios';
import { config } from '../utils/config';
import authService from './auth.service';

export interface Friend {
  id: number;
  username: string;
  displayName: string;
  isOnline: boolean;
  profilePictureUrl?: string;
  lastSeen?: string;
}

export interface FriendRequest {
  id: number;
  sender?: {
    id: number;
    username: string;
    displayName: string;
    profilePictureUrl?: string;
  };
  receiver?: {
    id: number;
    username: string;
    displayName: string;
    profilePictureUrl?: string;
  };
  message?: string;
  createdAt: string;
}

export interface SearchResult {
  user: {
    id: number;
    username: string;
    displayName: string;
    isOnline: boolean;
    profilePictureUrl?: string;
    isFriend: boolean;
    hasPendingRequest: boolean;
  };
}

class FriendshipService {
  private readonly apiUrl = `${config.API_BASE_URL}/api/friendship`;

  private getHeaders() {
    return authService.getAuthHeader();
  }

  async sendFriendRequest(receiverId: number, message?: string): Promise<{ message: string }> {
    const response = await axios.post(
      `${this.apiUrl}/send-request`,
      { receiverId, message },
      { headers: this.getHeaders() }
    );
    return response.data;
  }

  async acceptRequest(requestId: number): Promise<{ message: string }> {
    const response = await axios.post(
      `${this.apiUrl}/accept-request/${requestId}`,
      {},
      { headers: this.getHeaders() }
    );
    return response.data;
  }

  async rejectRequest(requestId: number): Promise<{ message: string }> {
    const response = await axios.post(
      `${this.apiUrl}/reject-request/${requestId}`,
      {},
      { headers: this.getHeaders() }
    );
    return response.data;
  }

  async removeFriend(friendId: number): Promise<{ message: string }> {
    const response = await axios.delete(
      `${this.apiUrl}/remove/${friendId}`,
      { headers: this.getHeaders() }
    );
    return response.data;
  }

  async getFriends(): Promise<Friend[]> {
    const response = await axios.get(
      `${this.apiUrl}/list`,
      { headers: this.getHeaders() }
    );
    return response.data.friends;
  }

  async getPendingRequests(): Promise<FriendRequest[]> {
    const response = await axios.get(
      `${this.apiUrl}/pending-requests`,
      { headers: this.getHeaders() }
    );
    return response.data.requests;
  }

  async getSentRequests(): Promise<FriendRequest[]> {
    const response = await axios.get(
      `${this.apiUrl}/sent-requests`,
      { headers: this.getHeaders() }
    );
    return response.data.requests;
  }

  async searchUser(username: string): Promise<SearchResult> {
    const response = await axios.get(
      `${this.apiUrl}/search/${username}`,
      { headers: this.getHeaders() }
    );
    return response.data;
  }
}

export default new FriendshipService();
```

### 3. Example Vue Component - `FriendsList.vue`

```vue
<template>
  <div class="friends-list">
    <h2>My Friends</h2>
    
    <div v-if="loading" class="loading">Loading...</div>
    
    <div v-else-if="error" class="error">{{ error }}</div>
    
    <div v-else-if="friends.length === 0" class="empty">
      No friends yet. Search for users to add friends!
    </div>
    
    <div v-else class="friends-grid">
      <div 
        v-for="friend in friends" 
        :key="friend.id"
        class="friend-card"
        :class="{ online: friend.isOnline }"
      >
        <img 
          :src="friend.profilePictureUrl || '/default-avatar.png'" 
          :alt="friend.displayName"
          class="avatar"
        />
        
        <div class="friend-info">
          <h3>{{ friend.displayName }}</h3>
          <p class="username">@{{ friend.username }}</p>
          <span class="status" :class="{ online: friend.isOnline }">
            {{ friend.isOnline ? 'Online' : 'Offline' }}
          </span>
        </div>
        
        <div class="actions">
          <button 
            @click="startVideoCall(friend)" 
            class="btn-call"
            :disabled="!friend.isOnline"
          >
            ?? Call
          </button>
          <button 
            @click="removeFriend(friend)" 
            class="btn-remove"
          >
            ? Remove
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import friendshipService, { Friend } from '@/services/friendship.service';

const friends = ref<Friend[]>([]);
const loading = ref(false);
const error = ref('');

const loadFriends = async () => {
  loading.value = true;
  error.value = '';
  
  try {
    friends.value = await friendshipService.getFriends();
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to load friends';
  } finally {
    loading.value = false;
  }
};

const removeFriend = async (friend: Friend) => {
  if (!confirm(`Remove ${friend.displayName} from your friends?`)) return;
  
  try {
    await friendshipService.removeFriend(friend.id);
    friends.value = friends.value.filter(f => f.id !== friend.id);
  } catch (err: any) {
    alert(err.response?.data?.message || 'Failed to remove friend');
  }
};

const startVideoCall = (friend: Friend) => {
  // Implement video call logic
  console.log('Starting call with', friend);
};

onMounted(() => {
  loadFriends();
});
</script>

<style scoped>
.friends-list {
  padding: 20px;
}

.friends-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 20px;
}

.friend-card {
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.friend-card.online {
  border-color: #4caf50;
}

.avatar {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  object-fit: cover;
}

.friend-info {
  flex: 1;
}

.friend-info h3 {
  margin: 0;
  font-size: 18px;
}

.username {
  color: #666;
  margin: 4px 0;
}

.status {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 12px;
  background: #e0e0e0;
}

.status.online {
  background: #4caf50;
  color: white;
}

.actions {
  display: flex;
  gap: 8px;
}

button {
  flex: 1;
  padding: 8px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.btn-call {
  background: #4caf50;
  color: white;
}

.btn-call:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.btn-remove {
  background: #f44336;
  color: white;
}

.loading, .error, .empty {
  text-align: center;
  padding: 40px;
}

.error {
  color: #f44336;
}
</style>
```

### 4. Example Vue Component - `FriendRequests.vue`

```vue
<template>
  <div class="friend-requests">
    <h2>Friend Requests</h2>
    
    <div class="tabs">
      <button 
        @click="activeTab = 'received'" 
        :class="{ active: activeTab === 'received' }"
      >
        Received ({{ pendingRequests.length }})
      </button>
      <button 
        @click="activeTab = 'sent'" 
        :class="{ active: activeTab === 'sent' }"
      >
        Sent ({{ sentRequests.length }})
      </button>
    </div>
    
    <div v-if="loading" class="loading">Loading...</div>
    
    <div v-else-if="activeTab === 'received'">
      <div v-if="pendingRequests.length === 0" class="empty">
        No pending friend requests
      </div>
      
      <div v-else class="requests-list">
        <div 
          v-for="request in pendingRequests" 
          :key="request.id"
          class="request-card"
        >
          <img 
            :src="request.sender.profilePictureUrl || '/default-avatar.png'" 
            :alt="request.sender.displayName"
            class="avatar"
          />
          
          <div class="request-info">
            <h3>{{ request.sender.displayName }}</h3>
            <p class="username">@{{ request.sender.username }}</p>
            <p v-if="request.message" class="message">{{ request.message }}</p>
            <p class="time">{{ formatDate(request.createdAt) }}</p>
          </div>
          
          <div class="actions">
            <button @click="acceptRequest(request)" class="btn-accept">
              ? Accept
            </button>
            <button @click="rejectRequest(request)" class="btn-reject">
              ? Reject
            </button>
          </div>
        </div>
      </div>
    </div>
    
    <div v-else>
      <div v-if="sentRequests.length === 0" class="empty">
        No sent friend requests
      </div>
      
      <div v-else class="requests-list">
        <div 
          v-for="request in sentRequests" 
          :key="request.id"
          class="request-card"
        >
          <img 
            :src="request.receiver.profilePictureUrl || '/default-avatar.png'" 
            :alt="request.receiver.displayName"
            class="avatar"
          />
          
          <div class="request-info">
            <h3>{{ request.receiver.displayName }}</h3>
            <p class="username">@{{ request.receiver.username }}</p>
            <p class="time">Sent {{ formatDate(request.createdAt) }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import friendshipService, { FriendRequest } from '@/services/friendship.service';

const activeTab = ref<'received' | 'sent'>('received');
const pendingRequests = ref<FriendRequest[]>([]);
const sentRequests = ref<FriendRequest[]>([]);
const loading = ref(false);

const loadRequests = async () => {
  loading.value = true;
  
  try {
    const [pending, sent] = await Promise.all([
      friendshipService.getPendingRequests(),
      friendshipService.getSentRequests()
    ]);
    
    pendingRequests.value = pending;
    sentRequests.value = sent;
  } catch (err) {
    console.error('Failed to load requests', err);
  } finally {
    loading.value = false;
  }
};

const acceptRequest = async (request: FriendRequest) => {
  try {
    await friendshipService.acceptRequest(request.id);
    pendingRequests.value = pendingRequests.value.filter(r => r.id !== request.id);
  } catch (err: any) {
    alert(err.response?.data?.message || 'Failed to accept request');
  }
};

const rejectRequest = async (request: FriendRequest) => {
  try {
    await friendshipService.rejectRequest(request.id);
    pendingRequests.value = pendingRequests.value.filter(r => r.id !== request.id);
  } catch (err: any) {
    alert(err.response?.data?.message || 'Failed to reject request');
  }
};

const formatDate = (dateStr: string) => {
  const date = new Date(dateStr);
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
};

onMounted(() => {
  loadRequests();
});
</script>

<style scoped>
/* Add your styles here */
</style>
```

## Integration Checklist

- [ ] Update `auth.service.ts` with backend endpoints
- [ ] Update `friendship.service.ts` with backend endpoints
- [ ] Configure Axios interceptors for auth token
- [ ] Update `Login.vue` to call auth service
- [ ] Update `FriendsList.vue` to display friends
- [ ] Update `FriendRequests.vue` to handle requests
- [ ] Update `UserSearch.vue` to search and add friends
- [ ] Add error handling and loading states
- [ ] Add success/error notifications
- [ ] Test all features end-to-end

## API Configuration

Make sure your `config.ts` has the correct backend URL:

```typescript
export const config = {
  API_BASE_URL: import.meta.env.VITE_API_URL || 'http://localhost:5274',
  WS_URL: import.meta.env.VITE_WS_URL || 'ws://localhost:5274'
};
```

## CORS Configuration

The backend already has CORS enabled for:
- `http://localhost:3000` (Vite dev server)
- `https://localhost:3000`
- `http://localhost:5274`
- `https://localhost:5274`

If you need additional origins, update `Program.cs`.

## Next Steps

1. ? Backend API is ready
2. ?? Update frontend services with API calls
3. ?? Update Vue components to use services
4. ?? Add error handling and loading states
5. ?? Test the complete flow
6. ?? Add real-time notifications via SignalR (optional)

---

**The backend is ready! Now integrate with your frontend components.** ??

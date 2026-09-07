# ?? Frontend Login & Friendship System - Implementation Complete!

## Files Created

### Services
? **client-app/src/services/auth.service.ts** - Authentication service
? **client-app/src/services/friendship.service.ts** - Friendship management service

### Components
? **client-app/src/components/Login.vue** - Registration & Login
? **client-app/src/components/FriendsList.vue** - Friends list with online status
? **client-app/src/components/FriendRequests.vue** - Manage friend requests
? **client-app/src/components/UserSearch.vue** - Search & add friends

---

## Component Overview

### 1. Login.vue
**Purpose:** User registration and login

**Features:**
- ?? Registration form with email validation
- ?? Login form with JWT token storage
- ?? Toggle between login/register tabs
- ?? Demo user quick login buttons
- ? Form validation & error messages

**Props:** None
**Emits:** None (navigates to home on successful login)

---

### 2. FriendsList.vue
**Purpose:** Display and manage friends

**Features:**
- ?? Shows all friends with avatar
- ?? Online/Offline status indicator
- ? Last seen timestamp
- ?? Call button (only when online)
- ??? Remove friend with confirmation
- ?? Refresh friends list

**Props:** None
**Emits:** 
- `callFriend(friendId, friendName)` - When call button clicked

---

### 3. FriendRequests.vue
**Purpose:** Manage incoming and outgoing friend requests

**Features:**
- ?? Display pending incoming requests
- ?? Show optional message from requester
- ? Accept friend requests
- ? Reject friend requests
- ?? Show sent requests status
- ? Time stamps for all requests

**Props:** None
**Emits:** None

---

### 4. UserSearch.vue
**Purpose:** Search for users and send friend requests

**Features:**
- ?? Search users by username
- ?? Display user profile
- ?? Show friendship status
- ? Send friend request with optional message
- ? Success/error notifications
- ?? Message field for friend requests

**Props:** None
**Emits:** None

---

## Services

### AuthService
```typescript
// Methods
register(username, email, password): Promise<{success, message, userId}>
login(email, password): Promise<{success, message}>
logout(): Promise<void>
getToken(): string | null
getUser(): AuthUser | null
isAuthenticated(): boolean
isLoggedIn(): boolean
```

**Storage:**
- JWT token in localStorage as `auth_token`
- User data in localStorage as `current_user`

---

### FriendshipService
```typescript
// Methods
sendFriendRequest(receiverId, message): Promise<{success, message}>
acceptFriendRequest(requestId): Promise<{success, message}>
rejectFriendRequest(requestId): Promise<{success, message}>
getFriends(): Promise<Friend[]>
getPendingRequests(): Promise<FriendRequest[]>
getSentRequests(): Promise<SentRequest[]>
removeFriend(friendId): Promise<{success, message}>
searchUser(username): Promise<user>
```

**All methods include:**
- ? Error handling
- ? JWT token authorization
- ? Logging on errors
- ? Type safety

---

## Integration Steps

### Step 1: Update Your Router

Add routes to your `router/index.ts`:

```typescript
import Login from '@/components/Login.vue'
import Home from '@/views/Home.vue'
import FriendRequests from '@/components/FriendRequests.vue'
import UserSearch from '@/components/UserSearch.vue'

const routes = [
  {
    path: '/login',
    component: Login,
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: Home,
    meta: { requiresAuth: true }
  },
  {
    path: '/friend-requests',
    component: FriendRequests,
    meta: { requiresAuth: true }
  },
  {
    path: '/search',
    component: UserSearch,
    meta: { requiresAuth: true }
  }
]

// Add route guard
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('auth_token')
  const requiresAuth = to.meta.requiresAuth

  if (requiresAuth && !token) {
    next('/login')
  } else if (to.path === '/login' && token) {
    next('/')
  } else {
    next()
  }
})
```

### Step 2: Create Home Component

Create `client-app/src/views/Home.vue`:

```vue
<template>
  <div class="min-h-screen bg-gray-100 p-6">
    <!-- Header -->
    <div class="max-w-7xl mx-auto">
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-4xl font-bold text-gray-800">Welcome, {{ currentUser?.displayName }}</h1>
          <p class="text-gray-600">Stay connected with your friends</p>
        </div>
        <button
          @click="logout"
          class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg"
        >
          Logout
        </button>
      </div>

      <!-- Main Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Friends List (Main) -->
        <div class="lg:col-span-2">
          <FriendsList @callFriend="handleCallFriend" />
        </div>

        <!-- Sidebar -->
        <div class="space-y-6">
          <!-- Friend Requests -->
          <div class="bg-white rounded-xl shadow-lg p-4">
            <h3 class="font-bold text-gray-800 mb-3">Friend Requests</h3>
            <!-- Show preview of requests -->
          </div>

          <!-- Quick Stats -->
          <div class="bg-white rounded-xl shadow-lg p-4">
            <div class="text-center">
              <p class="text-3xl font-bold text-blue-600">{{ friendsCount }}</p>
              <p class="text-gray-600">Friends Online</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Bottom Tabs -->
      <div class="mt-8 grid grid-cols-1 lg:grid-cols-2 gap-6">
        <UserSearch />
        <FriendRequests />
      </div>
    </div>

    <!-- Video Chat Modal (if calling) -->
    <VideoChat v-if="callingFriend" :friend="callingFriend" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { authService } from '@/services/auth.service'
import { friendshipService } from '@/services/friendship.service'
import FriendsList from '@/components/FriendsList.vue'
import FriendRequests from '@/components/FriendRequests.vue'
import UserSearch from '@/components/UserSearch.vue'
import VideoChat from '@/views/VideoChat.vue'

const router = useRouter()
const currentUser = computed(() => authService.getUser())
const friends = ref<any[]>([])
const callingFriend = ref<any>(null)

const friendsCount = computed(() => {
  return friends.value.filter(f => f.isOnline).length
})

const logout = async () => {
  await authService.logout()
  router.push('/login')
}

const handleCallFriend = (friendId: number, friendName: string) => {
  callingFriend.value = { id: friendId, name: friendName }
}

onMounted(async () => {
  friends.value = await friendshipService.getFriends()
})
</script>
```

### Step 3: Update App.vue

```vue
<template>
  <router-view />
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { authService } from '@/services/auth.service'
import { onMounted } from 'vue'

const router = useRouter()

onMounted(() => {
  // Redirect to login if not authenticated
  if (!authService.isLoggedIn() && router.currentRoute.value.path !== '/login') {
    router.push('/login')
  }
})
</script>
```

---

## Usage Example

### Login Flow
```typescript
// User enters email and password
const result = await authService.login('alice@example.com', 'Pass123')

if (result.success) {
  // Token stored automatically
  // Redirect to home
  router.push('/')
}
```

### Add Friend Flow
```typescript
// User searches for friend
const user = await friendshipService.searchUser('bob')

// User sends friend request
const result = await friendshipService.sendFriendRequest(user.id, 'Let\'s connect!')

if (result.success) {
  console.log('Request sent!')
}
```

### Get Friends List
```typescript
const friends = await friendshipService.getFriends()

// Filter online friends
const onlineFriends = friends.filter(f => f.isOnline)

// Call online friend
if (onlineFriends.length > 0) {
  callFriend(onlineFriends[0].id)
}
```

---

## Styling Notes

All components use **Tailwind CSS** classes. Make sure Tailwind is installed and configured:

```bash
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init
```

---

## Security Notes

? **JWT Tokens:**
- Stored in localStorage
- Included in all API requests
- Expires after 24 hours

? **Password Handling:**
- Never stored in localStorage
- Sent only over HTTPS
- Hashed on backend

?? **To Remember:**
- Always use HTTPS in production
- Implement token refresh
- Add logout timer
- Validate tokens on route access

---

## Testing

### Register Demo Users
```bash
# User 1
Username: alice
Email: alice@example.com
Password: Pass123

# User 2
Username: bob
Email: bob@example.com
Password: Pass123
```

### Test Flow
1. Register Alice
2. Register Bob
3. Login as Alice
4. Search for "bob"
5. Send friend request
6. Login as Bob
7. Accept friend request from Alice
8. See Alice in friends list
9. Click call button to start video chat

---

## Files Structure

```
client-app/
??? src/
?   ??? services/
?   ?   ??? auth.service.ts ?
?   ?   ??? friendship.service.ts ?
?   ?   ??? webrtc.service.ts (existing)
?   ?   ??? signalr.service.ts (existing)
?   ??? components/
?   ?   ??? Login.vue ?
?   ?   ??? FriendsList.vue ?
?   ?   ??? FriendRequests.vue ?
?   ?   ??? UserSearch.vue ?
?   ?   ??? VideoGrid.vue (existing)
?   ?   ??? ...
?   ??? views/
?   ?   ??? Home.vue (TO CREATE)
?   ?   ??? VideoChat.vue (existing)
?   ?   ??? ...
?   ??? router/
?   ?   ??? index.ts (UPDATE)
?   ??? App.vue (UPDATE)
?   ??? main.ts
??? ...
```

---

## Next Steps

1. ? Update router configuration
2. ? Create Home.vue view
3. ? Update App.vue entry
4. ? Install Tailwind CSS (if not already)
5. ? Test login flow
6. ? Test friendship features
7. ? Integrate with video chat

---

## API Endpoints Used

All endpoints are called automatically by services:

```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/friendship/send-request
POST   /api/friendship/accept-request/:id
POST   /api/friendship/reject-request/:id
GET    /api/friendship/list
GET    /api/friendship/pending-requests
GET    /api/friendship/sent-requests
GET    /api/friendship/search/:username
DELETE /api/friendship/remove/:id
```

---

## Troubleshooting

### "No token found"
- Make sure login is successful
- Check localStorage for `auth_token`
- Clear localStorage and re-login

### "404 API endpoints not found"
- Ensure backend is running
- Check backend services are registered
- Verify database migrations ran

### "Friends list empty"
- Make sure you're friends with someone
- Try accepting a friend request
- Check friend requests page

---

**Frontend implementation is complete! Ready to integrate with your video chat!** ??

See backend docs for API details: `FRIENDSHIP_SYSTEM_DOCS.md`

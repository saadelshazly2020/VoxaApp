# ? QUICK START - Frontend Login & Friends System

## What Was Created ?

### Services (2 files)
- `client-app/src/services/auth.service.ts` - Authentication
- `client-app/src/services/friendship.service.ts` - Friend management

### Components (4 files)
- `client-app/src/components/Login.vue` - Login/Register
- `client-app/src/components/FriendsList.vue` - Show friends
- `client-app/src/components/FriendRequests.vue` - Manage requests
- `client-app/src/components/UserSearch.vue` - Find & add friends

---

## 1. Setup Routes

Create or update `client-app/src/router/index.ts`:

```typescript
import { createRouter, createWebHistory } from 'vue-router'
import Login from '@/components/Login.vue'
import Home from '@/views/Home.vue'

const routes = [
  { path: '/login', component: Login, meta: { requiresAuth: false } },
  { path: '/', component: Home, meta: { requiresAuth: true } }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('auth_token')
  if (to.meta.requiresAuth && !token) {
    next('/login')
  } else {
    next()
  }
})

export default router
```

---

## 2. Create Home Component

Create `client-app/src/views/Home.vue`:

```vue
<template>
  <div class="p-6 max-w-6xl mx-auto">
    <div class="flex justify-between items-center mb-8">
      <h1 class="text-3xl font-bold">Welcome, {{ currentUser?.username }}</h1>
      <button @click="logout" class="px-4 py-2 bg-red-600 text-white rounded">
        Logout
      </button>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-2">
        <FriendsList />
      </div>
      <div>
        <FriendRequests />
      </div>
    </div>

    <div class="mt-6">
      <UserSearch />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { authService } from '@/services/auth.service'
import FriendsList from '@/components/FriendsList.vue'
import FriendRequests from '@/components/FriendRequests.vue'
import UserSearch from '@/components/UserSearch.vue'

const router = useRouter()
const currentUser = computed(() => authService.getUser())

const logout = async () => {
  await authService.logout()
  router.push('/login')
}
</script>
```

---

## 3. Update App.vue

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
  if (!authService.isLoggedIn() && router.currentRoute.value.path !== '/login') {
    router.push('/login')
  }
})
</script>
```

---

## 4. Install Dependencies

```bash
cd client-app
npm install
# Tailwind should already be installed
```

---

## 5. Run Application

**Terminal 1 - Backend:**
```bash
dotnet run
```

**Terminal 2 - Frontend (dev):**
```bash
cd client-app
npm run dev
```

---

## 6. Test Login

Open browser:
```
http://localhost:3000
```

### Demo Users:
```
Email: alice@example.com
Password: Pass123

Email: bob@example.com
Password: Pass123
```

Or click **"Demo Users"** buttons on login page.

---

## 7. Test Features

### After Login:
- ? See friends list (if you have friends)
- ? Click **UserSearch** tab to find users
- ? Send friend requests
- ? View pending requests
- ? Accept/Reject requests
- ? Call online friends

---

## Architecture

```
User Interface (Vue)
       ?
Services (TypeScript)
  - AuthService
  - FriendshipService
       ?
API Endpoints (.NET)
  - /api/auth/*
  - /api/friendship/*
       ?
Database (SQLite)
  - Users
  - Friendships
  - FriendshipRequests
```

---

## Key Features

? **Authentication**
- Register new users
- Login with JWT token
- Automatic token storage
- Logout functionality

? **Friends Management**
- View all friends with online status
- Search for users
- Send friend requests
- Accept/Reject requests
- Remove friends

? **User Interface**
- Modern, responsive design
- Tailwind CSS styling
- Loading states
- Error messages
- Success notifications

---

## File Checklist

- [ ] Services created (auth.service.ts, friendship.service.ts)
- [ ] Components created (Login, FriendsList, FriendRequests, UserSearch)
- [ ] Router configured (index.ts)
- [ ] Home.vue created
- [ ] App.vue updated
- [ ] Backend running (`dotnet run`)
- [ ] Frontend running (`npm run dev`)
- [ ] Database exists (`app.db`)

---

## Common Issues

### "Cannot find module"
- Make sure all 4 service/component files exist
- Check file paths are correct

### "401 Unauthorized"
- Make sure you're logged in
- Check token is in localStorage
- Try login again

### "Friends list empty"
- Add some friends first
- Try accepting a friend request

### "Cannot access backend"
- Make sure backend is running on port 5274
- Check CORS is enabled

---

## API Verification

Quick test in browser console:

```javascript
// Get token
const token = localStorage.getItem('auth_token')

// Test endpoint
fetch('/api/friendship/list', {
  headers: { 'Authorization': `Bearer ${token}` }
})
.then(r => r.json())
.then(data => console.log(data))
```

---

## Next Steps

1. ? Backend services running
2. ? Frontend components in place
3. ? Routes configured
4. ? Login working
5. Next: Integrate video chat with friend list

---

**Frontend implementation complete!** ??

**Status:** Ready to test the complete login & friendship system!

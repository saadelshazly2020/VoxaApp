import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import './assets/styles/main.css'
import { authService } from './services/auth.service'

// Import views
import Login from './components/Login.vue'
import Dashboard from './views/Dashboard.vue'
import VideoChat from './views/VideoChat.vue'
import Room from './views/Room.vue'

// Create router
const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: Login,
      meta: { requiresGuest: true }
    },
    {
      path: '/',
      name: 'Dashboard',
      component: Dashboard,
      meta: { requiresAuth: true }
    },
    {
      path: '/video-chat',
      name: 'VideoChat',
      component: VideoChat,
      meta: { requiresAuth: true }
    },
    {
      path: '/room/:roomId',
      name: 'Room',
      component: Room,
      props: true,
      meta: { requiresAuth: true }
    }
  ]
})

// Navigation guards
router.beforeEach((to, _from, next) => {
  const isAuthenticated = authService.isAuthenticated()
  
  // If route requires auth and user is not authenticated
  if (to.meta.requiresAuth && !isAuthenticated) {
    next('/login')
  }
  // If route is for guests only and user is authenticated
  else if (to.meta.requiresGuest && isAuthenticated) {
    next('/')
  }
  // Otherwise, allow navigation
  else {
    next()
  }
})

// Create and mount app
const app = createApp(App)
app.use(router)
app.mount('#app')

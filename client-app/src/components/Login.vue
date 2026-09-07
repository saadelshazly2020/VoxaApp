<template>
  <div class="min-h-screen bg-gradient-to-br from-blue-500 via-purple-500 to-pink-500 flex items-center justify-center p-4">
    <div class="bg-white rounded-2xl shadow-2xl max-w-md w-full p-8">
      <!-- Logo/Title -->
      <div class="text-center mb-8">
        <h1 class="text-4xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
          VideoChat
        </h1>
        <p class="text-gray-600 mt-2">Connect with friends instantly</p>
      </div>

      <!-- Tab Navigation -->
      <div class="flex gap-4 mb-6">
        <button
          @click="activeTab = 'login'"
          :class="[
            'flex-1 py-2 px-4 rounded-lg font-semibold transition-all',
            activeTab === 'login'
              ? 'bg-blue-600 text-white shadow-lg'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          ]"
        >
          Login
        </button>
        <button
          @click="activeTab = 'register'"
          :class="[
            'flex-1 py-2 px-4 rounded-lg font-semibold transition-all',
            activeTab === 'register'
              ? 'bg-blue-600 text-white shadow-lg'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          ]"
        >
          Register
        </button>
      </div>

      <!-- Error Message -->
      <div v-if="errorMessage" class="mb-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded-lg">
        {{ errorMessage }}
      </div>

      <!-- Success Message -->
      <div v-if="successMessage" class="mb-4 p-4 bg-green-100 border border-green-400 text-green-700 rounded-lg">
        {{ successMessage }}
      </div>

      <!-- Login Form -->
      <form v-if="activeTab === 'login'" @submit.prevent="handleLogin" class="space-y-4">
        <div>
          <label class="block text-gray-700 font-semibold mb-2">Email</label>
          <input
            v-model="loginForm.email"
            type="email"
            placeholder="your@email.com"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            required
          />
        </div>

        <div>
          <label class="block text-gray-700 font-semibold mb-2">Password</label>
          <input
            v-model="loginForm.password"
            type="password"
            placeholder="Enter your password"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            required
          />
        </div>

        <button
          type="submit"
          :disabled="loading"
          class="w-full bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold py-2 rounded-lg hover:shadow-lg transition-all disabled:opacity-50"
        >
          {{ loading ? 'Logging in...' : 'Login' }}
        </button>
      </form>

      <!-- Register Form -->
      <form v-if="activeTab === 'register'" @submit.prevent="handleRegister" class="space-y-4">
        <div>
          <label class="block text-gray-700 font-semibold mb-2">Username</label>
          <input
            v-model="registerForm.username"
            type="text"
            placeholder="john_doe"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            required
          />
        </div>

        <div>
          <label class="block text-gray-700 font-semibold mb-2">Email</label>
          <input
            v-model="registerForm.email"
            type="email"
            placeholder="your@email.com"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            required
          />
        </div>

        <div>
          <label class="block text-gray-700 font-semibold mb-2">Password</label>
          <input
            v-model="registerForm.password"
            type="password"
            placeholder="Choose a password"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            required
          />
        </div>

        <div>
          <label class="block text-gray-700 font-semibold mb-2">Confirm Password</label>
          <input
            v-model="registerForm.confirmPassword"
            type="password"
            placeholder="Confirm your password"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            required
          />
        </div>

        <button
          type="submit"
          :disabled="loading"
          class="w-full bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold py-2 rounded-lg hover:shadow-lg transition-all disabled:opacity-50"
        >
          {{ loading ? 'Registering...' : 'Register' }}
        </button>
      </form>

      <!-- Demo Users -->
      <div class="mt-6 pt-6 border-t border-gray-200">
        <p class="text-sm text-gray-600 mb-3 font-semibold">Demo Users:</p>
        <div class="space-y-2 text-xs">
          <button
            @click="demoLogin('alice@example.com', 'Pass123')"
            class="w-full text-left px-3 py-2 bg-gray-100 hover:bg-gray-200 rounded text-gray-700 transition"
          >
            ?? alice@example.com / Pass123
          </button>
          <button
            @click="demoLogin('bob@example.com', 'Pass123')"
            class="w-full text-left px-3 py-2 bg-gray-100 hover:bg-gray-200 rounded text-gray-700 transition"
          >
            ?? bob@example.com / Pass123
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { authService } from '@/services/auth.service';

const router = useRouter();
const activeTab = ref<'login' | 'register'>('login');
const loading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

const loginForm = ref({
  email: '',
  password: ''
});

const registerForm = ref({
  username: '',
  email: '',
  password: '',
  confirmPassword: ''
});

const handleLogin = async () => {
  errorMessage.value = '';
  successMessage.value = '';
  loading.value = true;

  try {
    if (!loginForm.value.email || !loginForm.value.password) {
      errorMessage.value = 'Please fill in all fields';
      return;
    }

    const result = await authService.login(loginForm.value.email, loginForm.value.password);

    if (result.success) {
      successMessage.value = result.message;
      setTimeout(() => {
        router.push('/');
      }, 1500);
    } else {
      errorMessage.value = result.message || 'Login failed';
    }
  } catch (error) {
    errorMessage.value = 'An error occurred during login';
    console.error(error);
  } finally {
    loading.value = false;
  }
};

const handleRegister = async () => {
  errorMessage.value = '';
  successMessage.value = '';
  loading.value = true;

  try {
    if (!registerForm.value.username || !registerForm.value.email || !registerForm.value.password) {
      errorMessage.value = 'Please fill in all fields';
      return;
    }

    if (registerForm.value.password !== registerForm.value.confirmPassword) {
      errorMessage.value = 'Passwords do not match';
      return;
    }

    if (registerForm.value.password.length < 6) {
      errorMessage.value = 'Password must be at least 6 characters';
      return;
    }

    const result = await authService.register(
      registerForm.value.username,
      registerForm.value.email,
      registerForm.value.password
    );

    if (result.success) {
      successMessage.value = result.message + '. Please login now.';
      registerForm.value = {
        username: '',
        email: '',
        password: '',
        confirmPassword: ''
      };
      setTimeout(() => {
        activeTab.value = 'login';
        successMessage.value = '';
      }, 2000);
    } else {
      errorMessage.value = result.message || 'Registration failed';
    }
  } catch (error) {
    errorMessage.value = 'An error occurred during registration';
    console.error(error);
  } finally {
    loading.value = false;
  }
};

const demoLogin = async (email: string, password: string) => {
  loginForm.value.email = email;
  loginForm.value.password = password;
  await handleLogin();
};
</script>

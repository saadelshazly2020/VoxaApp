<template>
  <div v-if="show" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="$emit('close')">
    <div class="absolute inset-0 bg-black/50" />
    <div class="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
      <!-- Header -->
      <div class="sticky top-0 bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between rounded-t-2xl">
        <h2 class="text-xl font-bold text-gray-800">Edit Profile</h2>
        <button @click="$emit('close')" class="p-2 hover:bg-gray-100 rounded-lg transition">
          <svg class="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="p-6 space-y-6">
        <!-- Profile Picture -->
        <div class="flex flex-col items-center gap-4">
          <div class="relative group">
            <div class="w-24 h-24 rounded-full overflow-hidden bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white text-3xl font-bold">
              <img v-if="previewUrl || form.profilePictureUrl" :src="previewUrl || form.profilePictureUrl" class="w-full h-full object-cover" />
              <span v-else>{{ initial }}</span>
            </div>
            <label class="absolute inset-0 bg-black/40 rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition cursor-pointer">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              <input type="file" accept="image/*" class="hidden" @change="handleImageSelect" />
            </label>
          </div>
          <p class="text-xs text-gray-400">Click to change photo</p>
        </div>

        <!-- Display Name -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 mb-1.5">Display Name</label>
          <input
            v-model="form.displayName"
            type="text"
            maxlength="50"
            placeholder="Your display name"
            class="w-full px-4 py-2.5 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
          />
        </div>

        <!-- Bio -->
        <div>
          <label class="block text-sm font-semibold text-gray-700 mb-1.5">Bio</label>
          <textarea
            v-model="form.bio"
            maxlength="500"
            rows="3"
            placeholder="Tell us about yourself..."
            class="w-full px-4 py-2.5 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition resize-none"
          />
          <p class="text-xs text-gray-400 mt-1 text-right">{{ (form.bio || '').length }}/500</p>
        </div>

        <!-- Error -->
        <div v-if="error" class="p-3 bg-red-50 border border-red-200 text-red-700 text-sm rounded-xl">
          {{ error }}
        </div>

        <!-- Success -->
        <div v-if="success" class="p-3 bg-green-50 border border-green-200 text-green-700 text-sm rounded-xl">
          {{ success }}
        </div>

        <!-- Actions -->
        <div class="flex gap-3">
          <button
            @click="$emit('close')"
            class="flex-1 py-2.5 border border-gray-200 text-gray-700 font-semibold rounded-xl hover:bg-gray-50 transition"
          >
            Cancel
          </button>
          <button
            @click="save"
            :disabled="saving"
            class="flex-1 py-2.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold rounded-xl hover:shadow-lg transition disabled:opacity-50"
          >
            {{ saving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, watch, computed } from 'vue';
import { authService } from '@/services/auth.service';
import { profileService } from '@/services/profile.service';
import { uploadService } from '@/services/upload.service';

const props = defineProps<{ show: boolean }>();
const emit = defineEmits<{ close: []; updated: [] }>();

const user = computed(() => authService.getUser());
const initial = computed(() => (user.value?.displayName || user.value?.username || '?')[0].toUpperCase());

const form = reactive({
  displayName: '',
  bio: '',
  profilePictureUrl: ''
});

const previewUrl = ref('');
const selectedFile = ref<File | null>(null);
const saving = ref(false);
const error = ref('');
const success = ref('');

watch(() => props.show, async (visible) => {
  if (visible) {
    error.value = '';
    success.value = '';
    selectedFile.value = null;
    previewUrl.value = '';

    const profile = await profileService.getMyProfile();
    if (profile) {
      form.displayName = profile.displayName || '';
      form.bio = profile.bio || '';
      form.profilePictureUrl = profile.profilePictureUrl || '';
    }
  }
});

const handleImageSelect = (event: Event) => {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;

  if (file.size > 5 * 1024 * 1024) {
    error.value = 'Image must be under 5 MB';
    return;
  }

  selectedFile.value = file;
  previewUrl.value = URL.createObjectURL(file);
  input.value = '';
};

const save = async () => {
  saving.value = true;
  error.value = '';
  success.value = '';

  try {
    // Upload image if selected
    if (selectedFile.value) {
      const results = await uploadService.uploadImages([selectedFile.value]);
      if (results.length > 0) {
        form.profilePictureUrl = results[0].url;
      }
    }

    const result = await profileService.updateMyProfile({
      displayName: form.displayName || undefined,
      bio: form.bio,
      profilePictureUrl: form.profilePictureUrl || undefined
    });

    if (result.success && result.user) {
      // Update local user data
      const currentUser = authService.getUser();
      if (currentUser) {
        const updated = { ...currentUser, ...result.user };
        localStorage.setItem('current_user', JSON.stringify(updated));
      }
      success.value = 'Profile updated!';
      emit('updated');
      setTimeout(() => emit('close'), 800);
    } else {
      error.value = result.message;
    }
  } catch {
    error.value = 'Failed to save profile';
  }

  saving.value = false;
};
</script>

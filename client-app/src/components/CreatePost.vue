<template>
  <div class="bg-white rounded-xl shadow-lg p-6 mb-6">
    <div class="flex items-start gap-4">
      <!-- User Avatar -->
      <div class="flex-shrink-0">
        <div class="w-12 h-12 bg-gradient-to-br from-blue-400 to-purple-500 rounded-full flex items-center justify-center text-white font-bold">
          {{ currentUser?.username?.[0]?.toUpperCase() }}
        </div>
      </div>

      <!-- Post Input -->
      <div class="flex-1">
        <textarea
          v-model="postContent"
          placeholder="What's on your mind?"
          rows="3"
          class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
          :disabled="isSubmitting"
        ></textarea>

        <!-- Image URL Input (optional) -->
        <div v-if="showImageInput" class="mt-3">
          <input
            v-model="imageUrl"
            type="text"
            placeholder="Image URL (optional)"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            :disabled="isSubmitting"
          />
          <button
            @click="showImageInput = false"
            class="mt-2 text-sm text-gray-500 hover:text-gray-700"
            :disabled="isSubmitting"
          >
            Remove image
          </button>
        </div>

        <!-- Actions -->
        <div class="flex items-center justify-between mt-4">
          <div class="flex items-center gap-2">
            <button
              v-if="!showImageInput"
              @click="showImageInput = true"
              class="p-2 text-gray-600 hover:bg-gray-100 rounded-lg transition"
              title="Add image"
              :disabled="isSubmitting"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </button>
          </div>

          <button
            @click="handleSubmit"
            :disabled="!canSubmit"
            class="px-6 py-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-semibold rounded-lg hover:shadow-lg transition disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isSubmitting ? 'Posting...' : 'Post' }}
          </button>
        </div>

        <!-- Error Message -->
        <div v-if="errorMessage" class="mt-3 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {{ errorMessage }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { authService } from '@/services/auth.service';
import { postsService, type Post } from '@/services/posts.service';

const emit = defineEmits<{
  'post-created': [post: Post]
}>();

const currentUser = computed(() => authService.getUser());
const postContent = ref('');
const imageUrl = ref('');
const showImageInput = ref(false);
const isSubmitting = ref(false);
const errorMessage = ref('');

const canSubmit = computed(() => {
  return postContent.value.trim().length > 0 && 
         postContent.value.trim().length <= 2000 && 
         !isSubmitting.value;
});

const handleSubmit = async () => {
  if (!canSubmit.value) return;

  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    const result = await postsService.createPost(
      postContent.value.trim(),
      imageUrl.value.trim() ? [imageUrl.value.trim()] : undefined
    );

    if (result.success && result.post) {
      emit('post-created', result.post);
      
      postContent.value = '';
      imageUrl.value = '';
      showImageInput.value = false;
    } else {
      errorMessage.value = result.message || 'Failed to create post';
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to create post';
  } finally {
    isSubmitting.value = false;
  }
};
</script>

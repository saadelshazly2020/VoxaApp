<template>
  <div class="space-y-4">
    <!-- Create Post Card -->
    <div class="bg-white rounded-xl shadow-lg p-5">
      <div class="flex items-start gap-3">
        <div class="w-10 h-10 bg-gradient-to-br from-blue-400 to-purple-500 rounded-full flex items-center justify-center text-white font-bold flex-shrink-0">
          {{ currentUser?.username?.[0]?.toUpperCase() }}
        </div>
        <div class="flex-1">
          <textarea
            v-model="newPostContent"
            placeholder="What's on your mind?"
            rows="3"
            maxlength="2000"
            class="w-full resize-none rounded-xl border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white transition"
            @keydown.ctrl.enter="submitPost"
          />

          <!-- Image Previews -->
          <div v-if="imagePreviews.length > 0" class="flex gap-2 mt-2 flex-wrap">
            <div v-for="(preview, idx) in imagePreviews" :key="idx" class="relative group">
              <img :src="preview" class="w-20 h-20 object-cover rounded-lg border border-gray-200" />
              <button
                @click="removeImage(idx)"
                class="absolute -top-1.5 -right-1.5 w-5 h-5 bg-red-500 text-white rounded-full text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition"
              >&times;</button>
            </div>
          </div>

          <div class="flex items-center justify-between mt-2">
            <div class="flex items-center gap-2">
              <label class="cursor-pointer p-1.5 text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition" title="Add image">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                <input
                  type="file"
                  accept="image/*"
                  multiple
                  class="hidden"
                  @change="handleImageSelect"
                  ref="fileInputRef"
                />
              </label>
              <span class="text-xs text-gray-400">{{ newPostContent.length }}/2000 | Ctrl+Enter to post</span>
            </div>
            <button
              @click="submitPost"
              :disabled="(!newPostContent.trim() && selectedImages.length === 0) || posting"
              class="px-5 py-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white text-sm font-semibold rounded-lg hover:shadow-md transition disabled:opacity-50 disabled:cursor-not-allowed"
            >
            {{ posting ? 'Posting...' : 'Post' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-10">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600"></div>
    </div>

    <!-- Empty state -->
    <div v-else-if="posts.length === 0" class="bg-white rounded-xl shadow-lg p-12 text-center">
      <svg class="mx-auto h-14 w-14 text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z" />
      </svg>
      <p class="text-gray-500 font-semibold">No posts yet</p>
      <p class="text-gray-400 text-sm mt-1">Be the first to post something!</p>
    </div>

    <!-- Post cards -->
    <PostCard
      v-for="post in posts"
      :key="post.id"
      :post="post"
      @deleted="handlePostDeleted"
      @updated="handlePostUpdated"
      @reaction-changed="handleReactionChanged"
      @comment-added="handleCommentAdded"
      @comment-deleted="handleCommentDeleted"
    />

    <!-- Load more -->
    <div v-if="hasMore && !loading" class="flex justify-center">
      <button
        @click="loadMore"
        :disabled="loadingMore"
        class="px-6 py-2 bg-white border border-gray-200 text-gray-600 text-sm font-semibold rounded-full hover:bg-gray-50 shadow transition disabled:opacity-50"
      >
        {{ loadingMore ? 'Loading...' : 'Load more' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { postsService, type Post } from '@/services/posts.service';
import { authService } from '@/services/auth.service';
import { uploadService } from '@/services/upload.service';
import { signalRManager } from '@/services/signalr-manager';
import type { SignalRService } from '@/services/signalr.service';
import PostCard from './PostCard.vue';

const PAGE_SIZE = 20;

const posts = ref<Post[]>([]);
const loading = ref(true);
const loadingMore = ref(false);
const posting = ref(false);
const newPostContent = ref('');
const hasMore = ref(true);
const currentUser = computed(() => authService.getUser());
let signalRService: SignalRService | null = null;

// Image upload state
const fileInputRef = ref<HTMLInputElement>();
const selectedImages = ref<File[]>([]);
const imagePreviews = ref<string[]>([]);

// ?? Load ???????????????????????????????????????????????????????????????????
const loadFeed = async () => {
  loading.value = true;
  const data = await postsService.getFeed(0, PAGE_SIZE);
  posts.value = data;
  hasMore.value = data.length === PAGE_SIZE;
  loading.value = false;
};

const loadMore = async () => {
  loadingMore.value = true;
  const data = await postsService.getFeed(posts.value.length, PAGE_SIZE);
  posts.value.push(...data);
  hasMore.value = data.length === PAGE_SIZE;
  loadingMore.value = false;
};

// Create post
const submitPost = async () => {
  if ((!newPostContent.value.trim() && selectedImages.value.length === 0) || posting.value) return;
  posting.value = true;

  try {
    // Upload images first
    let imageUrls: string[] = [];
    if (selectedImages.value.length > 0) {
      const results = await uploadService.uploadImages(selectedImages.value);
      imageUrls = results.map(r => r.url);
    }

    const result = await postsService.createPost(newPostContent.value.trim(), imageUrls);
    if (result.success && result.post) {
      posts.value.unshift(result.post);
      newPostContent.value = '';
      clearImageSelection();
    }
  } catch (error) {
    console.error('Failed to create post:', error);
  }
  posting.value = false;
};

const handleImageSelect = (event: Event) => {
  const input = event.target as HTMLInputElement;
  if (!input.files) return;

  const newFiles = Array.from(input.files).slice(0, 4 - selectedImages.value.length);
  for (const file of newFiles) {
    if (file.size > 10 * 1024 * 1024) {
      alert(`${file.name} exceeds 10 MB limit`);
      continue;
    }
    selectedImages.value.push(file);
    imagePreviews.value.push(URL.createObjectURL(file));
  }
  input.value = '';
};

const removeImage = (index: number) => {
  URL.revokeObjectURL(imagePreviews.value[index]);
  selectedImages.value.splice(index, 1);
  imagePreviews.value.splice(index, 1);
};

const clearImageSelection = () => {
  imagePreviews.value.forEach(url => URL.revokeObjectURL(url));
  selectedImages.value = [];
  imagePreviews.value = [];
};

// ?? Event handlers from PostCard ???????????????????????????????????????????
const handlePostDeleted = (postId: number) => {
  posts.value = posts.value.filter(p => p.id !== postId);
};

const handlePostUpdated = (updated: Post) => {
  const idx = posts.value.findIndex(p => p.id === updated.id);
  if (idx !== -1) posts.value[idx] = updated;
};

const handleReactionChanged = (postId: number, myReaction: string | null, reactionCounts: Record<string, number>, totalReactions: number) => {
  const post = posts.value.find(p => p.id === postId);
  if (post) {
    post.myReaction = myReaction;
    post.reactionCounts = reactionCounts;
    post.totalReactions = totalReactions;
  }
};

const handleCommentAdded = (postId: number, comment: any) => {
  const post = posts.value.find(p => p.id === postId);
  if (post) {
    post.comments.push(comment);
    post.totalComments++;
  }
};

const handleCommentDeleted = (postId: number, commentId: number) => {
  const post = posts.value.find(p => p.id === postId);
  if (post) {
    post.comments = post.comments.filter(c => c.id !== commentId);
    post.totalComments = Math.max(0, post.totalComments - 1);
  }
};

// SignalR real-time handlers
const onNewPost = (post: Post) => {
  if (post.authorId !== currentUser.value?.id) {
    posts.value.unshift(post);
  }
};

const onPostUpdated = (updated: Post) => {
  const idx = posts.value.findIndex(p => p.id === updated.id);
  if (idx !== -1) posts.value[idx] = { ...posts.value[idx], ...updated };
};

const onPostDeleted = (postId: number) => {
  posts.value = posts.value.filter(p => p.id !== postId);
};

const onPostReactionUpdated = (payload: { postId: number; reaction: any }) => {
  const post = posts.value.find(p => p.id === payload.postId);
  if (post) {
    const type: string = payload.reaction.type;
    post.reactionCounts[type] = (post.reactionCounts[type] ?? 0) + 1;
    post.totalReactions = Object.values(post.reactionCounts).reduce((s, v) => s + v, 0);
  }
};

const onPostReactionRemoved = (payload: { postId: number; userId: number }) => {
  const post = posts.value.find(p => p.id === payload.postId);
  if (post) {
    post.totalReactions = Math.max(0, post.totalReactions - 1);
  }
};

const onNewComment = (payload: { postId: number; comment: any }) => {
  const post = posts.value.find(p => p.id === payload.postId);
  if (post && !post.comments.some(c => c.id === payload.comment.id)) {
    post.comments.push(payload.comment);
    post.totalComments++;
  }
};

const onCommentDeleted = (commentId: number) => {
  for (const post of posts.value) {
    const before = post.comments.length;
    post.comments = post.comments.filter(c => c.id !== commentId);
    if (post.comments.length < before) post.totalComments = Math.max(0, post.totalComments - 1);
  }
};

const setupSignalR = async () => {
  try {
    signalRService = await signalRManager.acquire();

    signalRService.on('NewPost', onNewPost);
    signalRService.on('PostUpdated', onPostUpdated);
    signalRService.on('PostDeleted', onPostDeleted);
    signalRService.on('PostReactionUpdated', onPostReactionUpdated);
    signalRService.on('PostReactionRemoved', onPostReactionRemoved);
    signalRService.on('NewComment', onNewComment);
    signalRService.on('CommentDeleted', onCommentDeleted);

    console.log('SignalR connected for posts feed');
  } catch (err) {
    console.error('SignalR setup failed for posts feed:', err);
  }
};

onMounted(async () => {
  await loadFeed();
  await setupSignalR();
});

onUnmounted(async () => {
  if (signalRService) {
    signalRService.off('NewPost', onNewPost);
    signalRService.off('PostUpdated', onPostUpdated);
    signalRService.off('PostDeleted', onPostDeleted);
    signalRService.off('PostReactionUpdated', onPostReactionUpdated);
    signalRService.off('PostReactionRemoved', onPostReactionRemoved);
    signalRService.off('NewComment', onNewComment);
    signalRService.off('CommentDeleted', onCommentDeleted);
    signalRManager.release();
  }
});
</script>

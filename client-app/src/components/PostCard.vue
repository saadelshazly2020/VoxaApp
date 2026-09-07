<template>
  <div class="bg-white rounded-xl shadow-lg overflow-hidden">
    <div class="p-5">
      <div class="flex items-start justify-between gap-3">
        <div class="flex items-start gap-3 min-w-0">
          <div class="w-10 h-10 bg-gradient-to-br from-blue-400 to-purple-500 rounded-full flex items-center justify-center text-white font-bold flex-shrink-0">
            {{ post.author.username[0].toUpperCase() }}
          </div>
          <div class="min-w-0">
            <p class="font-semibold text-sm text-gray-900 truncate">
              {{ post.author.displayName || post.author.username }}
            </p>
            <p class="text-xs text-gray-500">
              {{ formatDate(post.createdAt) }}
              <span v-if="post.updatedAt" class="ml-1">(edited)</span>
            </p>
          </div>
        </div>

        <div v-if="post.isOwnPost" class="relative flex-shrink-0">
          <button
            @click="showMenu = !showMenu"
            class="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition"
          >
            <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" />
            </svg>
          </button>

          <div
            v-if="showMenu"
            class="absolute right-0 mt-1 w-32 bg-white rounded-lg shadow-xl border border-gray-200 z-20"
          >
            <button
              @click="startEdit"
              class="w-full px-3 py-2 text-left text-sm text-gray-700 hover:bg-gray-100"
            >
              Edit
            </button>
            <button
              @click="handleDelete"
              class="w-full px-3 py-2 text-left text-sm text-red-600 hover:bg-red-50"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      <div v-if="!isEditing" class="mt-3">
        <p class="text-sm text-gray-800 whitespace-pre-wrap break-words">{{ post.content }}</p>
        <img
          v-if="post.imageUrl"
          :src="post.imageUrl"
          alt="Post image"
          class="mt-3 w-full rounded-lg object-cover max-h-96"
        />
      </div>

      <div v-else class="mt-3">
        <textarea
          v-model="editContent"
          rows="3"
          maxlength="2000"
          class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
        ></textarea>
        <div class="flex justify-end gap-2 mt-2">
          <button
            @click="cancelEdit"
            class="px-3 py-1.5 text-sm bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition"
          >
            Cancel
          </button>
          <button
            @click="saveEdit"
            :disabled="!editContent.trim() || isSaving"
            class="px-4 py-1.5 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition"
          >
            {{ isSaving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </div>

      <div v-if="post.totalReactions > 0" class="flex items-center gap-1 mt-3 text-sm text-gray-500">
        <span
          v-for="(count, type) in visibleReactions"
          :key="type"
          class="inline-flex items-center gap-1 px-2 py-0.5 bg-gray-100 rounded-full"
        >
          {{ reactionEmoji(String(type)) }} {{ count }}
        </span>
        <span v-if="post.totalReactions > 0" class="ml-1">
          {{ post.totalReactions }} {{ post.totalReactions === 1 ? 'reaction' : 'reactions' }}
        </span>
      </div>

      <div class="flex items-center gap-1 mt-3 pt-3 border-t border-gray-100 relative">
        <button
          @click="toggleReaction('Like')"
          :class="[
            'flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg transition',
            post.myReaction ? 'text-blue-600 bg-blue-50' : 'text-gray-600 hover:bg-gray-100'
          ]"
        >
          {{ post.myReaction ? reactionEmoji(post.myReaction) : reactionEmoji('Like') }}
          {{ post.myReaction || 'Like' }}
        </button>

        <button
          @mouseenter="showReactions = true"
          class="p-1.5 text-gray-500 hover:bg-gray-100 rounded-lg transition"
          title="More reactions"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </button>

        <div
          v-if="showReactions"
          @mouseleave="showReactions = false"
          class="absolute bottom-full left-0 mb-1 flex gap-1 p-2 bg-white rounded-full shadow-xl border border-gray-200 z-20"
        >
          <button
            v-for="(emoji, type) in REACTION_EMOJIS"
            :key="type"
            @click="toggleReaction(String(type))"
            :title="String(type)"
            class="w-9 h-9 flex items-center justify-center text-xl rounded-full hover:bg-gray-100 hover:scale-110 transition"
          >
            {{ emoji }}
          </button>
        </div>

        <button
          @click="showComments = !showComments"
          class="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100 rounded-lg transition ml-auto"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
          {{ post.totalComments }} {{ post.totalComments === 1 ? 'comment' : 'comments' }}
        </button>
      </div>

      <div v-if="showComments" class="mt-4 pt-4 border-t border-gray-100">
        <PostComments
          :post-id="post.id"
          :initial-comments="post.comments"
          :total-comments="post.totalComments"
          @comment-added="handleCommentAdded"
          @comment-deleted="handleCommentDeleted"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { postsService, type Post, type PostComment, REACTION_EMOJIS } from '@/services/posts.service';
import PostComments from './PostComments.vue';

interface Props {
  post: Post;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'deleted': [postId: number];
  'updated': [post: Post];
  'reaction-changed': [postId: number, myReaction: string | null, reactionCounts: Record<string, number>, totalReactions: number];
  'comment-added': [postId: number, comment: PostComment];
  'comment-deleted': [postId: number, commentId: number];
}>();

const showMenu = ref(false);
const showReactions = ref(false);
const showComments = ref(false);
const isEditing = ref(false);
const isSaving = ref(false);
const editContent = ref('');

const visibleReactions = computed(() =>
  Object.fromEntries(
    Object.entries(props.post.reactionCounts ?? {}).filter(([, count]) => count > 0)
  )
);

const reactionEmoji = (type: string): string => REACTION_EMOJIS[type] ?? REACTION_EMOJIS['Like'];

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  const diffMs = Date.now() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;

  return date.toLocaleDateString();
};

const toggleReaction = async (type: string) => {
  showReactions.value = false;

  const previous = props.post.myReaction;
  const counts: Record<string, number> = { ...(props.post.reactionCounts ?? {}) };
  let total = props.post.totalReactions ?? 0;

  if (previous === type) {
    const result = await postsService.removeReaction(props.post.id);
    if (!result.success) return;
    counts[type] = Math.max(0, (counts[type] ?? 1) - 1);
    if (counts[type] === 0) delete counts[type];
    total = Math.max(0, total - 1);
    emit('reaction-changed', props.post.id, null, counts, total);
    return;
  }

  const result = await postsService.reactToPost(props.post.id, type);
  if (!result.success) return;

  if (previous && counts[previous]) {
    counts[previous] = Math.max(0, counts[previous] - 1);
    if (counts[previous] === 0) delete counts[previous];
    total = Math.max(0, total - 1);
  }
  counts[type] = (counts[type] ?? 0) + 1;
  total += 1;

  emit('reaction-changed', props.post.id, type, counts, total);
};

const startEdit = () => {
  showMenu.value = false;
  editContent.value = props.post.content;
  isEditing.value = true;
};

const cancelEdit = () => {
  isEditing.value = false;
  editContent.value = '';
};

const saveEdit = async () => {
  if (!editContent.value.trim() || isSaving.value) return;

  isSaving.value = true;
  const result = await postsService.editPost(props.post.id, editContent.value.trim());
  if (result.success && result.post) {
    emit('updated', result.post);
    isEditing.value = false;
  }
  isSaving.value = false;
};

const handleDelete = async () => {
  showMenu.value = false;
  if (!confirm('Are you sure you want to delete this post?')) return;

  const result = await postsService.deletePost(props.post.id);
  if (result.success) emit('deleted', props.post.id);
};

const handleCommentAdded = (comment: PostComment) => {
  emit('comment-added', props.post.id, comment);
};

const handleCommentDeleted = (commentId: number) => {
  emit('comment-deleted', props.post.id, commentId);
};
</script>

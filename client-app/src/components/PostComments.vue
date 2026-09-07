<template>
  <div>
    <!-- Comments List -->
    <div v-if="comments.length > 0" class="space-y-3 mb-4">
      <div
        v-for="comment in comments"
        :key="comment.id"
        class="flex items-start gap-3 p-3 bg-gray-50 rounded-lg"
      >
        <!-- Comment Author Avatar -->
        <div class="w-8 h-8 bg-gradient-to-br from-green-400 to-blue-500 rounded-full flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
          {{ comment.author.username[0].toUpperCase() }}
        </div>

        <!-- Comment Content -->
        <div class="flex-1 min-w-0">
          <div class="flex items-center justify-between mb-1">
            <p class="font-semibold text-sm text-gray-900">
              {{ comment.author.displayName || comment.author.username }}
            </p>
            <div class="flex items-center gap-2">
              <p class="text-xs text-gray-500">
                {{ formatDate(comment.createdAt) }}
              </p>
              
              <!-- Comment Actions -->
              <div v-if="comment.isOwnComment" class="relative">
                <button
                  @click="toggleCommentMenu(comment.id)"
                  class="p-1 text-gray-400 hover:text-gray-600 rounded"
                >
                  <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" />
                  </svg>
                </button>

                <!-- Comment Menu -->
                <div
                  v-if="activeCommentMenu === comment.id"
                  class="absolute right-0 mt-1 w-32 bg-white rounded-lg shadow-xl border border-gray-200 z-10"
                >
                  <button
                    @click="startEditComment(comment)"
                    class="w-full px-3 py-2 text-left text-sm text-gray-700 hover:bg-gray-100"
                  >
                    Edit
                  </button>
                  <button
                    @click="handleDeleteComment(comment.id)"
                    class="w-full px-3 py-2 text-left text-sm text-red-600 hover:bg-red-50"
                  >
                    Delete
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Comment Text or Edit Form -->
          <div v-if="editingCommentId !== comment.id">
            <p class="text-sm text-gray-700 whitespace-pre-wrap">{{ comment.content }}</p>
          </div>
          <div v-else class="mt-2">
            <textarea
              v-model="editCommentContent"
              rows="2"
              class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            ></textarea>
            <div class="flex gap-2 mt-2">
              <button
                @click="saveCommentEdit(comment.id)"
                :disabled="!editCommentContent.trim()"
                class="px-3 py-1 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
              >
                Save
              </button>
              <button
                @click="cancelCommentEdit"
                class="px-3 py-1 text-sm bg-gray-200 text-gray-700 rounded hover:bg-gray-300"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Load More Comments -->
    <button
      v-if="comments.length < totalComments"
      @click="loadMoreComments"
      :disabled="isLoadingMore"
      class="w-full py-2 text-sm text-blue-600 hover:bg-blue-50 rounded-lg transition mb-4"
    >
      {{ isLoadingMore ? 'Loading...' : `Load more comments (${totalComments - comments.length} remaining)` }}
    </button>

    <!-- Add Comment Form -->
    <div class="flex items-start gap-3">
      <div class="w-8 h-8 bg-gradient-to-br from-blue-400 to-purple-500 rounded-full flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
        {{ currentUser?.username?.[0]?.toUpperCase() }}
      </div>

      <div class="flex-1">
        <textarea
          v-model="newComment"
          placeholder="Write a comment..."
          rows="2"
          class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
          @keydown.enter.ctrl="handleAddComment"
          :disabled="isSubmitting"
        ></textarea>

        <div class="flex justify-between items-center mt-2">
          <p class="text-xs text-gray-500">Press Ctrl+Enter to post</p>
          <button
            @click="handleAddComment"
            :disabled="!newComment.trim() || isSubmitting"
            class="px-4 py-1.5 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 disabled:opacity-50 transition"
          >
            {{ isSubmitting ? 'Posting...' : 'Comment' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { authService } from '@/services/auth.service';
import { postsService, type PostComment } from '@/services/posts.service';

interface Props {
  postId: number;
  initialComments: PostComment[];
  totalComments: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'comment-added': [comment: PostComment];
  'comment-deleted': [commentId: number];
}>();

const currentUser = computed(() => authService.getUser());
const comments = ref<PostComment[]>([...props.initialComments]);
const newComment = ref('');
const isSubmitting = ref(false);
const isLoadingMore = ref(false);
const activeCommentMenu = ref<number | null>(null);
const editingCommentId = ref<number | null>(null);
const editCommentContent = ref('');

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;
  
  return date.toLocaleDateString();
};

const toggleCommentMenu = (commentId: number) => {
  activeCommentMenu.value = activeCommentMenu.value === commentId ? null : commentId;
};

const handleAddComment = async () => {
  if (!newComment.value.trim() || isSubmitting.value) return;

  isSubmitting.value = true;
  try {
    const result = await postsService.addComment(props.postId, newComment.value.trim());
    if (result.success && result.comment) {
      comments.value.push(result.comment);
      newComment.value = '';
      emit('comment-added', result.comment);
    }
  } catch (error) {
    console.error('Error adding comment:', error);
  } finally {
    isSubmitting.value = false;
  }
};

const loadMoreComments = async () => {
  if (isLoadingMore.value) return;

  isLoadingMore.value = true;
  try {
    const moreComments = await postsService.getComments(props.postId, comments.value.length, 20);
    comments.value.push(...moreComments);
  } catch (error) {
    console.error('Error loading more comments:', error);
  } finally {
    isLoadingMore.value = false;
  }
};

const startEditComment = (comment: PostComment) => {
  editingCommentId.value = comment.id;
  editCommentContent.value = comment.content;
  activeCommentMenu.value = null;
};

const saveCommentEdit = async (commentId: number) => {
  if (!editCommentContent.value.trim()) return;

  try {
    const result = await postsService.editComment(commentId, editCommentContent.value.trim());
    if (result.success) {
      const comment = comments.value.find(c => c.id === commentId);
      if (comment) {
        comment.content = editCommentContent.value.trim();
        comment.updatedAt = new Date().toISOString();
      }
      cancelCommentEdit();
    }
  } catch (error) {
    console.error('Error editing comment:', error);
  }
};

const cancelCommentEdit = () => {
  editingCommentId.value = null;
  editCommentContent.value = '';
};

const handleDeleteComment = async (commentId: number) => {
  if (!confirm('Are you sure you want to delete this comment?')) return;

  activeCommentMenu.value = null;
  try {
    const result = await postsService.deleteComment(commentId);
    if (result.success) {
      const index = comments.value.findIndex(c => c.id === commentId);
      if (index > -1) {
        comments.value.splice(index, 1);
        emit('comment-deleted', commentId);
      }
    }
  } catch (error) {
    console.error('Error deleting comment:', error);
  }
};
</script>

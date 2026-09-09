<template>
  <div>
    <!-- Role toggle -->
    <div class="flex items-center gap-2 mb-6">
      <button
        @click="activeRole = 'student'"
        :class="[
          'px-4 py-2 rounded-xl text-sm font-medium transition',
          activeRole === 'student' ? 'bg-blue-600 text-white' : 'bg-white/10 text-gray-400 hover:text-white'
        ]"
      >
        As Student
      </button>
      <button
        @click="activeRole = 'teacher'"
        :class="[
          'px-4 py-2 rounded-xl text-sm font-medium transition',
          activeRole === 'teacher' ? 'bg-purple-600 text-white' : 'bg-white/10 text-gray-400 hover:text-white'
        ]"
      >
        As Teacher
      </button>
    </div>

    <!-- Status filter -->
    <div class="flex flex-wrap gap-2 mb-4">
      <button
        v-for="filter in statusFilters"
        :key="filter.value"
        @click="activeFilter = filter.value"
        :class="[
          'px-3 py-1 rounded-lg text-xs font-medium transition',
          activeFilter === filter.value ? 'bg-white/20 text-white' : 'bg-white/5 text-gray-400 hover:text-white'
        ]"
      >
        {{ filter.label }}
        <span v-if="filter.count > 0" class="ml-1 px-1.5 py-0.5 bg-white/10 rounded-full">
          {{ filter.count }}
        </span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="text-center py-12 text-gray-400">Loading sessions...</div>

    <!-- Empty state -->
    <div v-else-if="filteredSessions.length === 0" class="text-center py-12">
      <svg class="w-12 h-12 mx-auto text-gray-500 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
      </svg>
      <p class="text-gray-400">No sessions found</p>
    </div>

    <!-- Sessions list -->
    <div v-else class="space-y-3">
      <div
        v-for="session in filteredSessions"
        :key="session.id"
        class="bg-white/5 backdrop-blur-xl border border-white/10 rounded-2xl p-4 hover:bg-white/10 transition"
      >
        <div class="flex items-start justify-between gap-3">
          <div class="flex items-start gap-3 min-w-0">
            <img
              v-if="activeRole === 'student' ? session.teacherProfilePictureUrl : session.studentProfilePictureUrl"
              :src="activeRole === 'student' ? session.teacherProfilePictureUrl : session.studentProfilePictureUrl"
              class="w-10 h-10 rounded-full object-cover"
            />
            <div v-else class="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white text-sm font-bold">
              {{ (activeRole === 'student' ? session.teacherDisplayName : session.studentDisplayName || '?')[0] }}
            </div>
            <div class="min-w-0">
              <h4 class="text-white font-medium truncate">
                {{ activeRole === 'student' ? session.teacherDisplayName : session.studentDisplayName }}
              </h4>
              <div class="flex items-center gap-2 text-sm text-gray-400">
                <span v-if="session.subjectIcon">{{ session.subjectIcon }}</span>
                <span>{{ session.subjectName || 'General' }}</span>
                <span>·</span>
                <span>{{ formatDateTime(session.scheduledAt) }}</span>
              </div>
            </div>
          </div>

          <!-- Status badge -->
          <span :class="statusBadgeClass(session.status)" class="px-2.5 py-1 rounded-lg text-xs font-medium whitespace-nowrap">
            {{ session.status }}
          </span>
        </div>

        <!-- Notes preview -->
        <p v-if="session.studentNotes && activeRole === 'teacher'" class="mt-2 text-sm text-gray-400 line-clamp-2">
          "{{ session.studentNotes }}"
        </p>

        <!-- Actions -->
        <div class="flex items-center gap-2 mt-3">
          <!-- Teacher: Accept/Decline pending -->
          <template v-if="activeRole === 'teacher' && session.status === 'Pending'">
            <button
              @click="acceptSession(session)"
              class="px-3 py-1.5 bg-emerald-500/20 border border-emerald-500/30 text-emerald-400 rounded-lg text-xs font-medium hover:bg-emerald-500/30 transition"
            >
              Accept
            </button>
            <button
              @click="declineSession(session)"
              class="px-3 py-1.5 bg-red-500/20 border border-red-500/30 text-red-400 rounded-lg text-xs font-medium hover:bg-red-500/30 transition"
            >
              Decline
            </button>
          </template>

          <!-- Both: Start accepted session (within join window) -->
          <template v-if="canJoin(session)">
            <button
              @click="startSession(session)"
              class="px-3 py-1.5 bg-blue-500/20 border border-blue-500/30 text-blue-400 rounded-lg text-xs font-medium hover:bg-blue-500/30 transition"
              :class="{ 'animate-pulse': isStartingSoon(session) }"
            >
              Join Call
            </button>
          </template>

          <!-- Ready / countdown indicator for accepted/ready sessions -->
          <span
            v-if="session.status === 'Accepted' || session.status === 'Ready'"
            :class="canJoin(session) ? 'text-emerald-400' : 'text-amber-400'"
            class="text-xs font-medium"
          >
            {{ canJoin(session) ? '● Ready to join' : 'Starts ' + countdownLabel(session.scheduledAt) }}
          </span>

          <!-- Teacher: Complete in-progress -->
          <template v-if="activeRole === 'teacher' && session.status === 'InProgress'">
            <button
              @click="completeSession(session)"
              class="px-3 py-1.5 bg-purple-500/20 border border-purple-500/30 text-purple-400 rounded-lg text-xs font-medium hover:bg-purple-500/30 transition"
            >
              Complete
            </button>
          </template>

          <!-- Cancel (if not completed/cancelled) -->
          <template v-if="session.status === 'Pending' || session.status === 'Accepted' || session.status === 'Ready'">
            <button
              @click="cancelSession(session)"
              class="px-3 py-1.5 bg-white/5 text-gray-400 rounded-lg text-xs font-medium hover:text-red-400 transition"
            >
              Cancel
            </button>
          </template>

          <!-- Review (if completed and no review yet) -->
          <template v-if="session.status === 'Completed' && !session.review">
            <button
              @click="openReview(session)"
              class="px-3 py-1.5 bg-yellow-500/20 border border-yellow-500/30 text-yellow-400 rounded-lg text-xs font-medium hover:bg-yellow-500/30 transition"
            >
              Leave Review
            </button>
          </template>

          <!-- Show existing review -->
          <div v-if="session.review" class="flex items-center gap-1 text-sm">
            <div class="flex">
              <svg v-for="i in 5" :key="i" class="w-3.5 h-3.5" :class="i <= session.review.rating ? 'text-yellow-400' : 'text-gray-600'" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
              </svg>
            </div>
            <span class="text-gray-400 text-xs">{{ session.review.rating }}/5</span>
          </div>
        </div>

        <!-- Duration -->
        <p class="text-xs text-gray-500 mt-2">{{ session.durationMinutes }} min</p>
      </div>
    </div>

    <!-- Review modal -->
    <Teleport to="body">
      <div v-if="reviewSession" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="reviewSession = null">
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm"></div>
        <div class="relative bg-gray-900 border border-white/10 rounded-2xl w-full max-w-md p-5">
          <h3 class="text-lg font-bold text-white mb-4">Rate your session</h3>

          <!-- Star rating -->
          <div class="flex items-center gap-1 mb-4">
            <button
              v-for="i in 5"
              :key="i"
              @click="reviewForm.rating = i"
              class="transition"
            >
              <svg class="w-8 h-8" :class="i <= reviewForm.rating ? 'text-yellow-400' : 'text-gray-600 hover:text-gray-400'" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
              </svg>
            </button>
          </div>

          <textarea
            v-model="reviewForm.comment"
            rows="3"
            placeholder="How was the session? (optional)"
            class="w-full px-3 py-2.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-gray-400 focus:outline-none focus:border-blue-500 resize-none mb-4"
          ></textarea>

          <p v-if="reviewError" class="text-sm text-red-400 mb-3">{{ reviewError }}</p>

          <div class="flex items-center justify-end gap-3">
            <button @click="reviewSession = null" class="text-gray-400 hover:text-white transition">Cancel</button>
            <button
              @click="submitReview"
              :disabled="reviewForm.rating === 0 || reviewSubmitting"
              class="px-4 py-2 bg-yellow-500 text-black rounded-xl font-medium hover:bg-yellow-400 transition disabled:opacity-50"
            >
              {{ reviewSubmitting ? 'Submitting...' : 'Submit Review' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import { tutoringService, type TutoringSession } from '@/services/tutoring.service';

const emit = defineEmits<{
  startCall: [userId: number, userName: string];
}>();

const activeRole = ref<'student' | 'teacher'>('student');
const activeFilter = ref('all');
const sessions = ref<TutoringSession[]>([]);
const loading = ref(true);
const reviewSession = ref<TutoringSession | null>(null);
const reviewForm = ref({ rating: 0, comment: '' });
const reviewSubmitting = ref(false);
const reviewError = ref('');

const statusFilters = computed(() => {
  const counts: Record<string, number> = {};
  sessions.value.forEach(s => { counts[s.status.toLowerCase()] = (counts[s.status.toLowerCase()] || 0) + 1; });

  return [
    { value: 'all', label: 'All', count: sessions.value.length },
    { value: 'pending', label: 'Pending', count: counts['pending'] || 0 },
    { value: 'accepted', label: 'Accepted', count: counts['accepted'] || 0 },
    { value: 'ready', label: 'Ready', count: counts['ready'] || 0 },
    { value: 'inprogress', label: 'In Progress', count: counts['inprogress'] || 0 },
    { value: 'completed', label: 'Completed', count: counts['completed'] || 0 },
    { value: 'cancelled', label: 'Cancelled', count: counts['cancelled'] || 0 },
  ];
});

const filteredSessions = computed(() => {
  if (activeFilter.value === 'all') return sessions.value;
  return sessions.value.filter(s => s.status.toLowerCase() === activeFilter.value);
});

const loadSessions = async () => {
  loading.value = true;
  try {
    sessions.value = await tutoringService.getMySessions(activeRole.value);
  } catch (err) {
    console.error('Failed to load sessions:', err);
  }
  loading.value = false;
};

watch(activeRole, loadSessions);

const formatDateTime = (iso: string) => {
  const d = new Date(iso);
  return d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' }) +
    ' at ' + d.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
};

// Live clock so "ready"/countdown states update without a manual refresh
const now = ref(Date.now());
let clockTimer: ReturnType<typeof setInterval> | null = null;
onMounted(() => {
  clockTimer = setInterval(() => { now.value = Date.now(); }, 30000);
});
onUnmounted(() => { if (clockTimer) clearInterval(clockTimer); });

// Allow joining from 15 min before the scheduled time until 15 min after the session ends
const JOIN_GRACE_MS = 15 * 60 * 1000;

const canJoin = (session: TutoringSession) => {
  if (session.status !== 'Accepted' && session.status !== 'Ready') return false;
  const start = new Date(session.scheduledAt).getTime();
  const end = start + session.durationMinutes * 60 * 1000;
  return now.value >= start - JOIN_GRACE_MS && now.value <= end + JOIN_GRACE_MS;
};

const isStartingSoon = (session: TutoringSession) => {
  if (session.status !== 'Accepted') return false;
  const start = new Date(session.scheduledAt).getTime();
  return now.value >= start - JOIN_GRACE_MS && now.value < start;
};

const countdownLabel = (iso: string) => {
  const diff = new Date(iso).getTime() - now.value;
  if (diff <= 0) return 'Ready now';
  const mins = Math.floor(diff / 60000);
  if (mins >= 60) {
    const h = Math.floor(mins / 60);
    return `in ${h}h ${mins % 60}m`;
  }
  if (mins <= 0) return 'in <1m';
  return `in ${mins}m`;
};

const statusBadgeClass = (status: string) => {
  const map: Record<string, string> = {
    Pending: 'bg-amber-500/20 text-amber-400 border border-amber-500/30',
    Accepted: 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30',
    Ready: 'bg-emerald-400/30 text-emerald-300 border border-emerald-400/40',
    InProgress: 'bg-blue-500/20 text-blue-400 border border-blue-500/30',
    Completed: 'bg-purple-500/20 text-purple-400 border border-purple-500/30',
    Cancelled: 'bg-white/10 text-gray-400',
    Declined: 'bg-red-500/20 text-red-400 border border-red-500/30',
  };
  return map[status] || 'bg-white/10 text-gray-400';
};

const acceptSession = async (session: TutoringSession) => {
  try {
    await tutoringService.acceptSession(session.id);
    await loadSessions();
  } catch (err: any) {
    alert(err.message);
  }
};

const declineSession = async (session: TutoringSession) => {
  try {
    await tutoringService.declineSession(session.id);
    await loadSessions();
  } catch (err: any) {
    alert(err.message);
  }
};

const startSession = async (session: TutoringSession) => {
  try {
    await tutoringService.startSession(session.id);
    // Launch the actual video call with the other participant
    const peerId = activeRole.value === 'student' ? (session.teacherId || 0) : (session.studentId || 0);
    const peerName = activeRole.value === 'student' ? session.teacherDisplayName : session.studentDisplayName;
    if (peerId > 0) emit('startCall', peerId, peerName);
    await loadSessions();
  } catch (err: any) {
    alert(err.message);
  }
};

const completeSession = async (session: TutoringSession) => {
  try {
    await tutoringService.completeSession(session.id);
    await loadSessions();
  } catch (err: any) {
    alert(err.message);
  }
};

const cancelSession = async (session: TutoringSession) => {
  if (!confirm('Cancel this session?')) return;
  try {
    await tutoringService.cancelSession(session.id);
    await loadSessions();
  } catch (err: any) {
    alert(err.message);
  }
};

const openReview = (session: TutoringSession) => {
  reviewSession.value = session;
  reviewForm.value = { rating: 0, comment: '' };
  reviewError.value = '';
};

const submitReview = async () => {
  if (!reviewSession.value || reviewForm.value.rating === 0) return;
  reviewSubmitting.value = true;
  reviewError.value = '';

  try {
    await tutoringService.createReview(
      reviewSession.value.id,
      reviewForm.value.rating,
      reviewForm.value.comment || undefined
    );
    reviewSession.value = null;
    await loadSessions();
  } catch (err: any) {
    reviewError.value = err.message;
  }
  reviewSubmitting.value = false;
};

onMounted(loadSessions);
</script>

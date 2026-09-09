<template>
  <div>
    <!-- Loading -->
    <div v-if="loading" class="text-center py-12 text-gray-400">Loading profile...</div>

    <!-- Not a teacher yet -->
    <div v-else-if="!profile && !editing" class="text-center py-12">
      <svg class="w-16 h-16 mx-auto text-gray-500 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
      </svg>
      <h3 class="text-xl font-bold text-white mb-2">Become a Teacher</h3>
      <p class="text-gray-400 mb-6 max-w-md mx-auto">
        Share your knowledge and earn by tutoring students in your areas of expertise.
      </p>
      <button
        @click="startSetup"
        class="px-6 py-3 bg-gradient-to-r from-blue-600 to-purple-600 text-white rounded-xl font-medium hover:from-blue-700 hover:to-purple-700 transition"
      >
        Set Up Teacher Profile
      </button>
    </div>

    <!-- Profile form -->
    <div v-else>
      <div class="flex items-center justify-between mb-6">
        <h3 class="text-lg font-bold text-white">Teacher Profile</h3>
        <div class="flex items-center gap-3">
          <span v-if="profile" :class="profile.isAcceptingStudents ? 'text-emerald-400' : 'text-gray-400'" class="text-sm">
            {{ profile.isAcceptingStudents ? 'Accepting Students' : 'Not Accepting' }}
          </span>
          <button
            v-if="profile"
            @click="toggleAccepting"
            class="px-3 py-1.5 bg-white/10 border border-white/20 rounded-lg text-sm text-white hover:bg-white/20 transition"
          >
            {{ profile.isAcceptingStudents ? 'Pause' : 'Resume' }}
          </button>
        </div>
      </div>

      <!-- Subjects -->
      <div class="mb-4">
        <label class="block text-sm font-medium text-gray-300 mb-2">Subjects</label>
        <div class="flex flex-wrap gap-2">
          <button
            v-for="subject in allSubjects"
            :key="subject.id"
            @click="toggleSubject(subject.id)"
            :class="[
              'px-3 py-1.5 rounded-xl text-sm border transition',
              form.subjectIds.includes(subject.id)
                ? 'bg-blue-500/20 border-blue-500/30 text-blue-400'
                : 'bg-white/5 border-white/10 text-gray-400 hover:text-white'
            ]"
          >
            {{ subject.icon }} {{ subject.name }}
          </button>
        </div>
      </div>

      <!-- Hourly rate -->
      <div class="mb-4">
        <label class="block text-sm font-medium text-gray-300 mb-1">Hourly Rate</label>
        <input
          v-model="form.hourlyRate"
          type="text"
          placeholder="e.g. $25/hr"
          class="w-full px-3 py-2.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-gray-400 focus:outline-none focus:border-blue-500"
        />
      </div>

      <!-- Experience -->
      <div class="mb-4">
        <label class="block text-sm font-medium text-gray-300 mb-1">Years of Experience</label>
        <input
          v-model.number="form.experienceYears"
          type="number"
          min="0"
          max="50"
          class="w-full px-3 py-2.5 bg-white/10 border border-white/20 rounded-xl text-white focus:outline-none focus:border-blue-500"
        />
      </div>

      <!-- Availability -->
      <div class="mb-6">
        <label class="block text-sm font-medium text-gray-300 mb-2">Weekly Availability</label>
        <div class="space-y-2">
          <div
            v-for="(day, idx) in dayNames"
            :key="idx"
            class="flex items-center gap-3 p-3 bg-white/5 rounded-xl"
          >
            <label class="flex items-center gap-2 min-w-[120px]">
              <input
                type="checkbox"
                :checked="isDayEnabled(idx)"
                @change="toggleDay(idx)"
                class="w-4 h-4 rounded border-gray-600 bg-white/10 text-blue-500 focus:ring-blue-500"
              />
              <span class="text-sm text-white">{{ day }}</span>
            </label>

            <template v-if="isDayEnabled(idx)">
              <input
                :value="getDaySlot(idx)?.startTime || '09:00'"
                @input="updateDaySlot(idx, 'startTime', ($event.target as HTMLInputElement).value)"
                type="time"
                class="px-2 py-1 bg-white/10 border border-white/20 rounded-lg text-white text-sm focus:outline-none focus:border-blue-500"
              />
              <span class="text-gray-400">to</span>
              <input
                :value="getDaySlot(idx)?.endTime || '17:00'"
                @input="updateDaySlot(idx, 'endTime', ($event.target as HTMLInputElement).value)"
                type="time"
                class="px-2 py-1 bg-white/10 border border-white/20 rounded-lg text-white text-sm focus:outline-none focus:border-blue-500"
              />
            </template>
          </div>
        </div>
      </div>

      <p v-if="error" class="text-sm text-red-400 mb-3">{{ error }}</p>
      <p v-if="success" class="text-sm text-emerald-400 mb-3">{{ success }}</p>

      <button
        @click="saveProfile"
        :disabled="saving"
        class="px-6 py-2.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white rounded-xl font-medium hover:from-blue-700 hover:to-purple-700 transition disabled:opacity-50"
      >
        {{ saving ? 'Saving...' : 'Save Profile' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { tutoringService, type SubjectTag, type TeacherProfile } from '@/services/tutoring.service';

const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

const profile = ref<TeacherProfile | null>(null);
const allSubjects = ref<SubjectTag[]>([]);
const loading = ref(true);
const editing = ref(false);
const saving = ref(false);
const error = ref('');
const success = ref('');

const form = reactive({
  hourlyRate: '',
  experienceYears: 0,
  subjectIds: [] as number[],
  availability: [] as { dayOfWeek: number; startTime: string; endTime: string }[],
});

const startSetup = () => {
  editing.value = true;
};

const toggleSubject = (id: number) => {
  const idx = form.subjectIds.indexOf(id);
  if (idx >= 0) form.subjectIds.splice(idx, 1);
  else form.subjectIds.push(id);
};

const isDayEnabled = (dayIdx: number) => form.availability.some(a => a.dayOfWeek === dayIdx);
const getDaySlot = (dayIdx: number) => form.availability.find(a => a.dayOfWeek === dayIdx);

const toggleDay = (dayIdx: number) => {
  const idx = form.availability.findIndex(a => a.dayOfWeek === dayIdx);
  if (idx >= 0) {
    form.availability.splice(idx, 1);
  } else {
    form.availability.push({ dayOfWeek: dayIdx, startTime: '09:00', endTime: '17:00' });
  }
};

const updateDaySlot = (dayIdx: number, field: 'startTime' | 'endTime', value: string) => {
  const slot = form.availability.find(a => a.dayOfWeek === dayIdx);
  if (slot) slot[field] = value;
};

const toggleAccepting = async () => {
  if (!profile.value) return;
  try {
    await tutoringService.toggleAccepting(!profile.value.isAcceptingStudents);
    profile.value.isAcceptingStudents = !profile.value.isAcceptingStudents;
  } catch (err: any) {
    alert(err.message);
  }
};

const saveProfile = async () => {
  saving.value = true;
  error.value = '';
  success.value = '';

  try {
    await tutoringService.saveMyTeacherProfile({
      hourlyRate: form.hourlyRate || undefined,
      experienceYears: form.experienceYears,
      subjectIds: form.subjectIds,
      availability: form.availability,
    });

    success.value = 'Profile saved!';
    editing.value = false;
    await loadProfile();
  } catch (err: any) {
    error.value = err.message;
  }
  saving.value = false;
};

const loadProfile = async () => {
  loading.value = true;
  try {
    allSubjects.value = await tutoringService.getSubjects();
    profile.value = await tutoringService.getMyTeacherProfile();

    if (profile.value) {
      form.hourlyRate = profile.value.hourlyRate || '';
      form.experienceYears = profile.value.experienceYears;
      form.subjectIds = profile.value.subjects?.map(s => s.id) || [];
      form.availability = profile.value.availability?.map(a => ({
        dayOfWeek: a.dayOfWeek,
        startTime: (a.startTime || '').slice(0, 5),
        endTime: (a.endTime || '').slice(0, 5),
      })) || [];
    }
  } catch (err) {
    console.error('Failed to load profile:', err);
  }
  loading.value = false;
};

onMounted(loadProfile);
</script>

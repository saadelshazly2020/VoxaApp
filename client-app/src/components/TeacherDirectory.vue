<template>
  <div>
    <!-- Search & Filter Bar -->
    <div class="flex flex-col sm:flex-row gap-3 mb-6">
      <div class="relative flex-1">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
        </svg>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search teachers by name or subject..."
          class="w-full pl-10 pr-4 py-2.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-gray-400 focus:outline-none focus:border-blue-500"
          @input="debouncedSearch"
        />
      </div>
      <select
        v-model="selectedSubjectId"
        class="px-4 py-2.5 bg-slate-800 border border-white/20 rounded-xl text-white focus:outline-none focus:border-blue-500 min-w-[180px]"
        @change="loadTeachers"
      >
        <option :value="undefined" class="bg-slate-800 text-white">All Subjects</option>
        <option v-for="s in subjects" :key="s.id" :value="s.id" class="bg-slate-800 text-white">{{ s.icon }} {{ s.name }}</option>
      </select>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="text-center py-12 text-gray-400">Loading teachers...</div>

    <!-- No results -->
    <div v-else-if="teachers.length === 0" class="text-center py-12 text-gray-400">
      <p class="text-lg mb-2">No teachers found</p>
      <p class="text-sm">Try adjusting your search or filter</p>
    </div>

    <!-- Teacher Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="teacher in teachers"
        :key="teacher.id"
        class="bg-white/5 backdrop-blur-xl border border-white/10 rounded-2xl p-5 hover:bg-white/10 transition-all cursor-pointer group"
        @click="$emit('selectTeacher', teacher)"
      >
        <!-- Header -->
        <div class="flex items-center gap-3 mb-3">
          <div class="relative">
            <img
              v-if="teacher.profilePictureUrl"
              :src="teacher.profilePictureUrl"
              class="w-12 h-12 rounded-full object-cover"
            />
            <div v-else class="w-12 h-12 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white font-bold text-lg">
              {{ (teacher.displayName || teacher.username || '?')[0] }}
            </div>
            <div
              v-if="teacher.isOnline"
              class="absolute -bottom-0.5 -right-0.5 w-3.5 h-3.5 bg-emerald-400 rounded-full border-2 border-gray-900"
            ></div>
          </div>
          <div class="min-w-0">
            <h3 class="text-white font-semibold truncate">{{ teacher.displayName || teacher.username }}</h3>
            <p class="text-gray-400 text-sm">@{{ teacher.username }}</p>
          </div>
        </div>

        <!-- Subjects -->
        <div class="flex flex-wrap gap-1.5 mb-3">
          <span
            v-for="subject in teacher.subjects"
            :key="subject.id"
            class="px-2 py-0.5 bg-white/10 rounded-full text-xs text-gray-300"
          >
            {{ subject.icon }} {{ subject.name }}
          </span>
        </div>

        <!-- Stats -->
        <div class="flex items-center justify-between text-sm">
          <div class="flex items-center gap-3">
            <span v-if="teacher.experienceYears" class="text-gray-400">
              {{ teacher.experienceYears }}y exp
            </span>
            <span v-if="teacher.hourlyRate" class="text-emerald-400 font-semibold">
              {{ teacher.hourlyRate }}
            </span>
          </div>
          <div class="flex items-center gap-1">
            <svg class="w-4 h-4 text-yellow-400" fill="currentColor" viewBox="0 0 24 24">
              <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
            </svg>
            <span class="text-gray-300">New</span>
          </div>
        </div>

        <!-- Accepting badge -->
        <div
          v-if="teacher.isAcceptingStudents"
          class="mt-3 inline-flex items-center gap-1 px-2 py-0.5 bg-emerald-500/20 border border-emerald-500/30 rounded-full text-xs text-emerald-400"
        >
          <div class="w-1.5 h-1.5 bg-emerald-400 rounded-full"></div>
          Accepting Students
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { tutoringService, type SubjectTag, type TeacherProfile } from '@/services/tutoring.service';

defineEmits<{
  selectTeacher: [teacher: TeacherProfile];
}>();

const subjects = ref<SubjectTag[]>([]);
const teachers = ref<TeacherProfile[]>([]);
const searchQuery = ref('');
const selectedSubjectId = ref<number | undefined>();
const loading = ref(true);
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const debouncedSearch = () => {
  if (searchTimeout) clearTimeout(searchTimeout);
  searchTimeout = setTimeout(loadTeachers, 300);
};

const loadTeachers = async () => {
  loading.value = true;
  try {
    teachers.value = await tutoringService.searchTeachers(selectedSubjectId.value, searchQuery.value || undefined);
  } catch (err) {
    console.error('Failed to load teachers:', err);
  }
  loading.value = false;
};

onMounted(async () => {
  try {
    subjects.value = await tutoringService.getSubjects();
  } catch (err) {
    console.error('Failed to load subjects:', err);
  }
  await loadTeachers();
});
</script>

<template>
  <Teleport to="body">
    <div v-if="show && teacher" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="$emit('close')">
      <div class="absolute inset-0 bg-black/60 backdrop-blur-sm"></div>
      <div class="relative bg-gray-900 border border-white/10 rounded-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <!-- Header -->
        <div class="flex items-center justify-between p-5 border-b border-white/10">
          <div>
            <h2 class="text-lg font-bold text-white">Book a Session</h2>
            <p class="text-sm text-gray-400">with {{ teacher.displayName || teacher.username }}</p>
          </div>
          <button @click="$emit('close')" class="text-gray-400 hover:text-white transition">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <!-- Body -->
        <div class="p-5 space-y-4">
          <!-- Subject -->
          <div>
            <label class="block text-sm font-medium text-gray-300 mb-1">Subject</label>
            <select
              v-model="form.subjectId"
              class="w-full px-3 py-2.5 bg-slate-800 border border-white/20 rounded-xl text-white focus:outline-none focus:border-blue-500"
            >
              <option :value="undefined" class="bg-slate-800 text-white">General</option>
              <option v-for="s in teacher.subjects" :key="s.id" :value="s.id" class="bg-slate-800 text-white">{{ s.icon }} {{ s.name }}</option>
            </select>
          </div>

          <!-- Date -->
          <div>
            <label class="block text-sm font-medium text-gray-300 mb-1">Date</label>
            <input
              v-model="form.date"
              type="date"
              :min="minDate"
              class="w-full px-3 py-2.5 bg-white/10 border border-white/20 rounded-xl text-white focus:outline-none focus:border-blue-500"
            />
          </div>

          <!-- Time -->
          <div>
            <label class="block text-sm font-medium text-gray-300 mb-1">Time</label>
            <select
              v-model="form.time"
              class="w-full px-3 py-2.5 bg-slate-800 border border-white/20 rounded-xl text-white focus:outline-none focus:border-blue-500"
            >
              <option value="" disabled class="bg-slate-800 text-white">Select a time</option>
              <option v-for="slot in availableTimeSlots" :key="slot" :value="slot" class="bg-slate-800 text-white">{{ slot }}</option>
            </select>
            <p v-if="form.date && availableTimeSlots.length === 0" class="text-sm text-amber-400 mt-1">
              No available slots for this day
            </p>
          </div>

          <!-- Duration -->
          <div>
            <label class="block text-sm font-medium text-gray-300 mb-1">Duration</label>
            <select
              v-model.number="form.durationMinutes"
              class="w-full px-3 py-2.5 bg-slate-800 border border-white/20 rounded-xl text-white focus:outline-none focus:border-blue-500"
            >
              <option :value="30" class="bg-slate-800 text-white">30 minutes</option>
              <option :value="60" class="bg-slate-800 text-white">1 hour</option>
              <option :value="90" class="bg-slate-800 text-white">1.5 hours</option>
              <option :value="120" class="bg-slate-800 text-white">2 hours</option>
            </select>
          </div>

          <!-- Notes -->
          <div>
            <label class="block text-sm font-medium text-gray-300 mb-1">Notes (optional)</label>
            <textarea
              v-model="form.notes"
              rows="3"
              placeholder="What would you like to cover?"
              class="w-full px-3 py-2.5 bg-white/10 border border-white/20 rounded-xl text-white placeholder-gray-400 focus:outline-none focus:border-blue-500 resize-none"
            ></textarea>
          </div>

          <!-- Error -->
          <p v-if="error" class="text-sm text-red-400">{{ error }}</p>
        </div>

        <!-- Footer -->
        <div class="flex items-center justify-end gap-3 p-5 border-t border-white/10">
          <button
            @click="$emit('close')"
            class="px-4 py-2 text-gray-400 hover:text-white transition"
          >
            Cancel
          </button>
          <button
            @click="submitBooking"
            :disabled="!isValid || submitting"
            class="px-5 py-2.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white rounded-xl font-medium hover:from-blue-700 hover:to-purple-700 transition disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ submitting ? 'Booking...' : 'Book Session' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { tutoringService, type TeacherProfile } from '@/services/tutoring.service';

const props = defineProps<{
  show: boolean;
  teacher: TeacherProfile | null;
}>();

const emit = defineEmits<{
  close: [];
  booked: [];
}>();

const form = reactive({
  subjectId: undefined as number | undefined,
  date: '',
  time: '',
  durationMinutes: 60,
  notes: '',
});

const submitting = ref(false);
const error = ref('');

const minDate = computed(() => {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
});

const isValid = computed(() => form.date && form.time && form.durationMinutes > 0);

const availableTimeSlots = computed(() => {
  if (!form.date || !props.teacher?.availability) return [];

  // Parse as LOCAL date (avoid UTC shift from new Date("YYYY-MM-DD"))
  const [y, m, d] = form.date.split('-').map(Number);
  const date = new Date(y, m - 1, d);
  const dayOfWeek = date.getDay();

  const daySlots = props.teacher.availability.filter(a => a.dayOfWeek === dayOfWeek);
  if (daySlots.length === 0) return [];

  const slots: string[] = [];
  for (const slot of daySlots) {
    const [startH, startM] = slot.startTime.split(':').map(Number);
    const [endH, endM] = slot.endTime.split(':').map(Number);
    let start = startH * 60 + startM;
    const end = endH * 60 + endM;

    while (start + form.durationMinutes <= end) {
      const h = Math.floor(start / 60).toString().padStart(2, '0');
      const m = (start % 60).toString().padStart(2, '0');
      slots.push(`${h}:${m}`);
      start += 30; // 30-min increments
    }
  }

  return slots;
});

watch(() => form.date, () => { form.time = ''; });

const submitBooking = async () => {
  if (!isValid.value) return;
  submitting.value = true;
  error.value = '';

  try {
    // Send as a naive LOCAL datetime (no 'Z') so the server compares against
    // availability using the same local day/time the teacher entered.
    const scheduledAt = `${form.date}T${form.time}:00`;

    await tutoringService.createSession({
      teacherProfileId: props.teacher!.id,
      subjectId: form.subjectId,
      scheduledAt,
      durationMinutes: form.durationMinutes,
      notes: form.notes || undefined,
    });

    emit('booked');
    emit('close');
  } catch (err: any) {
    error.value = err.message || 'Failed to book session';
  }
  submitting.value = false;
};
</script>

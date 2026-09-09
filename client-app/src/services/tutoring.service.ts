import { authService } from './auth.service';

const API_BASE = '/api/tutoring';

async function apiFetch(path: string, options: RequestInit = {}) {
  const token = authService.getToken();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...((options.headers as Record<string, string>) || {}),
  };
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const response = await fetch(`${API_BASE}${path}`, { ...options, headers });
  if (!response.ok) {
    const data = await response.json().catch(() => ({ message: 'Request failed' }));
    throw new Error(data.message || `HTTP ${response.status}`);
  }
  return response.json();
}

// ─── Types ────────────────────────────────────────────────

export interface SubjectTag {
  id: number;
  name: string;
  description?: string;
  icon?: string;
}

export interface TeacherProfile {
  id: number;
  displayName: string;
  username: string;
  profilePictureUrl?: string;
  isOnline: boolean;
  hourlyRate?: string;
  experienceYears: number;
  isAcceptingStudents: boolean;
  subjects: SubjectTag[];
  averageRating?: number;
  reviewCount?: number;
  reviews?: TutoringReview[];
  availability?: AvailabilitySlot[];
}

export interface AvailabilitySlot {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface TutoringSession {
  id: number;
  status: string;
  teacherDisplayName: string;
  teacherProfilePictureUrl?: string;
  teacherId?: number;
  studentDisplayName: string;
  studentProfilePictureUrl?: string;
  studentId?: number;
  subjectName?: string;
  subjectIcon?: string;
  scheduledAt: string;
  durationMinutes: number;
  studentNotes?: string;
  teacherNotes?: string;
  createdAt: string;
  startedAt?: string;
  endedAt?: string;
  review?: TutoringReview;
}

export interface TutoringReview {
  id: number;
  rating: number;
  comment?: string;
  createdAt: string;
  reviewerDisplayName?: string;
  reviewerProfilePictureUrl?: string;
  subjectName?: string;
}

// ─── Service ──────────────────────────────────────────────

export const tutoringService = {
  // Subjects
  async getSubjects(): Promise<SubjectTag[]> {
    return apiFetch('/subjects');
  },

  // Teacher directory
  async searchTeachers(subjectId?: number, query?: string): Promise<TeacherProfile[]> {
    const params = new URLSearchParams();
    if (subjectId) params.set('subjectId', subjectId.toString());
    if (query) params.set('query', query);
    const qs = params.toString();
    return apiFetch(`/teachers${qs ? '?' + qs : ''}`);
  },

  async getTeacherProfile(id: number): Promise<TeacherProfile> {
    return apiFetch(`/teachers/${id}`);
  },

  // My teacher profile
  async getMyTeacherProfile(): Promise<TeacherProfile | null> {
    return apiFetch('/my-teacher-profile');
  },

  async saveMyTeacherProfile(data: {
    hourlyRate?: string;
    experienceYears: number;
    subjectIds: number[];
    availability?: { dayOfWeek: number; startTime: string; endTime: string }[];
  }): Promise<{ message: string; profileId?: number }> {
    return apiFetch('/my-teacher-profile', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  async updateAvailability(slots: { dayOfWeek: number; startTime: string; endTime: string }[]): Promise<{ message: string }> {
    return apiFetch('/my-teacher-profile/availability', {
      method: 'PUT',
      body: JSON.stringify(slots),
    });
  },

  async toggleAccepting(accepting: boolean): Promise<{ message: string }> {
    return apiFetch('/my-teacher-profile/toggle-accepting', {
      method: 'PUT',
      body: JSON.stringify({ accepting }),
    });
  },

  // Sessions
  async createSession(data: {
    teacherProfileId: number;
    subjectId?: number;
    scheduledAt: string;
    durationMinutes: number;
    notes?: string;
  }): Promise<{ message: string; session?: TutoringSession }> {
    return apiFetch('/sessions', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  async getMySessions(role?: 'teacher' | 'student', status?: string): Promise<TutoringSession[]> {
    const params = new URLSearchParams();
    if (role) params.set('role', role);
    if (status) params.set('status', status);
    const qs = params.toString();
    return apiFetch(`/sessions${qs ? '?' + qs : ''}`);
  },

  async getUpcomingSessions(): Promise<TutoringSession[]> {
    return apiFetch('/sessions/upcoming');
  },

  async getSession(id: number): Promise<TutoringSession> {
    return apiFetch(`/sessions/${id}`);
  },

  async acceptSession(id: number, notes?: string): Promise<{ message: string }> {
    return apiFetch(`/sessions/${id}/accept`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    });
  },

  async declineSession(id: number, notes?: string): Promise<{ message: string }> {
    return apiFetch(`/sessions/${id}/decline`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    });
  },

  async cancelSession(id: number): Promise<{ message: string }> {
    return apiFetch(`/sessions/${id}/cancel`, { method: 'POST' });
  },

  async startSession(id: number): Promise<{ message: string }> {
    return apiFetch(`/sessions/${id}/start`, { method: 'POST' });
  },

  async completeSession(id: number, teacherNotes?: string): Promise<{ message: string }> {
    return apiFetch(`/sessions/${id}/complete`, {
      method: 'POST',
      body: JSON.stringify({ teacherNotes }),
    });
  },

  // Reviews
  async createReview(sessionId: number, rating: number, comment?: string): Promise<{ message: string }> {
    return apiFetch(`/sessions/${sessionId}/review`, {
      method: 'POST',
      body: JSON.stringify({ rating, comment }),
    });
  },
};

export interface User {
  userId: string;
  connectionId: string;
  currentRoomId: string | null;
  isOnline: boolean;
}

export interface UserStatus {
  userId: string;
  status: 'online' | 'offline' | 'in-call' | 'busy';
}

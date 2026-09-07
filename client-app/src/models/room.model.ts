export interface Room {
  roomId: string;
  participants: string[];
  createdAt: Date;
}

export interface RoomInfo {
  id: string;
  name: string;
  participantCount: number;
  isActive: boolean;
}

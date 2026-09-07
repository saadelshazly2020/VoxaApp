export interface Room {
    roomId: string;
    participantUserIds: string[];
    createdAt: Date;
    creatorUserId: string;
}

export interface RoomInfo {
    roomId: string;
    participants: string[];
}

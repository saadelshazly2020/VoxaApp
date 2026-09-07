export interface User {
    userId: string;
    currentRoomId: string | null;
}

export interface UserConnection {
    userId: string;
    connectionId: string;
    currentRoomId: string | null;
}

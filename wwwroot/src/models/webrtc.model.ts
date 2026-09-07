export interface IceCandidate {
    candidate: string;
    sdpMid: string | null;
    sdpMLineIndex: number | null;
}

export interface RTCSessionDescriptionInit {
    type: RTCSdpType;
    sdp?: string;
}

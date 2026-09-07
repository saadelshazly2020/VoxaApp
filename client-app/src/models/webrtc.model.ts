export interface RTCConfig {
  iceServers: RTCIceServer[];
}

export interface MediaConstraints {
  video: boolean | MediaTrackConstraints;
  audio: boolean | MediaTrackConstraints;
}

export interface SignalingMessage {
  type: 'offer' | 'answer';
  senderId: string;
  receiverId: string;
  sdp: string;
}

export interface IceCandidateMessage {
  senderId: string;
  receiverId: string;
  candidate: RTCIceCandidateInit;
}

export interface PeerConnection {
  userId: string;
  connection: RTCPeerConnection;
  stream?: MediaStream;
}

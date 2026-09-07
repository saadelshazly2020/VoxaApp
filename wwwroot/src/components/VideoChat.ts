import { defineComponent, ref, reactive, onMounted, onUnmounted } from 'vue';
import { SignalRService } from '../services/signalr.service';
import { WebRTCService } from '../services/webrtc.service';
import { User } from '../models/user.model';
import UserList from './UserList';
import VideoGrid from './VideoGrid';

export default defineComponent({
    name: 'VideoChat',
    components: {
        UserList,
        VideoGrid
    },
    setup() {
        // State
        const userId = ref<string>('');
        const isRegistered = ref<boolean>(false);
        const isConnected = ref<boolean>(false);
        const connectionState = ref<string>('Disconnected');
        const users = ref<User[]>([]);
        const localStream = ref<MediaStream | null>(null);
        const remoteStreams = reactive<Record<string, MediaStream>>({});
        const currentRoomId = ref<string | null>(null);
        const roomInput = ref<string>('');
        const errorMessage = ref<string>('');
        const successMessage = ref<string>('');
        const isAudioEnabled = ref<boolean>(true);
        const isVideoEnabled = ref<boolean>(true);
        const isInCall = ref<boolean>(false);

        // Services
        let signalRService: SignalRService;
        let webRTCService: WebRTCService | null = null;

        // Initialize SignalR
        const initializeSignalR = async () => {
            try {
                signalRService = new SignalRService('/videocallhub');
                
                // Setup event handlers
                signalRService.on('connected', () => {
                    console.log('Connected to SignalR');
                    isConnected.value = true;
                    connectionState.value = 'Connected';
                    clearError();
                });

                signalRService.on('disconnected', () => {
                    console.log('Disconnected from SignalR');
                    isConnected.value = false;
                    connectionState.value = 'Disconnected';
                    showError('Disconnected from server');
                });

                signalRService.on('reconnecting', () => {
                    connectionState.value = 'Reconnecting';
                    showError('Connection lost. Reconnecting...');
                });

                signalRService.on('reconnected', () => {
                    connectionState.value = 'Connected';
                    showSuccess('Reconnected successfully');
                    if (isRegistered.value && userId.value) {
                        registerUser();
                    }
                });

                signalRService.on('Registered', (registeredUserId: string) => {
                    console.log('User registered:', registeredUserId);
                    isRegistered.value = true;
                    showSuccess(`Registered as ${registeredUserId}`);
                });

                signalRService.on('UserListUpdated', (updatedUsers: User[]) => {
                    console.log('User list updated:', updatedUsers);
                    users.value = updatedUsers;
                });

                signalRService.on('RoomCreated', (roomId: string) => {
                    console.log('Room created:', roomId);
                    currentRoomId.value = roomId;
                    showSuccess(`Room ${roomId} created`);
                });

                signalRService.on('RoomJoined', async (roomId: string, participants: string[]) => {
                    console.log('Joined room:', roomId, 'Participants:', participants);
                    currentRoomId.value = roomId;
                    showSuccess(`Joined room ${roomId}`);

                    // Create offers for existing participants
                    for (const participantId of participants) {
                        if (participantId !== userId.value) {
                            await webRTCService?.createOfferForUser(participantId);
                        }
                    }
                });

                signalRService.on('RoomLeft', (roomId: string) => {
                    console.log('Left room:', roomId);
                    currentRoomId.value = null;
                    showSuccess(`Left room ${roomId}`);
                });

                signalRService.on('Error', (message: string) => {
                    console.error('Server error:', message);
                    showError(message);
                });

                await signalRService.start();
            } catch (error) {
                console.error('Failed to initialize SignalR:', error);
                showError('Failed to connect to server');
            }
        };

        // Register user
        const registerUser = async () => {
            if (!userId.value.trim()) {
                showError('Please enter a user ID');
                return;
            }

            try {
                await signalRService.registerUser(userId.value);
                
                // Initialize WebRTC service
                webRTCService = new WebRTCService(signalRService, userId.value);
                
                // Setup WebRTC event handlers
                webRTCService.on('localStreamReady', (stream: MediaStream) => {
                    console.log('Local stream ready');
                    localStream.value = stream;
                });

                webRTCService.on('remoteStream', (remoteUserId: string, stream: MediaStream) => {
                    console.log('Remote stream received from:', remoteUserId);
                    remoteStreams[remoteUserId] = stream;
                    isInCall.value = true;
                });

                webRTCService.on('connectionClosed', (remoteUserId: string) => {
                    console.log('Connection closed with:', remoteUserId);
                    delete remoteStreams[remoteUserId];
                    if (Object.keys(remoteStreams).length === 0) {
                        isInCall.value = false;
                    }
                });

                webRTCService.on('connectionStateChange', (remoteUserId: string, state: string) => {
                    console.log(`Connection state with ${remoteUserId}: ${state}`);
                    if (state === 'failed') {
                        showError(`Connection failed with ${remoteUserId}`);
                    }
                });

                // Initialize local media
                await webRTCService.initLocalMedia({
                    video: true,
                    audio: true
                });

            } catch (error) {
                console.error('Registration failed:', error);
                showError((error as Error).message || 'Registration failed');
            }
        };

        // Call user (1-to-1)
        const callUser = async (targetUserId: string) => {
            if (!webRTCService) {
                showError('WebRTC not initialized');
                return;
            }

            try {
                await webRTCService.createOfferForUser(targetUserId);
                showSuccess(`Calling ${targetUserId}...`);
                isInCall.value = true;
            } catch (error) {
                console.error('Call failed:', error);
                showError(`Failed to call ${targetUserId}`);
            }
        };

        // Create room
        const createRoom = async () => {
            if (!roomInput.value.trim()) {
                showError('Please enter a room ID');
                return;
            }

            try {
                await signalRService.createRoom(roomInput.value);
                roomInput.value = '';
            } catch (error) {
                console.error('Failed to create room:', error);
                showError('Failed to create room');
            }
        };

        // Join room
        const joinRoom = async () => {
            if (!roomInput.value.trim()) {
                showError('Please enter a room ID');
                return;
            }

            try {
                await signalRService.joinRoom(roomInput.value);
                roomInput.value = '';
                isInCall.value = true;
            } catch (error) {
                console.error('Failed to join room:', error);
                showError('Failed to join room');
            }
        };

        // Leave room
        const leaveRoom = async () => {
            if (!currentRoomId.value) return;

            try {
                await signalRService.leaveRoom(currentRoomId.value);
                webRTCService?.closeAllConnections();
                Object.keys(remoteStreams).forEach(key => {
                    delete remoteStreams[key];
                });
                isInCall.value = false;
            } catch (error) {
                console.error('Failed to leave room:', error);
                showError('Failed to leave room');
            }
        };

        // End call
        const endCall = () => {
            webRTCService?.closeAllConnections();
            Object.keys(remoteStreams).forEach(key => {
                delete remoteStreams[key];
            });
            isInCall.value = false;
            showSuccess('Call ended');
        };

        // Toggle audio
        const toggleAudio = async () => {
            if (!webRTCService) return;
            isAudioEnabled.value = !isAudioEnabled.value;
            await webRTCService.toggleAudio(isAudioEnabled.value);
        };

        // Toggle video
        const toggleVideo = async () => {
            if (!webRTCService) return;
            isVideoEnabled.value = !isVideoEnabled.value;
            await webRTCService.toggleVideo(isVideoEnabled.value);
        };

        // Utility functions
        const showError = (message: string) => {
            errorMessage.value = message;
            successMessage.value = '';
            setTimeout(() => {
                errorMessage.value = '';
            }, 5000);
        };

        const showSuccess = (message: string) => {
            successMessage.value = message;
            errorMessage.value = '';
            setTimeout(() => {
                successMessage.value = '';
            }, 3000);
        };

        const clearError = () => {
            errorMessage.value = '';
            successMessage.value = '';
        };

        // Lifecycle hooks
        onMounted(async () => {
            await initializeSignalR();
        });

        onUnmounted(() => {
            webRTCService?.cleanup();
            signalRService?.stop();
        });

        return {
            userId,
            isRegistered,
            isConnected,
            connectionState,
            users,
            localStream,
            remoteStreams,
            currentRoomId,
            roomInput,
            errorMessage,
            successMessage,
            isAudioEnabled,
            isVideoEnabled,
            isInCall,
            registerUser,
            callUser,
            createRoom,
            joinRoom,
            leaveRoom,
            endCall,
            toggleAudio,
            toggleVideo
        };
    },
    template: `
        <div>
            <div class="header">
                <h1>?? Video Chat Application</h1>
                <span :class="['status', isConnected ? 'connected' : 'disconnected']">
                    {{ connectionState }}
                </span>
            </div>

            <div class="main-container">
                <div class="sidebar">
                    <div v-if="!isRegistered">
                        <h2 style="margin-bottom: 15px; color: #374151;">Join Video Chat</h2>
                        <div class="input-group">
                            <label>Enter your User ID</label>
                            <input 
                                v-model="userId" 
                                type="text" 
                                placeholder="e.g., John"
                                @keyup.enter="registerUser"
                            />
                        </div>
                        <button 
                            class="btn btn-primary" 
                            @click="registerUser"
                            :disabled="!isConnected || !userId.trim()"
                        >
                            Register
                        </button>
                    </div>

                    <div v-else>
                        <div style="background: #f0fdf4; padding: 10px; border-radius: 5px; margin-bottom: 20px;">
                            <div style="font-weight: 600; color: #065f46;">Logged in as:</div>
                            <div style="color: #059669; font-size: 18px;">{{ userId }}</div>
                        </div>

                        <div v-if="!currentRoomId">
                            <h3 style="margin-bottom: 10px; color: #374151;">Room Options</h3>
                            <div class="input-group">
                                <label>Room ID</label>
                                <input 
                                    v-model="roomInput" 
                                    type="text" 
                                    placeholder="Enter room ID"
                                />
                            </div>
                            <div style="display: flex; gap: 10px; margin-bottom: 20px;">
                                <button 
                                    class="btn btn-success" 
                                    @click="createRoom"
                                    :disabled="!roomInput.trim()"
                                >
                                    Create
                                </button>
                                <button 
                                    class="btn btn-primary" 
                                    @click="joinRoom"
                                    :disabled="!roomInput.trim()"
                                >
                                    Join
                                </button>
                            </div>
                        </div>

                        <UserList 
                            :users="users"
                            :currentUserId="userId"
                            @call-user="callUser"
                        />
                    </div>
                </div>

                <div class="content">
                    <div v-if="errorMessage" class="error-message">
                        {{ errorMessage }}
                    </div>
                    <div v-if="successMessage" class="success-message">
                        {{ successMessage }}
                    </div>

                    <div v-if="currentRoomId" class="room-info">
                        <h3>?? Current Room</h3>
                        <p>{{ currentRoomId }}</p>
                    </div>

                    <div v-if="isRegistered">
                        <VideoGrid 
                            :localStream="localStream"
                            :remoteStreams="remoteStreams"
                            :localUserId="userId"
                        />

                        <div class="controls" v-if="localStream">
                            <button 
                                class="btn"
                                :class="isAudioEnabled ? 'btn-primary' : 'btn-danger'"
                                @click="toggleAudio"
                            >
                                {{ isAudioEnabled ? '?? Mute' : '?? Unmute' }}
                            </button>
                            <button 
                                class="btn"
                                :class="isVideoEnabled ? 'btn-primary' : 'btn-danger'"
                                @click="toggleVideo"
                            >
                                {{ isVideoEnabled ? '?? Stop Video' : '?? Start Video' }}
                            </button>
                            <button 
                                v-if="currentRoomId"
                                class="btn btn-danger" 
                                @click="leaveRoom"
                            >
                                ?? Leave Room
                            </button>
                            <button 
                                v-else-if="isInCall"
                                class="btn btn-danger" 
                                @click="endCall"
                            >
                                ?? End Call
                            </button>
                        </div>
                    </div>

                    <div v-else style="text-align: center; padding: 60px 20px; color: #9ca3af;">
                        <h2 style="margin-bottom: 10px;">Welcome to Video Chat</h2>
                        <p>Please register with a user ID to start</p>
                    </div>
                </div>
            </div>
        </div>
    `
});

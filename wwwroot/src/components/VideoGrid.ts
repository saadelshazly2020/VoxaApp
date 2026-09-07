import { defineComponent, PropType } from 'vue';

export interface VideoStreamInfo {
    userId: string;
    stream: MediaStream;
}

export default defineComponent({
    name: 'VideoGrid',
    props: {
        localStream: {
            type: Object as PropType<MediaStream | null>,
            default: null
        },
        remoteStreams: {
            type: Object as PropType<Record<string, MediaStream>>,
            required: true
        },
        localUserId: {
            type: String,
            required: true
        }
    },
    mounted() {
        this.updateVideoElements();
    },
    updated() {
        this.updateVideoElements();
    },
    methods: {
        updateVideoElements() {
            // Update local video
            if (this.localStream) {
                const localVideo = this.$refs.localVideo as HTMLVideoElement;
                if (localVideo && localVideo.srcObject !== this.localStream) {
                    localVideo.srcObject = this.localStream;
                    localVideo.muted = true;
                    
                    // Handle autoplay restrictions
                    localVideo.play().catch(error => {
                        console.warn('Local video autoplay failed:', error);
                    });
                }
            }

            // Update remote videos
            Object.entries(this.remoteStreams).forEach(([userId, stream]) => {
                const videoElement = this.$refs[`remote-${userId}`] as HTMLVideoElement | undefined;
                if (videoElement && Array.isArray(videoElement)) {
                    const video = videoElement[0] as HTMLVideoElement;
                    if (video && video.srcObject !== stream) {
                        video.srcObject = stream;
                        
                        // Handle autoplay restrictions
                        video.play().catch(error => {
                            console.warn(`Remote video autoplay failed for ${userId}:`, error);
                            // Add user interaction to enable playback
                            video.onclick = () => {
                                video.play();
                                video.onclick = null;
                            };
                        });
                    }
                }
            });
        }
    },
    template: `
        <div class="video-grid">
            <div v-if="localStream" class="video-container">
                <video 
                    ref="localVideo"
                    autoplay
                    playsinline
                    muted
                ></video>
                <div class="video-label">{{ localUserId }} (You)</div>
            </div>
            
            <div 
                v-for="(stream, userId) in remoteStreams" 
                :key="userId"
                class="video-container"
            >
                <video 
                    :ref="'remote-' + userId"
                    autoplay
                    playsinline
                ></video>
                <div class="video-label">{{ userId }}</div>
            </div>
        </div>
    `
});

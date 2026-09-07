import { defineComponent, PropType } from 'vue';
import { User } from '../models/user.model';

export default defineComponent({
    name: 'UserList',
    props: {
        users: {
            type: Array as PropType<User[]>,
            required: true
        },
        currentUserId: {
            type: String,
            required: true
        }
    },
    emits: ['call-user', 'invite-to-room'],
    setup(props, { emit }) {
        const callUser = (userId: string) => {
            emit('call-user', userId);
        };

        const inviteToRoom = (userId: string) => {
            emit('invite-to-room', userId);
        };

        return {
            callUser,
            inviteToRoom
        };
    },
    template: `
        <div class="user-list">
            <h3>Connected Users ({{ users.length }})</h3>
            <div v-if="users.length === 0" style="color: #9ca3af; padding: 20px; text-align: center;">
                No other users connected
            </div>
            <div 
                v-for="user in users" 
                :key="user.userId"
                class="user-item"
                v-show="user.userId !== currentUserId"
            >
                <div>
                    <div class="user-name">{{ user.userId }}</div>
                    <div v-if="user.currentRoomId" style="font-size: 11px; color: #6b7280;">
                        In room: {{ user.currentRoomId }}
                    </div>
                </div>
                <div class="user-actions">
                    <button 
                        class="btn btn-primary"
                        @click="callUser(user.userId)"
                        style="width: auto;"
                    >
                        Call
                    </button>
                </div>
            </div>
        </div>
    `
});

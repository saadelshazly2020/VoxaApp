# ?? Ringing Mechanism - Feature Documentation

## ? Feature Overview

Added a complete call notification system with accept/reject functionality for incoming calls, including visual and audio feedback.

---

## ?? What Was Added

### 1. **Server-Side (SignalR Hub)**

Added four new methods to `VideoCallHub.cs`:

| Method | Parameters | Description |
|--------|------------|-------------|
| `AcceptCall` | callerUserId | Notify caller that call was accepted |
| `RejectCall` | callerUserId, reason | Notify caller that call was rejected |
| `CancelCall` | targetUserId | Cancel an outgoing call |
| Updated `CallUser` | targetUserId, offer | Now sends `IncomingCall` notification |

### 2. **Client-Side Services**

#### SignalR Service Updates
Added event handlers for:
- `IncomingCall` - Triggered when receiving a call
- `CallAccepted` - Triggered when callee accepts
- `CallRejected` - Triggered when callee rejects
- `CallCancelled` - Triggered when caller cancels

Added methods:
```typescript
acceptCall(callerId: string): Promise<void>
rejectCall(callerId: string, reason?: string): Promise<void>
cancelCall(targetUserId: string): Promise<void>
```

#### WebRTC Service Updates
- Added `pendingOffers` map to store offers until user accepts
- Modified `ReceiveOffer` handler to NOT auto-accept (waits for user)
- Added methods: `acceptCall()`, `rejectCall()`, `cancelCall()`
- Added events: `incomingCall`, `callAccepted`, `callRejected`, `callCancelled`

### 3. **UI Components**

#### IncomingCallDialog.vue
Beautiful modal dialog for incoming calls with:
- ? Animated phone icon
- ? Caller name display
- ? Ringing animation (3 pulsing dots)
- ? Accept button (green)
- ? Decline button (red)
- ? Timer showing elapsed time
- ? Optional ringtone support
- ? Bounce-in animation

#### OutgoingCallDialog.vue
Modal dialog for outgoing calls with:
- ? Animated phone icon
- ? Callee name display
- ? "Calling..." status
- ? Loading animation
- ? Cancel button
- ? Timer showing elapsed time

---

## ?? Call Flow

### Scenario 1: Successful Call

```
???????????                                           ???????????
? Alice   ?                                           ?  Bob    ?
???????????                                           ???????????
     ?                                                      ?
     ? 1. Clicks "Call Bob"                                ?
     ???????????????????????????????????????????????????????
     ?    OutgoingCallDialog shows                         ?
     ?    CallUser(Bob, offer)                             ?
     ?                                                      ?
     ?                                       IncomingCall  ?
     ???????????????????????????????????????????????????????
     ?                                 IncomingCallDialog  ?
     ?                                         shows        ?
     ?                                                      ?
     ?                                    2. Bob clicks    ?
     ?                                       "Accept"      ?
     ???????????????????????????????????????????????????????
     ?    CallAccepted                                     ?
     ?    OutgoingCallDialog closes                        ?
     ?                                                      ?
     ?                                    AnswerCall       ?
     ???????????????????????????????????????????????????????
     ?    ReceiveAnswer                                    ?
     ?                                                      ?
     ?    3. WebRTC connection established                 ?
     ???????????????????????????????????????????????????????
     ?                                                      ?
```

### Scenario 2: Rejected Call

```
???????????                                           ???????????
? Alice   ?                                           ?  Bob    ?
???????????                                           ???????????
     ?                                                      ?
     ? 1. Clicks "Call Bob"                                ?
     ???????????????????????????????????????????????????????
     ?    OutgoingCallDialog shows                         ?
     ?                                                      ?
     ?                                       IncomingCall  ?
     ???????????????????????????????????????????????????????
     ?                                 IncomingCallDialog  ?
     ?                                         shows        ?
     ?                                                      ?
     ?                                    2. Bob clicks    ?
     ?                                      "Decline"      ?
     ???????????????????????????????????????????????????????
     ?    CallRejected("User declined")                    ?
     ?    OutgoingCallDialog closes                        ?
     ?    Error message shows                              ?
     ?                                                      ?
```

### Scenario 3: Cancelled Call

```
???????????                                           ???????????
? Alice   ?                                           ?  Bob    ?
???????????                                           ???????????
     ?                                                      ?
     ? 1. Clicks "Call Bob"                                ?
     ???????????????????????????????????????????????????????
     ?    OutgoingCallDialog shows                         ?
     ?                                                      ?
     ?                                       IncomingCall  ?
     ???????????????????????????????????????????????????????
     ?                                 IncomingCallDialog  ?
     ?                                         shows        ?
     ?                                                      ?
     ? 2. Alice clicks "Cancel"                            ?
     ???????????????????????????????????????????????????????
     ?    CancelCall                                       ?
     ?    OutgoingCallDialog closes                        ?
     ?                                                      ?
     ?                                    CallCancelled    ?
     ? ?????????????????????????????????????????????????????
     ?                                IncomingCallDialog   ?
     ?                                        closes       ?
     ?                                                      ?
```

---

## ?? UI/UX Features

### IncomingCallDialog
- **Visual Design**: Clean, modern modal with rounded corners
- **Animation**: Bounce-in effect when appearing
- **Icons**: Animated phone icon with pulse effect
- **Status**: Ringing animation with 3 pulsing dots
- **Buttons**: Large, accessible accept (green) and decline (red) buttons
- **Timer**: Shows elapsed time since call started
- **Sound**: Optional ringtone support (commented out, can be enabled)
- **Backdrop**: Semi-transparent black overlay (50% opacity)
- **Z-index**: 50 (above all other content)

### OutgoingCallDialog
- **Visual Design**: Similar to incoming call dialog
- **Animation**: Phone icon with pulse
- **Status**: "Calling..." message with loading dots
- **Button**: Red cancel button
- **Timer**: Shows elapsed time
- **Message**: "Waiting for {name} to answer"

---

## ?? API Reference

### Server Methods

#### AcceptCall
```csharp
Task AcceptCall(string callerUserId)
```
**Called by**: Callee when accepting the call
**Sends to caller**: `CallAccepted` event

#### RejectCall
```csharp
Task RejectCall(string callerUserId, string reason = "Call rejected")
```
**Called by**: Callee when rejecting the call
**Sends to caller**: `CallRejected` event with reason

#### CancelCall
```csharp
Task CancelCall(string targetUserId)
```
**Called by**: Caller when cancelling the call
**Sends to callee**: `CallCancelled` event

### Client Methods

#### acceptCall
```typescript
async acceptCall(senderId: string): Promise<void>
```
Accepts incoming call from specified user

#### rejectCall
```typescript
async rejectCall(senderId: string, reason?: string): Promise<void>
```
Rejects incoming call with optional reason

#### cancelCall
```typescript
async cancelCall(userId: string): Promise<void>
```
Cancels outgoing call to specified user

### Client Events

```typescript
// Triggered when receiving a call
webRTCService.on('incomingCall', (fromUserId: string) => { });

// Triggered when callee accepts
webRTCService.on('callAccepted', (fromUserId: string) => { });

// Triggered when callee rejects
webRTCService.on('callRejected', (fromUserId: string, reason: string) => { });

// Triggered when caller cancels
webRTCService.on('callCancelled', (fromUserId: string) => { });
```

---

## ?? Testing Instructions

### Test 1: Accept Call Flow
1. **Setup**: Open two browser tabs (Alice and Bob)
2. **Register**: Both users register
3. **Call**: Alice clicks "Call" next to Bob's name
4. **Verify**: 
   - Alice sees "Outgoing Call" dialog
   - Bob sees "Incoming Call" dialog with ringing animation
   - Timer starts counting
5. **Accept**: Bob clicks "Accept"
6. **Verify**:
   - Both dialogs close
   - Video connection establishes
   - Both see each other's video

### Test 2: Reject Call Flow
1. **Setup**: Same as Test 1
2. **Call**: Alice calls Bob
3. **Verify**: Both see dialogs
4. **Reject**: Bob clicks "Decline"
5. **Verify**:
   - Both dialogs close
   - Alice sees error: "Bob rejected your call"
   - No WebRTC connection established

### Test 3: Cancel Call Flow
1. **Setup**: Same as Test 1
2. **Call**: Alice calls Bob
3. **Verify**: Both see dialogs
4. **Cancel**: Alice clicks "Cancel Call"
5. **Verify**:
   - Alice's dialog closes
   - Bob's dialog closes
   - Bob sees message: "Alice cancelled the call"

### Test 4: Multiple Calls
1. **Setup**: Three users (Alice, Bob, Charlie)
2. **Call 1**: Alice calls Bob
3. **While ringing**: Charlie calls Alice
4. **Verify**:
   - Alice sees outgoing call to Bob
   - Alice receives incoming call from Charlie
   - Can handle both independently

---

## ?? Adding Ringtone (Optional)

### Step 1: Add Sound File
```
client-app/
??? public/
    ??? sounds/
        ??? ringtone.mp3
```

### Step 2: Uncomment in IncomingCallDialog.vue
```typescript
audioElement = new Audio();
audioElement.src = '/sounds/ringtone.mp3'; // Uncomment this line
audioElement.loop = true;
audioElement.play().catch(err => console.log('Audio play failed:', err));
```

### Recommended Ringtone Settings
- **Duration**: 2-5 seconds (loops)
- **Format**: MP3 or WAV
- **Volume**: Moderate (not too loud)
- **Size**: <100KB for fast loading

---

## ?? Customization Options

### Change Colors
```css
/* Accept button - change green to custom color */
.bg-green-500 { background-color: #your-color; }

/* Decline button - change red to custom color */
.bg-red-500 { background-color: #your-color; }

/* Animation dots color */
.bg-green-500 { background-color: #your-color; }
```

### Change Animation Speed
```typescript
// Timer update interval (default: 1000ms = 1 second)
timer = window.setInterval(() => {
  elapsedTime.value++;
}, 1000); // Change this value
```

### Auto-Reject After Timeout
```typescript
// In IncomingCallDialog.vue
const maxRingTime = 30; // 30 seconds

watch(() => elapsedTime.value, (time) => {
  if (time >= maxRingTime) {
    handleReject();
  }
});
```

---

## ?? Configuration

### Adjust Dialog Appearance
```vue
<!-- In IncomingCallDialog.vue -->
<div class="bg-white rounded-lg shadow-2xl p-8 max-w-md w-full">
  <!-- Change max-w-md to max-w-lg for larger dialog -->
  <!-- Change p-8 to p-10 for more padding -->
</div>
```

### Adjust Z-Index
```vue
<!-- If dialog is hidden behind other elements -->
<div class="fixed inset-0 ... z-50">
  <!-- Change z-50 to z-[100] for higher priority -->
</div>
```

---

## ?? Troubleshooting

### Issue: Dialog Not Showing
**Cause**: Z-index conflict or Vue reactivity issue
**Solution**: 
```typescript
// Ensure show prop is reactive
const showIncomingCall = ref<boolean>(false);

// Force update if needed
nextTick(() => {
  showIncomingCall.value = true;
});
```

### Issue: Ringtone Not Playing
**Cause**: Browser autoplay policy
**Solution**: 
- User must interact with page first
- Use `.catch()` to handle autoplay failures
- Consider vibration API as fallback

### Issue: Multiple Dialogs
**Cause**: Not clearing previous state
**Solution**:
```typescript
const closeAllDialogs = () => {
  showIncomingCall.value = false;
  showOutgoingCall.value = false;
  incomingCallerId.value = '';
  outgoingCalleeId.value = '';
};
```

### Issue: Timer Not Stopping
**Cause**: Timer not cleared properly
**Solution**:
```typescript
onUnmounted(() => {
  stopRinging(); // Always cleanup on unmount
});
```

---

## ?? Files Changed/Created

### Created Files
1. `client-app/src/components/IncomingCallDialog.vue` ? NEW
2. `client-app/src/components/OutgoingCallDialog.vue` ? NEW
3. `RINGING_FEATURE_DOCS.md` ? NEW (this file)

### Modified Files
1. `Hubs/VideoCallHub.cs` - Added AcceptCall, RejectCall, CancelCall methods
2. `client-app/src/services/signalr.service.ts` - Added call notification events/methods
3. `client-app/src/services/webrtc.service.ts` - Added call state management
4. `client-app/src/views/VideoChat.vue` - Integrated call dialogs

---

## ?? Next Steps

### Recommended Enhancements
1. **Call History**: Store accepted/rejected calls
2. **Do Not Disturb**: Toggle to auto-reject calls
3. **Custom Ringtones**: Let users choose ringtones
4. **Vibration**: Use Vibration API for mobile devices
5. **Call Waiting**: Handle multiple simultaneous calls
6. **Busy Status**: Show when user is already in a call
7. **Contact Photos**: Show caller's profile picture
8. **Call Statistics**: Track call duration, quality, etc.

### Mobile Optimizations
1. Add haptic feedback
2. Use native notification API
3. Optimize dialog size for small screens
4. Add swipe gestures for accept/reject

---

## ? Feature Status

- ? Server-side call notifications
- ? Client-side event handling
- ? Incoming call dialog with ringing
- ? Outgoing call dialog
- ? Accept/reject/cancel functionality
- ? Visual animations
- ? Timer display
- ? Error handling
- ? Ringtone (optional, ready to enable)
- ? Call history (future)
- ? Do not disturb (future)

---

## ?? Usage Example

```vue
<template>
  <!-- In your video chat component -->
  <IncomingCallDialog
    :show="showIncomingCall"
    :callerName="incomingCallerId"
    @accept="acceptIncomingCall"
    @reject="rejectIncomingCall"
  />
  
  <OutgoingCallDialog
    :show="showOutgoingCall"
    :calleeName="outgoingCalleeId"
    @cancel="cancelOutgoingCall"
  />
</template>

<script setup>
const showIncomingCall = ref(false);
const incomingCallerId = ref('');

// Listen for incoming calls
webRTCService.on('incomingCall', (fromUserId) => {
  incomingCallerId.value = fromUserId;
  showIncomingCall.value = true;
});

// Handle accept
const acceptIncomingCall = async () => {
  await webRTCService.acceptCall(incomingCallerId.value);
  showIncomingCall.value = false;
};

// Handle reject
const rejectIncomingCall = async () => {
  await webRTCService.rejectCall(incomingCallerId.value);
  showIncomingCall.value = false;
};
</script>
```

---

**Feature complete and ready for testing! ??**

To test:
1. Stop the currently running backend
2. Rebuild: `dotnet build`
3. Start backend: `dotnet run`
4. Start frontend: `cd client-app && npm run dev`
5. Test the call flows above!

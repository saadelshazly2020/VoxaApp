# Chat Feature - Quick Testing Guide

## ?? Quick Start

### 1. Start the Application
```bash
# Backend
dotnet run

# Frontend (if separate)
cd client-app
npm run dev
```

### 2. Open Two Browser Windows
- Window 1: User A (e.g., http://localhost:5274)
- Window 2: User B (e.g., http://localhost:5274 in incognito)

---

## ? Test Checklist

### Test 1: Real-Time Message Delivery
**Steps**:
1. User A logs in and opens chat with User B
2. User B logs in and opens chat with User A
3. User A sends message: "Hello!"
4. **Check User B's screen immediately**

**Expected Result**: ?
- Message appears **instantly** on User B's screen
- No page refresh needed
- Message displays in correct order at bottom

**If Failed**: ?
- Check browser console for SignalR errors
- Verify SignalR connection shows "connected"
- Check server logs for message delivery

---

### Test 2: Message Order
**Steps**:
1. Open existing chat with 5+ messages
2. Scroll through messages
3. Identify oldest and newest messages

**Expected Result**: ?
- **Oldest message at TOP**
- **Newest message at BOTTOM**
- Matches standard chat app behavior (WhatsApp, Messenger, etc.)

**Visual Check**:
```
???????????????????????????
? [09:00 AM] Old msg 1   ? ? TOP (Oldest)
? [09:01 AM] Old msg 2   ?
? [09:02 AM] Recent msg  ?
? [09:03 AM] New msg     ? ? BOTTOM (Newest)
? [Type message...]      ?
???????????????????????????
```

**If Failed**: ?
- Clear browser cache
- Hard refresh (Ctrl + Shift + R)
- Check API response in Network tab

---

### Test 3: Online Status Indicator
**Steps**:
1. User A views conversation list
2. User B is **offline** (logged out)
3. **Check**: User B should have **gray dot**
4. User B **logs in**
5. **Observe User A's screen immediately**

**Expected Result**: ?
- User B's dot turns **green** instantly
- No page refresh needed
- Status updates in real-time

**Visual Check**:
```
Offline: ? (gray dot)
Online:  ?? (green dot)
```

**Steps (Offline Test)**:
1. User B closes browser tab
2. **Observe User A's screen within 5 seconds**
3. User B's dot should turn **gray**

**If Failed**: ?
- Check SignalR connection status
- Verify `UserOnlineStatusChanged` event in console
- Check server logs for disconnect events

---

### Test 4: Smart Scrolling (No Page Jump)
**Steps**:
1. Open chat with 10+ messages
2. Scroll up to read **old messages**
3. **Stay scrolled up** (don't scroll to bottom)
4. Receive a **new message** from other user

**Expected Result**: ?
- New message arrives silently
- **No auto-scroll** (user stays at old messages)
- **No page jump or flicker**
- User can continue reading

**Steps (Part 2)**:
1. **Manually scroll to bottom**
2. Receive another new message

**Expected Result**: ?
- Message appears at bottom
- **Smooth auto-scroll** to show new message
- Scrolling animation is smooth, not instant

**If Failed**: ?
- Check `scrolledToBottom` tracking
- Verify smooth scroll is enabled
- Check for duplicate messages

---

### Test 5: Load More Messages (Pagination)
**Steps**:
1. Open chat with 50+ messages
2. Scroll to **very top**
3. **Stay at top** for 1 second
4. Older messages should load automatically

**Expected Result**: ?
- Older messages appear **above** current messages
- Scroll position **maintained** (no jump)
- User stays at same message they were reading

**Visual Check**:
```
Before:
???????????????????????????
? [09:00 AM] Msg 50      ? ? TOP (oldest visible)
? [09:01 AM] Msg 51      ?
? ...                    ?

After loading more:
???????????????????????????
? [08:55 AM] Msg 40      ? ? NEW older messages
? [08:56 AM] Msg 41      ?
? ...                    ?
? [09:00 AM] Msg 50      ? ? User still at same message
? [09:01 AM] Msg 51      ?
```

**If Failed**: ?
- Check scroll handler is attached
- Verify message count > 50 before loading
- Check previousHeight calculation

---

### Test 6: No Duplicate Messages
**Steps**:
1. User A and User B both have chat open
2. User A sends message
3. **Watch both screens carefully**

**Expected Result**: ?
- User A sees message **once** (sent message)
- User B sees message **once** (received message)
- No duplicate messages on either screen

**If Failed**: ?
- Check duplicate detection in `handleNewMessage`
- Verify message ID comparison
- Check SignalR isn't firing multiple times

---

### Test 7: Unread Count Updates
**Steps**:
1. User A opens dashboard (conversation list visible)
2. User B sends message to User A
3. **Observe conversation list immediately**

**Expected Result**: ?
- Unread badge appears **instantly**
- Count increments by 1
- Conversation moves to top of list
- Red badge shows correct number

**Steps (Part 2)**:
1. User A opens the conversation
2. **Observe unread badge**

**Expected Result**: ?
- Badge **disappears** when chat is opened
- Messages marked as read automatically

**If Failed**: ?
- Check `ReceiveMessage` handler in ConversationsList
- Verify `refreshConversations` is called
- Check mark-as-read API call

---

## ?? Common Issues & Solutions

### Issue: SignalR Not Connecting
**Symptoms**:
- Messages not delivered in real-time
- Online status not updating

**Solution**:
```typescript
// Check browser console for:
"SignalR connected for chat" ?
// or
"Failed to setup SignalR: [error]" ?

// If failed, check:
1. Backend is running
2. /videocallhub endpoint is accessible
3. CORS settings allow connection
```

### Issue: Messages in Wrong Order
**Symptoms**:
- Newest messages at top
- Oldest messages at bottom

**Solution**:
1. Clear browser cache
2. Hard refresh (Ctrl + Shift + R)
3. Check API response includes `.Reverse()`

### Issue: Page Still Jumping
**Symptoms**:
- Chat jumps when new message arrives
- Annoying scroll behavior

**Solution**:
1. Check `scrolledToBottom` ref is defined
2. Verify `handleScroll` function is attached
3. Ensure `smooth` parameter is passed correctly

---

## ?? Performance Metrics

### Expected Response Times
- **Send Message**: < 100ms
- **Real-time Delivery**: < 500ms
- **Online Status Update**: < 1 second
- **Load Messages**: < 200ms
- **Mark as Read**: < 100ms

### SignalR Connection
- **Initial Connect**: < 2 seconds
- **Reconnect**: < 5 seconds
- **Ping Interval**: 15 seconds
- **Timeout**: 30 seconds

---

## ?? Success Criteria

All tests should pass for production deployment:

- [x] Real-time message delivery works
- [x] Message order correct (old?new, top?bottom)
- [x] Online status updates in real-time
- [x] No page jumping or refresh
- [x] Smooth scrolling behavior
- [x] Smart auto-scroll (only when at bottom)
- [x] Pagination loads more messages
- [x] Scroll position maintained on load more
- [x] No duplicate messages
- [x] Unread counts update correctly

---

## ?? Test Report Template

```
Test Date: __________
Tester: __________

| Test | Status | Notes |
|------|--------|-------|
| Real-time Delivery | ?/? | |
| Message Order | ?/? | |
| Online Status | ?/? | |
| Smart Scrolling | ?/? | |
| Load More | ?/? | |
| No Duplicates | ?/? | |
| Unread Count | ?/? | |

Overall: PASS / FAIL

Additional Notes:
________________________________
________________________________
```

---

## ?? Emergency Rollback

If critical issues found in production:

1. Stop the application
2. Revert to previous Git commit:
   ```bash
   git log --oneline
   git revert <commit-hash>
   ```
3. Restart application
4. Report issue in GitHub

---

**Ready to test? Start with Test 1!** ??

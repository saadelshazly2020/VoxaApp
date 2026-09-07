# ?? Posts Feature - Quick Reference

## ? 30-Second Start

```bash
dotnet run
```

Open: `http://localhost:5274` ? **Posts tab is already active!** ??

---

## ?? Quick Actions

### Create a Post
1. Type in "What's on your mind?"
2. Press **Ctrl+Enter** or click **Post**

### React to Post
1. Click **React** button
2. Choose emoji: ?? ?? ?? ?? ?? ??

### Add Comment
1. Click **Comment** button
2. Type comment
3. Press **Ctrl+Enter** or click **Comment**

### Edit Your Post
1. Click **?** menu on your post
2. Choose **Edit**
3. Make changes
4. Click **Save**

### Delete Your Post
1. Click **?** menu
2. Choose **Delete**
3. Confirm

---

## ?? Key Features

| Feature | Status | Shortcut |
|---------|--------|----------|
| Create Post | ? | Ctrl+Enter |
| React (6 types) | ? | Click React |
| Comment | ? | Ctrl+Enter |
| Edit Post | ? | Menu ? Edit |
| Delete Post | ? | Menu ? Delete |
| Load More | ? | Click button |
| Real-Time Updates | ? | Automatic |

---

## ?? Technical Stack

```
Backend:  .NET 9 + EF Core + SQLite
Frontend: Vue 3 + TypeScript + Tailwind CSS
Real-Time: SignalR
Auth:     JWT Bearer tokens
```

---

## ?? API Endpoints

```
GET    /api/posts/feed              ? Get posts
POST   /api/posts                   ? Create post
PUT    /api/posts/{id}              ? Edit post
DELETE /api/posts/{id}              ? Delete post
POST   /api/posts/{id}/react        ? React to post
DELETE /api/posts/{id}/react        ? Remove reaction
POST   /api/posts/{id}/comments     ? Add comment
GET    /api/posts/{id}/comments     ? Get comments
PUT    /api/posts/comments/{id}     ? Edit comment
DELETE /api/posts/comments/{id}     ? Delete comment
```

---

## ?? Components

```
Dashboard.vue
  ??? PostFeed.vue                   Main feed container
        ??? CreatePost.vue           Create new posts
        ??? PostCard.vue             Display individual posts
              ??? PostComments.vue   Comments section
```

---

## ?? Quick Test

### Test Real-Time
1. Open app in **two browser tabs**
2. Tab 1: Create a post
3. Tab 2: **Post appears instantly!** ?
4. Tab 2: React with ??
5. Tab 1: **Reaction updates in real-time!** ?

---

## ?? Reaction Types

| Type | Emoji | Code |
|------|-------|------|
| Like | ?? | `'Like'` |
| Love | ?? | `'Love'` |
| Haha | ?? | `'Haha'` |
| Wow | ?? | `'Wow'` |
| Sad | ?? | `'Sad'` |
| Angry | ?? | `'Angry'` |

---

## ?? Limits

| Item | Max Length |
|------|------------|
| Post Content | 2000 chars |
| Comment | 1000 chars |
| Image URL | 500 chars |
| Posts per page | 20 |
| Comments per page | 20 |

---

## ?? Quick Fixes

### Posts not loading?
```typescript
// Check: Browser Console ? Look for errors
// Fix: Verify token in localStorage['auth_token']
```

### Real-time not working?
```typescript
// Check: Console for "SignalR setup" log
// Fix: Refresh page to reconnect
```

### Can't create post?
```typescript
// Check: Content length < 2000 chars
// Fix: Shorten content
```

---

## ?? Code Snippets

### Import Service
```typescript
import { postsService } from '@/services/posts.service';
```

### Create Post
```typescript
const result = await postsService.createPost('My post content');
if (result.success) {
  console.log('Created:', result.post);
}
```

### React
```typescript
await postsService.reactToPost(postId, 'Love');
```

### Comment
```typescript
const result = await postsService.addComment(postId, 'Nice!');
if (result.success) {
  console.log('Comment added:', result.comment);
}
```

---

## ?? Tailwind Classes Used

```css
/* Cards */
bg-white rounded-xl shadow-lg p-6

/* Buttons */
bg-gradient-to-r from-blue-600 to-purple-600 text-white

/* Avatars */
bg-gradient-to-br from-blue-400 to-purple-500 rounded-full

/* Inputs */
border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500
```

---

## ?? Responsive Breakpoints

```css
Mobile:  default (< 768px)
Tablet:  md: (768px+)
Desktop: lg: (1024px+)
```

All components fully responsive!

---

## ?? Demo Data

### Sample Post
```json
{
  "id": 1,
  "content": "Hello world! ??",
  "author": {
    "username": "john_doe",
    "displayName": "John Doe"
  },
  "totalReactions": 5,
  "totalComments": 3,
  "myReaction": "Like"
}
```

---

## ?? Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Submit Post | Ctrl+Enter |
| Submit Comment | Ctrl+Enter |
| Close Menu | Esc or Click Outside |

---

## ?? SignalR Events

### Received Events
```typescript
NewPost              ? New post created
PostUpdated          ? Post edited
PostDeleted          ? Post deleted
PostReactionUpdated  ? New reaction
PostReactionRemoved  ? Reaction removed
NewComment           ? Comment added
CommentDeleted       ? Comment removed
```

---

## ?? Pro Tips

1. **Use Ctrl+Enter** for quick posting
2. **Click reaction again** to remove it
3. **Auto-refresh** happens every 60s
4. **Menu closes** when clicking outside
5. **Timestamps update** automatically

---

## ?? Success Indicators

? Posts tab visible in navigation  
? Can create posts with/without images  
? Reactions work (click, change, remove)  
? Comments load and expand  
? Edit/delete only on own posts  
? Real-time updates working  
? Mobile responsive  
? No console errors  

---

## ?? Quick Help

**Can't see Posts tab?**
? Refresh browser, check if logged in

**Posts not loading?**
? Check backend is running (`dotnet run`)

**Real-time not working?**
? Check console for SignalR connection logs

---

## ?? You're Ready!

Everything works out of the box. Just:

1. **Run**: `dotnet run`
2. **Open**: `http://localhost:5274`
3. **Post**: Start sharing! ???

---

**Full Documentation**: `POSTS_FRONTEND_COMPLETE.md`

*Feature Status: ? Complete*

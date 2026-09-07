# Quick Start Guide - Testing Friendship Feature

## Prerequisites
- .NET 9 SDK installed
- SQLite database (automatically created)
- API testing tool (Postman, curl, or REST Client)

## Running the Application

```bash
cd D:\POC\VideoChatingApp.WebRTC
dotnet run
```

The API will be available at: `https://localhost:5274` or `http://localhost:5274`

## Testing Workflow

### Step 1: Register Two Users

**Register User 1 (Alice)**
```http
POST http://localhost:5274/api/auth/register
Content-Type: application/json

{
  "username": "alice",
  "email": "alice@example.com",
  "password": "password123"
}
```

**Expected Response:**
```json
{
  "message": "Registration successful",
  "userId": 1
}
```

**Register User 2 (Bob)**
```http
POST http://localhost:5274/api/auth/register
Content-Type: application/json

{
  "username": "bob",
  "email": "bob@example.com",
  "password": "password123"
}
```

**Expected Response:**
```json
{
  "message": "Registration successful",
  "userId": 2
}
```

### Step 2: Login as Alice

```http
POST http://localhost:5274/api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "password123"
}
```

**Expected Response:**
```json
{
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "alice",
    "email": "alice@example.com",
    "displayName": "alice",
    "isOnline": true
  }
}
```

**Save the token** - You'll need it for subsequent requests!

### Step 3: Send Friend Request (Alice ? Bob)

```http
POST http://localhost:5274/api/friendship/send-request
Authorization: Bearer {alice_token}
Content-Type: application/json

{
  "receiverId": 2,
  "message": "Hi Bob! Let's be friends!"
}
```

**Expected Response:**
```json
{
  "message": "Friend request sent successfully"
}
```

### Step 4: Login as Bob

```http
POST http://localhost:5274/api/auth/login
Content-Type: application/json

{
  "email": "bob@example.com",
  "password": "password123"
}
```

**Save Bob's token!**

### Step 5: Check Pending Requests (Bob)

```http
GET http://localhost:5274/api/friendship/pending-requests
Authorization: Bearer {bob_token}
```

**Expected Response:**
```json
{
  "requests": [
    {
      "id": 1,
      "sender": {
        "id": 1,
        "username": "alice",
        "displayName": "alice",
        "profilePictureUrl": null
      },
      "message": "Hi Bob! Let's be friends!",
      "createdAt": "2024-02-26T19:30:00Z"
    }
  ]
}
```

### Step 6: Accept Friend Request (Bob)

```http
POST http://localhost:5274/api/friendship/accept-request/1
Authorization: Bearer {bob_token}
```

**Expected Response:**
```json
{
  "message": "Friend request accepted"
}
```

### Step 7: Get Friends List (Alice)

```http
GET http://localhost:5274/api/friendship/list
Authorization: Bearer {alice_token}
```

**Expected Response:**
```json
{
  "friends": [
    {
      "id": 2,
      "username": "bob",
      "displayName": "bob",
      "isOnline": true,
      "profilePictureUrl": null,
      "lastSeen": "2024-02-26T19:32:00Z"
    }
  ]
}
```

### Step 8: Get Friends List (Bob)

```http
GET http://localhost:5274/api/friendship/list
Authorization: Bearer {bob_token}
```

**Expected Response:**
```json
{
  "friends": [
    {
      "id": 1,
      "username": "alice",
      "displayName": "alice",
      "isOnline": true,
      "profilePictureUrl": null,
      "lastSeen": "2024-02-26T19:30:00Z"
    }
  ]
}
```

## Additional Test Scenarios

### Search for Users

```http
GET http://localhost:5274/api/friendship/search/bob
Authorization: Bearer {alice_token}
```

### Check Sent Requests

```http
GET http://localhost:5274/api/friendship/sent-requests
Authorization: Bearer {alice_token}
```

### Reject Friend Request

```http
POST http://localhost:5274/api/friendship/reject-request/1
Authorization: Bearer {bob_token}
```

### Remove Friend

```http
DELETE http://localhost:5274/api/friendship/remove/2
Authorization: Bearer {alice_token}
```

## Error Scenarios to Test

### 1. Send Friend Request to Yourself
```http
POST http://localhost:5274/api/friendship/send-request
Authorization: Bearer {alice_token}
Content-Type: application/json

{
  "receiverId": 1
}
```
**Expected:** `400 Bad Request` - "You cannot send a friend request to yourself"

### 2. Duplicate Friend Request
Send the same friend request twice
**Expected:** `400 Bad Request` - "A friendship request already exists between you and this user"

### 3. Send Request to Non-existent User
```http
POST http://localhost:5274/api/friendship/send-request
Authorization: Bearer {alice_token}
Content-Type: application/json

{
  "receiverId": 999
}
```
**Expected:** `400 Bad Request` - "User not found"

### 4. Unauthorized Access
Try to access any endpoint without Authorization header
**Expected:** `401 Unauthorized`

## Using Postman

1. Create a new collection called "Friendship API"
2. Add an environment variable `{{base_url}}` = `http://localhost:5274`
3. Add environment variables for tokens:
   - `{{alice_token}}`
   - `{{bob_token}}`
4. Import the requests above
5. Use Test Scripts to automatically save tokens:

```javascript
// In Login request's Test tab
pm.test("Save token", function () {
    var jsonData = pm.response.json();
    pm.environment.set("token", jsonData.token);
});
```

## Using cURL

### Register
```bash
curl -X POST http://localhost:5274/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"alice","email":"alice@example.com","password":"password123"}'
```

### Login
```bash
curl -X POST http://localhost:5274/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"password123"}'
```

### Send Friend Request (replace TOKEN with actual token)
```bash
curl -X POST http://localhost:5274/api/friendship/send-request \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"receiverId":2,"message":"Hi!"}'
```

### Get Friends
```bash
curl -X GET http://localhost:5274/api/friendship/list \
  -H "Authorization: Bearer TOKEN"
```

## Database Inspection

To inspect the SQLite database:

```bash
# Install SQLite CLI if not already installed
# Windows: Download from https://www.sqlite.org/download.html

# Open the database
sqlite3 app.db

# View tables
.tables

# View users
SELECT * FROM Users;

# View friendships
SELECT * FROM Friendships;

# View friend requests
SELECT * FROM FriendshipRequests;

# Exit
.exit
```

## Troubleshooting

### Issue: "Build failed"
**Solution:** Run `dotnet restore` then `dotnet build`

### Issue: "Database is locked"
**Solution:** Stop the application and try again

### Issue: "Unauthorized"
**Solution:** Check that:
- You included the Authorization header
- Token is valid and not expired
- Token format is: `Bearer {token}`

### Issue: "User not found"
**Solution:** Ensure users are registered first and you're using correct user IDs

### Issue: Port already in use
**Solution:** 
- Stop other instances of the application
- Change port in `Properties/launchSettings.json`

## Next Steps

1. ? Test all endpoints manually
2. ? Verify database entries
3. ? Test error scenarios
4. ?? Integrate with frontend Vue.js components
5. ?? Add real-time SignalR notifications

---

**Happy Testing! ??**

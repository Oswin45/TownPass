# Push Notification & Disaster Trigger API Documentation

This document describes the API endpoints for device registration, disaster event triggering, and push notifications via Firebase Cloud Messaging (FCM).

## Base URL
```
http://localhost:5000/api/Notification
```

## Endpoints

### 1. Register Device Token
Register a device to receive push notifications.

**Endpoint:** `POST /api/Notification/RegisterDevice`

**Request Body:**
```json
{
  "token": "fcm_device_token_here",
  "deviceId": "device_unique_id",
  "userId": "user123",
  "platform": "Android"
}
```

**Parameters:**
- `token` (required): FCM device token
- `deviceId` (optional): Unique device identifier
- `userId` (optional): User identifier
- `platform` (optional): Device platform (iOS, Android)

**Response Success (200):**
```json
{
  "success": true,
  "message": "裝置註冊成功",
  "data": {
    "id": 1,
    "isNew": true
  }
}
```

**Example cURL:**
```bash
curl -X POST "http://localhost:5000/api/Notification/RegisterDevice" \
  -H "Content-Type: application/json" \
  -d '{
    "token": "your_fcm_token_here",
    "deviceId": "device_001",
    "userId": "user_123",
    "platform": "Android"
  }'
```

---

### 2. Unregister Device Token
Remove a device from receiving push notifications.

**Endpoint:** `DELETE /api/Notification/UnregisterDevice`

**Query Parameters:**
- `token` (required): FCM device token to unregister

**Response Success (200):**
```json
{
  "success": true,
  "message": "裝置取消註冊成功"
}
```

**Example cURL:**
```bash
curl -X DELETE "http://localhost:5000/api/Notification/UnregisterDevice?token=your_fcm_token_here"
```

---

### 3. Trigger Disaster Event
Create a disaster event at specified coordinates and send push notifications to all registered devices.

**Endpoint:** `POST /api/Notification/TriggerDisaster`

**Request Body:**
```json
{
  "title": "地震警報",
  "description": "台北市發生規模5.6地震，請注意安全",
  "latitude": 25.0330,
  "longitude": 121.5654,
  "tags": ["earthquake", "emergency"],
  "imageBase64": "data:image/png;base64,iVBORw0KG...",
  "sendNotification": true,
  "notificationRadiusKm": 50.0
}
```

**Parameters:**
- `title` (required): Disaster event title
- `description` (required): Disaster event description
- `latitude` (required): Latitude coordinate
- `longitude` (required): Longitude coordinate
- `tags` (optional): Array of tags (default: ["emergency"])
- `imageBase64` (optional): Base64 encoded image
- `sendNotification` (optional): Whether to send push notifications (default: true)
- `notificationRadiusKm` (optional): Notification radius in kilometers (null = all devices)

**Response Success (200):**
```json
{
  "success": true,
  "message": "災害事件觸發成功",
  "data": {
    "disasterId": "550e8400-e29b-41d4-a716-446655440000",
    "title": "地震警報",
    "description": "台北市發生規模5.6地震，請注意安全",
    "latitude": 25.0330,
    "longitude": 121.5654,
    "tags": ["earthquake", "emergency"],
    "createdAt": "2025-11-09T12:34:56Z",
    "notificationsSent": 15
  }
}
```

**Example cURL:**
```bash
curl -X POST "http://localhost:5000/api/Notification/TriggerDisaster" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "地震警報",
    "description": "台北市發生規模5.6地震，請注意安全",
    "latitude": 25.0330,
    "longitude": 121.5654,
    "tags": ["earthquake", "emergency"],
    "sendNotification": true
  }'
```

---

### 4. Get Registered Devices
Retrieve list of all registered devices (for admin purposes).

**Endpoint:** `GET /api/Notification/RegisteredDevices`

**Query Parameters:**
- `activeOnly` (optional): Filter active devices only (default: true)

**Response Success (200):**
```json
{
  "success": true,
  "message": "取得裝置清單成功",
  "data": [
    {
      "id": 1,
      "deviceId": "device_001",
      "userId": "user_123",
      "platform": "Android",
      "isActive": true,
      "createdAt": "2025-11-09T10:00:00Z",
      "updatedAt": "2025-11-09T12:00:00Z",
      "tokenPreview": "fcm_token_preview..."
    }
  ],
  "count": 1
}
```

**Example cURL:**
```bash
curl -X GET "http://localhost:5000/api/Notification/RegisteredDevices?activeOnly=true"
```

---

### 5. Test Notification
Send a test push notification to verify FCM is working.

**Endpoint:** `POST /api/Notification/TestNotification`

**Query Parameters:**
- `token` (optional): Specific FCM token to test (if not provided, uses first active device)

**Response Success (200):**
```json
{
  "success": true,
  "message": "測試通知發送成功"
}
```

**Example cURL:**
```bash
curl -X POST "http://localhost:5000/api/Notification/TestNotification?token=your_fcm_token_here"
```

---

## Error Responses

All endpoints return consistent error responses:

**Validation Error (400):**
```json
{
  "success": false,
  "message": "驗證失敗",
  "errors": ["Error message 1", "Error message 2"]
}
```

**Not Found (404):**
```json
{
  "success": false,
  "message": "找不到該裝置 Token"
}
```

**Server Error (500):**
```json
{
  "success": false,
  "message": "發生錯誤",
  "error": "Detailed error message"
}
```

---

## Setup Instructions

### 1. Install Required NuGet Package
```bash
cd Backend
dotnet add package FirebaseAdmin --version 3.0.1
```

### 2. Configure Firebase
Ensure `google-services.json` is in the Backend project root directory with valid Firebase credentials.

### 3. Update Database
Run the following to create the new DeviceTokens table:
```bash
dotnet ef migrations add AddDeviceTokens
dotnet ef database update
```

Or simply restart the application (it will auto-create tables with `EnsureCreated()`).

### 4. Test the API
1. Register a test device token
2. Trigger a disaster event
3. Verify push notifications are received on the device

---

## Flutter Client Integration

### Register Device Token
```dart
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

Future<void> registerDeviceToken() async {
  final messaging = FirebaseMessaging.instance;
  
  // Request permission
  await messaging.requestPermission();
  
  // Get FCM token
  final token = await messaging.getToken();
  
  if (token != null) {
    // Register with backend
    final response = await http.post(
      Uri.parse('http://your-server/api/Notification/RegisterDevice'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'token': token,
        'platform': 'Android',
        'deviceId': 'device_unique_id',
      }),
    );
    
    if (response.statusCode == 200) {
      print('Device registered successfully');
    }
  }
}
```

### Handle Incoming Notifications
```dart
FirebaseMessaging.onMessage.listen((RemoteMessage message) {
  print('Got a message in the foreground!');
  print('Message data: ${message.data}');

  if (message.notification != null) {
    print('Title: ${message.notification!.title}');
    print('Body: ${message.notification!.body}');
  }
  
  // Handle disaster event
  if (message.data['type'] == 'disaster_alert') {
    final disasterId = message.data['disasterId'];
    final lat = double.parse(message.data['latitude']);
    final lng = double.parse(message.data['longitude']);
    
    // Show disaster on map or navigate to details
    showDisasterAlert(disasterId, lat, lng);
  }
});
```

---

## Notes

- All timestamps are in UTC format (ISO 8601)
- Base64 images should include the data URI prefix (e.g., `data:image/png;base64,`)
- Invalid or expired FCM tokens are automatically deactivated
- The notification service uses multicast messaging for efficiency
- Android notifications use a high-priority channel with sound
- iOS notifications include badge count and sound


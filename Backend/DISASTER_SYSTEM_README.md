# 🚨 Disaster Event Trigger & Push Notification System

A complete system for triggering disaster events at specified coordinates and automatically sending push notifications to registered mobile devices via Firebase Cloud Messaging (FCM).

## 🎯 Features

### Core Functionality
- ✅ **Trigger Disaster Events** at specific GPS coordinates
- ✅ **Automatic Push Notifications** to all registered devices via FCM
- ✅ **Device Registration Management** for Android and iOS
- ✅ **Web Admin Interface** for easy disaster management
- ✅ **RESTful API** for programmatic access
- ✅ **Image Upload Support** with automatic Base64 conversion
- ✅ **Location Presets** for quick coordinate selection
- ✅ **Geolocation Support** using browser's current location
- ✅ **Recent Events History** tracking
- ✅ **Test Notifications** to verify FCM setup

### Technical Features
- 🔔 High-priority push notifications
- 📱 Cross-platform support (Android & iOS)
- 🗄️ SQLite database for persistence
- 🔄 Automatic invalid token cleanup
- 📊 Real-time device status monitoring
- 🎨 Responsive Bootstrap 5 UI
- 🔒 Token-based device identification

## 📸 Screenshots

### Admin Interface
Access the disaster trigger interface at `/Admin/TriggerDisaster`:

- **Trigger Form**: Title, description, coordinates, tags, image upload
- **Device Panel**: View all registered devices with platform info
- **Recent Events**: Table showing last 5 disaster events
- **Quick Locations**: Pre-configured city coordinates
- **Test Notification**: Verify FCM configuration

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Firebase project with Cloud Messaging enabled
- `google-services.json` file with Firebase credentials

### Installation

1. **Clone the repository**
   ```bash
   cd Backend
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Ensure Firebase credentials exist**
   - Place `google-services.json` in the `Backend` project root

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the interface**
   - Admin Dashboard: http://localhost:5000/Admin
   - Disaster Trigger: http://localhost:5000/Admin/TriggerDisaster

## 📱 Mobile Client Integration

### Register Device (Flutter)

```dart
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

Future<void> registerDevice() async {
  // Get FCM token
  final token = await FirebaseMessaging.instance.getToken();
  
  if (token != null) {
    // Register with backend
    final response = await http.post(
      Uri.parse('http://your-server/api/Notification/RegisterDevice'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'token': token,
        'platform': Platform.isAndroid ? 'Android' : 'iOS',
        'deviceId': 'unique_device_id',
      }),
    );
    
    if (response.statusCode == 200) {
      print('Device registered successfully');
    }
  }
}
```

### Handle Notifications (Flutter)

```dart
// Foreground notifications
FirebaseMessaging.onMessage.listen((RemoteMessage message) {
  if (message.data['type'] == 'disaster_alert') {
    final disasterId = message.data['disasterId'];
    final lat = double.parse(message.data['latitude']);
    final lng = double.parse(message.data['longitude']);
    
    // Show disaster alert
    showDisasterAlert(
      title: message.notification?.title ?? '',
      body: message.notification?.body ?? '',
      latitude: lat,
      longitude: lng,
    );
  }
});

// Background notification tap
FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
  if (message.data['type'] == 'disaster_alert') {
    navigateToDisasterDetail(message.data['disasterId']);
  }
});
```

## 🔧 API Endpoints

### Notification Management

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/Notification/RegisterDevice` | POST | Register device FCM token |
| `/api/Notification/UnregisterDevice` | DELETE | Unregister device |
| `/api/Notification/TriggerDisaster` | POST | **Trigger disaster + send notifications** |
| `/api/Notification/RegisteredDevices` | GET | List all registered devices |
| `/api/Notification/TestNotification` | POST | Send test notification |

### Disaster Events

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/DisasterEvent` | POST | Create disaster event |
| `/api/DisasterEvent` | GET | Get all disaster events |
| `/api/DisasterEvent/{id}` | GET | Get specific event |
| `/api/DisasterEvent/{id}` | PUT | Update event |
| `/api/DisasterEvent/{id}` | DELETE | Delete event |

## 📝 API Examples

### Register a Device

```bash
curl -X POST "http://localhost:5000/api/Notification/RegisterDevice" \
  -H "Content-Type: application/json" \
  -d '{
    "token": "fcm_device_token_here",
    "platform": "Android",
    "deviceId": "device_001"
  }'
```

### Trigger Disaster Event

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

**Response:**
```json
{
  "success": true,
  "message": "災害事件觸發成功",
  "data": {
    "disasterId": "550e8400-e29b-41d4-a716-446655440000",
    "title": "地震警報",
    "latitude": 25.0330,
    "longitude": 121.5654,
    "notificationsSent": 15
  }
}
```

### List Registered Devices

```bash
curl -X GET "http://localhost:5000/api/Notification/RegisteredDevices?activeOnly=true"
```

## 🗄️ Database Schema

### DeviceTokens Table
```sql
CREATE TABLE DeviceTokens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Token VARCHAR(500) NOT NULL UNIQUE,
    DeviceId VARCHAR(200),
    UserId VARCHAR(100),
    Platform VARCHAR(50),
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    IsActive BIT NOT NULL
);
```

### DisasterEvents Table
```sql
CREATE TABLE DisasterEvents (
    Id VARCHAR(50) PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    Description TEXT NOT NULL,
    Lat FLOAT NOT NULL,
    Lnt FLOAT NOT NULL,
    TagsString VARCHAR(500),
    Img TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL
);
```

## 🏗️ Architecture

```
Web Interface (Bootstrap 5)
         ↓
  ASP.NET Core API
         ↓
  Business Logic Layer
  (FcmNotificationService)
         ↓
  Firebase Admin SDK
         ↓
Firebase Cloud Messaging
         ↓
  Mobile Devices (FCM)
```

## 📦 Project Structure

```
Backend/
├── Controllers/
│   ├── NotificationController.cs      # Notification API
│   ├── DisasterEventController.cs     # Disaster CRUD API
│   └── AdminViewController.cs         # Web interface routes
├── Services/
│   └── FcmNotificationService.cs      # FCM integration
├── Models/
│   ├── DeviceToken.cs                 # Device registration model
│   └── DisasterEvent.cs               # Disaster event model
├── DTOs/
│   └── DeviceTokenDto.cs              # Request/response DTOs
├── Data/
│   └── ShelterDbContext.cs            # EF Core DbContext
├── Views/
│   └── Admin/
│       ├── Index.cshtml               # Admin dashboard
│       └── TriggerDisaster.cshtml     # Disaster trigger UI
├── google-services.json               # Firebase credentials
└── Backend.csproj                     # Project file
```

## 🔐 Security Considerations

⚠️ **Important for Production:**

1. **Add Authentication** - Protect `/Admin` routes with authentication
2. **Enable HTTPS** - Use TLS encryption for all traffic
3. **Implement Authorization** - Role-based access control
4. **Rate Limiting** - Prevent API abuse
5. **Input Validation** - Already implemented server-side
6. **Audit Logging** - Track all disaster triggers
7. **Secure Credentials** - Use environment variables for secrets
8. **CORS Configuration** - Restrict allowed origins

## 🧪 Testing

### Test Device Registration
```bash
curl -X POST "http://localhost:5000/api/Notification/RegisterDevice" \
  -H "Content-Type: application/json" \
  -d '{"token":"test_token_123","platform":"Android"}'
```

### Send Test Notification
```bash
curl -X POST "http://localhost:5000/api/Notification/TestNotification"
```

### Trigger Test Disaster
Use the web interface at `/Admin/TriggerDisaster` or:
```bash
curl -X POST "http://localhost:5000/api/Notification/TriggerDisaster" \
  -H "Content-Type: application/json" \
  -d '{
    "title":"Test Alert",
    "description":"This is a test",
    "latitude":25.0330,
    "longitude":121.5654,
    "sendNotification":true
  }'
```

## 📚 Documentation

- 📖 [API Documentation](NOTIFICATION_API.md) - Complete API reference
- 🖥️ [Interface Guide](DISASTER_TRIGGER_INTERFACE.md) - Web interface usage
- 📋 [Implementation Summary](DISASTER_TRIGGER_SUMMARY.md) - Development details
- 🏗️ [Architecture Overview](ARCHITECTURE_OVERVIEW.md) - System architecture
- 🚀 [Quick Start Guide](QUICK_START.md) - Get started in 3 steps

## 🛠️ Technology Stack

### Backend
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- SQLite Database
- FirebaseAdmin SDK 3.0.1
- C# 12

### Frontend (Web)
- Bootstrap 5
- jQuery 3.x
- SweetAlert2
- Bootstrap Icons

### Mobile Client
- Flutter
- firebase_messaging package
- http package

## 🔄 Workflow

1. **Device Registration**
   - Mobile app gets FCM token
   - Calls `/api/Notification/RegisterDevice`
   - Token stored in database

2. **Disaster Triggering**
   - Admin opens web interface
   - Fills disaster form (title, description, coordinates)
   - Submits form
   - Backend creates disaster event
   - Automatically sends FCM notifications

3. **Notification Delivery**
   - FCM distributes to all registered devices
   - Mobile apps receive notification
   - Apps parse disaster data
   - Show alert and update UI

## 🐛 Troubleshooting

### No devices showing
- Ensure devices have registered via API
- Check `IsActive` status in database
- Verify API endpoint is accessible

### Notifications not received
- Verify `google-services.json` is valid
- Check FCM tokens are not expired
- Review server logs for errors
- Use test notification feature

### Database errors
```bash
# Delete old database
del shelters.db

# Restart app (auto-creates tables)
dotnet run
```

## 📄 License

This project is part of the TownPass application.

## 🤝 Contributing

Contributions are welcome! Please ensure:
- Code follows existing patterns
- All endpoints are documented
- Security best practices are followed

## 📞 Support

For issues or questions:
1. Check the documentation files
2. Review server logs
3. Test with cURL/Postman
4. Verify Firebase configuration

---

**Built with ❤️ for disaster management and public safety**

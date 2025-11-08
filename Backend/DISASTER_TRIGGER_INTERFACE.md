# Disaster Trigger Interface - Setup & Usage Guide

## Overview
A web-based admin interface for triggering disaster events at specified coordinates and sending push notifications to registered devices via Firebase Cloud Messaging (FCM).

## Features
✅ **Trigger Disaster Events** - Create disaster events with custom coordinates, titles, and descriptions
✅ **Push Notifications** - Automatically send FCM notifications to all registered devices
✅ **Device Management** - View all registered devices and their status
✅ **Quick Location Presets** - Pre-configured locations for major cities
✅ **Geolocation Support** - Use browser's current location
✅ **Image Upload** - Attach images to disaster events (auto-converts to Base64)
✅ **Recent Events History** - View recently triggered disaster events
✅ **Test Notifications** - Send test notifications to verify FCM setup

## Setup Instructions

### 1. Install Dependencies
```bash
cd Backend
dotnet add package FirebaseAdmin --version 3.0.1
dotnet restore
```

### 2. Configure Firebase
Ensure your `google-services.json` file is in the `Backend` project root with valid Firebase credentials.

### 3. Update Database
The database will auto-create tables on first run. Alternatively, use migrations:
```bash
dotnet ef migrations add AddDeviceTokensAndNotifications
dotnet ef database update
```

### 4. Run the Application
```bash
dotnet run
```

## Access the Interface

### Main Admin Dashboard
```
http://localhost:5000/Admin
```

### Disaster Trigger Interface
```
http://localhost:5000/Admin/TriggerDisaster
```

## Using the Interface

### 1. Register Devices First
Before triggering disasters, devices must register their FCM tokens via the API:

```bash
curl -X POST "http://localhost:5000/api/Notification/RegisterDevice" \
  -H "Content-Type: application/json" \
  -d '{
    "token": "fcm_device_token_here",
    "platform": "Android"
  }'
```

### 2. Trigger a Disaster Event

**Via Web Interface:**
1. Navigate to `/Admin/TriggerDisaster`
2. Fill in the form:
   - **Title**: Disaster event title (e.g., "地震警報")
   - **Description**: Detailed description
   - **Latitude/Longitude**: Coordinates of the disaster
   - **Tags**: Optional tags (comma-separated)
   - **Image**: Optional disaster image
   - **Send Notification**: Toggle push notifications
   - **Notification Radius**: Optional radius in km (leave empty for all devices)
3. Click "觸發災害事件" to submit

**Via API:**
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

### 3. View Results
After triggering, the interface displays:
- Disaster ID
- Number of notifications sent
- Event details
- Timestamp

## Interface Sections

### 📍 Quick Location Presets
Pre-configured coordinates for:
- 台北市政府 (Taipei City Hall)
- 台中市政府 (Taichung City Hall)
- 高雄市政府 (Kaohsiung City Hall)
- Current browser location

### 📱 Registered Devices Panel
Shows:
- Total device count
- Device platform (Android/iOS)
- Device ID
- Last update time
- Token preview

### 🔔 Test Notification Button
Sends a test notification to the first active device to verify FCM configuration.

### 📊 Recent Disasters Table
Displays the 5 most recent disaster events with:
- Title and description
- Location coordinates
- Tags
- Creation timestamp

## Form Validation

The interface includes client-side validation:
- ✅ Title (required)
- ✅ Description (required)
- ✅ Latitude (-90 to 90)
- ✅ Longitude (-180 to 180)
- ✅ Tags (optional, comma-separated)
- ✅ Image (optional, auto-converted to Base64)

## Notification Settings

### Send Notification Toggle
- **Checked**: Sends push notifications to registered devices
- **Unchecked**: Only creates the disaster event without notifications

### Notification Radius
- **Empty**: Notifies all registered devices
- **With value**: Future feature for radius-based notifications (currently notifies all)

## API Endpoints Used

The interface interacts with these backend APIs:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/Notification/TriggerDisaster` | POST | Trigger disaster + send notifications |
| `/api/Notification/RegisteredDevices` | GET | List registered devices |
| `/api/Notification/TestNotification` | POST | Send test notification |
| `/api/DisasterEvent` | GET | Get recent disasters |

## Troubleshooting

### No Devices Showing
- Ensure devices have registered their FCM tokens
- Check the device is marked as `IsActive = true`
- Verify the API endpoint is accessible

### Notifications Not Received
1. Check Firebase credentials in `google-services.json`
2. Verify FCM tokens are valid and not expired
3. Use the "Test Notification" button to diagnose
4. Check server logs for FCM errors

### Image Upload Issues
- Ensure image file size is reasonable (< 5MB recommended)
- Supported formats: JPG, PNG, GIF
- Images are automatically converted to Base64

## Browser Requirements

- Modern browser with JavaScript enabled
- Geolocation API support (for "Current Location" feature)
- LocalStorage/SessionStorage for SweetAlert2

## Dependencies

### Frontend
- Bootstrap 5.x (included via CDN)
- Bootstrap Icons (included via CDN)
- jQuery (included via CDN)
- SweetAlert2 (included via CDN)

### Backend
- ASP.NET Core 8.0
- Entity Framework Core
- FirebaseAdmin SDK
- SQLite

## Security Considerations

⚠️ **Important:** This is an admin interface. In production:

1. **Add Authentication** - Protect the `/Admin` routes
2. **Use HTTPS** - Encrypt all traffic
3. **Rate Limiting** - Prevent abuse of the trigger endpoint
4. **Validate Input** - Server-side validation is already implemented
5. **Audit Logging** - Log all disaster triggers with user info
6. **Role-Based Access** - Limit who can trigger disasters

## Example Workflow

```
1. User registers device → POST /api/Notification/RegisterDevice
2. Admin opens interface → GET /Admin/TriggerDisaster
3. Admin fills form and submits
4. Backend creates disaster event
5. Backend sends FCM notifications to all devices
6. Devices receive notification with disaster data
7. Interface shows success + notification count
```

## Customization

### Add More Location Presets
Edit `TriggerDisaster.cshtml`, section "Quick Location Presets":
```html
<button class="btn btn-sm btn-outline-primary" 
        onclick="setLocation(YOUR_LAT, YOUR_LNG, 'Location Name')">
    📍 Your Location
</button>
```

### Modify Notification Content
Edit `FcmNotificationService.cs`, method `SendDisasterNotificationAsync`:
```csharp
Notification = new Notification()
{
    Title = $"⚠️ {disasterEvent.Title}",
    Body = disasterEvent.Description,
}
```

### Change Styling
The interface uses Bootstrap 5 classes. Modify the card colors and styles in `TriggerDisaster.cshtml`.

## Support

For issues or questions:
1. Check server logs for errors
2. Verify Firebase configuration
3. Test API endpoints directly with curl/Postman
4. Review the `NOTIFICATION_API.md` for API documentation


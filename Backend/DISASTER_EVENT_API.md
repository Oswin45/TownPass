# Disaster Event API Documentation

## Overview
CRUD API for managing disaster events. Users can upload disaster event information, and the server will automatically assign and return a unique ID.

## Base URL
```
/api/DisasterEvent
```

## Endpoints

### 1. Create Disaster Event
**POST** `/api/DisasterEvent`

Creates a new disaster event. The server automatically generates a unique ID.

**Request Body:**
```json
{
  "img": "base64_encoded_image_string",
  "tags": ["tag1", "tag2", "tag3"],
  "description": "詳細的災害事件描述",
  "title": "災害事件標題",
  "lnt": 121.5654,
  "lat": 25.0330
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "災害事件建立成功",
  "data": {
    "id": "generated-uuid-here",
    "img": "base64_encoded_image_string",
    "tags": ["tag1", "tag2", "tag3"],
    "description": "詳細的災害事件描述",
    "title": "災害事件標題",
    "lnt": 121.5654,
    "lat": 25.0330,
    "createdAt": "2025-11-09T10:30:00Z",
    "updatedAt": "2025-11-09T10:30:00Z"
  }
}
```

---

### 2. Get All Disaster Events
**GET** `/api/DisasterEvent`

Retrieves all disaster events with pagination support.

**Query Parameters:**
- `skip` (optional, default: 0) - Number of records to skip
- `take` (optional, default: 100, max: 500) - Number of records to retrieve

**Example Request:**
```
GET /api/DisasterEvent?skip=0&take=50
```

**Response (200 OK):**
```json
{
  "success": true,
  "totalCount": 150,
  "count": 50,
  "skip": 0,
  "take": 50,
  "data": [
    {
      "id": "uuid-1",
      "img": "base64_encoded_image_string",
      "tags": ["tag1", "tag2"],
      "description": "描述",
      "title": "標題",
      "lnt": 121.5654,
      "lat": 25.0330,
      "createdAt": "2025-11-09T10:30:00Z",
      "updatedAt": "2025-11-09T10:30:00Z"
    }
  ]
}
```

---

### 3. Get Disaster Event by ID
**GET** `/api/DisasterEvent/{id}`

Retrieves a specific disaster event by its ID.

**Example Request:**
```
GET /api/DisasterEvent/uuid-123
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "uuid-123",
    "img": "base64_encoded_image_string",
    "tags": ["tag1", "tag2"],
    "description": "詳細描述",
    "title": "標題",
    "lnt": 121.5654,
    "lat": 25.0330,
    "createdAt": "2025-11-09T10:30:00Z",
    "updatedAt": "2025-11-09T10:30:00Z"
  }
}
```

**Response (404 Not Found):**
```json
{
  "success": false,
  "message": "找不到 ID 為 uuid-123 的災害事件"
}
```

---

### 4. Update Disaster Event
**PUT** `/api/DisasterEvent/{id}`

Updates an existing disaster event. Only provided fields will be updated.

**Request Body (all fields optional):**
```json
{
  "img": "new_base64_image",
  "tags": ["new_tag1", "new_tag2"],
  "description": "更新的描述",
  "title": "更新的標題",
  "lnt": 121.5700,
  "lat": 25.0400
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "災害事件更新成功",
  "data": {
    "id": "uuid-123",
    "img": "new_base64_image",
    "tags": ["new_tag1", "new_tag2"],
    "description": "更新的描述",
    "title": "更新的標題",
    "lnt": 121.5700,
    "lat": 25.0400,
    "createdAt": "2025-11-09T10:30:00Z",
    "updatedAt": "2025-11-09T11:45:00Z"
  }
}
```

---

### 5. Delete Disaster Event
**DELETE** `/api/DisasterEvent/{id}`

Deletes a disaster event by its ID.

**Example Request:**
```
DELETE /api/DisasterEvent/uuid-123
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "災害事件刪除成功",
  "deletedId": "uuid-123"
}
```

**Response (404 Not Found):**
```json
{
  "success": false,
  "message": "找不到 ID 為 uuid-123 的災害事件"
}
```

---

### 6. Get Nearby Disaster Events
**GET** `/api/DisasterEvent/nearby`

Retrieves disaster events within a specified radius from a location.

**Query Parameters:**
- `lat` (required) - Latitude of the center point
- `lnt` (required) - Longitude of the center point
- `radius` (optional, default: 5, max: 100) - Search radius in kilometers

**Example Request:**
```
GET /api/DisasterEvent/nearby?lat=25.0330&lnt=121.5654&radius=10
```

**Response (200 OK):**
```json
{
  "success": true,
  "count": 3,
  "center": {
    "lat": 25.0330,
    "lnt": 121.5654
  },
  "radius": 10,
  "data": [
    {
      "id": "uuid-1",
      "img": "base64_image",
      "tags": ["tag1"],
      "description": "描述",
      "title": "標題",
      "lnt": 121.5700,
      "lat": 25.0350,
      "createdAt": "2025-11-09T10:30:00Z",
      "updatedAt": "2025-11-09T10:30:00Z",
      "distance": 0.5
    }
  ]
}
```

---

## Validation Rules

### CreateDisasterEventDto
- `img`: Required
- `tags`: Required (array)
- `description`: Required, max length 2000 characters
- `title`: Required, max length 200 characters
- `lnt`: Required, range -180 to 180
- `lat`: Required, range -90 to 90

### UpdateDisasterEventDto
- All fields are optional
- `description`: Max length 2000 characters (if provided)
- `title`: Max length 200 characters (if provided)
- `lnt`: Range -180 to 180 (if provided)
- `lat`: Range -90 to 90 (if provided)

---

## Error Responses

### Validation Error (400 Bad Request)
```json
{
  "success": false,
  "message": "驗證失敗",
  "errors": [
    "標題必須提供",
    "經度必須在-180到180之間"
  ]
}
```

### Server Error (500 Internal Server Error)
```json
{
  "success": false,
  "message": "建立災害事件時發生錯誤",
  "error": "Error details here"
}
```

---

## Notes

1. **ID Generation**: Server automatically generates UUID for new events
2. **Timestamps**: `createdAt` and `updatedAt` are automatically managed by the server
3. **Image Format**: Images should be base64 encoded strings
4. **Coordinate System**: Uses WGS84 coordinate system (lat/lng)
5. **Pagination**: Default page size is 100, maximum is 500
6. **Distance Calculation**: Uses approximate great-circle distance calculation

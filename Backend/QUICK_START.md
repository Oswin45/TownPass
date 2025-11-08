# 🚀 Quick Start Guide - Disaster Trigger with Push Notifications

## 快速開始 (3 steps)

### Step 1: 安裝依賴
```bash
cd Backend
dotnet restore
```

### Step 2: 啟動應用程式
```bash
dotnet run
```

### Step 3: 開啟介面
在瀏覽器中開啟:
```
http://localhost:5000/Admin/TriggerDisaster
```

---

## 📱 測試流程

### 1️⃣ 註冊測試裝置
使用 Postman 或 cURL 註冊一個測試裝置:

```bash
curl -X POST "http://localhost:5000/api/Notification/RegisterDevice" ^
  -H "Content-Type: application/json" ^
  -d "{\"token\":\"test_fcm_token_12345\",\"platform\":\"Android\",\"deviceId\":\"test_device_001\"}"
```

**回應:**
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

### 2️⃣ 觸發災害事件 (透過 Web 介面)

1. 開啟 `http://localhost:5000/Admin/TriggerDisaster`
2. 填寫表單:
   - **標題**: `地震警報`
   - **描述**: `台北市發生規模5.6地震，請注意安全`
   - **緯度**: `25.0330`
   - **經度**: `121.5654`
   - **標籤**: `earthquake,emergency`
   - **勾選** "發送推播通知"
3. 點擊 **"觸發災害事件"**

### 3️⃣ 觸發災害事件 (透過 API)

```bash
curl -X POST "http://localhost:5000/api/Notification/TriggerDisaster" ^
  -H "Content-Type: application/json" ^
  -d "{\"title\":\"地震警報\",\"description\":\"台北市發生規模5.6地震，請注意安全\",\"latitude\":25.0330,\"longitude\":121.5654,\"tags\":[\"earthquake\",\"emergency\"],\"sendNotification\":true}"
```

**回應:**
```json
{
  "success": true,
  "message": "災害事件觸發成功",
  "data": {
    "disasterId": "550e8400-e29b-41d4-a716-446655440000",
    "title": "地震警報",
    "description": "台北市發生規模5.6地震，請注意安全",
    "latitude": 25.033,
    "longitude": 121.5654,
    "tags": ["earthquake", "emergency"],
    "createdAt": "2025-11-09T12:34:56Z",
    "notificationsSent": 1
  }
}
```

---

## 🔍 查看已註冊的裝置

```bash
curl -X GET "http://localhost:5000/api/Notification/RegisteredDevices?activeOnly=true"
```

**回應:**
```json
{
  "success": true,
  "message": "取得裝置清單成功",
  "data": [
    {
      "id": 1,
      "deviceId": "test_device_001",
      "platform": "Android",
      "isActive": true,
      "createdAt": "2025-11-09T10:00:00Z",
      "updatedAt": "2025-11-09T10:00:00Z",
      "tokenPreview": "test_fcm_token_12345..."
    }
  ],
  "count": 1
}
```

---

## 🧪 測試推播通知

### 方法 1: 透過 Web 介面
1. 在災害觸發頁面，點擊 **"發送測試通知"** 按鈕
2. 系統會發送測試通知到第一個啟用的裝置

### 方法 2: 透過 API
```bash
curl -X POST "http://localhost:5000/api/Notification/TestNotification"
```

---

## 📍 使用快速位置

在 Web 介面中，點擊快速位置按鈕自動填入座標:
- 📍 **台北市政府**: 25.0330, 121.5654
- 📍 **台中市政府**: 24.1477, 120.6736
- 📍 **高雄市政府**: 22.6273, 120.3014
- 🎯 **使用當前位置**: 自動偵測瀏覽器位置

---

## 📊 查看最近的災害事件

```bash
curl -X GET "http://localhost:5000/api/DisasterEvent?limit=5"
```

---

## 🔧 故障排除

### 問題: 沒有看到已註冊的裝置
**解決方案:**
- 確認已透過 API 註冊裝置
- 檢查 `shelters.db` 中的 `DeviceTokens` 表

### 問題: 推播通知未收到
**解決方案:**
1. 確認 `google-services.json` 存在且有效
2. 檢查 FCM Token 是否有效
3. 查看伺服器日誌中的錯誤訊息
4. 使用 "測試通知" 功能診斷

### 問題: 資料庫錯誤
**解決方案:**
```bash
# 刪除舊的資料庫
del shelters.db

# 重新啟動應用程式（會自動建立新資料庫）
dotnet run
```

---

## 📱 Flutter 客戶端整合

### 1. 註冊 FCM Token
```dart
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

Future<void> registerDevice() async {
  final token = await FirebaseMessaging.instance.getToken();
  
  if (token != null) {
    final response = await http.post(
      Uri.parse('http://YOUR_SERVER/api/Notification/RegisterDevice'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'token': token,
        'platform': Platform.isAndroid ? 'Android' : 'iOS',
      }),
    );
    
    if (response.statusCode == 200) {
      print('Device registered successfully');
    }
  }
}
```

### 2. 處理推播通知
```dart
// 前景通知
FirebaseMessaging.onMessage.listen((RemoteMessage message) {
  if (message.data['type'] == 'disaster_alert') {
    final disasterId = message.data['disasterId'];
    final lat = double.parse(message.data['latitude']);
    final lng = double.parse(message.data['longitude']);
    
    // 顯示災害警報
    showDisasterAlert(
      title: message.notification?.title ?? '',
      body: message.notification?.body ?? '',
      latitude: lat,
      longitude: lng,
    );
  }
});

// 背景通知點擊
FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
  // 導航到災害詳情頁面
  if (message.data['type'] == 'disaster_alert') {
    navigateToDisasterDetail(message.data['disasterId']);
  }
});
```

---

## 🎯 主要功能

✅ **觸發災害事件** - 在指定座標建立災害  
✅ **自動推播通知** - 同時發送 FCM 通知給所有裝置  
✅ **裝置管理** - 註冊、取消註冊、查看裝置列表  
✅ **圖片上傳** - 支援災害圖片（自動轉 Base64）  
✅ **測試功能** - 測試推播通知確認 FCM 設定  
✅ **歷史記錄** - 查看最近的災害事件  
✅ **Web 介面** - 友善的管理介面  

---

## 📚 詳細文件

- 📖 **API 文件**: `NOTIFICATION_API.md`
- 🖥️ **介面使用指南**: `DISASTER_TRIGGER_INTERFACE.md`
- 📋 **實作摘要**: `DISASTER_TRIGGER_SUMMARY.md`

---

## 🎉 完成!

您現在可以:
1. ✅ 透過 Web 介面觸發災害事件
2. ✅ 自動發送推播通知給所有已註冊的裝置
3. ✅ 管理裝置註冊
4. ✅ 查看災害事件歷史

**享受您的災害管理系統! 🚨**

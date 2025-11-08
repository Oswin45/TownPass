# Town Pass API 文件

版本：v1.0  
更新日期：2025-11-08

## 目錄

- [概述](#概述)
- [基礎資訊](#基礎資訊)
- [認證](#認證)
- [回應格式](#回應格式)
- [錯誤處理](#錯誤處理)
- [API 端點](#api-端點)
  - [健康檢查](#健康檢查)
  - [避難疏散 API](#避難疏散-api)
  - [避難所管理 API](#避難所管理-api)

---

## 概述

Town Pass API 提供避難所管理和疏散相關的功能，支援避難所的 CRUD 操作、地理位置搜尋，以及即時可用性查詢。

## 基礎資訊

- **Base URL（開發環境）**: `https://localhost:5001`
- **Base URL（生產環境）**: `https://api.townpass.taipei`
- **Protocol**: HTTPS
- **Content-Type**: `application/json`
- **Character Encoding**: UTF-8

## 認證

目前版本不需要認證。未來版本將支援 OAuth 2.0 / JWT Token。

## 回應格式

所有 API 回應均使用 JSON 格式。

### 成功回應
```json
{
  "id": 1,
  "name": "台北市政府大樓",
  "address": "台北市信義區市府路1號",
  "latitude": 25.0375,
  "longitude": 121.5645,
  "capacity": 500,
  "currentOccupancy": 0,
  "availableSpaces": 500,
  "contactPhone": "02-27208889",
  "facilities": "飲水機、廁所、醫療站",
  "isActive": true,
  "createdAt": "2025-11-08T10:00:00Z"
}
```

### 錯誤回應
```json
{
  "message": "找不到 ID 為 999 的避難所"
}
```

## 錯誤處理

API 使用標準 HTTP 狀態碼：

| 狀態碼 | 說明 |
|--------|------|
| 200 OK | 請求成功 |
| 201 Created | 資源建立成功 |
| 204 No Content | 請求成功但無回應內容 |
| 400 Bad Request | 請求格式錯誤或參數無效 |
| 404 Not Found | 找不到請求的資源 |
| 500 Internal Server Error | 伺服器內部錯誤 |

---

## API 端點

### 健康檢查

#### 檢查服務狀態

**端點**: `GET /api/health`

**描述**: 檢查 API 服務是否正常運作

**請求範例**:
```bash
curl -X GET "https://localhost:5001/api/health"
```

**回應範例**:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-08T10:30:00Z",
  "service": "TownPass API"
}
```

**狀態碼**:
- `200 OK` - 服務正常

---

### 避難疏散 API

#### 1. 取得所有避難所

**端點**: `GET /api/evacuation/shelters`

**描述**: 取得系統中所有的避難所列表（包含啟用和停用的）

**請求範例**:
```bash
curl -X GET "https://localhost:5001/api/evacuation/shelters"
```

**回應範例**:
```json
[
  {
    "id": 1,
    "name": "台北市政府大樓",
    "address": "台北市信義區市府路1號",
    "latitude": 25.0375,
    "longitude": 121.5645,
    "capacity": 500,
    "currentOccupancy": 150,
    "availableSpaces": 350,
    "contactPhone": "02-27208889",
    "facilities": "飲水機、廁所、醫療站",
    "isActive": true,
    "createdAt": "2025-11-08T10:00:00Z"
  },
  {
    "id": 2,
    "name": "大安森林公園活動中心",
    "address": "台北市大安區新生南路二段1號",
    "latitude": 25.0265,
    "longitude": 121.5364,
    "capacity": 300,
    "currentOccupancy": 0,
    "availableSpaces": 300,
    "contactPhone": "02-27003830",
    "facilities": "飲水機、廁所、發電機",
    "isActive": true,
    "createdAt": "2025-11-08T10:00:00Z"
  }
]
```

**狀態碼**:
- `200 OK` - 成功取得列表
- `500 Internal Server Error` - 伺服器錯誤

---

#### 2. 取得可用的避難所

**端點**: `GET /api/evacuation/shelters/available`

**描述**: 取得所有啟用且有空位的避難所

**請求範例**:
```bash
curl -X GET "https://localhost:5001/api/evacuation/shelters/available"
```

**回應範例**:
```json
[
  {
    "id": 2,
    "name": "大安森林公園活動中心",
    "address": "台北市大安區新生南路二段1號",
    "latitude": 25.0265,
    "longitude": 121.5364,
    "capacity": 300,
    "currentOccupancy": 0,
    "availableSpaces": 300,
    "contactPhone": "02-27003830",
    "facilities": "飲水機、廁所、發電機",
    "isActive": true,
    "createdAt": "2025-11-08T10:00:00Z"
  }
]
```

**狀態碼**:
- `200 OK` - 成功取得列表
- `500 Internal Server Error` - 伺服器錯誤

---

#### 3. 尋找附近的避難所

**端點**: `GET /api/evacuation/shelters/nearby`

**描述**: 根據經緯度座標尋找指定半徑內的避難所

**查詢參數**:

| 參數 | 類型 | 必填 | 預設值 | 說明 |
|------|------|------|--------|------|
| latitude | number | 是 | - | 緯度（-90 到 90） |
| longitude | number | 是 | - | 經度（-180 到 180） |
| radius | number | 否 | 5.0 | 搜尋半徑（公里） |

**請求範例**:
```bash
# 尋找台北市政府附近 5 公里內的避難所
curl -X GET "https://localhost:5001/api/evacuation/shelters/nearby?latitude=25.0375&longitude=121.5645&radius=5"
```

**回應範例**:
```json
[
  {
    "id": 1,
    "name": "台北市政府大樓",
    "address": "台北市信義區市府路1號",
    "latitude": 25.0375,
    "longitude": 121.5645,
    "capacity": 500,
    "currentOccupancy": 150,
    "availableSpaces": 350,
    "contactPhone": "02-27208889",
    "facilities": "飲水機、廁所、醫療站",
    "isActive": true,
    "createdAt": "2025-11-08T10:00:00Z"
  },
  {
    "id": 2,
    "name": "大安森林公園活動中心",
    "address": "台北市大安區新生南路二段1號",
    "latitude": 25.0265,
    "longitude": 121.5364,
    "capacity": 300,
    "currentOccupancy": 0,
    "availableSpaces": 300,
    "contactPhone": "02-27003830",
    "facilities": "飲水機、廁所、發電機",
    "isActive": true,
    "createdAt": "2025-11-08T10:00:00Z"
  }
]
```

**狀態碼**:
- `200 OK` - 成功取得列表
- `400 Bad Request` - 座標參數無效
- `500 Internal Server Error` - 伺服器錯誤

---

#### 4. 取得特定避難所資訊

**端點**: `GET /api/evacuation/shelters/{id}`

**描述**: 根據 ID 取得特定避難所的詳細資訊

**路徑參數**:

| 參數 | 類型 | 說明 |
|------|------|------|
| id | integer | 避難所 ID |

**請求範例**:
```bash
curl -X GET "https://localhost:5001/api/evacuation/shelters/1"
```

**回應範例**:
```json
{
  "id": 1,
  "name": "台北市政府大樓",
  "address": "台北市信義區市府路1號",
  "latitude": 25.0375,
  "longitude": 121.5645,
  "capacity": 500,
  "currentOccupancy": 150,
  "availableSpaces": 350,
  "contactPhone": "02-27208889",
  "facilities": "飲水機、廁所、醫療站",
  "isActive": true,
  "createdAt": "2025-11-08T10:00:00Z"
}
```

**狀態碼**:
- `200 OK` - 成功取得資料
- `404 Not Found` - 找不到該避難所
- `500 Internal Server Error` - 伺服器錯誤

---

### 避難所管理 API

#### 1. 取得所有避難所

**端點**: `GET /api/shelters`

**描述**: 取得所有避難所列表

**請求範例**:
```bash
curl -X GET "https://localhost:5001/api/shelters"
```

**回應**: 與 `/api/evacuation/shelters` 相同

---

#### 2. 取得特定避難所

**端點**: `GET /api/shelters/{id}`

**描述**: 根據 ID 取得特定避難所

**路徑參數**:

| 參數 | 類型 | 說明 |
|------|------|------|
| id | integer | 避難所 ID |

**請求範例**:
```bash
curl -X GET "https://localhost:5001/api/shelters/1"
```

**回應**: 與 `/api/evacuation/shelters/{id}` 相同

---

#### 3. 建立新避難所

**端點**: `POST /api/shelters`

**描述**: 建立一個新的避難所

**請求 Body**:

| 欄位 | 類型 | 必填 | 說明 |
|------|------|------|------|
| name | string | 是 | 避難所名稱 |
| address | string | 是 | 地址 |
| latitude | number | 是 | 緯度 |
| longitude | number | 是 | 經度 |
| capacity | integer | 是 | 容納人數 |
| contactPhone | string | 否 | 聯絡電話 |
| facilities | string | 否 | 設施描述 |

**請求範例**:
```bash
curl -X POST "https://localhost:5001/api/shelters" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "信義國中",
    "address": "台北市信義區松仁路158巷1號",
    "latitude": 25.0333,
    "longitude": 121.5654,
    "capacity": 200,
    "contactPhone": "02-27201066",
    "facilities": "飲水機、廁所、操場"
  }'
```

**回應範例**:
```json
{
  "id": 3,
  "name": "信義國中",
  "address": "台北市信義區松仁路158巷1號",
  "latitude": 25.0333,
  "longitude": 121.5654,
  "capacity": 200,
  "currentOccupancy": 0,
  "availableSpaces": 200,
  "contactPhone": "02-27201066",
  "facilities": "飲水機、廁所、操場",
  "isActive": true,
  "createdAt": "2025-11-08T11:00:00Z"
}
```

**狀態碼**:
- `201 Created` - 成功建立，Location header 包含新資源 URL
- `400 Bad Request` - 請求格式錯誤或必填欄位缺失
- `500 Internal Server Error` - 伺服器錯誤

---

#### 4. 更新避難所資訊

**端點**: `PUT /api/shelters/{id}`

**描述**: 更新指定避難所的資訊（部分更新）

**路徑參數**:

| 參數 | 類型 | 說明 |
|------|------|------|
| id | integer | 避難所 ID |

**請求 Body**（所有欄位皆為選填）:

| 欄位 | 類型 | 說明 |
|------|------|------|
| name | string | 避難所名稱 |
| address | string | 地址 |
| latitude | number | 緯度 |
| longitude | number | 經度 |
| capacity | integer | 容納人數 |
| currentOccupancy | integer | 目前人數 |
| contactPhone | string | 聯絡電話 |
| facilities | string | 設施描述 |
| isActive | boolean | 是否啟用 |

**請求範例**:
```bash
curl -X PUT "https://localhost:5001/api/shelters/1" \
  -H "Content-Type: application/json" \
  -d '{
    "currentOccupancy": 200,
    "facilities": "飲水機、廁所、醫療站、發電機"
  }'
```

**回應範例**:
```json
{
  "id": 1,
  "name": "台北市政府大樓",
  "address": "台北市信義區市府路1號",
  "latitude": 25.0375,
  "longitude": 121.5645,
  "capacity": 500,
  "currentOccupancy": 200,
  "availableSpaces": 300,
  "contactPhone": "02-27208889",
  "facilities": "飲水機、廁所、醫療站、發電機",
  "isActive": true,
  "createdAt": "2025-11-08T10:00:00Z"
}
```

**狀態碼**:
- `200 OK` - 成功更新
- `400 Bad Request` - 請求格式錯誤
- `404 Not Found` - 找不到該避難所
- `500 Internal Server Error` - 伺服器錯誤

---

#### 5. 刪除避難所

**端點**: `DELETE /api/shelters/{id}`

**描述**: 刪除指定的避難所

**路徑參數**:

| 參數 | 類型 | 說明 |
|------|------|------|
| id | integer | 避難所 ID |

**請求範例**:
```bash
curl -X DELETE "https://localhost:5001/api/shelters/3"
```

**回應**: 無內容

**狀態碼**:
- `204 No Content` - 成功刪除
- `404 Not Found` - 找不到該避難所
- `500 Internal Server Error` - 伺服器錯誤

---

## 資料模型

### Shelter (避難所)

```json
{
  "id": "integer (唯一識別碼)",
  "name": "string (避難所名稱)",
  "address": "string (地址)",
  "latitude": "number (緯度)",
  "longitude": "number (經度)",
  "capacity": "integer (容納人數)",
  "currentOccupancy": "integer (目前人數)",
  "availableSpaces": "integer (可用空位，自動計算)",
  "contactPhone": "string (聯絡電話，可選)",
  "facilities": "string (設施描述，可選)",
  "isActive": "boolean (是否啟用)",
  "createdAt": "datetime (建立時間)"
}
```

---

## Flutter 整合範例

### 建立 API Service 類別

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

class TownPassApiService {
  static const String baseUrl = 'https://localhost:5001';
  
  // 取得所有避難所
  Future<List<Shelter>> getAllShelters() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/shelters'),
      headers: {'Content-Type': 'application/json'},
    );
    
    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Shelter.fromJson(json)).toList();
    } else {
      throw Exception('無法取得避難所列表');
    }
  }
  
  // 取得可用避難所
  Future<List<Shelter>> getAvailableShelters() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/evacuation/shelters/available'),
      headers: {'Content-Type': 'application/json'},
    );
    
    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Shelter.fromJson(json)).toList();
    } else {
      throw Exception('無法取得可用避難所');
    }
  }
  
  // 尋找附近避難所
  Future<List<Shelter>> findNearbyShelters(
    double latitude, 
    double longitude, 
    {double radius = 5.0}
  ) async {
    final uri = Uri.parse('$baseUrl/api/evacuation/shelters/nearby')
      .replace(queryParameters: {
        'latitude': latitude.toString(),
        'longitude': longitude.toString(),
        'radius': radius.toString(),
      });
    
    final response = await http.get(
      uri,
      headers: {'Content-Type': 'application/json'},
    );
    
    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Shelter.fromJson(json)).toList();
    } else {
      throw Exception('無法尋找附近避難所');
    }
  }
  
  // 取得特定避難所
  Future<Shelter> getShelterById(int id) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/shelters/$id'),
      headers: {'Content-Type': 'application/json'},
    );
    
    if (response.statusCode == 200) {
      return Shelter.fromJson(json.decode(response.body));
    } else if (response.statusCode == 404) {
      throw Exception('找不到該避難所');
    } else {
      throw Exception('無法取得避難所資訊');
    }
  }
  
  // 建立新避難所
  Future<Shelter> createShelter(CreateShelterRequest request) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/shelters'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode(request.toJson()),
    );
    
    if (response.statusCode == 201) {
      return Shelter.fromJson(json.decode(response.body));
    } else {
      throw Exception('無法建立避難所');
    }
  }
  
  // 更新避難所
  Future<Shelter> updateShelter(int id, UpdateShelterRequest request) async {
    final response = await http.put(
      Uri.parse('$baseUrl/api/shelters/$id'),
      headers: {'Content-Type': 'application/json'},
      body: json.encode(request.toJson()),
    );
    
    if (response.statusCode == 200) {
      return Shelter.fromJson(json.decode(response.body));
    } else if (response.statusCode == 404) {
      throw Exception('找不到該避難所');
    } else {
      throw Exception('無法更新避難所');
    }
  }
  
  // 刪除避難所
  Future<void> deleteShelter(int id) async {
    final response = await http.delete(
      Uri.parse('$baseUrl/api/shelters/$id'),
      headers: {'Content-Type': 'application/json'},
    );
    
    if (response.statusCode != 204) {
      if (response.statusCode == 404) {
        throw Exception('找不到該避難所');
      } else {
        throw Exception('無法刪除避難所');
      }
    }
  }
}
```

### 資料模型類別

```dart
class Shelter {
  final int id;
  final String name;
  final String address;
  final double latitude;
  final double longitude;
  final int capacity;
  final int currentOccupancy;
  final int availableSpaces;
  final String? contactPhone;
  final String? facilities;
  final bool isActive;
  final DateTime createdAt;
  
  Shelter({
    required this.id,
    required this.name,
    required this.address,
    required this.latitude,
    required this.longitude,
    required this.capacity,
    required this.currentOccupancy,
    required this.availableSpaces,
    this.contactPhone,
    this.facilities,
    required this.isActive,
    required this.createdAt,
  });
  
  factory Shelter.fromJson(Map<String, dynamic> json) {
    return Shelter(
      id: json['id'],
      name: json['name'],
      address: json['address'],
      latitude: json['latitude'],
      longitude: json['longitude'],
      capacity: json['capacity'],
      currentOccupancy: json['currentOccupancy'],
      availableSpaces: json['availableSpaces'],
      contactPhone: json['contactPhone'],
      facilities: json['facilities'],
      isActive: json['isActive'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}

class CreateShelterRequest {
  final String name;
  final String address;
  final double latitude;
  final double longitude;
  final int capacity;
  final String? contactPhone;
  final String? facilities;
  
  CreateShelterRequest({
    required this.name,
    required this.address,
    required this.latitude,
    required this.longitude,
    required this.capacity,
    this.contactPhone,
    this.facilities,
  });
  
  Map<String, dynamic> toJson() {
    return {
      'name': name,
      'address': address,
      'latitude': latitude,
      'longitude': longitude,
      'capacity': capacity,
      if (contactPhone != null) 'contactPhone': contactPhone,
      if (facilities != null) 'facilities': facilities,
    };
  }
}

class UpdateShelterRequest {
  final String? name;
  final String? address;
  final double? latitude;
  final double? longitude;
  final int? capacity;
  final int? currentOccupancy;
  final String? contactPhone;
  final String? facilities;
  final bool? isActive;
  
  UpdateShelterRequest({
    this.name,
    this.address,
    this.latitude,
    this.longitude,
    this.capacity,
    this.currentOccupancy,
    this.contactPhone,
    this.facilities,
    this.isActive,
  });
  
  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{};
    if (name != null) map['name'] = name;
    if (address != null) map['address'] = address;
    if (latitude != null) map['latitude'] = latitude;
    if (longitude != null) map['longitude'] = longitude;
    if (capacity != null) map['capacity'] = capacity;
    if (currentOccupancy != null) map['currentOccupancy'] = currentOccupancy;
    if (contactPhone != null) map['contactPhone'] = contactPhone;
    if (facilities != null) map['facilities'] = facilities;
    if (isActive != null) map['isActive'] = isActive;
    return map;
  }
}
```

### 使用範例

```dart
// 在 Widget 中使用
class ShelterListPage extends StatefulWidget {
  @override
  _ShelterListPageState createState() => _ShelterListPageState();
}

class _ShelterListPageState extends State<ShelterListPage> {
  final apiService = TownPassApiService();
  List<Shelter> shelters = [];
  bool isLoading = true;
  
  @override
  void initState() {
    super.initState();
    loadShelters();
  }
  
  Future<void> loadShelters() async {
    try {
      final data = await apiService.getAvailableShelters();
      setState(() {
        shelters = data;
        isLoading = false;
      });
    } catch (e) {
      setState(() {
        isLoading = false;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('載入失敗: $e')),
      );
    }
  }
  
  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return Center(child: CircularProgressIndicator());
    }
    
    return ListView.builder(
      itemCount: shelters.length,
      itemBuilder: (context, index) {
        final shelter = shelters[index];
        return ListTile(
          title: Text(shelter.name),
          subtitle: Text(shelter.address),
          trailing: Text('可容納: ${shelter.availableSpaces}人'),
        );
      },
    );
  }
}
```

---

## 測試工具

### Postman Collection

您可以匯入以下 Postman Collection 快速測試所有 API：

[下載 Postman Collection](./TownPass-API.postman_collection.json)

### cURL 測試腳本

```bash
#!/bin/bash
BASE_URL="https://localhost:5001"

echo "=== 健康檢查 ==="
curl -X GET "$BASE_URL/api/health"

echo -e "\n\n=== 取得所有避難所 ==="
curl -X GET "$BASE_URL/api/shelters"

echo -e "\n\n=== 取得可用避難所 ==="
curl -X GET "$BASE_URL/api/evacuation/shelters/available"

echo -e "\n\n=== 尋找附近避難所 ==="
curl -X GET "$BASE_URL/api/evacuation/shelters/nearby?latitude=25.0375&longitude=121.5645&radius=5"

echo -e "\n\n=== 建立新避難所 ==="
curl -X POST "$BASE_URL/api/shelters" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "測試避難所",
    "address": "台北市測試路1號",
    "latitude": 25.04,
    "longitude": 121.57,
    "capacity": 100
  }'
```

---

## 常見問題 (FAQ)

### Q1: API 有速率限制嗎？
A: 目前版本沒有速率限制。未來版本可能會加入。

### Q2: 如何處理 HTTPS 憑證錯誤（開發環境）？
A: 在開發環境中，如果遇到自簽憑證錯誤，可以：
- Flutter: 使用 `HttpClient` 並設定 `badCertificateCallback`
- cURL: 使用 `-k` 或 `--insecure` 參數

### Q3: 座標搜尋的準確度如何？
A: 使用 Haversine 公式計算球面距離，誤差約在 0.5% 以內。

### Q4: 可以同時更新多個避難所嗎？
A: 目前版本不支援批次操作。需要逐一呼叫 API。

### Q5: 刪除的避難所可以復原嗎？
A: 目前是硬刪除，無法復原。建議使用 `isActive=false` 代替刪除。

---

## 版本歷史

### v1.0 (2025-11-08)
- 初始版本
- 基本 CRUD 操作
- 地理位置搜尋
- Swagger UI 整合

---

## 支援與回饋

- **GitHub Issues**: https://github.com/Oswin45/TownPass/issues
- **Email**: support@townpass.taipei
- **文件**: https://docs.townpass.taipei

---

## 授權

本 API 採用與 Town Pass 主專案相同的開源授權。

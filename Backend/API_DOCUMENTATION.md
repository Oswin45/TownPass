# Shelter API 文件
## TownPass 避難所 API 文件

本文件描述了 TownPass 系統中避難所 API 的所有端點。此 API 整合了天然災害避難所和防空避難所的資料，提供統一的查詢介面。

---

## 基本資訊

- **Base URL**: `/api/Shelter`
- **資料格式**: JSON
- **編碼**: UTF-8
- **支援的災害類型**: `None`, `Flooding`, `Earthquake`, `Landslide`, `Tsunami`, `AirRaid`

---

## 資料模型

### Shelter (避難所)

```json
{
  "id": 0,                
  "type": "string",              // 避難所類型 (NaturalDisaster 或 AirRaid)
  "name": "string",              // 避難所名稱
  "capacity": 0,                 // 容納人數
  "currentOccupancy": 0,         // 目前收容人數
  "supportedDisasters": 0,       // 支援的災害類型 (位元旗標)
  "accesibility": false,         // 是否有無障礙設施
  "address": "string",           // 地址
  "latitude": 0.0,               // 緯度
  "longitude": 0.0,              // 經度
  "telephone": "string",         // 聯絡電話 (可能為 null)
  "sizeInSquareMeters": 0        // 面積 (平方公尺)
}
```

### DisasterTypes (災害類型)

支援的災害類型使用位元旗標表示，可以組合多種類型：

- `None` = 0
- `Flooding` = 1 (淹水)
- `Earthquake` = 2 (地震)
- `Landslide` = 4 (土石流)
- `Tsunami` = 8 (海嘯)
- `AirRaid` = 16 (防空)

---

## API 端點

### 1. 獲取所有避難所

取得所有類型的避難所資料，包含天然災害避難所和防空避難所。

**端點**: `GET /api/Shelter/all`

**請求參數**: 無

**回應範例**:
```json
{
  "success": true,
  "count": 1234,
  "data": [
    {
      "id": "1",
      "type": "學校",
      "name": "臺北市立中正國小",
      "capacity": 500,
      "currentOccupancy": 0,
      "supportedDisasters": 7,
      "accesibility": true,
      "address": "臺北市中正區某某路123號",
      "latitude": 25.033,
      "longitude": 121.516,
      "telephone": "02-12345678",
      "sizeInSquareMeters": 2000
    }
  ]
}
```

**錯誤回應**:
```json
{
  "success": false,
  "message": "錯誤訊息"
}
```

**HTTP 狀態碼**:
- `200 OK`: 成功
- `500 Internal Server Error`: 伺服器錯誤

---

### 2. 根據災害類型篩選避難所

根據指定的災害類型篩選避難所。

**端點**: `GET /api/Shelter/by-disaster`

**請求參數**:
| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| type | string | 是 | 災害類型 (None, Flooding, Earthquake, Landslide, Tsunami, AirRaid) |

**請求範例**:
```
GET /api/Shelter/by-disaster?type=Flooding
```

**回應範例**:
```json
{
  "success": true,
  "disasterType": "Flooding",
  "count": 567,
  "data": [
    {
      "id": "2",
      "type": "學校",
      "name": "某某國小",
      "capacity": 300,
      "currentOccupancy": 0,
      "supportedDisasters": 1,
      "accesibility": false,
      "address": "臺北市某某區某某路456號",
      "latitude": 25.045,
      "longitude": 121.520,
      "telephone": "02-87654321",
      "sizeInSquareMeters": 1500
    }
  ]
}
```

**錯誤回應**:
```json
{
  "success": false,
  "message": "無效的災害類型。可用類型: None, Flooding, Earthquake, Landslide, Tsunami, AirRaid"
}
```

**HTTP 狀態碼**:
- `200 OK`: 成功
- `400 Bad Request`: 無效的災害類型
- `500 Internal Server Error`: 伺服器錯誤

---

### 3. 根據區域篩選避難所

根據地址中的區域名稱篩選避難所 (主要適用於天然災害避難所)。

**端點**: `GET /api/Shelter/by-district`

**請求參數**:
| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| district | string | 是 | 區域名稱 (例如: 中正區) |

**請求範例**:
```
GET /api/Shelter/by-district?district=中正區
```

**回應範例**:
```json
{
  "success": true,
  "district": "中正區",
  "count": 45,
  "data": [
    {
      "type": "區公所",
      "name": "中正區公所",
      "capacity": 200,
      "supportedDisasters": 3,
      "accesibility": true,
      "address": "臺北市中正區某某路789號",
      "latitude": 25.040,
      "longitude": 121.515,
      "telephone": null,
      "sizeInSquareMeters": 1200
    }
  ]
}
```

**錯誤回應**:
```json
{
  "success": false,
  "message": "請提供區域名稱"
}
```

**HTTP 狀態碼**:
- `200 OK`: 成功
- `400 Bad Request`: 缺少必要參數
- `500 Internal Server Error`: 伺服器錯誤

---

### 4. 根據容量篩選避難所

篩選出容量大於或等於指定值的避難所，並按容量由大到小排序。

**端點**: `GET /api/Shelter/by-capacity`

**請求參數**:
| 參數 | 類型 | 必填 | 預設值 | 說明 |
|------|------|------|--------|------|
| minCapacity | int | 否 | 0 | 最小容納人數 |

**請求範例**:
```
GET /api/Shelter/by-capacity?minCapacity=100
```

**回應範例**:
```json
{
  "success": true,
  "minCapacity": 100,
  "count": 890,
  "data": [
    {
      "type": "體育場館/運動中心",
      "name": "大型體育館",
      "capacity": 5000,
      "supportedDisasters": 15,
      "accesibility": true,
      "address": "臺北市某某區某某路1號",
      "latitude": 25.050,
      "longitude": 121.530,
      "telephone": "02-11111111",
      "sizeInSquareMeters": 10000
    }
  ]
}
```

**HTTP 狀態碼**:
- `200 OK`: 成功
- `500 Internal Server Error`: 伺服器錯誤

---

### 5. 根據名稱搜尋避難所

根據關鍵字搜尋避難所名稱。

**端點**: `GET /api/Shelter/search`

**請求參數**:
| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| name | string | 是 | 搜尋關鍵字 |

**請求範例**:
```
GET /api/Shelter/search?name=學校
```

**回應範例**:
```json
{
  "success": true,
  "keyword": "學校",
  "count": 234,
  "data": [
    {
      "id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "type": "學校",
      "name": "臺北市立某某國小",
      "capacity": 400,
      "currentOccupancy": 0,
      "supportedDisasters": 7,
      "accesibility": true,
      "address": "臺北市某某區某某路100號",
      "latitude": 25.035,
      "longitude": 121.525,
      "telephone": "02-22222222",
      "sizeInSquareMeters": 1800
    }
  ]
}
```

**錯誤回應**:
```json
{
  "success": false,
  "message": "請提供搜尋關鍵字"
}
```

**HTTP 狀態碼**:
- `200 OK`: 成功
- `400 Bad Request`: 缺少搜尋關鍵字
- `500 Internal Server Error`: 伺服器錯誤

---

### 6. 獲取有無障礙設施的避難所

取得所有具備無障礙設施的避難所。

**端點**: `GET /api/Shelter/accessible`

**請求參數**: 無

**回應範例**:
```json
{
  "success": true,
  "count": 456,
  "data": [
    {
      "id": "3",
      "type": "學校",
      "name": "無障礙友善學校",
      "capacity": 300,
      "currentOccupancy": 0,
      "supportedDisasters": 7,
      "accesibility": true,
      "address": "臺北市某某區某某路200號",
      "latitude": 25.042,
      "longitude": 121.518,
      "telephone": "02-33333333",
      "sizeInSquareMeters": 1600
    }
  ]
}
```

**HTTP 狀態碼**:
- `200 OK`: 成功
- `500 Internal Server Error`: 伺服器錯誤

---

### 7. 獲取避難所統計資訊

取得避難所的整體統計資訊，包含總數、容量、類型分佈等。

**端點**: `GET /api/Shelter/statistics`

**請求參數**: 無

**回應範例**:
```json
{
  "success": true,
  "totalShelters": 1234,
  "totalCapacity": 500000,
  "shelterTypes": {
    "naturalDisaster": 1000,
    "airRaid": 234
  },
  "disasterSupport": {
    "flooding": 800,
    "earthquake": 950,
    "landslide": 600,
    "tsunami": 300,
    "airRaid": 234
  },
  "largestShelter": {
    "type": "體育場館/運動中心",
    "name": "大型體育館",
    "capacity": 5000,
    "supportedDisasters": 15,
    "accesibility": true,
    "address": "臺北市某某區某某路1號",
    "latitude": 25.050,
    "longitude": 121.530,
    "telephone": "02-11111111",
    "sizeInSquareMeters": 10000
  },
  "smallestShelter": {
    "type": "AirRaid",
    "name": "小型地下室",
    "capacity": 10,
    "supportedDisasters": 16,
    "accesibility": false,
    "address": "臺北市某某區某某路999號",
    "latitude": 25.025,
    "longitude": 121.510,
    "telephone": null,
    "sizeInSquareMeters": 50
  }
}
```

**HTTP 狀態碼**:
- `200 OK`: 成功
- `500 Internal Server Error`: 伺服器錯誤

---

### 8. 根據地理位置搜尋附近的避難所

根據經緯度座標搜尋指定半徑範圍內的避難所，並按距離由近到遠排序。

**端點**: `GET /api/Shelter/nearby`

**請求參數**:
| 參數 | 類型 | 必填 | 預設值 | 說明 |
|------|------|------|--------|------|
| latitude | double | 是 | - | 緯度 (-90 到 90) |
| longitude | double | 是 | - | 經度 (-180 到 180) |
| radius | double | 否 | 5.0 | 搜尋半徑 (公里，0 到 100) |

**請求範例**:
```
GET /api/Shelter/nearby?latitude=25.060459&longitude=121.509074&radius=2
```

**回應範例**:
```json
{
  "success": true,
  "searchLocation": {
    "latitude": 25.060459,
    "longitude": 121.509074
  },
  "radiusInKm": 2.0,
  "count": 15,
  "data": [
    {
      "id": "4",
      "type": "學校",
      "name": "附近的學校",
      "capacity": 400,
      "currentOccupancy": 0,
      "supportedDisasters": 7,
      "accesibility": true,
      "address": "臺北市中正區某某路50號",
      "latitude": 25.061,
      "longitude": 121.510,
      "telephone": "02-44444444",
      "sizeInSquareMeters": 1700
    }
  ]
}
```

**錯誤回應**:
```json
{
  "success": false,
  "message": "緯度必須在 -90 到 90 之間"
}
```

**HTTP 狀態碼**:
- `200 OK`: 成功
- `400 Bad Request`: 無效的座標或半徑參數
- `500 Internal Server Error`: 伺服器錯誤

---

## 錯誤處理

所有 API 端點在發生錯誤時都會返回一致的錯誤格式：

```json
{
  "success": false,
  "message": "錯誤描述訊息"
}
```

常見的 HTTP 狀態碼：
- `200 OK`: 請求成功
- `400 Bad Request`: 請求參數無效或缺少必要參數
- `500 Internal Server Error`: 伺服器內部錯誤

---

## 使用範例

### 範例 1: 搜尋台北車站附近 3 公里的防空避難所

```bash
# 1. 先搜尋附近的所有避難所
GET /api/Shelter/nearby?latitude=25.047675&longitude=121.517054&radius=3

# 2. 再篩選防空避難所
GET /api/Shelter/by-disaster?type=AirRaid
```

### 範例 2: 尋找中正區內具備無障礙設施且容量超過 100 人的避難所

```bash
# 1. 先取得中正區的避難所
GET /api/Shelter/by-district?district=中正區

# 2. 在前端或後續處理中篩選容量和無障礙設施
# (或使用前端過濾，因為 API 不支援多條件組合查詢)
```

### 範例 3: 查詢所有支援地震災害的避難所

```bash
GET /api/Shelter/by-disaster?type=Earthquake
```

---

## 注意事項

1. **資料來源**: 本 API 整合了兩個外部資料來源：
   - 天然災害避難所 (政府開放資料)
   - 防空避難所 (政府開放資料)

2. **資料更新**: 資料在每次 API 請求時從外部來源即時獲取，確保資料最新。

3. **效能考量**: 
   - 建議使用適當的篩選條件以減少回傳的資料量
   - 地理位置搜尋建議半徑不要超過 10 公里以確保回應速度

4. **座標系統**: 使用 WGS84 座標系統 (與 GPS 相同)

5. **距離計算**: 使用 Haversine 公式計算地球表面兩點之間的距離

6. **災害類型組合**: `supportedDisasters` 欄位使用位元旗標，一個避難所可能支援多種災害類型


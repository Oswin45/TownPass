# 災害事件 API 測試指南

## 啟動應用程式

```bash
cd Backend
dotnet run
```

應用程式將啟動在：
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

## 測試步驟

### 1. 建立災害事件 (POST)

使用 PowerShell 測試：

```powershell
$headers = @{
    "Content-Type" = "application/json"
}

$body = @{
    img = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
    tags = @("火災", "緊急", "台北市")
    description = "信義區發生小型火災，已通報消防隊處理"
    title = "信義區火災事件"
    lnt = 121.5654
    lat = 25.0330
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:5001/api/DisasterEvent" -Method Post -Headers $headers -Body $body -SkipCertificateCheck
$response | ConvertTo-Json -Depth 10

# 儲存回傳的 ID 供後續測試使用
$eventId = $response.data.id
Write-Host "Event ID: $eventId"
```

預期回應：
```json
{
  "success": true,
  "message": "災害事件建立成功",
  "data": {
    "id": "generated-uuid-here",
    "img": "...",
    "tags": ["火災", "緊急", "台北市"],
    "description": "信義區發生小型火災，已通報消防隊處理",
    "title": "信義區火災事件",
    "lnt": 121.5654,
    "lat": 25.033,
    "createdAt": "2025-11-09T10:30:00Z",
    "updatedAt": "2025-11-09T10:30:00Z"
  }
}
```

### 2. 取得所有災害事件 (GET)

```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/DisasterEvent?skip=0&take=10" -Method Get -SkipCertificateCheck
$response | ConvertTo-Json -Depth 10
```

### 3. 根據 ID 取得災害事件 (GET)

```powershell
# 使用前面建立的 $eventId
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/DisasterEvent/$eventId" -Method Get -SkipCertificateCheck
$response | ConvertTo-Json -Depth 10
```

### 4. 更新災害事件 (PUT)

```powershell
$headers = @{
    "Content-Type" = "application/json"
}

$body = @{
    description = "更新：火災已撲滅，現場清理中"
    tags = @("火災", "已處理", "台北市")
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:5001/api/DisasterEvent/$eventId" -Method Put -Headers $headers -Body $body -SkipCertificateCheck
$response | ConvertTo-Json -Depth 10
```

### 5. 搜尋附近的災害事件 (GET)

```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/DisasterEvent/nearby?lat=25.0330&lnt=121.5654&radius=10" -Method Get -SkipCertificateCheck
$response | ConvertTo-Json -Depth 10
```

### 6. 刪除災害事件 (DELETE)

```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/DisasterEvent/$eventId" -Method Delete -SkipCertificateCheck
$response | ConvertTo-Json -Depth 10
```

## 使用 Postman 測試

1. 匯入 `TownPass-API.postman_collection.json`
2. 在 "Disaster Event API" 資料夾中找到所有測試請求
3. 執行 "Create Disaster Event" 建立新事件
4. 從回應中複製 `id` 值
5. 設定 Postman 變數 `eventId` 為複製的 ID
6. 執行其他測試請求

## 使用 curl 測試

### 建立事件
```bash
curl -k -X POST https://localhost:5001/api/DisasterEvent \
  -H "Content-Type: application/json" \
  -d '{
    "img": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
    "tags": ["火災", "緊急", "台北市"],
    "description": "信義區發生小型火災，已通報消防隊處理",
    "title": "信義區火災事件",
    "lnt": 121.5654,
    "lat": 25.0330
  }'
```

### 取得所有事件
```bash
curl -k https://localhost:5001/api/DisasterEvent?skip=0&take=10
```

### 根據 ID 取得事件
```bash
curl -k https://localhost:5001/api/DisasterEvent/{eventId}
```

### 更新事件
```bash
curl -k -X PUT https://localhost:5001/api/DisasterEvent/{eventId} \
  -H "Content-Type: application/json" \
  -d '{
    "description": "更新：火災已撲滅，現場清理中",
    "tags": ["火災", "已處理", "台北市"]
  }'
```

### 刪除事件
```bash
curl -k -X DELETE https://localhost:5001/api/DisasterEvent/{eventId}
```

## 驗證資料庫

如果已安裝 SQLite 工具，可以直接查詢資料庫：

```bash
# 進入資料庫
sqlite3 shelters.db

# 查看所有災害事件
SELECT * FROM DisasterEvents;

# 查看特定事件
SELECT * FROM DisasterEvents WHERE Id = 'your-event-id';

# 離開
.exit
```

## 注意事項

1. **ID 生成**: 伺服器會自動生成 UUID 作為事件 ID
2. **圖片格式**: 圖片必須是 base64 編碼字串
3. **座標系統**: 使用 WGS84 座標系統 (lat/lng)
4. **分頁**: 預設每頁 100 筆，最多 500 筆
5. **搜尋半徑**: 附近事件搜尋的半徑範圍為 0-100 公里

## 錯誤處理

所有 API 回應都包含 `success` 欄位：
- `true`: 操作成功
- `false`: 操作失敗，檢查 `message` 和 `errors` 欄位

範例錯誤回應：
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

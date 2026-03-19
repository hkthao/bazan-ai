# OpenAPI 3.1 — Agronomy Engine · Market Service · API Gateway

| Trường | Nội dung |
|--------|---------|
| **Phiên bản API** | v1.0.0 |
| **Base URL** | `https://api.bazanai.vn/api/v1` |
| **Ngày tạo** | 2026-03-19 |
| **Liên quan** | ADR-004 (Semantic Kernel), Prompt Engineering Playbook |

> File này gộp Agronomy (port 5004), Market (port 5005), và Gateway aggregated spec để tiện tham khảo.

---

# PHẦN 1 — Agronomy Engine Service

## Tổng quan

Agronomy Engine xử lý logic nghiệp vụ cho 6 yếu tố canh tác cà phê. Tất cả endpoints yêu cầu JWT.

---

### `POST /agronomy/diagnose`

Chẩn đoán bệnh/sâu hại từ mô tả triệu chứng hoặc ảnh.

**Request Body:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "symptomDescription": "Lá vàng từ mép vào, mặt dưới có bột màu cam",
  "imageUrl": "s3://farm-photos/an/2026/03/leaf_001.jpg",
  "affectedPercentage": 30,
  "whenNoticed": "2026-03-12",
  "previousTreatment": null
}
```

**Response `200 OK`:**
```json
{
  "alertId": "alert-uuid-v4",
  "diagnosis": {
    "diseaseCode": "leaf_rust",
    "diseaseName": "Bệnh gỉ sắt (Hemileia vastatrix)",
    "confidence": 0.87,
    "severity": "high",
    "affectedArea_pct": 30
  },
  "treatment": {
    "immediateActions": [
      {
        "priority": 1,
        "action": "Phun Carbendazim 50SC — 20ml/10 lít nước",
        "within_hours": 48,
        "totalQuantity": "650 lít nước cho 1.5 ha"
      }
    ],
    "prevention": ["Thu gom lá rụng đốt", "Tạo thông thoáng tán lá"]
  },
  "sourceDocuments": [
    {
      "docId": "wcr-leaf-rust-2023",
      "title": "WCR Leaf Rust Management Guide 2023",
      "relevanceScore": 0.92
    }
  ],
  "requiresFieldVisit": false,
  "generatedAt": "2026-03-19T07:32:00Z"
}
```

**Response `422 Unprocessable`** (ảnh không rõ / confidence thấp):
```json
{
  "error": "low_confidence_diagnosis",
  "confidence": 0.45,
  "message": "Ảnh không đủ rõ để xác định bệnh. Vui lòng mô tả thêm triệu chứng.",
  "clarificationNeeded": ["Triệu chứng ở lá già hay lá non?", "Xuất hiện trên bao nhiêu % cây?"]
}
```

---

### `POST /agronomy/recommend`

Lấy khuyến nghị canh tác cá nhân hóa theo thời điểm hiện tại.

**Request Body:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "topic": "fertilizer",
  "growthStage": "fruit_development"
}
```

**`topic` values:** `fertilizer | irrigation | harvest | pruning | pest_prevention | general`

**Response `200 OK`:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "recommendations": [
    {
      "category": "fertilizer",
      "priority": "high",
      "title": "Bón Kali giai đoạn nuôi trái",
      "details": "Bón 150g KCl/gốc, kết hợp tưới đủ ẩm",
      "timing": "Thực hiện trong 7 ngày tới",
      "quantityFor_ha": "337.5 kg KCl cho 1.5 ha",
      "source": "WASI — Kỹ thuật canh tác Robusta 2024"
    }
  ],
  "nextReviewDate": "2026-04-19"
}
```

---

### `GET /agronomy/schedule/{farmId}`

Lấy lịch canh tác cá nhân hóa 30 ngày tới.

**Response `200 OK`:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "season": "2025-2026",
  "activities": [
    {
      "date": "2026-03-22",
      "type": "irrigation",
      "title": "Tưới đợt 3 tháng 3",
      "details": "Tưới 250 lít/gốc, tổng ~560m³",
      "status": "upcoming",
      "priority": "high"
    },
    {
      "date": "2026-04-01",
      "type": "fertilizer",
      "title": "Bón phân đợt 2",
      "details": "NPK 12-12-17+TE — 200g/gốc",
      "status": "upcoming",
      "priority": "medium"
    }
  ],
  "generatedAt": "2026-03-19T00:00:00Z"
}
```

---

### `POST /agronomy/soil-analysis`

Nhập kết quả phân tích đất và nhận khuyến nghị cải tạo.

**Request Body:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "analysisDate": "2026-03-15",
  "ph": 5.8,
  "nitrogen_ppm": 180,
  "phosphorus_ppm": 45,
  "potassium_ppm": 120,
  "organicMatter_pct": 3.2
}
```

**Response `201 Created`:**
```json
{
  "analysisId": "soil-analysis-uuid",
  "assessment": {
    "ph": { "value": 5.8, "status": "low", "target": "6.0–6.5" },
    "nitrogen": { "value": 180, "status": "adequate" },
    "phosphorus": { "value": 45, "status": "low", "target": "> 60 ppm" }
  },
  "amendments": [
    {
      "material": "Vôi nông nghiệp (CaCO3)",
      "quantity_per_ha": "1,000 kg",
      "timing": "Trước mùa mưa 2 tháng",
      "purpose": "Nâng pH lên 6.2–6.5"
    }
  ]
}
```

---

### `POST /agronomy/irrigation-plan`

Tạo lịch tưới tối ưu dựa trên sensor và thời tiết.

**Request Body:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "currentSoilMoisture_pct": 38,
  "weatherForecast": {
    "rainExpected": false,
    "nextRainfall_days": 8
  }
}
```

**Response `200 OK`:**
```json
{
  "planId": "irr-plan-uuid",
  "urgency": "high",
  "nextIrrigationDate": "2026-03-20",
  "schedule": [
    {
      "date": "2026-03-20",
      "waterVolume_L_per_tree": 250,
      "totalVolume_m3": 562,
      "duration_hours": 4
    },
    {
      "date": "2026-03-27",
      "waterVolume_L_per_tree": 200,
      "totalVolume_m3": 450,
      "duration_hours": 3
    }
  ]
}
```

---

### `GET /agronomy/pest-alerts`

Lấy danh sách cảnh báo sâu bệnh đang hoạt động theo vùng.

**Query Params:**
- `province` (bắt buộc): tên tỉnh
- `district`: tên huyện
- `severity`: `low | medium | high | critical`

**Response `200 OK`:**
```json
{
  "alerts": [
    {
      "alertId": "alert-uuid",
      "diseaseCode": "leaf_rust",
      "diseaseName": "Bệnh gỉ sắt",
      "severity": "high",
      "affectedRegion": "Cư M'gar, Đắk Lắk",
      "affectedFarmsCount": 47,
      "recommendation": "Phun phòng ngay cả khi chưa thấy triệu chứng",
      "validUntil": "2026-04-19"
    }
  ],
  "totalActive": 2
}
```

---

# PHẦN 2 — Market & Economic Service

## Tổng quan

Market Service quản lý giá cà phê, tính toán ROI, và tìm đại lý thu mua. Tất cả endpoints yêu cầu JWT.

---

### `GET /market/prices/current`

Lấy giá cà phê hiện tại.

**Query Params:**
- `type`: `robusta | arabica | all` (default: `all`)
- `region`: tên tỉnh (để lấy giá địa phương)

**Response `200 OK`:**
```json
{
  "prices": [
    {
      "coffeeType": "Robusta",
      "grade": "Grade 2, 5% black & broken",
      "price_vnd_per_kg": 68500,
      "price_usd_per_mt": 2680,
      "change_pct": 5.2,
      "changeDirection": "up",
      "source": "ICE Futures + Local market",
      "region": "Đắk Lắk",
      "recordedAt": "2026-03-19T09:00:00Z"
    }
  ],
  "lastUpdated": "2026-03-19T09:00:00Z",
  "nextUpdateAt": "2026-03-19T12:00:00Z"
}
```

---

### `GET /market/prices/history`

Lịch sử giá cà phê theo khoảng thời gian.

**Query Params:**
- `type`: loại cà phê
- `from`, `to`: ISO 8601 date
- `interval`: `daily | weekly | monthly`

**Response `200 OK`:**
```json
{
  "coffeeType": "Robusta",
  "interval": "weekly",
  "data": [
    { "date": "2026-03-12", "price_vnd": 65200, "price_usd": 2540 },
    { "date": "2026-03-19", "price_vnd": 68500, "price_usd": 2680 }
  ]
}
```

---

### `POST /economics/roi-calculator`

Tính ROI cho mùa vụ hiện tại.

**Request Body:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "season": "2025-2026",
  "estimatedYield_kg": 4500,
  "sellingPrice_vnd_per_kg": 68500,
  "costs": {
    "fertilizer_vnd": 15000000,
    "pesticide_vnd": 3000000,
    "irrigation_vnd": 2000000,
    "labor_vnd": 20000000,
    "other_vnd": 5000000
  }
}
```

**Response `200 OK`:**
```json
{
  "calculationId": "roi-calc-uuid",
  "revenue_vnd": 308250000,
  "totalCosts_vnd": 45000000,
  "netProfit_vnd": 263250000,
  "roi_pct": 584.9,
  "breakevenPrice_vnd_per_kg": 10000,
  "profitPerHa_vnd": 175500000,
  "assessment": "excellent",
  "comparison": {
    "vsLastSeason_pct": 12.3,
    "vsRegionalAvg_pct": 8.5
  }
}
```

---

### `POST /economics/breakeven`

Tính giá hòa vốn.

**Request Body:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "totalCosts_vnd": 45000000,
  "estimatedYield_kg": 4500
}
```

**Response `200 OK`:**
```json
{
  "breakevenPrice_vnd_per_kg": 10000,
  "currentMarketPrice_vnd": 68500,
  "safetyMargin_pct": 585,
  "recommendation": "Giá thị trường hiện tại cao hơn giá hòa vốn 585% — rất an toàn để thu hoạch"
}
```

---

### `GET /market/buyers`

Tìm đại lý thu mua gần vườn.

**Query Params:**
- `lat`, `lng`: tọa độ GPS (bắt buộc)
- `radius_km` (default: 10, max: 50)
- `coffeeType`: loại cà phê

**Response `200 OK`:**
```json
{
  "buyers": [
    {
      "id": "buyer-uuid",
      "companyName": "Đại lý Minh Tâm",
      "contactPhone": "+84901111111",
      "distance_km": 3.2,
      "currentBuyingPrice_vnd": 68000,
      "grade": "Grade 2",
      "lastUpdated": "2026-03-19T08:00:00Z",
      "location": { "lat": 12.6500, "lng": 108.0200 }
    }
  ],
  "total": 5,
  "bestPrice": { "buyerName": "Đại lý Minh Tâm", "price_vnd": 68000 }
}
```

---

### `POST /economics/sell-decision`

Phân tích nên bán ngay hay giữ thêm.

**Request Body:**
```json
{
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "availableStock_kg": 3000,
  "storageCost_per_day_vnd": 50000,
  "cashFlowNeed": false
}
```

**Response `200 OK`:**
```json
{
  "recommendation": "hold",
  "reasoning": "Giá đang tăng theo xu hướng 30 ngày, dự báo tăng thêm 2-3%",
  "currentValue_vnd": 205500000,
  "projectedValue_30d_vnd": 212000000,
  "holdingCost_30d_vnd": 1500000,
  "netGain_if_hold_vnd": 5000000,
  "riskLevel": "medium",
  "breakEvenHoldDays": 5
}
```

---

### `GET /market/forecast`

Dự báo giá trong N ngày tới.

**Query Params:**
- `days`: 7 | 14 | 30 (default: 30)
- `coffeeType`: robusta | arabica

**Response `200 OK`:**
```json
{
  "coffeeType": "Robusta",
  "forecastDays": 30,
  "currentPrice_vnd": 68500,
  "forecasts": [
    { "date": "2026-03-26", "price_vnd": 69200, "confidence": 0.78 },
    { "date": "2026-04-02", "price_vnd": 70100, "confidence": 0.65 },
    { "date": "2026-04-19", "price_vnd": 71000, "confidence": 0.45 }
  ],
  "trend": "bullish",
  "disclaimer": "Dự báo chỉ mang tính tham khảo, không phải lời khuyên đầu tư"
}
```

---

# PHẦN 3 — API Gateway Aggregated Spec

## Tổng quan

Gateway là single entry point tại `https://api.bazanai.vn`. Toàn bộ routing dựa trên path prefix.

## Routing Table

| Path Pattern | Upstream Service | Auth Required |
|-------------|-----------------|---------------|
| `/api/v1/auth/**` | identity-svc:5001 | No |
| `/api/v1/farmers/**` | identity-svc:5001 | Yes |
| `/api/v1/sessions/**` | conversation-svc:5002 | Yes |
| `/api/v1/messages/**` | conversation-svc:5002 | Yes |
| `/api/v1/feedback/**` | conversation-svc:5002 | Yes |
| `/api/v1/agronomy/**` | agronomy-svc:5004 | Yes |
| `/api/v1/market/**` | market-svc:5005 | Yes |
| `/api/v1/economics/**` | market-svc:5005 | Yes |
| `/api/v1/media/**` | media-svc:5009 | Yes |
| `/hub/**` | ai-orchestrator:5010 | Yes (WebSocket) |
| `/healthz` | Gateway self | No |

## Rate Limiting

| Tier | Limit | Window | Header |
|------|-------|--------|--------|
| Free (default) | 60 req | 1 phút | `X-RateLimit-Tier: free` |
| Premium (HTX) | 300 req | 1 phút | `X-RateLimit-Tier: premium` |
| AI endpoints `/hub/*` | 20 req | 1 phút | Riêng biệt |

**Rate limit headers trong response:**
```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1710835260
```

**Response khi vượt giới hạn `429`:**
```json
{
  "error": "rate_limit_exceeded",
  "message": "Quá nhiều yêu cầu, vui lòng thử lại sau.",
  "retryAfter": 60
}
```

## Gateway Headers

**Request headers forwarded đến upstream:**
```
X-Farmer-Id: {farmerId từ JWT sub}
X-Farm-Ids: {farm_ids từ JWT, comma-separated}
X-Farmer-Role: {role từ JWT}
X-Correlation-Id: {generated UUID}
X-Forwarded-For: {client IP}
```

## Health Check

### `GET /healthz`

Kiểm tra trạng thái Gateway và tất cả upstream services.

**Response `200 OK`:**
```json
{
  "status": "healthy",
  "timestamp": "2026-03-19T08:00:00Z",
  "services": {
    "identity-svc":     { "status": "healthy", "latency_ms": 12 },
    "conversation-svc": { "status": "healthy", "latency_ms": 8 },
    "agronomy-svc":     { "status": "healthy", "latency_ms": 15 },
    "market-svc":       { "status": "healthy", "latency_ms": 9 },
    "ai-orchestrator":  { "status": "healthy", "latency_ms": 45 },
    "knowledge-rag":    { "status": "healthy", "latency_ms": 32 }
  }
}
```

**Response `503 Service Unavailable`** (khi có service down):
```json
{
  "status": "degraded",
  "degradedServices": ["agronomy-svc"],
  "message": "Chức năng chẩn đoán bệnh tạm thời không khả dụng"
}
```

**© 2026 Bazan AI Project — Confidential**

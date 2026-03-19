# Bazan AI — Tài Liệu Kiến Trúc Hệ Thống
**Phiên bản:** 1.0.0  
**Ngày:** 2026-03-19  
**Tác giả:** Principal Software Architect  
**Trạng thái:** Draft — Pending Review  

---

## Mục lục

1. [Tổng quan dự án](#1-tổng-quan-dự-án)
2. [Mục tiêu kiến trúc & Nguyên tắc thiết kế](#2-mục-tiêu-kiến-trúc--nguyên-tắc-thiết-kế)
3. [Kiến trúc tổng thể](#3-kiến-trúc-tổng-thể)
4. [Chi tiết các Microservices](#4-chi-tiết-các-microservices)
5. [Cơ sở hạ tầng dữ liệu](#5-cơ-sở-hạ-tầng-dữ-liệu)
6. [MongoDB Schema Design](#6-mongodb-schema-design)
7. [Messaging & Event Contracts](#7-messaging--event-contracts)
8. [Cấu trúc thư mục (Clean Architecture)](#8-cấu-trúc-thư-mục-clean-architecture)
9. [Docker Compose — Full Stack](#9-docker-compose--full-stack)
10. [Bảo mật & Xác thực](#10-bảo-mật--xác-thực)
11. [Observability Stack](#11-observability-stack)
12. [Architecture Decision Records (ADR)](#12-architecture-decision-records-adr)
13. [Lộ trình phát triển (Roadmap)](#13-lộ-trình-phát-triển-roadmap)
14. [Phân tích Gap — Các thành phần cần bổ sung](#14-phân-tích-gap--các-thành-phần-cần-bổ-sung)

---

## 1. Tổng quan dự án

### 1.1 Giới thiệu

**Bazan AI** là nền tảng trí tuệ nhân tạo hướng đến nông dân trồng cà phê tại khu vực Tây Nguyên, Việt Nam. Hệ thống cung cấp tư vấn canh tác theo thời gian thực, phân tích kinh tế, và dự báo dựa trên dữ liệu đa nguồn: cảm biến IoT, cơ sở dữ liệu tri thức nông nghiệp (WCR), giá thị trường, và lịch sử hành vi của từng nông dân.

### 1.2 Đối tượng người dùng

| Nhóm | Mô tả | Kênh tương tác |
|------|--------|----------------|
| Nông dân nhỏ lẻ | 0.5–2 ha, ít kinh nghiệm kỹ thuật | Mobile App, Zalo Bot |
| Nông dân lớn | 2–10 ha, có nhu cầu phân tích ROI | Mobile App, Web |
| Hợp tác xã | Quản lý nhóm nông dân | Web Dashboard |
| Cán bộ kỹ thuật | Cập nhật tri thức, giám sát | Admin Portal |

### 1.3 Phạm vi hệ thống

Hệ thống tập trung vào **6 yếu tố canh tác cà phê:**

1. **Đất** — Phân tích pH, dinh dưỡng, cấu trúc đất bazan
2. **Nước** — Lịch tưới, lượng mưa, độ ẩm từ cảm biến
3. **Giống** — Nhận diện chủng loại, đặc tính từng giống
4. **Phân bón** — Công thức bón theo giai đoạn sinh trưởng
5. **Sâu bệnh** — Phát hiện sớm từ ảnh, cảnh báo dịch
6. **Thu hoạch** — Tối ưu thời điểm, dự báo sản lượng

---

## 2. Mục tiêu kiến trúc & Nguyên tắc thiết kế

### 2.1 Quality Attributes (ISO 25010)

| Thuộc tính | Mục tiêu | Metric |
|-----------|---------|--------|
| **Availability** | 99.5% uptime | < 4.4 giờ downtime/tháng |
| **Latency** | API response | P95 < 2 giây cho AI response |
| **Scalability** | Horizontal scale | Hỗ trợ 10.000 nông dân đồng thời |
| **Resilience** | Fault tolerance | Circuit breaker, retry policy |
| **Security** | Data privacy | Mã hóa at-rest và in-transit |
| **Offline-first** | Rural network | Sync khi có kết nối |

### 2.2 Nguyên tắc thiết kế

- **Domain-Driven Design (DDD):** Mỗi service là một Bounded Context độc lập
- **Event-Driven Architecture:** Giao tiếp async qua RabbitMQ, giảm coupling
- **CQRS (Command Query Responsibility Segregation):** Tách đọc/ghi để tối ưu performance
- **API-First:** Thiết kế contract trước khi implement
- **Immutable Infrastructure:** Mọi thứ chạy trong container, reproducible
- **Zero-Trust Security:** Mọi service phải xác thực lẫn nhau

---

## 3. Kiến trúc tổng thể

### 3.1 Sơ đồ các lớp

```
┌─────────────────────────────────────────────────────────┐
│                    CLIENT LAYER                          │
│   Mobile App (Flutter)  │  Web (React)  │  Zalo Bot     │
└─────────────────┬───────────────────────────────────────┘
                  │ HTTPS / WSS
┌─────────────────▼───────────────────────────────────────┐
│              API GATEWAY (YARP + .NET 8)                 │
│      Auth · Rate Limit · Routing · Load Balance          │
└──────┬──────────┬──────────┬──────────┬─────────────────┘
       │          │          │          │
┌──────▼──────────▼──────────▼──────────▼─────────────────┐
│            AI AGENT ORCHESTRATOR                         │
│         (Semantic Kernel / .NET 8)                       │
│   Multi-step reasoning · Tool selection · RAG routing    │
└──┬────────┬────────┬────────┬────────┬──────────────────┘
   │        │        │        │        │
┌──▼──┐  ┌──▼──┐  ┌──▼──┐  ┌──▼──┐  ┌──▼──┐
│ IDS │  │ CMS │  │ KRS │  │ AES │  │ MES │  (Existing)
└──┬──┘  └──┬──┘  └──┬──┘  └──┬──┘  └──┬──┘
   │                            │
┌──▼───────────────────────────▼──────────────────────────┐
│         RABBITMQ EVENT BUS                               │
└──┬────────┬────────┬────────┬────────┬──────────────────┘
   │        │        │        │        │
┌──▼──┐  ┌──▼──┐  ┌──▼──┐  ┌──▼──┐  ┌──▼──┐
│ NS  │  │ IoT │  │ WS  │  │ MS  │  │ SS  │  (New)
└─────┘  └─────┘  └─────┘  └─────┘  └─────┘

┌─────────────────────────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                     │
│  MongoDB RS │ Qdrant │ Redis │ MinIO │ RabbitMQ         │
├─────────────────────────────────────────────────────────┤
│                 OBSERVABILITY STACK                      │
│  Seq (Logs) │ Prometheus │ Grafana │ OpenTelemetry       │
└─────────────────────────────────────────────────────────┘

Legend:
  IDS = Identity & User Context Service
  CMS = Conversation Management Service
  KRS = Knowledge RAG Service (Python)
  AES = Agronomy Engine Service
  MES = Market & Economic Service
  NS  = Notification Service
  IoT = IoT Ingestion Service
  WS  = Weather Integration Service
  MS  = Media & File Service
  SS  = Scheduler Service
```

### 3.2 Communication Patterns

| Pattern | Khi nào dùng | Công nghệ |
|---------|-------------|-----------|
| **Sync REST** | Client → Gateway, Gateway → Services | HTTPS/JSON |
| **Sync gRPC** | Service-to-service (low latency) | gRPC + Protobuf |
| **Async Event** | Pub/Sub, fire-and-forget | RabbitMQ |
| **WebSocket** | Chat streaming (AI tokens) | SignalR |
| **MQTT** | IoT sensor data | MQTT Broker |

---

## 4. Chi tiết các Microservices

### 4.1 Identity & User Context Service (IDS)

**Bounded Context:** Quản lý danh tính và hồ sơ nông dân  
**Technology:** .NET 8, C#, MongoDB  
**Port:** 5001  

**Trách nhiệm:**
- Xác thực và cấp JWT token
- Quản lý profile nông dân (thông tin cá nhân, vị trí GPS)
- Lưu trữ metadata vườn: diện tích, số cây, chủng giống, lịch sử canh tác
- Thu thập và lưu dữ liệu hành vi người dùng (User Context) phục vụ AI personalization
- Quản lý phiên làm việc (Session)

**APIs chính:**
```
POST   /api/v1/auth/register
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
GET    /api/v1/farmers/{id}
PUT    /api/v1/farmers/{id}/profile
POST   /api/v1/farmers/{id}/farms
PUT    /api/v1/farmers/{id}/farms/{farmId}
GET    /api/v1/farmers/{id}/context
PATCH  /api/v1/farmers/{id}/context
```

**Events Published:**
- `FarmerRegistered`
- `FarmProfileUpdated`
- `UserLocationUpdated`
- `CropVarietyChanged`

---

### 4.2 Conversation Management Service (CMS)

**Bounded Context:** Lịch sử hội thoại và học tăng cường  
**Technology:** .NET 8, C#, MongoDB  
**Port:** 5002  

**Trách nhiệm:**
- Lưu trữ toàn bộ lịch sử chat theo Session
- Quản lý Context Window của từng phiên AI (sliding window)
- Thu thập Feedback từ nông dân (thumbs up/down, rating)
- Cung cấp dữ liệu cho Reinforcement Learning từ Human Feedback (RLHF)
- Export conversation data cho fine-tuning pipeline

**APIs chính:**
```
POST   /api/v1/sessions
GET    /api/v1/sessions/{sessionId}
DELETE /api/v1/sessions/{sessionId}
POST   /api/v1/sessions/{sessionId}/messages
GET    /api/v1/sessions/{sessionId}/messages
POST   /api/v1/messages/{messageId}/feedback
GET    /api/v1/feedback/export          # For RLHF pipeline
GET    /api/v1/sessions/{sessionId}/context-window
```

**Events Published:**
- `ConversationSessionStarted`
- `MessageReceived`
- `FeedbackCollected`
- `SessionSummarized`

---

### 4.3 Knowledge RAG Service (KRS)

**Bounded Context:** Truy xuất tri thức nông nghiệp  
**Technology:** Python 3.12, FastAPI, Qdrant, LangChain  
**Port:** 5003  

**Trách nhiệm:**
- Index tài liệu PDF (WCR, kỹ thuật canh tác, nghiên cứu) vào Qdrant
- Thực hiện Hybrid Search (dense + sparse vector) để tìm kiếm tri thức liên quan
- Re-ranking kết quả theo context của nông dân (loại đất, giống cây, vùng)
- Cung cấp citation và nguồn gốc thông tin cho AI responses
- Quản lý Knowledge Base versions

**APIs chính:**
```
POST   /api/v1/knowledge/index           # Index PDF documents
POST   /api/v1/knowledge/search          # Hybrid search
POST   /api/v1/knowledge/search/context  # Context-aware search
DELETE /api/v1/knowledge/{docId}
GET    /api/v1/knowledge/stats
POST   /api/v1/knowledge/reindex
```

**Hybrid Search Configuration:**
```python
# Dense vector: text-embedding-3-large (3072 dims)
# Sparse vector: BM42 (Qdrant native)
# Fusion: Reciprocal Rank Fusion (RRF)
# Filter: by soil_type, coffee_variety, region
```

**Events Consumed:**
- `NewDocumentUploaded` → trigger re-index

**Events Published:**
- `NewCoffeeVarietyIndexed`
- `KnowledgeBaseUpdated`

---

### 4.4 Agronomy Engine Service (AES)

**Bounded Context:** Logic canh tác và khuyến nghị kỹ thuật  
**Technology:** .NET 8, C#, MongoDB  
**Port:** 5004  

**Trách nhiệm:**
- Xử lý logic nghiệp vụ cho 6 yếu tố canh tác cà phê
- Tạo lịch canh tác cá nhân hóa (fertilization schedule, irrigation plan)
- Phân tích dữ liệu cảm biến và đưa ra cảnh báo sớm
- Nhận diện sâu bệnh từ ảnh (tích hợp Computer Vision model)
- Tính toán nhu cầu nước và phân bón theo giai đoạn sinh trưởng

**Domain Logic — 6 Factors:**

| Factor | Input Sources | Output |
|--------|-------------|--------|
| Đất | Soil sensor, GPS, lab results | pH recommendation, amendment plan |
| Nước | Rain gauge, soil moisture, weather forecast | Irrigation schedule |
| Giống | Farmer profile, GPS region | Variety-specific advice |
| Phân bón | Growth stage, soil test, yield history | Fertilization formula |
| Sâu bệnh | Photo upload, weather, season | Disease alert, treatment plan |
| Thu hoạch | Maturity model, weather, market price | Harvest timing recommendation |

**APIs chính:**
```
POST   /api/v1/agronomy/diagnose         # Disease diagnosis from photo
POST   /api/v1/agronomy/recommend        # Personalized recommendations
GET    /api/v1/agronomy/schedule/{farmId}
POST   /api/v1/agronomy/soil-analysis
POST   /api/v1/agronomy/irrigation-plan
GET    /api/v1/agronomy/pest-alerts      # Active pest alerts by region
```

**Events Consumed:**
- `WeatherAlertTriggered`
- `SensorDataReceived`
- `UserLocationUpdated`

**Events Published:**
- `DiseaseAlertGenerated`
- `IrrigationReminderCreated`
- `HarvestWindowOpened`

---

### 4.5 Market & Economic Service (MES)

**Bounded Context:** Phân tích kinh tế và thị trường  
**Technology:** .NET 8, C#, MongoDB  
**Port:** 5005  

**Trách nhiệm:**
- Theo dõi giá cà phê Robusta/Arabica (ICE Futures, giá nội địa)
- Tính toán ROI theo mùa vụ, chi phí đầu vào
- Phân tích điểm hòa vốn và dự báo lợi nhuận
- So sánh giá đại lý thu mua trong vùng
- Tư vấn thời điểm bán tối ưu (hold vs sell)

**APIs chính:**
```
GET    /api/v1/market/prices/current
GET    /api/v1/market/prices/history?from=&to=
POST   /api/v1/economics/roi-calculator
POST   /api/v1/economics/breakeven
GET    /api/v1/market/buyers?lat=&lng=&radius=
GET    /api/v1/market/forecast?days=30
POST   /api/v1/economics/sell-decision   # Hold vs sell analysis
```

**Events Published:**
- `CoffeePriceUpdated`
- `PriceAlertTriggered`
- `MarketReportGenerated`

---

### 4.6 API Gateway (New)

**Technology:** .NET 8, YARP (Yet Another Reverse Proxy)  
**Port:** 8080 (HTTP), 8443 (HTTPS)  

**Trách nhiệm:**
- Single entry point cho toàn bộ hệ thống
- JWT validation và forward claims
- Rate limiting (sliding window, 60 req/min cho free tier)
- Request routing đến đúng upstream service
- Response aggregation (BFF pattern cho mobile)
- Circuit breaker với Polly
- Request/Response logging (correlation ID)

**YARP Configuration:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity-cluster",
        "Match": { "Path": "/api/v1/auth/{**catch-all}" }
      },
      "agronomy-route": {
        "ClusterId": "agronomy-cluster",
        "Match": { "Path": "/api/v1/agronomy/{**catch-all}" },
        "AuthorizationPolicy": "RequireAuthenticatedUser"
      }
    }
  }
}
```

---

### 4.7 AI Agent Orchestrator (New)

**Technology:** .NET 8, Microsoft Semantic Kernel  
**Port:** 5010  

**Trách nhiệm:**
- Nhận intent từ Conversation Service
- Lập kế hoạch multi-step (planning) để trả lời câu hỏi phức tạp
- Gọi đúng tool/service theo context
- Tổng hợp kết quả từ nhiều nguồn thành câu trả lời nhất quán
- Streaming response về client qua SignalR
- Quản lý token budget cho LLM calls

**Agent Tool Registry:**
```csharp
// Semantic Kernel Tool definitions
[KernelFunction("get_agronomy_advice")]
[Description("Get agronomic recommendations for coffee farming")]
public async Task<string> GetAgronomyAdvice(string farmId, string issue) { }

[KernelFunction("search_knowledge_base")]
[Description("Search WCR and agronomic knowledge base")]
public async Task<string> SearchKnowledge(string query, string context) { }

[KernelFunction("get_market_price")]
[Description("Get current coffee market prices and ROI analysis")]
public async Task<string> GetMarketData(string coffeeType) { }

[KernelFunction("get_weather_forecast")]
[Description("Get weather forecast for a specific farm location")]
public async Task<string> GetWeather(double lat, double lng) { }
```

---

### 4.8 Notification Service (New)

**Technology:** .NET 8, C#  
**Port:** 5006  

**Kênh phân phối:**
- **Zalo OA:** Kênh chính (>80% nông dân Tây Nguyên dùng Zalo)
- **FCM (Firebase):** Push notification cho Mobile App
- **SMS:** Fallback khi mất mạng (Twilio / VIETTEL SMS API)
- **Email:** Báo cáo định kỳ, tài khoản

**Events Consumed:**
- `DiseaseAlertGenerated` → push Zalo + FCM
- `CoffeePriceUpdated` (>5% change) → push notification
- `WeatherAlertTriggered` → push Zalo
- `IrrigationReminderCreated` → scheduled push
- `HarvestWindowOpened` → push Zalo + SMS

---

### 4.9 IoT Ingestion Service (New)

**Technology:** .NET 8, MQTTnet library  
**Port:** 5007, MQTT: 1883  

**Supported Sensor Types:**
- Độ ẩm đất (soil moisture)
- Nhiệt độ đất và không khí
- pH sensor
- Lượng mưa (rain gauge)
- Ánh sáng (lux meter)

**Pipeline:**
```
Sensor → MQTT Broker → IoT Ingestion → Normalize → MongoDB
                                                  → RabbitMQ (SensorDataReceived)
```

---

### 4.10 Weather Integration Service (New)

**Technology:** .NET 8 Worker Service  
**Port:** 5008  

**Data Sources:**
- OpenWeatherMap API (global forecast)
- Vietnam Meteorological API (local accuracy)

**Schedule:** Poll mỗi 3 giờ, alert trigger real-time  

**Events Published:**
- `WeatherAlertTriggered` (khi nhiệt độ/mưa vượt ngưỡng)
- `WeatherForecastUpdated`

---

## 5. Cơ sở hạ tầng dữ liệu

### 5.1 MongoDB — Replica Set

**Vai trò:** Primary datastore cho tất cả services  
**Cấu hình:** 1 Primary + 2 Secondary (trong Docker Compose: 3 nodes)  
**Databases:**

| Database | Service | Mục đích |
|----------|---------|---------|
| `bazan_identity` | IDS | Farmer profiles, farm data |
| `bazan_conversations` | CMS | Chat history, sessions, feedback |
| `bazan_agronomy` | AES | Schedules, diagnoses, alerts |
| `bazan_market` | MES | Price history, ROI calculations |
| `bazan_notifications` | NS | Notification logs, templates |
| `bazan_weather` | WS | Forecast cache, alert history |

**Indexing Strategy:**
```javascript
// Identity DB
db.farmers.createIndex({ "location.coordinates": "2dsphere" })  // Geospatial
db.farmers.createIndex({ "farmContext.soilType": 1, "farmContext.coffeeVariety": 1 })
db.farmers.createIndex({ "createdAt": 1 }, { expireAfterSeconds: 0 })

// Conversations DB
db.chatSessions.createIndex({ "farmerId": 1, "startedAt": -1 })
db.chatMessages.createIndex({ "sessionId": 1, "timestamp": 1 })
db.chatMessages.createIndex({ "farmerId": 1, "feedback.rating": 1 })  // RLHF queries
```

---

### 5.2 Qdrant — Vector Database

**Vai trò:** Lưu trữ và tìm kiếm vector embeddings cho RAG  
**Port:** 6333 (HTTP), 6334 (gRPC)  

**Collections:**

| Collection | Vector Dims | Distance | Nội dung |
|-----------|------------|---------|---------|
| `wcr_documents` | 3072 | Cosine | WCR research papers |
| `agronomy_knowledge` | 3072 | Cosine | Kỹ thuật canh tác |
| `pest_disease_library` | 3072 | Cosine | Sâu bệnh, triệu chứng |
| `market_reports` | 3072 | Cosine | Báo cáo thị trường |

**Payload Schema (wcr_documents):**
```json
{
  "doc_id": "wcr-2024-arabica-processing",
  "title": "Arabica Processing Methods",
  "source": "WCR Technical Report 2024",
  "language": "vi",
  "coffee_variety": ["Arabica", "Bourbon"],
  "topics": ["processing", "fermentation", "quality"],
  "region": ["Tây Nguyên", "Lâm Đồng"],
  "chunk_index": 3,
  "total_chunks": 12,
  "created_at": "2024-01-15T00:00:00Z"
}
```

---

### 5.3 Redis — Cache Layer

**Vai trò:** Session cache, rate limiting, real-time price cache  
**Port:** 6379  

**Cache Strategy:**

| Key Pattern | TTL | Nội dung |
|------------|-----|---------|
| `session:{sessionId}` | 30 phút | Active session context |
| `price:robusta:current` | 5 phút | Giá Robusta hiện tại |
| `weather:{lat}:{lng}` | 3 giờ | Forecast cache |
| `farmer:{id}:context` | 10 phút | UserContext cache |
| `rate_limit:{ip}:{endpoint}` | 1 phút | Rate limiting counter |

---

### 5.4 MinIO — Object Storage

**Vai trò:** S3-compatible storage cho files và media  
**Port:** 9000 (API), 9001 (Console)  

**Buckets:**

| Bucket | Nội dung | Access |
|--------|---------|--------|
| `knowledge-docs` | PDF tài liệu WCR | Private (service only) |
| `farm-photos` | Ảnh vườn, lá bệnh từ nông dân | Private |
| `reports` | PDF báo cáo xuất ra | Private |
| `avatars` | Ảnh đại diện | Public CDN |

---

## 6. MongoDB Schema Design

### 6.1 UserContext Document

```json
{
  "_id": "ObjectId('64f1a2b3c4d5e6f7a8b9c0d1')",
  "farmerId": "ObjectId('...')",
  "version": 3,
  
  "personalInfo": {
    "fullName": "Nguyễn Văn An",
    "phoneNumber": "+84901234567",
    "zaloId": "zalo_uid_123456",
    "preferredLanguage": "vi",
    "literacyLevel": "basic"
  },

  "location": {
    "province": "Đắk Lắk",
    "district": "Cư M'gar",
    "commune": "Quảng Tiến",
    "coordinates": {
      "type": "Point",
      "coordinates": [108.0388, 12.6702]
    },
    "altitude_m": 650,
    "microclimateZone": "highland_wet"
  },

  "farmContext": {
    "totalArea_ha": 1.5,
    "numberOfTrees": 2250,
    "coffeeVariety": ["Robusta", "TR4"],
    "treeAgeDistribution": {
      "0-3yr": 300,
      "4-8yr": 1500,
      "9yr+": 450
    },
    "soilType": "basalt",
    "soilPH": 6.2,
    "irrigationSystem": "drip",
    "shadingTrees": true,
    "certifications": ["VietGAP"],
    "intercropTypes": ["pepper", "durian"]
  },

  "cultivationHistory": [
    {
      "season": "2024-2025",
      "yieldKg": 3200,
      "fertilizerUsed": [
        { "type": "NPK 16-16-8", "kg": 450 },
        { "type": "Organic", "kg": 1200 }
      ],
      "pestIncidents": ["leaf_rust_2024_03", "berry_borer_2024_08"],
      "irrigationCycles": 18,
      "sellingPrice_vnd": 67000,
      "revenue_vnd": 214400000
    }
  ],

  "sensorMetadata": {
    "devices": [
      {
        "deviceId": "SENSOR_001_AN",
        "type": "soil_moisture",
        "installLocation": { "type": "Point", "coordinates": [108.0390, 12.6705] },
        "installedAt": "2024-06-01T00:00:00Z",
        "lastHeartbeat": "2026-03-19T08:00:00Z",
        "status": "active"
      }
    ],
    "lastSensorReadings": {
      "soilMoisture_pct": 65.3,
      "soilTemp_c": 24.1,
      "airTemp_c": 28.5,
      "rainfall_mm_7d": 12.4,
      "recordedAt": "2026-03-19T08:00:00Z"
    }
  },

  "behaviorProfile": {
    "preferredQueryTopics": ["fertilizer", "pest_control", "price"],
    "sessionFrequency": "weekly",
    "avgSessionDuration_min": 8,
    "mostActiveHour": 17,
    "feedbackResponseRate": 0.72,
    "aiInteractionStyle": "concise"
  },

  "aiPersonalization": {
    "knowledgeLevel": "intermediate",
    "adaptivePromptVersion": "v2.3",
    "lastSuccessfulRecommendations": [
      {
        "type": "irrigation_reduction",
        "appliedAt": "2026-02-15T00:00:00Z",
        "outcome": "positive",
        "feedbackScore": 5
      }
    ]
  },

  "createdAt": "2023-11-01T00:00:00Z",
  "updatedAt": "2026-03-19T08:00:00Z",
  "schemaVersion": "2.1"
}
```

---

### 6.2 ChatSession & ChatMessage Documents

```json
// Collection: chatSessions
{
  "_id": "ObjectId('...')",
  "sessionId": "sess_20260319_AN_001",
  "farmerId": "ObjectId('...')",
  "farmerName": "Nguyễn Văn An",
  
  "sessionMeta": {
    "startedAt": "2026-03-19T07:30:00Z",
    "endedAt": "2026-03-19T07:45:00Z",
    "duration_sec": 900,
    "channel": "mobile_app",
    "deviceInfo": { "os": "Android", "appVersion": "2.1.0" },
    "networkQuality": "3G"
  },

  "contextSnapshot": {
    "farmContextVersion": 3,
    "activeAlerts": ["leaf_rust_risk_high"],
    "recentWeather": "dry_spell_7d",
    "currentCoffeePrice_vnd": 68500,
    "sessionIntent": "pest_diagnosis"
  },

  "summary": "Nông dân hỏi về triệu chứng lá vàng, được tư vấn bệnh gỉ sắt và phác đồ xử lý.",
  "summaryEmbedding": [0.023, -0.145, ...],

  "messageCount": 8,
  "tokenUsage": {
    "promptTokens": 2340,
    "completionTokens": 876,
    "totalCost_usd": 0.034
  },

  "satisfactionRating": 5,
  "usedForRLHF": false,
  "createdAt": "2026-03-19T07:30:00Z"
}
```

```json
// Collection: chatMessages
{
  "_id": "ObjectId('...')",
  "messageId": "msg_001_sess_20260319",
  "sessionId": "sess_20260319_AN_001",
  "farmerId": "ObjectId('...')",
  
  "role": "user",
  "content": "Cây cà phê của tôi bị vàng lá từ tuần trước, chụp ảnh đây thầy ơi",
  "contentType": "text_with_image",
  
  "attachments": [
    {
      "type": "image",
      "url": "s3://farm-photos/an/2026/03/leaf_001.jpg",
      "thumbnailUrl": "s3://farm-photos/an/2026/03/leaf_001_thumb.jpg",
      "analysisResult": {
        "detectedIssue": "leaf_rust",
        "confidence": 0.87,
        "model": "agronomy-cv-v1.2"
      }
    }
  ],

  "contextUsed": {
    "ragDocuments": [
      { "docId": "wcr-leaf-rust-2023", "relevanceScore": 0.92 }
    ],
    "sensorDataUsed": true,
    "weatherDataUsed": true
  },

  "feedback": {
    "rating": 5,
    "thumbs": "up",
    "comment": "Đúng rồi thầy ơi, thuốc đó tôi đã thấy ở đại lý",
    "submittedAt": "2026-03-19T07:44:00Z",
    "usedForTraining": false
  },

  "latency_ms": 1847,
  "timestamp": "2026-03-19T07:31:00Z"
}
```

---

## 7. Messaging & Event Contracts

### 7.1 RabbitMQ Topology

```
Exchange: bazan.events (type: topic)
Exchange: bazan.commands (type: direct)

Queues:
├── bazan.farmer.events          # Identity events
├── bazan.conversation.events    # Chat events
├── bazan.agronomy.events        # Farming alerts
├── bazan.market.events          # Price events
├── bazan.notification.dispatch  # Notification triggers
├── bazan.iot.ingestion          # Sensor data
├── bazan.weather.events         # Weather alerts
└── bazan.deadletter             # DLQ for failed messages
```

---

### 7.2 Event Schema Chuẩn (Base)

```json
{
  "eventId": "uuid-v4",
  "eventType": "bazan.farmer.location.updated",
  "version": "1.0",
  "source": "identity-service",
  "correlationId": "req-uuid-v4",
  "causationId": "parent-event-id",
  "occurredAt": "2026-03-19T07:30:00.000Z",
  "payload": { }
}
```

---

### 7.3 Định nghĩa các Events

#### `UserLocationUpdated`
```json
{
  "eventType": "bazan.farmer.location.updated",
  "payload": {
    "farmerId": "ObjectId",
    "farmId": "ObjectId",
    "previousCoordinates": { "lat": 12.6700, "lng": 108.0385 },
    "newCoordinates": { "lat": 12.6702, "lng": 108.0388 },
    "altitude_m": 650,
    "updatedBy": "farmer_self"
  }
}
```

**Consumers:** `WeatherService` (cập nhật forecast zone), `AgronomyEngine` (cập nhật vùng sâu bệnh)

---

#### `NewCoffeeVarietyIndexed`
```json
{
  "eventType": "bazan.knowledge.variety.indexed",
  "payload": {
    "varietyCode": "TR4",
    "varietyName": "Thiên Rang 4",
    "documentIds": ["qdrant-doc-id-1", "qdrant-doc-id-2"],
    "sourceDocuments": ["wcr-tr4-2024.pdf"],
    "characteristics": {
      "resistances": ["leaf_rust", "CBD"],
      "yieldPotential": "high",
      "altitudeRange": "600-1200m"
    },
    "indexedAt": "2026-03-19T06:00:00Z"
  }
}
```

**Consumers:** `AgronomyEngine` (cập nhật variety knowledge cache)

---

#### `DiseaseAlertGenerated`
```json
{
  "eventType": "bazan.agronomy.disease.alert",
  "payload": {
    "alertId": "alert-uuid",
    "farmerId": "ObjectId",
    "farmId": "ObjectId",
    "disease": {
      "code": "leaf_rust",
      "name": "Bệnh gỉ sắt",
      "severity": "high",
      "confidenceScore": 0.87
    },
    "affectedArea_pct": 30,
    "detectionMethod": "image_cv",
    "recommendedActions": [
      {
        "priority": 1,
        "action": "Phun thuốc Carbendazim 50SC pha 20ml/10L",
        "within_hours": 48
      }
    ],
    "sourceMessageId": "msg-001",
    "generatedAt": "2026-03-19T07:32:00Z"
  }
}
```

**Consumers:** `NotificationService` (gửi cảnh báo Zalo/FCM), `ConversationService` (log)

---

#### `CoffeePriceUpdated`
```json
{
  "eventType": "bazan.market.price.updated",
  "payload": {
    "priceId": "price-uuid",
    "coffeeType": "Robusta",
    "grade": "Grade 2, 5% black & broken",
    "price_vnd_per_kg": 68500,
    "price_usd_per_mt": 2680,
    "change_pct": 5.2,
    "changeDirection": "up",
    "source": "ICE_Futures",
    "marketRegion": "Vietnam_Central_Highlands",
    "recordedAt": "2026-03-19T09:00:00Z",
    "shouldAlert": true
  }
}
```

**Consumers:** `NotificationService` (alert nếu change > 5%), `MES` (update history)

---

#### `SensorDataReceived`
```json
{
  "eventType": "bazan.iot.sensor.data",
  "payload": {
    "deviceId": "SENSOR_001_AN",
    "farmerId": "ObjectId",
    "farmId": "ObjectId",
    "readings": {
      "soilMoisture_pct": 38.5,
      "soilTemp_c": 26.8,
      "airTemp_c": 31.2,
      "soilPH": 6.1,
      "lux": 45000
    },
    "batteryLevel_pct": 78,
    "signalStrength_rssi": -67,
    "recordedAt": "2026-03-19T08:00:00Z"
  }
}
```

**Consumers:** `AgronomyEngine` (kiểm tra ngưỡng tưới), `IDS` (cập nhật lastSensorReadings)

---

#### `WeatherAlertTriggered`
```json
{
  "eventType": "bazan.weather.alert.triggered",
  "payload": {
    "alertId": "weather-alert-uuid",
    "alertType": "dry_spell",
    "severity": "medium",
    "affectedFarms": ["ObjectId-farm1", "ObjectId-farm2"],
    "affectedRegion": {
      "province": "Đắk Lắk",
      "district": "Cư M'gar"
    },
    "forecast": {
      "nextRainfall_days": 12,
      "expectedRainfall_mm": 8,
      "avgTemp_c": 31.5
    },
    "agronomicImpact": "Nguy cơ thiếu nước cao, khuyến nghị tưới bổ sung",
    "triggeredAt": "2026-03-19T06:00:00Z"
  }
}
```

**Consumers:** `AgronomyEngine`, `NotificationService`

---

## 8. Cấu trúc thư mục (Clean Architecture)

### 8.1 Solution Structure

```
BazanAI/
├── src/
│   ├── Services/
│   │   ├── BazanAI.Gateway/                    # API Gateway (YARP)
│   │   ├── BazanAI.Orchestrator/               # AI Agent Orchestrator
│   │   ├── BazanAI.Identity/                   # IDS
│   │   ├── BazanAI.Conversations/              # CMS
│   │   ├── BazanAI.Agronomy/                   # AES
│   │   ├── BazanAI.Market/                     # MES
│   │   ├── BazanAI.Notifications/              # NS
│   │   ├── BazanAI.IoTIngestion/               # IoT Service
│   │   ├── BazanAI.Weather/                    # Weather Service
│   │   ├── BazanAI.Media/                      # Media Service
│   │   └── BazanAI.Scheduler/                  # Job Scheduler
│   │
│   ├── SharedKernel/
│   │   ├── BazanAI.SharedKernel/               # Domain primitives
│   │   └── BazanAI.Infrastructure.Common/      # Shared infra
│   │
│   └── Python/
│       └── bazan-knowledge-rag/                # KRS (Python)
│
├── tests/
│   ├── Unit/
│   ├── Integration/
│   └── E2E/
│
├── infra/
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   ├── mongo-init/
│   ├── nginx/
│   └── monitoring/
│
├── docs/
│   ├── architecture/
│   │   ├── ADR-001-mongodb-primary-db.md
│   │   ├── ADR-002-qdrant-vector-db.md
│   │   └── ADR-003-rabbitmq-messaging.md
│   └── api/
│       └── openapi/
│
└── BazanAI.sln
```

---

### 8.2 Clean Architecture cho từng .NET Service

```
BazanAI.Agronomy/
├── BazanAI.Agronomy.Domain/
│   ├── Entities/
│   │   ├── Farm.cs
│   │   ├── CoffeeTree.cs
│   │   ├── DiseaseAlert.cs
│   │   └── CultivationSchedule.cs
│   ├── ValueObjects/
│   │   ├── GpsCoordinate.cs
│   │   ├── SoilProfile.cs
│   │   └── CoffeeVariety.cs
│   ├── Aggregates/
│   │   └── FarmAggregate.cs
│   ├── Events/
│   │   ├── DiseaseAlertGeneratedEvent.cs
│   │   └── HarvestWindowOpenedEvent.cs
│   ├── Repositories/
│   │   └── IFarmRepository.cs          # Interface only
│   └── Services/
│       └── IDiseaseDetectionService.cs
│
├── BazanAI.Agronomy.Application/
│   ├── Commands/
│   │   ├── DiagnoseFarm/
│   │   │   ├── DiagnoseFarmCommand.cs
│   │   │   ├── DiagnoseFarmCommandHandler.cs
│   │   │   └── DiagnoseFarmCommandValidator.cs
│   │   └── CreateIrrigationPlan/
│   ├── Queries/
│   │   ├── GetFarmSchedule/
│   │   │   ├── GetFarmScheduleQuery.cs
│   │   │   └── GetFarmScheduleQueryHandler.cs
│   │   └── GetActivePestAlerts/
│   ├── DTOs/
│   ├── Mappings/
│   └── EventHandlers/
│       └── WeatherAlertTriggeredHandler.cs
│
├── BazanAI.Agronomy.Infrastructure/
│   ├── Persistence/
│   │   ├── MongoDbContext.cs
│   │   ├── Repositories/
│   │   │   └── FarmRepository.cs       # Implementation
│   │   └── Configurations/
│   │       └── FarmDocumentConfig.cs
│   ├── Messaging/
│   │   ├── Publishers/
│   │   │   └── AgronomyEventPublisher.cs
│   │   └── Consumers/
│   │       └── WeatherAlertConsumer.cs
│   ├── ExternalServices/
│   │   └── ComputerVisionClient.cs
│   └── DependencyInjection.cs
│
└── BazanAI.Agronomy.API/
    ├── Controllers/
    │   └── AgronomyController.cs
    ├── Middleware/
    │   └── CorrelationIdMiddleware.cs
    ├── Program.cs
    ├── appsettings.json
    └── Dockerfile
```

---

### 8.3 Python RAG Service Structure

```
bazan-knowledge-rag/
├── app/
│   ├── api/
│   │   ├── routers/
│   │   │   ├── knowledge.py
│   │   │   └── health.py
│   │   └── dependencies.py
│   ├── core/
│   │   ├── config.py
│   │   ├── logging.py
│   │   └── exceptions.py
│   ├── domain/
│   │   ├── models/
│   │   │   ├── document.py
│   │   │   └── search_result.py
│   │   └── services/
│   │       └── hybrid_search_service.py
│   ├── infrastructure/
│   │   ├── qdrant/
│   │   │   ├── client.py
│   │   │   └── collections.py
│   │   ├── embeddings/
│   │   │   └── openai_embedder.py
│   │   └── document_loader/
│   │       ├── pdf_loader.py
│   │       └── chunker.py
│   └── main.py
├── tests/
├── requirements.txt
├── Dockerfile
└── pyproject.toml
```

---

## 9. Docker Compose — Full Stack

```yaml
version: "3.9"

networks:
  bazan-network:
    driver: bridge

volumes:
  mongo-data-1:
  mongo-data-2:
  mongo-data-3:
  qdrant-data:
  redis-data:
  minio-data:
  rabbitmq-data:
  prometheus-data:
  grafana-data:

services:

  # ─────────────────────────────────────────
  # INFRASTRUCTURE
  # ─────────────────────────────────────────

  mongo1:
    image: mongo:7.0
    container_name: bazan-mongo-1
    command: ["--replSet", "rs0", "--bind_ip_all", "--keyFile", "/etc/mongo-keyfile"]
    volumes:
      - mongo-data-1:/data/db
      - ./infra/mongo-init/keyfile:/etc/mongo-keyfile:ro
      - ./infra/mongo-init/init-replica.js:/docker-entrypoint-initdb.d/init-replica.js:ro
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_ROOT_USER}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_ROOT_PASSWORD}
    ports:
      - "27017:27017"
    networks:
      - bazan-network
    healthcheck:
      test: echo 'db.runCommand("ping").ok' | mongosh localhost:27017/test --quiet
      interval: 10s
      timeout: 10s
      retries: 5

  mongo2:
    image: mongo:7.0
    container_name: bazan-mongo-2
    command: ["--replSet", "rs0", "--bind_ip_all", "--keyFile", "/etc/mongo-keyfile"]
    volumes:
      - mongo-data-2:/data/db
      - ./infra/mongo-init/keyfile:/etc/mongo-keyfile:ro
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_ROOT_USER}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_ROOT_PASSWORD}
    networks:
      - bazan-network

  mongo3:
    image: mongo:7.0
    container_name: bazan-mongo-3
    command: ["--replSet", "rs0", "--bind_ip_all", "--keyFile", "/etc/mongo-keyfile"]
    volumes:
      - mongo-data-3:/data/db
      - ./infra/mongo-init/keyfile:/etc/mongo-keyfile:ro
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_ROOT_USER}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_ROOT_PASSWORD}
    networks:
      - bazan-network

  qdrant:
    image: qdrant/qdrant:v1.9.0
    container_name: bazan-qdrant
    volumes:
      - qdrant-data:/qdrant/storage
    ports:
      - "6333:6333"
      - "6334:6334"
    environment:
      QDRANT__SERVICE__API_KEY: ${QDRANT_API_KEY}
      QDRANT__LOG_LEVEL: INFO
    networks:
      - bazan-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:6333/healthz"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7.2-alpine
    container_name: bazan-redis
    command: redis-server --requirepass ${REDIS_PASSWORD} --maxmemory 512mb --maxmemory-policy allkeys-lru
    volumes:
      - redis-data:/data
    ports:
      - "6379:6379"
    networks:
      - bazan-network
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD}", "ping"]
      interval: 5s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    container_name: bazan-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASS}
      RABBITMQ_DEFAULT_VHOST: bazan
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
      - ./infra/rabbitmq/definitions.json:/etc/rabbitmq/definitions.json:ro
      - ./infra/rabbitmq/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro
    ports:
      - "5672:5672"
      - "15672:15672"
    networks:
      - bazan-network
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 10s
      timeout: 10s
      retries: 5

  minio:
    image: minio/minio:RELEASE.2024-03-15T01-07-19Z
    container_name: bazan-minio
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: ${MINIO_ACCESS_KEY}
      MINIO_ROOT_PASSWORD: ${MINIO_SECRET_KEY}
    volumes:
      - minio-data:/data
    ports:
      - "9000:9000"
      - "9001:9001"
    networks:
      - bazan-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
      interval: 10s
      timeout: 5s
      retries: 5

  # ─────────────────────────────────────────
  # OBSERVABILITY
  # ─────────────────────────────────────────

  seq:
    image: datalust/seq:2024.3
    container_name: bazan-seq
    environment:
      ACCEPT_EULA: Y
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_ADMIN_PASSWORD_HASH}
    volumes:
      - ./infra/seq-data:/data
    ports:
      - "5341:80"
    networks:
      - bazan-network

  prometheus:
    image: prom/prometheus:v2.51.2
    container_name: bazan-prometheus
    volumes:
      - ./infra/monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus
    ports:
      - "9090:9090"
    networks:
      - bazan-network

  grafana:
    image: grafana/grafana:10.3.5
    container_name: bazan-grafana
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_ADMIN_PASSWORD}
      GF_INSTALL_PLUGINS: grafana-clock-panel
    volumes:
      - grafana-data:/var/lib/grafana
      - ./infra/monitoring/grafana/dashboards:/etc/grafana/provisioning/dashboards:ro
    ports:
      - "3000:3000"
    depends_on:
      - prometheus
    networks:
      - bazan-network

  # ─────────────────────────────────────────
  # APPLICATION SERVICES
  # ─────────────────────────────────────────

  api-gateway:
    build:
      context: ./src/Services/BazanAI.Gateway
      dockerfile: Dockerfile
    container_name: bazan-gateway
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      JwtSettings__SecretKey: ${JWT_SECRET}
      ReverseProxy__Clusters__identity-cluster__Destinations__default__Address: http://identity-svc:5001
      ReverseProxy__Clusters__agronomy-cluster__Destinations__default__Address: http://agronomy-svc:5004
      ReverseProxy__Clusters__market-cluster__Destinations__default__Address: http://market-svc:5005
      ReverseProxy__Clusters__conversation-cluster__Destinations__default__Address: http://conversation-svc:5002
      Seq__ServerUrl: http://seq:80
    ports:
      - "8080:8080"
    depends_on:
      - identity-svc
      - agronomy-svc
    networks:
      - bazan-network

  ai-orchestrator:
    build:
      context: ./src/Services/BazanAI.Orchestrator
      dockerfile: Dockerfile
    container_name: bazan-orchestrator
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      OpenAI__ApiKey: ${OPENAI_API_KEY}
      OpenAI__Model: gpt-4o
      Services__AgronomyUrl: http://agronomy-svc:5004
      Services__KnowledgeRagUrl: http://knowledge-rag:5003
      Services__MarketUrl: http://market-svc:5005
      Services__WeatherUrl: http://weather-svc:5008
      Redis__ConnectionString: redis:6379,password=${REDIS_PASSWORD}
      RabbitMQ__Host: rabbitmq
      Seq__ServerUrl: http://seq:80
    ports:
      - "5010:5010"
    depends_on:
      rabbitmq:
        condition: service_healthy
      redis:
        condition: service_healthy
    networks:
      - bazan-network

  identity-svc:
    build:
      context: ./src/Services/BazanAI.Identity
      dockerfile: Dockerfile
    container_name: bazan-identity-svc
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      MongoDB__ConnectionString: mongodb://${MONGO_ROOT_USER}:${MONGO_ROOT_PASSWORD}@mongo1:27017,mongo2:27017,mongo3:27017/?replicaSet=rs0&authSource=admin
      MongoDB__Database: bazan_identity
      JwtSettings__SecretKey: ${JWT_SECRET}
      JwtSettings__Issuer: bazan-ai
      JwtSettings__ExpiryHours: 24
      RabbitMQ__Host: rabbitmq
      RabbitMQ__VirtualHost: bazan
      Seq__ServerUrl: http://seq:80
    ports:
      - "5001:5001"
    depends_on:
      mongo1:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    networks:
      - bazan-network

  conversation-svc:
    build:
      context: ./src/Services/BazanAI.Conversations
      dockerfile: Dockerfile
    container_name: bazan-conversation-svc
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      MongoDB__ConnectionString: mongodb://${MONGO_ROOT_USER}:${MONGO_ROOT_PASSWORD}@mongo1:27017,mongo2:27017,mongo3:27017/?replicaSet=rs0&authSource=admin
      MongoDB__Database: bazan_conversations
      RabbitMQ__Host: rabbitmq
      Redis__ConnectionString: redis:6379,password=${REDIS_PASSWORD}
      Seq__ServerUrl: http://seq:80
    ports:
      - "5002:5002"
    depends_on:
      mongo1:
        condition: service_healthy
    networks:
      - bazan-network

  knowledge-rag:
    build:
      context: ./src/Python/bazan-knowledge-rag
      dockerfile: Dockerfile
    container_name: bazan-knowledge-rag
    environment:
      QDRANT_URL: http://qdrant:6333
      QDRANT_API_KEY: ${QDRANT_API_KEY}
      OPENAI_API_KEY: ${OPENAI_API_KEY}
      EMBEDDING_MODEL: text-embedding-3-large
      MINIO_ENDPOINT: minio:9000
      MINIO_ACCESS_KEY: ${MINIO_ACCESS_KEY}
      MINIO_SECRET_KEY: ${MINIO_SECRET_KEY}
      LOG_LEVEL: INFO
    ports:
      - "5003:5003"
    depends_on:
      qdrant:
        condition: service_healthy
    networks:
      - bazan-network

  agronomy-svc:
    build:
      context: ./src/Services/BazanAI.Agronomy
      dockerfile: Dockerfile
    container_name: bazan-agronomy-svc
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      MongoDB__ConnectionString: mongodb://${MONGO_ROOT_USER}:${MONGO_ROOT_PASSWORD}@mongo1:27017,mongo2:27017,mongo3:27017/?replicaSet=rs0&authSource=admin
      MongoDB__Database: bazan_agronomy
      RabbitMQ__Host: rabbitmq
      ComputerVisionApi__Endpoint: ${CV_API_ENDPOINT}
      ComputerVisionApi__ApiKey: ${CV_API_KEY}
      Seq__ServerUrl: http://seq:80
    ports:
      - "5004:5004"
    depends_on:
      mongo1:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    networks:
      - bazan-network

  market-svc:
    build:
      context: ./src/Services/BazanAI.Market
      dockerfile: Dockerfile
    container_name: bazan-market-svc
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      MongoDB__ConnectionString: mongodb://${MONGO_ROOT_USER}:${MONGO_ROOT_PASSWORD}@mongo1:27017,mongo2:27017,mongo3:27017/?replicaSet=rs0&authSource=admin
      MongoDB__Database: bazan_market
      RabbitMQ__Host: rabbitmq
      ICEFutures__ApiKey: ${ICE_FUTURES_API_KEY}
      Seq__ServerUrl: http://seq:80
    ports:
      - "5005:5005"
    networks:
      - bazan-network

  notification-svc:
    build:
      context: ./src/Services/BazanAI.Notifications
      dockerfile: Dockerfile
    container_name: bazan-notification-svc
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      RabbitMQ__Host: rabbitmq
      Zalo__OAAccessToken: ${ZALO_OA_ACCESS_TOKEN}
      FCM__ServerKey: ${FCM_SERVER_KEY}
      Twilio__AccountSid: ${TWILIO_ACCOUNT_SID}
      Twilio__AuthToken: ${TWILIO_AUTH_TOKEN}
      Twilio__FromNumber: ${TWILIO_FROM_NUMBER}
      Seq__ServerUrl: http://seq:80
    ports:
      - "5006:5006"
    depends_on:
      rabbitmq:
        condition: service_healthy
    networks:
      - bazan-network

  iot-ingestion-svc:
    build:
      context: ./src/Services/BazanAI.IoTIngestion
      dockerfile: Dockerfile
    container_name: bazan-iot-ingestion-svc
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      MQTT__BrokerHost: "0.0.0.0"
      MQTT__BrokerPort: 1883
      MongoDB__ConnectionString: mongodb://${MONGO_ROOT_USER}:${MONGO_ROOT_PASSWORD}@mongo1:27017,mongo2:27017,mongo3:27017/?replicaSet=rs0&authSource=admin
      RabbitMQ__Host: rabbitmq
      Seq__ServerUrl: http://seq:80
    ports:
      - "5007:5007"
      - "1883:1883"
    networks:
      - bazan-network

  weather-svc:
    build:
      context: ./src/Services/BazanAI.Weather
      dockerfile: Dockerfile
    container_name: bazan-weather-svc
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      OpenWeatherMap__ApiKey: ${OPENWEATHER_API_KEY}
      MongoDB__ConnectionString: mongodb://${MONGO_ROOT_USER}:${MONGO_ROOT_PASSWORD}@mongo1:27017,mongo2:27017,mongo3:27017/?replicaSet=rs0&authSource=admin
      RabbitMQ__Host: rabbitmq
      Redis__ConnectionString: redis:6379,password=${REDIS_PASSWORD}
      Seq__ServerUrl: http://seq:80
    ports:
      - "5008:5008"
    networks:
      - bazan-network

  media-svc:
    build:
      context: ./src/Services/BazanAI.Media
      dockerfile: Dockerfile
    container_name: bazan-media-svc
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      MinIO__Endpoint: minio:9000
      MinIO__AccessKey: ${MINIO_ACCESS_KEY}
      MinIO__SecretKey: ${MINIO_SECRET_KEY}
      RabbitMQ__Host: rabbitmq
      Seq__ServerUrl: http://seq:80
    ports:
      - "5009:5009"
    depends_on:
      minio:
        condition: service_healthy
    networks:
      - bazan-network
```

---

## 10. Bảo mật & Xác thực

### 10.1 Authentication Flow

```
1. Farmer → POST /api/v1/auth/login (phone + OTP)
2. Identity Service → validate OTP → issue JWT (access 24h) + Refresh Token (30d)
3. Client → lưu token vào secure storage
4. Client → mọi request kèm: Authorization: Bearer {access_token}
5. API Gateway → validate JWT signature → forward claims đến upstream service
6. Service → đọc claims từ header (X-Farmer-Id, X-Farm-Ids, X-Role)
```

### 10.2 JWT Payload

```json
{
  "sub": "farmer_objectid",
  "name": "Nguyễn Văn An",
  "phone": "+84901234567",
  "role": "farmer",
  "farm_ids": ["farm_id_1", "farm_id_2"],
  "province": "Dak Lak",
  "iat": 1710835200,
  "exp": 1710921600,
  "iss": "bazan-ai",
  "aud": "bazan-ai-services"
}
```

### 10.3 Service-to-Service Auth

Các service trong internal network giao tiếp qua **mTLS** hoặc API Key (đơn giản hơn cho giai đoạn đầu). API Key được truyền qua header `X-Service-Api-Key` và validate tại mỗi service.

### 10.4 Data Security

- **At-rest:** MongoDB encryption at rest, MinIO SSE-S3
- **In-transit:** TLS 1.3 cho mọi kết nối external, TLS 1.2+ cho internal
- **PII:** Số điện thoại, GPS coordinates mã hóa field-level trong MongoDB
- **Secrets:** Docker Secrets hoặc HashiCorp Vault (production)

---

## 11. Observability Stack

### 11.1 Logging — Structured Logs với Serilog + Seq

```csharp
// appsettings.json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://seq:80" }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithCorrelationId"],
    "Properties": {
      "Application": "bazan-agronomy-svc",
      "Environment": "Production"
    }
  }
}
```

**Log Schema chuẩn:**
```json
{
  "timestamp": "2026-03-19T08:00:00.000Z",
  "level": "Information",
  "application": "bazan-agronomy-svc",
  "correlationId": "req-uuid",
  "farmerId": "ObjectId",
  "sessionId": "sess_xxx",
  "message": "Disease diagnosis completed",
  "disease": "leaf_rust",
  "confidence": 0.87,
  "duration_ms": 1234
}
```

### 11.2 Metrics — Prometheus + Grafana

**Metrics cần thu thập:**

| Metric | Type | Mô tả |
|--------|------|-------|
| `bazan_api_requests_total` | Counter | Tổng số request theo service, status |
| `bazan_api_latency_ms` | Histogram | Độ trễ P50/P95/P99 |
| `bazan_ai_token_usage_total` | Counter | Token consumed theo model |
| `bazan_rag_search_duration_ms` | Histogram | Thời gian Qdrant search |
| `bazan_rabbitmq_messages_total` | Counter | Messages theo queue, status |
| `bazan_active_sessions` | Gauge | Số session đang active |
| `bazan_sensor_readings_total` | Counter | Số đọc sensor theo device |

### 11.3 Distributed Tracing — OpenTelemetry

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMongoDBInstrumentation()
        .SetSampler(new TraceIdRatioBasedSampler(0.1)) // 10% sampling
        .AddOtlpExporter(opt => opt.Endpoint = new Uri("http://otel-collector:4317")));
```

---

## 12. Architecture Decision Records (ADR)

### ADR-001: MongoDB làm Primary Database

**Trạng thái:** Accepted  
**Ngày:** 2026-01-15  

**Bối cảnh:** Dữ liệu nông dân có cấu trúc phi tuyến tính cao: mỗi nông dân có số lượng vườn khác nhau, mỗi vườn có số loại cảm biến khác nhau, lịch sử canh tác thay đổi theo từng mùa. Dữ liệu metadata từ AI hành vi người dùng cũng rất đa dạng.

**Quyết định:** Dùng MongoDB Replica Set (3 nodes) làm primary store.

**Lý do:**
- Schema-less phù hợp cho dữ liệu nông nghiệp không đồng nhất
- Native geospatial indexing cho tìm kiếm theo GPS
- Replica Set đảm bảo HA và đọc từ secondary để scale read
- Change Streams để trigger event khi data thay đổi

**Trade-offs:**
- Không có ACID transaction đa collection (workaround: saga pattern)
- Join phức tạp hơn SQL (workaround: denormalize hợp lý)

---

### ADR-002: Qdrant làm Vector Database

**Trạng thái:** Accepted  
**Ngày:** 2026-01-15  

**Bối cảnh:** RAG system cần tìm kiếm tài liệu kỹ thuật với độ chính xác cao, kết hợp cả semantic search (dense vector) và keyword search (sparse vector).

**Quyết định:** Dùng Qdrant v1.9+ với Hybrid Search (BM42 + dense embedding).

**Lý do:**
- Hybrid Search native (không cần Elasticsearch riêng)
- Payload filter giúp search theo giống cây, vùng đất trực tiếp trong vector DB
- Self-hosted, không phụ thuộc cloud vendor
- gRPC interface cho low-latency retrieval

**Trade-offs:**
- Ít ecosystem hơn Pinecone/Weaviate
- Cần tự quản lý infrastructure

---

### ADR-003: RabbitMQ cho Event-Driven Communication

**Trạng thái:** Accepted  
**Ngày:** 2026-01-20  

**Bối cảnh:** Các service cần giao tiếp async để giảm coupling và xử lý các event như cảnh báo sâu bệnh, cập nhật giá cà phê mà không ảnh hưởng đến latency của request chính.

**Quyết định:** Dùng RabbitMQ với Topic Exchange và Dead Letter Queue.

**Lý do:**
- Mature, battle-tested, .NET client tốt (MassTransit)
- Topic Exchange linh hoạt cho routing event phức tạp
- Persistent messages đảm bảo không mất event khi service restart
- Management UI built-in để debug

**Trade-offs:**
- Không phải là distributed log như Kafka (không replay message cũ)
- Scale horizontal phức tạp hơn Kafka (dùng Queue Federation nếu cần)
- Chấp nhận được cho giai đoạn MVP (<10.000 farmers)

---

### ADR-004: Semantic Kernel làm AI Orchestration Framework

**Trạng thái:** Accepted  
**Ngày:** 2026-02-01  

**Bối cảnh:** Hệ thống cần orchestrate nhiều AI tools (RAG, agronomy engine, market data) để trả lời câu hỏi phức tạp của nông dân theo kiểu multi-step reasoning.

**Quyết định:** Dùng Microsoft Semantic Kernel với Planner.

**Lý do:**
- Native .NET/C# — consistent với toàn bộ backend stack
- Function Calling abstraction chuẩn hóa tool definitions
- Built-in memory management và context window handling
- Dễ swap LLM provider (OpenAI → Azure OpenAI → local model)

**Trade-offs:**
- Còn evolving nhanh, API có thể breaking change
- Ít documentation hơn LangChain/LangGraph

---

## 13. Lộ trình phát triển (Roadmap)

### Phase 1 — MVP (Tháng 1-3)

**Mục tiêu:** 100 nông dân thử nghiệm tại Đắk Lắk

- Identity Service + Conversation Service + Knowledge RAG
- API Gateway cơ bản
- Mobile App (Flutter) — chat interface
- Docker Compose deployment trên single server
- Zalo Bot integration

---

### Phase 2 — Core AI (Tháng 4-6)

**Mục tiêu:** 1.000 nông dân, AI recommendations hoạt động

- AI Agent Orchestrator + Semantic Kernel
- Agronomy Engine — 6 yếu tố
- Market & Economic Service
- Notification Service (Zalo + FCM)
- Weather Integration
- MongoDB Replica Set production

---

### Phase 3 — IoT & Analytics (Tháng 7-9)

**Mục tiêu:** 5.000 nông dân, IoT integration

- IoT Ingestion Service + MQTT Broker
- Media Service + MinIO
- Scheduler Service (Hangfire)
- Observability Stack (Prometheus + Grafana + Seq)
- RLHF Pipeline từ feedback data
- Computer Vision cho disease detection

---

### Phase 4 — Scale & Enterprise (Tháng 10-12)

**Mục tiêu:** 10.000+ nông dân, HTX integration

- Kubernetes migration (Helm charts)
- Multi-region deployment
- Fine-tuned Vietnamese agriculture LLM
- Analytics dashboard cho HTX
- Offline-first mobile (sync khi có mạng)
- Certification: ISO 27001

---

## 14. Phân tích Gap — Các thành phần cần bổ sung

Bảng dưới đây tổng hợp toàn bộ khoảng cách giữa kiến trúc ban đầu và kiến trúc đầy đủ:

| # | Thành phần | Mức độ ưu tiên | Phase | Lý do cần thiết |
|---|-----------|--------------|-------|----------------|
| 1 | **API Gateway (YARP)** | 🔴 Critical | Phase 1 | Single entry point, auth, rate limiting |
| 2 | **AI Agent Orchestrator** | 🔴 Critical | Phase 2 | Multi-step reasoning, tool coordination |
| 3 | **Redis Cache** | 🔴 Critical | Phase 1 | Session cache, rate limiting, price cache |
| 4 | **Notification Service** | 🟠 High | Phase 2 | Zalo/FCM alerts cho nông dân |
| 5 | **Weather Integration** | 🟠 High | Phase 2 | Input quan trọng cho Agronomy Engine |
| 6 | **MinIO Object Storage** | 🟠 High | Phase 2 | Lưu PDF cho RAG, ảnh vườn |
| 7 | **Observability Stack** | 🟠 High | Phase 2 | Không thể vận hành production không có monitoring |
| 8 | **IoT Ingestion Service** | 🟡 Medium | Phase 3 | Pipeline cho sensor data thực tế |
| 9 | **Media Service** | 🟡 Medium | Phase 3 | Upload ảnh bệnh, PDF reports |
| 10 | **Scheduler Service** | 🟡 Medium | Phase 3 | Daily price sync, periodic reports |
| 11 | **RLHF Pipeline** | 🟢 Low | Phase 4 | Fine-tuning từ feedback — long-term |
| 12 | **Computer Vision Service** | 🟢 Low | Phase 3 | Disease detection từ ảnh — specialized |

---

*Tài liệu này được xây dựng theo tiêu chuẩn IBM Architecture Framework và IBM Cloud Garage Method. Mọi thay đổi kiến trúc cần được ghi lại qua ADR process.*

---

**© 2026 Bazan AI Project — Confidential**

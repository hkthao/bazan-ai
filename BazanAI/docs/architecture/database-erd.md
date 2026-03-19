# Database ERD — Bazan AI
## Thiết kế toàn bộ MongoDB Collections

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Principal Software Architect · Backend Lead |
| **Liên quan** | ADR-001 (MongoDB), System Architecture Document |

> **Lưu ý:** MongoDB là document database, không có foreign key như SQL. Các mối quan hệ dưới đây thể hiện **tham chiếu logic** (reference by ObjectId) và **embedding** (document lồng nhau).

---

## 1. Sơ đồ quan hệ tổng thể

```
┌─────────────────────────────────────────────────────────────────┐
│                    bazan_identity                                │
│                                                                  │
│  ┌──────────────┐         ┌──────────────────────────────────┐  │
│  │   farmers    │ 1 ── * │           farms                   │  │
│  │──────────────│         │──────────────────────────────────│  │
│  │ _id (ObjId)  │◄────────│ _id (ObjId)                      │  │
│  │ phoneNumber  │         │ farmerId (ref → farmers._id)      │  │
│  │ fullName     │         │ name                             │  │
│  │ zaloId       │         │ totalArea_ha                     │  │
│  │ location {}  │         │ numberOfTrees                    │  │
│  │ createdAt    │         │ coffeeVarieties []               │  │
│  └──────────────┘         │ soilType                         │  │
│         │                 │ location {} (2dsphere)           │  │
│         │                 │ altitude_m                       │  │
│         │                 │ irrigationSystem                 │  │
│         │                 │ certifications []                │  │
│         │                 │ intercropTypes []                │  │
│         │                 └──────────────────────────────────┘  │
│         │                           │                           │
│         │                           │ embedded                  │
│         │                 ┌──────────────────────────────────┐  │
│         │                 │      sensor_devices (embedded)   │  │
│         │                 │──────────────────────────────────│  │
│         │                 │ deviceId, type, installLocation  │  │
│         │                 │ installedAt, lastHeartbeat       │  │
│         │                 └──────────────────────────────────┘  │
│         │                                                        │
│  ┌──────┴──────────────────────────────────────────────────┐    │
│  │                 user_contexts                            │    │
│  │─────────────────────────────────────────────────────────│    │
│  │ _id (ObjId)                                             │    │
│  │ farmerId (ref → farmers._id)                            │    │
│  │ farmContext {} (embedded snapshot)                      │    │
│  │ sensorMetadata {} (embedded)                            │    │
│  │ behaviorProfile {} (embedded)                           │    │
│  │ aiPersonalization {} (embedded)                         │    │
│  │ version, schemaVersion, updatedAt                       │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                  bazan_conversations                             │
│                                                                  │
│  ┌──────────────────────┐      ┌────────────────────────────┐   │
│  │    chat_sessions     │ 1─*  │       chat_messages        │   │
│  │──────────────────────│      │────────────────────────────│   │
│  │ _id (ObjId)          │◄─────│ _id (ObjId)                │   │
│  │ sessionId (string)   │      │ sessionId (ref)            │   │
│  │ farmerId (ref)       │      │ farmerId (ref)             │   │
│  │ startedAt, endedAt   │      │ role (user/assistant)      │   │
│  │ channel              │      │ content (string)           │   │
│  │ contextSnapshot {}   │      │ contentType                │   │
│  │ summary (string)     │      │ attachments [] (embedded)  │   │
│  │ summaryEmbedding []  │      │   - type, url              │   │
│  │ messageCount         │      │   - analysisResult {}      │   │
│  │ tokenUsage {}        │      │ contextUsed {}             │   │
│  │ satisfactionRating   │      │   - ragDocuments []        │   │
│  │ usedForRLHF          │      │   - sensorDataUsed         │   │
│  └──────────────────────┘      │ feedback {} (embedded)     │   │
│                                │   - rating (1-5)           │   │
│                                │   - thumbs (up/down)       │   │
│                                │   - comment                │   │
│                                │   - usedForTraining        │   │
│                                │ latency_ms, timestamp      │   │
│                                └────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    bazan_agronomy                                │
│                                                                  │
│  ┌────────────────────┐   ┌─────────────────────────────────┐   │
│  │   disease_alerts   │   │      irrigation_plans           │   │
│  │────────────────────│   │─────────────────────────────────│   │
│  │ _id (ObjId)        │   │ _id (ObjId)                     │   │
│  │ farmerId (ref)     │   │ farmId (ref → farms._id)        │   │
│  │ farmId (ref)       │   │ scheduledDate                   │   │
│  │ diseaseCode        │   │ waterVolume_L                   │   │
│  │ diseaseName        │   │ method                          │   │
│  │ severity (enum)    │   │ weatherCondition                │   │
│  │ confidenceScore    │   │ status (pending/done/skipped)   │   │
│  │ affectedArea_pct   │   │ createdAt                       │   │
│  │ detectionMethod    │   └─────────────────────────────────┘   │
│  │ recommendedActions │                                          │
│  │   [] (embedded)    │   ┌─────────────────────────────────┐   │
│  │ isResolved         │   │    cultivation_schedules        │   │
│  │ resolvedAt (TTL)   │   │─────────────────────────────────│   │
│  │ generatedAt        │   │ _id (ObjId)                     │   │
│  └────────────────────┘   │ farmId (ref)                    │   │
│                           │ season (string)                 │   │
│  ┌────────────────────┐   │ activities [] (embedded)        │   │
│  │   soil_analyses    │   │   - activityType, scheduledDate │   │
│  │────────────────────│   │   - inputs [], notes            │   │
│  │ _id (ObjId)        │   │ createdAt                       │   │
│  │ farmId (ref)       │   └─────────────────────────────────┘   │
│  │ analysisDate       │                                          │
│  │ ph, nitrogen_ppm   │                                          │
│  │ phosphorus_ppm     │                                          │
│  │ potassium_ppm      │                                          │
│  │ organicMatter_pct  │                                          │
│  │ recommendations [] │                                          │
│  └────────────────────┘                                          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     bazan_market                                 │
│                                                                  │
│  ┌────────────────────┐    ┌──────────────────────────────────┐  │
│  │   coffee_prices    │    │       roi_calculations           │  │
│  │────────────────────│    │──────────────────────────────────│  │
│  │ _id (ObjId)        │    │ _id (ObjId)                      │  │
│  │ coffeeType (enum)  │    │ farmerId (ref)                   │  │
│  │ grade (string)     │    │ farmId (ref)                     │  │
│  │ price_vnd_per_kg   │    │ season (string)                  │  │
│  │ price_usd_per_mt   │    │ revenue_vnd                      │  │
│  │ change_pct         │    │ inputCosts {} (embedded)         │  │
│  │ changeDirection    │    │   - fertilizer, pesticide        │  │
│  │ source (string)    │    │   - labor, irrigation            │  │
│  │ marketRegion       │    │ netProfit_vnd                    │  │
│  │ shouldAlert        │    │ roi_pct                          │  │
│  │ recordedAt         │    │ breakevenPrice_vnd               │  │
│  └────────────────────┘    │ calculatedAt                     │  │
│                            └──────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                   coffee_buyers                            │  │
│  │────────────────────────────────────────────────────────────│  │
│  │ _id (ObjId)                                               │  │
│  │ companyName, contactPhone                                  │  │
│  │ location {} (2dsphere — tìm đại lý gần nhất)             │  │
│  │ currentBuyingPrice_vnd, grade                             │  │
│  │ lastUpdated                                               │  │
│  └────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                   bazan_iot                                      │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  sensor_readings                         │    │
│  │─────────────────────────────────────────────────────────│    │
│  │ _id (ObjId)                                             │    │
│  │ deviceId (string)                                       │    │
│  │ farmerId (ref)                                          │    │
│  │ farmId (ref)                                            │    │
│  │ readings {} (embedded — flexible per sensor type)       │    │
│  │   soilMoisture_pct, soilTemp_c, airTemp_c               │    │
│  │   soilPH, lux, rainfall_mm                              │    │
│  │ batteryLevel_pct                                        │    │
│  │ signalStrength_rssi                                     │    │
│  │ recordedAt (TTL index — xóa sau 90 ngày)               │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                  bazan_knowledge                                  │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │               indexed_documents                          │    │
│  │─────────────────────────────────────────────────────────│    │
│  │ _id (ObjId)                                             │    │
│  │ docId (string — dùng để link với Qdrant point id)      │    │
│  │ minioPath (string)                                      │    │
│  │ collection (string — Qdrant collection name)            │    │
│  │ contentHash (string — detect thay đổi)                  │    │
│  │ totalChunks, totalTokens                                │    │
│  │ metadata {} (embedded — copy từ Qdrant payload)         │    │
│  │   coffeeVarieties [], topics [], region []              │    │
│  │   language, documentType, sourceYear                    │    │
│  │ embeddingModel, embeddingVersion                        │    │
│  │ status (indexing/indexed/failed)                        │    │
│  │ errorMessage (nullable)                                 │    │
│  │ indexedAt, updatedAt                                    │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Index Strategy tổng hợp

| Collection | Index | Type | Lý do |
|-----------|-------|------|-------|
| farmers | `location.coordinates` | 2dsphere | Tìm vườn gần GPS |
| farmers | `phoneNumber` | Unique | Login, không trùng |
| farmers | `farmContext.soilType + coffeeVarieties` | Compound | RAG filter |
| user_contexts | `farmerId` | Unique | 1 farmer → 1 context |
| chat_sessions | `farmerId + startedAt` (desc) | Compound | Lịch sử session |
| chat_messages | `sessionId + timestamp` | Compound | Load messages theo session |
| chat_messages | `farmerId + feedback.rating` | Sparse | RLHF export |
| disease_alerts | `isResolved + diseaseCode + severity` | Compound | Active alerts |
| disease_alerts | `resolvedAt` | TTL (90 ngày) | Auto cleanup |
| coffee_prices | `coffeeType + recordedAt` (desc) | Compound | Price history |
| coffee_buyers | `location` | 2dsphere | Đại lý gần nhất |
| sensor_readings | `deviceId + recordedAt` | Compound | Latest readings |
| sensor_readings | `recordedAt` | TTL (90 ngày) | Auto cleanup raw data |
| indexed_documents | `docId` | Unique | Idempotent indexing |
| indexed_documents | `minioPath` | Unique | Link với MinIO |

---

## 3. Embedding vs Reference — Quyết định thiết kế

| Trường hợp | Pattern | Lý do |
|-----------|---------|-------|
| Farm trong Farmer | **Reference** (farmId trong Farm) | Farmer có nhiều Farm, không biết trước số lượng |
| Sensor devices trong Farm | **Embedding** | Số lượng nhỏ (<10), luôn đọc cùng Farm |
| Messages trong Session | **Reference** (sessionId trong Message) | Có thể rất nhiều messages, cần paginate |
| Feedback trong Message | **Embedding** | 1:1, đọc cùng Message |
| Attachments trong Message | **Embedding** | Số lượng nhỏ (<5 per message) |
| RecommendedActions trong DiseaseAlert | **Embedding** | Số lượng cố định (<5), luôn đọc cùng Alert |
| InputCosts trong RoiCalculation | **Embedding** | Dict nhỏ, schema cố định |

---

*Tài liệu cập nhật khi thêm collection mới hoặc thay đổi schema version.*

**© 2026 Bazan AI Project — Confidential**

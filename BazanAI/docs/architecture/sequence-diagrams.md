# Sequence Diagrams — Bazan AI
## AI Chat Flow · IoT Sensor Pipeline · Disease Detection Flow

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Principal Software Architect |
| **Liên quan** | System Architecture Document, DFD, RAG Pipeline Design |

---

## 1. Sequence Diagram — AI Chat Flow (End-to-End)

Mô tả luồng đầy đủ từ lúc nông dân gửi câu hỏi đến khi nhận câu trả lời streaming.

```
Farmer     Gateway    Orchestrator   IntentClassifier   RAG Service    AgronomyService   MarketService   OpenAI   ConvService
  │           │              │               │               │               │               │              │          │
  │──msg──►   │              │               │               │               │               │              │          │
  │           │              │               │               │               │               │              │          │
  │           ├─validate─►   │               │               │               │               │              │          │
  │           │  JWT          │               │               │               │               │              │          │
  │           │◄─farmer ctx─ │               │               │               │               │              │          │
  │           │              │               │               │               │               │              │          │
  │           ├──route──►    │               │               │               │               │              │          │
  │           │    WebSocket  │               │               │               │               │              │          │
  │           │              ├─classify──►   │               │               │               │              │          │
  │           │              │               │               │               │               │              │          │
  │           │              │◄──intent──    │               │               │               │              │          │
  │           │              │  disease+market               │               │               │              │          │
  │           │              │               │               │               │               │              │          │
  │           │              ├──[parallel]──────────────────────────────────────────────────►│              │          │
  │           │              │              RAG search       │               │               │              │          │
  │           │              ├──search──────────────────►    │               │               │              │          │
  │           │              │                               │               │               │              │          │
  │           │              ├──diagnose─────────────────────────────────►   │               │              │          │
  │           │              │                               │               │               │              │          │
  │           │              │◄──chunks (top5)───────────────│               │               │              │          │
  │           │              │◄──diagnosis result────────────────────────────│               │              │          │
  │           │              │◄──price + ROI summary─────────────────────────────────────────│              │          │
  │           │              │               │               │               │               │              │          │
  │           │              ├──build prompt──────────────────────────────────────────────────────────────► │          │
  │           │              │   [system + context + retrieved + history]    │               │              │          │
  │           │              │                               │               │               │              │          │
  │           │              │◄═══════════════════ stream tokens ════════════════════════════│              │          │
  │◄─token─   │◄─token────   │                               │               │               │              │          │
  │◄─token─   │◄─token────   │  (SignalR push per token)     │               │               │              │          │
  │◄─token─   │◄─token────   │                               │               │               │              │          │
  │           │              │                               │               │               │              │          │
  │           │              ├─save message──────────────────────────────────────────────────────────────────────────►  │
  │           │              │   [full response + citations + context_used]  │               │              │          │
  │           │              │                               │               │               │              │          │
  │◄─StreamComplete─          │               │               │               │               │              │          │
  │           │              │               │               │               │               │              │          │

Timeline tổng: ~1,800ms
  - JWT validation:        10ms
  - Intent classification: 50ms
  - Parallel calls:        max(290ms RAG, 450ms Agronomy, 180ms Market) = 450ms
  - LLM streaming:         600ms (first token ~200ms, streaming ~400ms)
  - Save message:          async, không block response
  - SignalR overhead:      ~10ms per batch
```

### 1.1 Happy Path — timing breakdown

| Bước | Component | Duration | Note |
|------|-----------|---------|------|
| JWT validate | Gateway | 10ms | Redis cache |
| Farmer context load | Identity Service | 15ms | MongoDB indexed |
| Intent classification | OpenAI mini | 50ms | Short prompt |
| Query expansion | Orchestrator | 5ms | In-memory |
| Embedding generation | OpenAI API | 80ms | Cache hit → 5ms |
| Qdrant hybrid search | RAG Service | 150ms | Pre-filtered |
| Re-ranking | RAG Service | 80ms | LLM mini call |
| Agronomy diagnosis | Agronomy Service | 450ms | CV model call |
| Market summary | Market Service | 30ms | Redis cache |
| LLM first token | OpenAI GPT-4o | 200ms | TTFT |
| LLM stream complete | OpenAI GPT-4o | 600ms | ~300 tokens |
| **Total P50** | | **~1,200ms** | |
| **Total P95** | | **~2,800ms** | |

### 1.2 Fallback — Khi RAG không tìm được context

```
Orchestrator → RAG Service → Qdrant (no results / low score)
                           ↓
                    Return empty context
                           ↓
Orchestrator → Build prompt with:
               "Tôi chưa tìm được tài liệu liên quan. 
                Hãy liên hệ khuyến nông {district}."
                           ↓
Orchestrator → OpenAI (short prompt, fast response)
             → Stream about:
               1. Xác nhận không có đủ thông tin
               2. Đề xuất nguồn tham khảo offline
```

---

## 2. Sequence Diagram — IoT Sensor Pipeline

Luồng từ cảm biến gửi data đến khi được lưu và trigger action.

```
IoT Sensor  MQTT Broker  IoTIngestionSvc  RabbitMQ   AgronomySvc   IdentitySvc   NotificationSvc
    │            │               │            │            │              │               │
    │──PUBLISH──►│               │            │            │              │               │
    │  topic:    │               │            │            │              │               │
    │  bazan/    │               │            │            │              │               │
    │  {farmId}/ │               │            │            │              │               │
    │  soil_moisture             │            │            │              │               │
    │            │               │            │            │              │               │
    │            ├──deliver──►   │            │            │              │               │
    │            │  raw payload  │            │            │              │               │
    │            │               │            │            │              │               │
    │            │               ├─validate─  │            │              │               │
    │            │               │ deviceId   │            │              │               │
    │            │               │ signature  │            │              │               │
    │            │               │            │            │              │               │
    │            │               ├─normalize─ │            │              │               │
    │            │               │ ADC→%      │            │              │               │
    │            │               │            │            │              │               │
    │            │               ├─save──────►│            │              │               │
    │            │               │  MongoDB   │            │              │               │
    │            │               │  (bazan_iot)            │              │               │
    │            │               │            │            │              │               │
    │            │               ├─publish──► │            │              │               │
    │            │               │  SensorData│            │              │               │
    │            │               │  Received  │            │              │               │
    │            │               │  event     │            │              │               │
    │            │               │            │            │              │               │
    │            │               │            ├─deliver──► │              │               │
    │            │               │            │  (agronomy │              │               │
    │            │               │            │   queue)   │              │               │
    │            │               │            │            │              │               │
    │            │               │            │            ├─check─       │               │
    │            │               │            │            │ threshold    │               │
    │            │               │            │            │ moisture<30% │               │
    │            │               │            │            │              │               │
    │            │               │            ├─deliver──────────────►    │               │
    │            │               │            │  (identity queue)         │               │
    │            │               │            │            │              │               │
    │            │               │            │            │              ├─update─       │
    │            │               │            │            │              │ lastSensor    │
    │            │               │            │            │              │ Readings      │
    │            │               │            │            │              │               │
    │            │               │            │            │              │               │
    │            │               │            │   [IF threshold breach]   │               │
    │            │               │            │            ├─publish──►   │               │
    │            │               │            │            │ Irrigation   │               │
    │            │               │            │            │ Reminder     │               │
    │            │               │            │            │ Created      │               │
    │            │               │            │            │              │               │
    │            │               │            ├─deliver────────────────────────────────►  │
    │            │               │            │  (notification queue)     │               │
    │            │               │            │            │              │               │
    │            │               │            │            │              │               ├─send Zalo
    │            │               │            │            │              │               │ push
    │            │               │            │            │              │               │ "Vườn cần tưới"

Timeline: ~200ms từ sensor gửi đến nông dân nhận push
```

### 2.1 MQTT Topic Schema

```
Pattern: bazan/{farmId}/{sensorType}/{deviceId}

Ví dụ:
  bazan/farm_abc123/soil_moisture/SENSOR_001_AN
  bazan/farm_abc123/temperature/SENSOR_002_AN
  bazan/farm_xyz789/ph/SENSOR_003_BC
  bazan/farm_xyz789/rainfall/RAIN_001_BC

QoS Level: 1 (at-least-once) — sensor data có thể duplicate, idempotent
Retain: false — không cần message cũ khi subscribe
```

---

## 3. Sequence Diagram — Disease Detection Flow

Luồng từ nông dân chụp ảnh lá bệnh đến khi nhận cảnh báo và tư vấn.

```
Farmer    Gateway    Orchestrator   MediaSvc    CVModelAPI   AgronomySvc   RAGService   NotifSvc
  │          │             │           │            │             │            │            │
  │─upload──►│             │           │            │             │            │            │
  │ photo    │             │           │            │             │            │            │
  │ + query  │             │           │            │             │            │            │
  │          │             │           │            │             │            │            │
  │          ├─route──►    │           │            │             │            │            │
  │          │             │           │            │             │            │            │
  │          │             ├─upload──► │            │             │            │            │
  │          │             │  image    │            │             │            │            │
  │          │             │           ├─store──►   │             │            │            │
  │          │             │           │  MinIO     │             │            │            │
  │          │             │           │ (farm-     │             │            │            │
  │          │             │           │  photos)   │             │            │            │
  │          │             │           │◄─url───    │             │            │            │
  │          │             │◄─imageUrl─│            │             │            │            │
  │          │             │           │            │             │            │            │
  │          │             ├─analyze──────────────► │             │            │            │
  │          │             │  image_url │            │             │            │            │
  │          │             │           │            │             │            │            │
  │          │             │◄──────────────────────-│             │            │            │
  │          │             │  {disease:"leaf_rust", │             │            │            │
  │          │             │   confidence:0.87,     │             │            │            │
  │          │             │   severity:"high"}     │             │            │            │
  │          │             │           │            │             │            │            │
  │          │             ├─[parallel]─────────────────────────────────────────────────── │
  │          │             │                                      │            │            │
  │          │             ├─create alert──────────────────────►  │            │            │
  │          │             │   DiseaseAlertGenerated              │            │            │
  │          │             │                                      │            │            │
  │          │             ├─search knowledge──────────────────────────────►   │            │
  │          │             │   "leaf_rust treatment Robusta"      │            │            │
  │          │             │                                      │            │            │
  │          │             │◄─treatment docs (top3)────────────────────────────│            │
  │          │             │◄─alert saved OK────────────────────────────────── │            │
  │          │             │           │            │             │            │            │
  │          │             ├─build prompt + stream → OpenAI       │            │            │
  │◄─token─  │◄─token──    │                                      │            │            │
  │◄─"Gỉ sắt│             │                                      │            │            │
  │  mức độ  │             │                                      │            │            │
  │  cao..."  │            │                                      │            │            │
  │           │            │                                      │            │            │
  │           │            │  [async — không block response]      │            │            │
  │           │            ├─publish DiseaseAlertGenerated ──────────────────────────────► │
  │           │            │                                      │            │            │
  │           │            │                                      │            │            ├─Zalo push
  │           │            │                                      │            │            │ "⚠ Phát hiện
  │           │            │                                      │            │            │  gỉ sắt mức
  │           │            │                                      │            │            │  cao!"

Total: ~2,500ms (ảnh + CV model chiếm phần lớn)
  - Image upload to MinIO:    200ms
  - CV model inference:       800ms (GPU cold start) / 300ms (warm)
  - RAG search:               200ms
  - LLM first token:          300ms
  - Async Zalo push:          không block
```

### 3.1 CV Model Fallback

```
Scenario: CV model không chắc chắn (confidence < 0.6)

CVModelAPI → {disease: "unknown", confidence: 0.45}
                      ↓
Orchestrator → Skip auto-diagnosis
             → Build prompt:
               "Ảnh không đủ rõ để xác định bệnh tự động.
                Hãy mô tả thêm:
                - Triệu chứng ở mặt trên hay dưới lá?
                - Lá già hay lá non bị trước?"
             → Stream clarification request
             → Save image URL để xử lý sau khi có thêm thông tin
```

---

## 4. Error Handling trong Sequence Flows

### 4.1 Circuit Breaker States

```
Normal flow: CLOSED → requests pass through
After 5 failures in 30s: OPEN → fail fast (503)
After 30s: HALF-OPEN → allow 1 test request
If test succeeds: CLOSED
If test fails: OPEN again

Applied to:
  - Orchestrator → AgronomyService
  - Orchestrator → MarketService
  - Orchestrator → OpenAI API (với retry riêng)
  - RAGService → Qdrant
```

### 4.2 Timeout Chain

```
Client (mobile) WebSocket timeout:  30 seconds
Gateway proxy timeout:              25 seconds
Orchestrator total timeout:         20 seconds
  ├─ Intent classification:          3 seconds
  ├─ Parallel service calls:         5 seconds
  └─ LLM streaming:                 15 seconds (first token < 5s)
```

---

*Diagram được vẽ theo ký hiệu UML Sequence Diagram đơn giản hóa. Cập nhật khi flow thay đổi.*

**© 2026 Bazan AI Project — Confidential**

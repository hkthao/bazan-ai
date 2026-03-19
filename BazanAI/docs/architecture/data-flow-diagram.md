# Data Flow Diagram — Bazan AI
## DFD Level 0 & Level 1

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Principal Software Architect |
| **Tiêu chuẩn** | Yourdon-DeMarco DFD Notation |
| **Liên quan** | System Architecture Document, Sequence Diagrams |

---

## 1. DFD Level 0 — Context Diagram

Level 0 thể hiện Bazan AI như một "hộp đen" duy nhất, chỉ hiển thị các tác nhân bên ngoài và luồng dữ liệu chính.

```
                    ┌─────────────────┐
                    │   NÔNG DÂN      │
                    │  (Farmer User)  │
                    └────────┬────────┘
                             │ Câu hỏi, ảnh vườn,
                             │ feedback, thông tin vườn
                             ▼
┌──────────┐      ┌─────────────────────────┐      ┌──────────────┐
│  ZALO /  │──────►                         ├──────►  OPENAI API  │
│  SMS Bot │      │                         │      │  (GPT-4o,    │
└──────────┘      │      BAZAN AI           │      │  Embeddings) │
                  │   SYSTEM (Process 0)    │◄─────┤              │
┌──────────┐      │                         │      └──────────────┘
│  IoT     │──────►                         │
│ Sensors  │      │                         │      ┌──────────────┐
└──────────┘      │                         ├──────► OPENWEATHER  │
                  │                         │◄─────┤  MAP API     │
┌──────────┐      │                         │      └──────────────┘
│  ADMIN / │──────►                         │
│  Expert  │      │                         │      ┌──────────────┐
└──────────┘      │                         ├──────► ICE FUTURES  │
                  │                         │◄─────┤  (Giá cà phê)│
                  └─────────────────────────┘      └──────────────┘
                             │
                             │ Tư vấn, cảnh báo,
                             │ lịch canh tác, giá
                             ▼
                    ┌─────────────────┐
                    │   NÔNG DÂN      │
                    │  (nhận kết quả) │
                    └─────────────────┘

External Entities:
  [E1] Nông dân          — người dùng chính
  [E2] Zalo/SMS Bot      — kênh tương tác thay thế
  [E3] IoT Sensors       — thiết bị cảm biến vườn
  [E4] Admin/Expert      — cán bộ kỹ thuật, quản trị viên
  [E5] OpenAI API        — LLM provider
  [E6] OpenWeatherMap    — dữ liệu thời tiết
  [E7] ICE Futures       — giá cà phê thị trường
```

---

## 2. DFD Level 1 — Decomposition

Level 1 chia Bazan AI thành 6 process chính, hiển thị luồng dữ liệu giữa chúng và các data store.

```
[E1] NÔNG DÂN
     │
     │ raw_query, farm_photo, user_profile
     ▼
╔═══════════════════════╗
║  P1. GATEWAY &        ║◄──── JWT token ────── [D1] UserContext Store
║  AUTHENTICATION       ║                             (MongoDB)
╚══════════╤════════════╝
           │ authenticated_request
           │ farmer_context
           ▼
╔═══════════════════════╗
║  P2. INTENT           ║
║  CLASSIFICATION       ║
╚══════════╤════════════╝
           │ classified_intent
           │ routing_decision
     ┌─────┴──────────────────────────┐
     │                                │
     ▼                                ▼
╔══════════════╗              ╔═══════════════════╗
║  P3. AI      ║              ║  P4. MARKET &     ║
║  ORCHESTRATION║              ║  ECONOMIC         ║
║  & REASONING  ║              ║  ANALYSIS         ║
╚═══════╤══════╝              ╚═════════╤═════════╝
        │                               │
        │ rag_query                     │ price_request
        ▼                               ▼
╔══════════════╗      [D2]      ╔══════════════════╗
║  P3a. HYBRID ║  Vector Store  ║  [D3] Market DB  ║
║  RAG SEARCH  ║◄──(Qdrant)────►║  (MongoDB)       ║
╚═══════╤══════╝                ╚══════════════════╝
        │ retrieved_chunks
        ▼
╔══════════════╗      [E5]
║  P3b. LLM   ║◄────OpenAI
║  GENERATION  ║          API
╚═══════╤══════╝
        │ ai_response (streaming)
        ▼
╔═══════════════════════╗
║  P5. NOTIFICATION &   ║
║  RESPONSE DELIVERY    ║
╚══════════╤════════════╝
           │ response, alert
     ┌─────┴──────────────────┐
     ▼                        ▼
[E1] NÔNG DÂN          [E2] ZALO/SMS
     (app response)         (push message)

─────────────────────────────────────────────────

╔═══════════════════════╗
║  P6. IOT & WEATHER    ║◄── [E3] IoT Sensors
║  DATA INGESTION       ║◄── [E6] OpenWeatherMap
╚══════════╤════════════╝
           │ sensor_data, weather_data
           ▼
     [D4] Sensor & Weather Store
          (MongoDB + Redis cache)
           │
           │ context_enrichment
           └──────────────────► P2 (Intent Classification)
                                P3 (AI Orchestration)
```

---

## 3. Mô tả chi tiết các luồng dữ liệu

### 3.1 Luồng chính — AI Chat

| Số | Luồng | Từ | Đến | Dữ liệu |
|----|-------|----|-----|---------|
| F1 | User query | Nông dân | P1 Gateway | query text, images, session_id |
| F2 | Auth token | P1 Gateway | D1 UserContext | JWT claims lookup |
| F3 | Farmer context | D1 UserContext | P1 Gateway | farm profile, sensor readings |
| F4 | Auth request | P1 Gateway | P2 Intent | farmer_context + query |
| F5 | Intent result | P2 Intent | P3 Orchestration | intent_class, confidence, routing |
| F6 | RAG query | P3 Orchestration | P3a RAG Search | expanded_query, metadata_filter |
| F7 | Vector search | P3a RAG Search | D2 Qdrant | dense_vector, sparse_vector |
| F8 | Retrieved chunks | D2 Qdrant | P3a RAG Search | top_k chunks + citations |
| F9 | Prompt + context | P3a RAG Search | P3b LLM | system_prompt, retrieved_context |
| F10 | LLM completion | P3b LLM | E5 OpenAI | prompt tokens |
| F11 | Stream tokens | E5 OpenAI | P3b LLM | token stream |
| F12 | AI response | P3b LLM | P5 Delivery | response text + citations |
| F13 | Session save | P3 Orchestration | D5 Conv Store | chat_message, context_used |
| F14 | Delivery | P5 Delivery | Nông dân | streamed response via SignalR |

### 3.2 Luồng Cảnh báo — Disease Alert

| Số | Luồng | Từ | Đến | Dữ liệu |
|----|-------|----|-----|---------|
| A1 | Sensor reading | IoT Sensor | P6 Ingestion | moisture%, temp, pH |
| A2 | Normalized reading | P6 Ingestion | D4 Sensor Store | SensorReading document |
| A3 | Threshold event | P6 Ingestion | P3 Orchestration | SensorDataReceived event (RabbitMQ) |
| A4 | Weather alert | E6 OpenWeatherMap | P6 Ingestion | forecast JSON |
| A5 | Alert trigger | P6 Ingestion | P5 Notification | WeatherAlertTriggered event |
| A6 | Disease diagnosis | P3 Orchestration | External CV API | farm_photo bytes |
| A7 | CV result | External CV API | P3 Orchestration | disease_code, confidence |
| A8 | Alert event | P3 Orchestration | D6 Agronomy Store | DiseaseAlert document |
| A9 | Push notification | P5 Notification | Zalo/FCM | formatted alert message |

### 3.3 Luồng thị trường

| Số | Luồng | Từ | Đến | Dữ liệu |
|----|-------|----|-----|---------|
| M1 | Price fetch | P4 Market | E7 ICE Futures | API request |
| M2 | Price data | E7 ICE Futures | P4 Market | Robusta/Arabica price USD |
| M3 | Price save | P4 Market | D3 Market DB | CoffeePrice document |
| M4 | Price cache | P4 Market | Redis | price_vnd_current (TTL 5min) |
| M5 | Price event | P4 Market | P5 Notification | CoffeePriceUpdated (>5% change) |
| M6 | ROI request | Nông dân | P4 Market | farm_id, season |
| M7 | ROI response | P4 Market | Nông dân | ROI calculation result |

---

## 4. Data Stores

| ID | Tên | Công nghệ | Dữ liệu chính |
|----|-----|-----------|--------------|
| D1 | UserContext Store | MongoDB (bazan_identity) | Farmer, Farm, RefreshToken |
| D2 | Vector Store | Qdrant | Document chunks, embeddings |
| D3 | Market DB | MongoDB (bazan_market) | CoffeePrice, RoiCalculation |
| D4 | Sensor & Weather Store | MongoDB + Redis | SensorReading, WeatherForecast |
| D5 | Conversation Store | MongoDB (bazan_conversations) | ChatSession, ChatMessage |
| D6 | Agronomy Store | MongoDB (bazan_agronomy) | DiseaseAlert, IrrigationPlan |
| D7 | File Store | MinIO | PDF docs, farm photos, reports |
| D8 | Knowledge Index | Qdrant | WCR docs, agronomy knowledge |

---

*Tài liệu theo ký hiệu Yourdon-DeMarco DFD. Cập nhật khi có service mới hoặc luồng dữ liệu thay đổi.*

**© 2026 Bazan AI Project — Confidential**

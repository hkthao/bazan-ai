# Product Roadmap — Bazan AI
## Agentic Microservices Platform · Coffee Farmers Tây Nguyên

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.2.0 |
| **Ngày cập nhật** | 2026-03-19 |
| **Trạng thái** | Active — Sprint 1 Completed |
| **Tiêu chuẩn** | Enterprise Architecture · Clean Architecture |
| **Thay đổi so với v1.1** | Swap Sprint 6↔7 · Tách Sprint 3 → 3a/3b · Thêm CV Detection · Security Audit · WASI Prep |

---

## 1. Tầm nhìn Chiến lược

Xây dựng nền tảng AI hỗ trợ nông nghiệp thông minh, bảo mật và bền bỉ — giúp **600.000 nông dân trồng cà phê Tây Nguyên** tối ưu hóa canh tác và quyết định kinh tế thông qua dữ liệu thực địa và Agentic AI.

**North Star Metric:** Số nông dân nhận được giá trị thực từ Bazan AI mỗi tuần (WAVU — Weekly Active Value Users), đo bằng ≥ 1 session có thumbs-up trong 7 ngày.

---

## 2. Các Giai đoạn Phát triển

| Giai đoạn | Timeline | Mục tiêu | KPI chính |
|---------|---------|---------|----------|
| **Phase 1** — Foundation & Security | Tháng 1–3 | Hạ tầng lõi, xác thực, data sovereignty | 100 farmers pilot, uptime 99.5% |
| **Phase 2** — Data Quality & IoT | Tháng 4–6 | Cảm biến thực địa, offline-first, DQA | Sensor accuracy > 95%, hoạt động ở 3G |
| **Phase 3** — AI & Knowledge | Tháng 7–9 | Agentic AI, RAG, disease CV | AI accuracy > 85%, Recall@5 ≥ 0.85 |
| **Phase 4** — Economics & Scale | Tháng 10–12 | Market data, ROI, notifications, 5.000 farmers | Price latency < 5 phút, D30 retention ≥ 40% |

---

## 3. Hoạt động Chuẩn bị (Trước Sprint 2)

> **Quan trọng:** Các việc này phải thực hiện **song song với Sprint 2**, không phải chờ đến sprint liên quan. Thiếu chuẩn bị sẽ block Sprint 7 và Sprint 12.

### 3.1 WASI Partnership — Bắt đầu ngay tuần này

```
Tuần 1:  Soạn email → gửi đề xuất MOU cho WASI (Viện Nghiên cứu Tây Nguyên)
Tuần 2:  Họp khởi động với WASI — xác định 50 tài liệu ưu tiên
Tuần 3+: Nhận tài liệu → metadata annotation → chuẩn bị index

Deliverable cần có trước Sprint 7:
  - ≥ 30 tài liệu WASI (PDF) đã được annotate metadata
  - ≥ 20 tài liệu WCR (Open Access) đã download
  - 1 cán bộ WASI làm reviewer cho Golden Test Set
  - Danh mục thuốc BVTV 2024 (Bộ NN&PTNT) đã có
```

**Lý do:** Sprint 7 (Knowledge Base) RAG pipeline sẽ trống rỗng nếu không chuẩn bị tài liệu trước. Thu thập + review + annotation mất 3–4 tuần.

### 3.2 Pháp lý — Song song với Sprint 2

```
Luật sư chuyên Nghị định 13/2023/NĐ-CP review:
  - Data Privacy Policy (docs/legal/privacy-policy-dpa-dsr.md)
  - Personal Data Processing Agreement (DPA)
  - Terms of Service

Phải hoàn thành trước khi onboard farmer thật trong Sprint 12.
```

### 3.3 Golden Test Set — Chuẩn bị trước Sprint 7

```
WASI agronomist tạo 200 cặp (câu hỏi + expected answer):
  - 50 disease diagnosis cases
  - 40 fertilizer advice cases
  - 30 irrigation cases
  - 30 cross-lingual (VI query → EN doc)
  - 25 technical term queries
  - 25 market/ROI questions

Dùng để: Đo Recall@5 (target ≥ 0.80) trước khi claim AI production-ready.
```

---

## 4. Sprint Plan Chi tiết (13 Sprints)

### ✅ Sprint 1 — Infrastructure & Shared Kernel
**Status: COMPLETED**
**Phase:** 1

```
Đã hoàn thành:
  ✅ CI/CD Pipelines (GitHub Actions)
  ✅ Shared Kernel (.NET 8 / Python 3.12)
  ✅ API Gateway (YARP) — basic routing
  ✅ Docker Compose stack — tất cả services healthy
  ✅ MongoDB Replica Set (3 nodes) — khởi tạo thành công
  ✅ RabbitMQ + Qdrant + Redis baseline
  ✅ Seq + Prometheus + Grafana baseline
```

**Lưu ý cho Sprint tiếp theo:**
- Verify MongoDB keyFile đã generate đúng (`make gen-keyfile`)
- Confirm tất cả `.env` secrets đã được set trên staging server
- Branch protection rules đã bật trên `main` và `develop` chưa?

---

### Sprint 2 — Identity, Privacy & Data Sovereignty
**Phase:** 1 | **Phụ thuộc:** Sprint 1 ✅

**Sprint Goal:** Nông dân đăng ký và đăng nhập được. Mọi PII được bảo vệ đúng quy định.

```
Deliverables:
  □ OTP authentication — Zalo + SMS fallback
  □ JWT RS256 (asymmetric) — không dùng HS256
  □ Refresh token rotation — hashed in MongoDB
  □ PII Encryption (AES-256) cho phoneNumber, fullName, GPS
  □ Consent Management — farmer đồng ý trước khi lưu data
  □ Rate limiting OTP: 5 attempts / 15 phút / số điện thoại
  □ Farmer profile + Farm CRUD
  □ UserContext document (MongoDB) — seed data cho AI context
```

**Definition of Done:**
- JWT forge attempt → 401 (RS256 verified)
- PII fields encrypted in MongoDB Atlas view
- Privacy Policy text hiển thị và farmer must accept trước khi register

---

### Sprint 3a — Resilient Messaging (RabbitMQ Core)
**Phase:** 1 | **Phụ thuộc:** Sprint 2

**Sprint Goal:** Mọi domain event được gửi reliable, không mất message khi service restart.

> **Lý do tách Sprint 3:** Outbox Pattern + RabbitMQ setup là một sprint đầy. Gộp với Zalo integration sẽ thiếu thời gian test kịch bản mạng chập chờn — loại bug gây mất notification quan trọng.

```
Deliverables:
  □ Outbox Pattern implementation (MongoDB-backed)
  □ RabbitMQ Exchanges & Queues theo definitions.json
  □ MassTransit consumer base class với retry + DLQ
  □ Dead Letter Queue monitoring alert
  □ Idempotency store (Redis SET NX) cho consumers
  □ Event contracts đầy đủ (xem RabbitMQ Event Catalog)

Events cần có trong sprint này:
  - FarmerRegisteredEvent
  - FarmerLocationUpdatedEvent
  - FarmAddedEvent
```

---

### Sprint 3b — Notification Service & Zalo Integration
**Phase:** 1 | **Phụ thuộc:** Sprint 3a

**Sprint Goal:** Nông dân nhận được notification qua Zalo khi có cảnh báo quan trọng.

```
Deliverables:
  □ Zalo OA API integration
  □ FCM (Firebase Cloud Messaging) cho mobile push
  □ SMS fallback (Twilio)
  □ Notification Template Engine (disease alert, price alert, weather)
  □ Zalo Token refresh job (Hangfire, mỗi 80 ngày)
  □ Zalo Bot webhook — nhận câu hỏi qua Zalo

Test scenarios bắt buộc:
  - Mạng chập chờn: message retry đúng không?
  - Token expired: auto refresh không?
  - Zalo down: FCM fallback kịp thời không?
```

---

### Sprint 4 — IoT Ingestion & Data Quality Assurance
**Phase:** 2 | **Phụ thuộc:** Sprint 3a

**Sprint Goal:** Dữ liệu sensor được ingest đáng tin cậy, dữ liệu nhiễu bị loại bỏ trước khi vào AI.

```
Deliverables:
  □ MQTT Broker — port 1883 (plain) + 8883 (TLS)
  □ Sensor Topic Router: bazan/{farmId}/{sensorType}/{deviceId}
  □ HMAC device authentication
  □ Data Validation Layer:
      - Schema validation (đúng field, đúng type)
      - Range check (soil moisture 0–100%, pH 3.0–9.0, v.v.)
      - Logic check (so sánh với lần đo trước — outlier detection)
  □ Multi-sensor payload support (gateway device)
  □ DLQ cho sensor data lỗi (không block queue chính)
  □ IoT queue bounded: max 100,000 messages, drop-head policy
  □ Threshold alerts → publish lên RabbitMQ:
      soil_moisture < 30% → IrrigationReminderCreated
      pH < 5.5 hoặc > 7.0 → SoilPhAlertCreated

Sensor TTL: 90 ngày (TTL index trong MongoDB)
```

**KPI sprint:** Sensor data accuracy > 95% (đo bằng so sánh với ground truth sample).

---

### Sprint 5 — Offline-first Architecture
**Phase:** 2 | **Phụ thuộc:** Sprint 3b, Sprint 4

**Sprint Goal:** App hoạt động đủ tốt kể cả ở vùng không có sóng (30% vườn Tây Nguyên).

```
Deliverables (Flutter):
  □ Drift (SQLite) local storage — type-safe, compile-time checked
  □ 3-tier data cache:
      Tier 1 (luôn offline): lịch canh tác 30 ngày, 20 sessions gần nhất, cảnh báo chưa đọc
      Tier 2 (sync khi mở app): history cũ hơn
      Tier 3 (chỉ online): AI chat real-time, ảnh HD
  □ Outbox pattern (client): queue messages khi offline → flush khi có mạng
  □ Background Sync Worker (mỗi 15 phút khi có mạng)
  □ Conflict resolution: Server wins (AI content), Client wins (observations)
  □ Offline indicator UX — banner amber "Đang xem dữ liệu đã lưu"
  □ Delta sync API endpoint — chỉ pull changes từ lastSync

Test bắt buộc:
  - Airplane mode → ghi chép → restore network → data sync đúng không?
  - Mạng chập chờn (throttle 50kbps) → app không crash
```

---

### Sprint 6 — Knowledge Base & Vector Indexing
**Phase:** 3 | **Phụ thuộc:** Sprint 1 ✅, WASI Prep (xem Mục 3.1)

> **Lý do đổi vị trí (từ Sprint 7 → Sprint 6):** Knowledge Base là foundation của AI Orchestrator. Nếu làm Agronomy Engine trước, engine sẽ hard-code rules → phải refactor khi tích hợp RAG. Làm Knowledge Base trước = Agronomy Engine có thể dùng RAG context ngay từ đầu.

**Sprint Goal:** RAG Pipeline hoạt động, Recall@5 ≥ 0.80 trên Golden Test Set.

```
Deliverables:
  □ PDF ingestion pipeline (MinIO → parse → chunk → embed → Qdrant)
  □ Chunking: 512 tokens, 64 overlap, RecursiveCharacterTextSplitter
  □ Embedding: text-embedding-3-large (OpenAI), 3072 dims
  □ Qdrant collections:
      - wcr_documents (3072d, INT8 quantization)
      - agronomy_knowledge (3072d)
      - pest_disease_library (3072d)
      - market_reports (1536d — Matryoshka)
  □ Hybrid Search: Dense (text-embedding-3-large) + Sparse (BM42) + RRF fusion
  □ Payload indexes: coffee_varieties, topics, region, growth_stages, language
  □ Metadata annotation schema + validation
  □ Query embedding cache (Redis, TTL 1h)
  □ Vietnamese preprocessor:
      - Diacritic restoration (vncorenlp)
      - Regional term normalization ("mọt đục cành" → "Hypothenemus hampei")
      - Technical term protection (NPK 16-16-8, TR4, pH 6.2)
  □ Banned chemicals hardcode list (không phụ thuộc LLM)

Initial content (cần chuẩn bị xong từ WASI Prep):
  - 30 tài liệu WASI kỹ thuật canh tác
  - 20 tài liệu WCR (Open Access)
  - Danh mục thuốc BVTV 2024
```

**Gate:** Recall@5 ≥ 0.80 trên 200-case Golden Test Set TRƯỚC KHI proceed Sprint 7.

---

### Sprint 6b — Computer Vision Disease Detection
**Phase:** 3 | **Chạy song song với Sprint 6 hoặc ngay sau**

> **Lý do thêm sprint này:** PRD và User Story Map đều có tính năng chụp ảnh chẩn đoán bệnh — là tính năng nông dân muốn nhất. Không có trong roadmap gốc là gap quan trọng.

**Sprint Goal:** Nông dân chụp ảnh lá bệnh → AI chẩn đoán được bệnh với confidence score.

```
Deliverables:
  □ Azure Custom Vision setup (Phase 1 — không cần GPU)
  □ Image upload flow: Camera/Gallery → MinIO (pre-signed URL) → CV API
  □ 10 disease classes Phase 1:
      leaf_rust, anthracnose, phoma_blight, root_rot, berry_borer,
      mealybug, stem_borer, nutrient_deficiency_mg, nutrient_deficiency_n, healthy
  □ Confidence score display trong app
  □ Low confidence (< 0.60) → request more photos / clarification
  □ WASI image labeling workflow (CVAT tool)
  □ Training dataset: ≥ 300 ảnh/class (phối hợp WASI)
  □ CV result integration vào Agronomy Diagnosis flow

Targets:
  - Overall accuracy ≥ 80% (Phase 1)
  - Inference latency < 2s (Azure Custom Vision API)
```

---

### Sprint 7 — Agronomy Engine V1
**Phase:** 3 | **Phụ thuộc:** Sprint 6 (Knowledge Base ready, Recall@5 ≥ 0.80)

> **Lý do đổi vị trí (từ Sprint 6 → Sprint 7):** Agronomy Engine giờ có thể tận dụng RAG context ngay từ V1, thay vì hard-code rules rồi refactor.

**Sprint Goal:** Nông dân nhận được lịch canh tác cá nhân hóa và chẩn đoán bệnh có nguồn tham khảo.

```
Deliverables:
  □ Disease diagnosis endpoint — kết hợp:
      + CV result (Sprint 6b)
      + RAG knowledge retrieval (Sprint 6)
      + Farmer context (giống, đất, vùng)
  □ Irrigation plan algorithm (dựa trên sensor + weather + schedule)
  □ Fertilizer recommendation — cá nhân hóa theo:
      giống cây (Robusta/Arabica/TR4), giai đoạn sinh trưởng, kết quả phân tích đất
  □ 30-day cultivation schedule generator
  □ Soil analysis input + amendment recommendations
  □ Pest alert aggregation theo GPS vùng
  □ Seasonal calendar (12 tháng Tây Nguyên)

Domain rules (từ WASI, không hard-code):
  - 6 yếu tố canh tác: Đất, Nước, Giống, Phân bón, Sâu bệnh, Thu hoạch
  - Mọi recommendation phải có sourceDocument (WCR/WASI docId)
  - Mọi thuốc phải kiểm tra banned chemicals list trước khi recommend
```

---

### Sprint 8 — AI Orchestrator (Semantic Kernel)
**Phase:** 3 | **Phụ thuộc:** Sprint 6 (RAG), Sprint 7 (Agronomy), Sprint 3b (Notifications)

**Sprint Goal:** AI Agent tự lập kế hoạch gọi đúng tools, trả lời câu hỏi phức hợp (bệnh + thời tiết + giá) trong một response.

```
Deliverables:
  □ Semantic Kernel 1.x setup với GPT-4o (RS256)
  □ Plugin system:
      - AgronomyPlugin   → Agronomy Service (port 5004)
      - KnowledgePlugin  → RAG Service (port 5003)
      - WeatherPlugin    → Weather Service (port 5008)
      - MarketPlugin     → Market Service (port 5005) [stub, đầy đủ Sprint 10]
      - FarmerContextPlugin → Identity Service (port 5001)
  □ AutoFunctionCalling — AI tự quyết định gọi tool nào
  □ Intent Classifier (GPT-4o-mini) — 12 intent classes, confidence > 0.6
  □ Token budget management:
      system: 1200 + context: 300 + history: 2000 + RAG: 2000 + query: 200 = max 5700
  □ Sliding window conversation history (giữ first 3 + last N + summary marker)
  □ Prompt Engineering v1.0 — theo Prompt Playbook
  □ Prompt Registry: src/.../Prompts/v1/
  □ Diacritic restoration cho query expansion

Circuit breakers (Polly):
  - OpenAI API: retry 3x, exponential backoff, circuit breaker
  - Agronomy Service: timeout 5s
  - RAG Service: timeout 500ms
```

---

### Sprint 9 — Real-time Agentic Chat
**Phase:** 3 | **Phụ thuộc:** Sprint 8 (Orchestrator)

**Sprint Goal:** Nông dân nhận câu trả lời AI streaming từng token, P95 < 3 giây.

```
Deliverables:
  □ SignalR Hub: wss://api.bazanai.vn/hub/chat
  □ Streaming flow:
      SendMessage → TypingIndicator stages → ReceiveToken (per token) → StreamComplete
  □ Client events: SendMessage, CancelStream, SubmitFeedback, Ping
  □ Server events: ReceiveToken, StreamComplete, StreamError, TypingIndicator, Pong
  □ Auto-reconnect (exponential backoff: 0ms, 2s, 10s, 30s)
  □ Offline message queue (client-side outbox)
  □ RLHF feedback: 👍/👎 per message + star rating per session
  □ RLHF export pipeline:
      Daily job → anonymize PII → DPO format → MinIO bazan-backups/rlhf/
  □ StreamError handling với error codes: llm_timeout, rate_limit, invalid_session
  □ Citations display (sourceTitle, pageStart, pageEnd)

Performance targets:
  - TTFT (Time to First Token): < 500ms P50, < 2s P95
  - Full response: < 1.5s P50, < 3s P95
  - SignalR idle timeout: 5 phút
  - Ping interval: 25s (avoid load balancer timeout)
```

---

### Sprint 10 — Market & Price Monitoring
**Phase:** 4 | **Phụ thuộc:** Sprint 8 (MarketPlugin stub)

**Sprint Goal:** Nông dân biết giá cà phê hôm nay, nhận cảnh báo khi giá biến động > 5%.

```
Deliverables:
  □ ICE Futures API integration (Robusta London, cập nhật mỗi 3h)
  □ Redis cache giá (TTL 5 phút) — không gọi ICE API mỗi request
  □ Đại lý thu mua — geo-query MongoDB 2dsphere (tìm đại lý gần GPS vườn)
  □ ROI Calculator:
      revenue = yield_kg × selling_price
      netProfit = revenue - totalCosts
      roi_pct = netProfit / totalCosts × 100
      breakevenPrice = totalCosts / yield_kg
  □ Sell vs Hold analysis
  □ Price forecast (30 ngày, với disclaimer)
  □ Price alert: CoffeePriceUpdated event khi change > 5%
  □ Zalo/FCM notification khi alert triggered
  □ Price history chart (7/30/90 ngày)

API phụ thuộc:
  - ICE Futures: $200/tháng — confirm subscription trước sprint
  - OpenWeatherMap Pro: $40/tháng
```

---

### Sprint 11 — Proactive Alerts, Scheduler & Security Audit
**Phase:** 4 | **Phụ thuộc:** Sprint 10

**Sprint Goal:** Hệ thống chủ động cảnh báo nông dân. Bảo mật đã được audit trước pilot.

```
Deliverables — Alerts & Scheduler:
  □ Hangfire Jobs:
      - Daily RLHF export (2AM)
      - Weekly market summary (Thứ 2 7AM)
      - Disease alert aggregation (mỗi 6h)
      - Zalo token refresh (mỗi 80 ngày)
      - MongoDB backup (3AM daily → MinIO)
  □ Disease alert theo GPS vùng
      (tổng hợp: nếu > N farms trong bán kính R bị bệnh X → broadcast alert)
  □ Weather alert: dry_spell, heavy_rain, heat_stress, frost_risk
  □ Irrigation reminder từ sensor threshold (soil_moisture < 30%)
  □ Harvest window notification (maturityScore > 0.80)

Deliverables — Security Audit:
  □ OWASP ZAP automated scan trên staging
  □ Manual auth testing:
      - JWT RS256 forge attempt → 403
      - BOLA: Farmer A token → GET /farmers/{farmer_B_id} → 403
      - OTP brute force: bị block sau attempt thứ 6
      - Rate limit: 429 sau 61 requests/phút
  □ Trivy full scan — zero CRITICAL vulns trước pilot
  □ Penetration test nhẹ (internal hoặc external vendor)
  □ Fix mọi finding trước Sprint 12

Lý do audit ở Sprint 11 (không phải sau): Phải clean security trước khi mời
farmer thật vào Sprint 12. Không thể rollback sau khi có real user data.
```

---

### Sprint 12 — Enterprise Observability & Pilot Launch
**Phase:** 4 | **Phụ thuộc:** Sprint 11 (Security audit passed)

> **Lưu ý quan trọng:** Observability phải **stable và đã test** trước khi mời farmer thật. Sprint này chia thành 2 tuần rõ ràng: Tuần 1 = Observability hardening, Tuần 2 = Pilot launch.

**Sprint Goal:** 50 nông dân pilot sử dụng app thực tế. Team có đầy đủ visibility để detect và fix issues ngay.

```
Tuần 1 — Observability Hardening:
  □ Grafana dashboards đầy đủ:
      - System Overview (API rate, error rate, P95 latency)
      - AI Quality (thumbs-up rate, re-ask rate, safety violations)
      - Database Performance (MongoDB, Qdrant, Redis)
      - Business Metrics (DAU, sessions, feedback)
  □ Alert rules: ServiceDown (Critical), APIErrorRateHigh (Critical),
    FeedbackRateDrop (Warning), OpenAICostSpike (Warning)
  □ PagerDuty routing: Critical → on-call phone + Slack
  □ Disaster Recovery drill:
      - Kill MongoDB Primary → verify failover < 30s
      - Restore từ backup vào DR environment
  □ Smoke test suite (15 critical paths) tự động sau mỗi deploy
  □ Runbook hoàn chỉnh cho mọi Critical alert

Tuần 2 — Pilot Launch (50 nông dân):
  □ Onboard 50 nông dân qua 2 HTX (Cư M'gar + Ea H'leo)
  □ 1-on-1 setup session 15 phút/nông dân cho người ít quen tech
  □ Zalo group support riêng cho pilot group
  □ Daily check: Grafana AI quality dashboard
  □ Weekly WASI expert review: 20 sample responses
  □ NPS survey sau 2 tuần pilot
```

**Definition of Done cho roadmap Phase 1:**
- D7 retention ≥ 50%
- Thumbs-up rate ≥ 70%
- Zero Critical security incidents
- Expert accuracy ≥ 4.0/5.0 (WASI review)
- P95 response latency < 3s

---

## 5. Dependency Map

```
Sprint 1 ✅
    │
    ├──► Sprint 2 (Identity)
    │         │
    │         ├──► Sprint 3a (RabbitMQ)
    │         │         │
    │         │         ├──► Sprint 4 (IoT)
    │         │         │         │
    │         │         │         └──► Sprint 5 (Offline)
    │         │         │
    │         │         └──► Sprint 3b (Zalo)
    │         │                    │
    │         │                    └──► Sprint 10 (Market)
    │         │                                │
    │         │                                └──► Sprint 11 (Alerts + Audit)
    │         │                                              │
    │         │                                              └──► Sprint 12 (Pilot)
    │         │
    └─────────┤
              │
   [WASI Prep]──► Sprint 6 (RAG/KB) ──► [Gate: Recall@5 ≥ 0.80]
                        │
              Sprint 6b (CV) ──► Sprint 7 (Agronomy Engine)
                        │                │
                        └────────────────┼──► Sprint 8 (Orchestrator)
                                                   │
                                              Sprint 9 (Chat)
```

---

## 6. Các Trụ cột Enterprise

### 6.1 Data Validation Layer (Anti-GIGO)

Mọi dữ liệu từ sensor/người dùng phải đi qua 3 lớp kiểm chứng trước khi AI xử lý:

| Lớp | Kiểm tra | Ví dụ |
|-----|---------|-------|
| Schema validation | Đúng field, đúng type | `soilMoisture_pct` phải là number |
| Range check | Trong ngưỡng vật lý hợp lệ | moisture 0–100%, pH 3.0–9.0 |
| Logic check | Nhất quán với context | Độ ẩm tăng 50% trong 1 phút → outlier |

Dữ liệu fail validation → DLQ (không silently drop, không block queue).

### 6.2 Privacy by Design

- GPS vườn round đến 0.01° (~1km) trước khi lưu
- Phone number, fullName mã hóa AES-256 trong MongoDB
- Consent bắt buộc trước khi thu thập mỗi loại data
- Farmer có thể yêu cầu export toàn bộ data hoặc xóa tài khoản bất kỳ lúc nào
- OpenAI: PII stripping trước khi gửi prompt, Zero Data Retention option bật

### 6.3 Resilience & Offline-first

- Docker restart policy: `unless-stopped` cho tất cả services
- RabbitMQ Outbox Pattern — message không mất khi service restart
- Offline cache 3 tiers — core features hoạt động không cần mạng
- MongoDB Replica Set 3 nodes — failover tự động < 30 giây
- Circuit breakers (Polly) cho tất cả external API calls

### 6.4 AI Safety (Non-negotiable)

- Banned chemicals list hardcoded — không phụ thuộc LLM
- Mọi recommendation thuốc phải có citation từ Bộ NN&PTNT
- Confidence score bắt buộc cho disease diagnosis
- Safety incident alert → PagerDuty ngay lập tức (zero tolerance)
- WASI expert review mỗi tuần trong giai đoạn pilot

### 6.5 Observability-first

Không deploy feature mới nếu không có:
- Grafana panel theo dõi metric liên quan
- Alert rule cho threshold vi phạm
- Log query mẫu trong Runbook

---

## 7. KPIs Tổng hợp

### 7.1 Business KPIs

| Metric | Phase 1 (T3) | Phase 2 (T6) | Phase 3 (T9) | Phase 4 (T12) |
|--------|-------------|-------------|-------------|--------------|
| Registered farmers | 100 | 500 | 2,000 | 5,000 |
| DAU | 60 | 300 | 1,200 | 3,000 |
| D7 retention | ≥ 50% | ≥ 52% | ≥ 55% | ≥ 55% |
| D30 retention | ≥ 40% | ≥ 42% | ≥ 45% | ≥ 45% |
| NPS | ≥ 40 | ≥ 45 | ≥ 50 | ≥ 55 |
| HTX partners | 3 | 10 | 20 | 50 |
| MRR | $0 | $850 | $3,700 | $10,000 |

### 7.2 Technical KPIs

| Metric | Target | Đo bằng |
|--------|--------|---------|
| AI response P95 | < 3s | Grafana |
| API availability | ≥ 99.5% | Synthetic ping |
| RAG Recall@5 | ≥ 0.85 | Weekly eval |
| Thumbs-up rate | ≥ 75% | In-app feedback |
| Expert accuracy | ≥ 4.0/5.0 | WASI weekly review |
| Hallucination rate | < 1% | Safety monitor |
| Sensor data accuracy | > 95% | Ground truth sample |
| MongoDB failover | < 30s | Monthly DR drill |
| Price data latency | < 5 phút | Metrics |
| CV disease accuracy | ≥ 80% | Golden image set |

---

## 8. Risk Register

| # | Rủi ro | Likelihood | Impact | Mitigation |
|---|--------|-----------|--------|-----------|
| R1 | WASI chưa sẵn sàng cung cấp tài liệu | Trung bình | Rất cao | Bắt đầu liên hệ ngay tuần này. Fallback: WCR Open Access (200 docs) |
| R2 | OpenAI API outage | Thấp | Cao | Circuit breaker + retry. Phase 4: local LLM fallback |
| R3 | Nông dân không tải app (prefer Zalo) | Cao | Cao | Zalo Bot là entry point, upsell sang app sau |
| R4 | Mạng 3G không đủ để stream AI | Cao | Trung bình | Offline cache + aggressive response compression |
| R5 | PDPA violation trước khi có Privacy Policy | Thấp | Rất cao | Luật sư review TRƯỚC khi onboard farmer thật |
| R6 | OpenAI cost spike khi scale | Trung bình | Trung bình | Cache, gpt-4o-mini cho intent, cost alert |
| R7 | CV model accuracy thấp | Trung bình | Trung bình | Azure Custom Vision Phase 1, tăng training data |
| R8 | Security vulnerability phát hiện sau pilot | Thấp | Cao | Security audit bắt buộc Sprint 11 trước Sprint 12 |

---

## 9. Thay đổi so với v1.1.0

| Thay đổi | Lý do |
|---------|-------|
| Sprint 6↔7 hoán đổi | Knowledge Base là dependency của Agronomy Engine. Làm đúng thứ tự tránh refactor |
| Sprint 3 tách thành 3a + 3b | RabbitMQ và Zalo integration đủ lớn để là 2 sprints riêng |
| Thêm Sprint 6b (CV Detection) | Feature quan trọng trong PRD nhưng thiếu trong roadmap gốc |
| Security Audit vào Sprint 11 | Phải xong trước pilot (Sprint 12), không thể làm sau |
| Thêm Mục 3 (Hoạt động Chuẩn bị) | WASI partnership và pháp lý cần bắt đầu ngay, không chờ đến sprint liên quan |
| Cập nhật KPI table | Align với PRD và KPI Dashboard document |
| Thêm Dependency Map | Visualize dependencies để tránh bắt đầu sprint khi chưa đủ điều kiện |
| Sprint 12 tách thành 2 tuần rõ ràng | Observability phải stable trước khi mời farmer thật |

---

*Roadmap được review mỗi Sprint Retrospective. Mọi thay đổi phải được Engineering Lead + Product Lead approve.*

**© 2026 Bazan AI — Confidential Business Document**

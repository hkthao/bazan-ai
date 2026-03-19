# Capacity Planning Document — Bazan AI
## Kế hoạch Năng lực Hệ thống cho 10.000 Nông dân

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Principal Software Architect · DevOps Lead |
| **Horizon** | 18 tháng (Phase 1 → Phase 3) |
| **Liên quan** | System Architecture Document, Network Topology, ADR-001 đến ADR-005 |

---

## 1. Assumptions & Load Model

### 1.1 User Growth Projection

| Phase | Timeline | Farmers | DAU | Peak Concurrent |
|-------|---------|---------|-----|----------------|
| Phase 1 (MVP) | Tháng 1–3 | 100 | 60 | 10 |
| Phase 1 (Growth) | Tháng 4–6 | 500 | 300 | 50 |
| Phase 2 | Tháng 7–9 | 2,000 | 1,200 | 200 |
| Phase 2 (Scale) | Tháng 10–12 | 5,000 | 3,000 | 500 |
| Phase 3 | Tháng 13–18 | 10,000 | 6,000 | 1,000 |

### 1.2 Behavior Assumptions

```
Nông dân trung bình:
  - 5 messages / session
  - 2 sessions / tuần
  - Session buổi sáng (6–9h): 40% traffic
  - Session buổi chiều (17–20h): 35% traffic
  - Còn lại 25% rải đều trong ngày

Peak hour multiplier: 3x average
IoT sensors: 1 reading / 15 phút / sensor
  Phase 3 estimate: 3,000 farms × 3 sensors avg = 9,000 readings/15min
                  = 10 readings/second (thường)
                  = 30 readings/second (peak)
```

---

## 2. Compute Requirements

### 2.1 Phase 1 — Single Server

```
Khuyến nghị: 1 × VPS hoặc Dedicated Server

CPU:    8 cores (vCPU)
RAM:    16 GB
Disk:   200 GB SSD (NVMe preferred)
Net:    100 Mbps uplink
OS:     Ubuntu 22.04 LTS

Chi phí ước tính (Vultr/DigitalOcean/Linode):
  ~$80–120/tháng

Allocation:
  Docker overhead:      ~1GB RAM
  MongoDB RS (3 nodes): ~3GB RAM (1GB each, shared disk)
  Qdrant:               ~1GB RAM
  Redis:                ~512MB RAM
  RabbitMQ:             ~512MB RAM
  Application services: ~6GB RAM (10 services × ~600MB avg)
  Observability stack:  ~2GB RAM
  OS + Buffer:          ~2GB RAM
  ─────────────────────────────
  Total:                ~16GB RAM  ✅
```

### 2.2 Phase 2 — 2 Servers

```
Khi DAU > 500, tách Data Layer ra server riêng:

Server 1 — Application Server:
  CPU: 16 cores
  RAM: 32 GB
  Disk: 100 GB SSD (logs, tmp)
  Chạy: Tất cả application services + Gateway + Observability

Server 2 — Data Server:
  CPU: 8 cores
  RAM: 32 GB
  Disk: 500 GB SSD
  Chạy: MongoDB RS (3 containers), Qdrant, Redis, RabbitMQ, MinIO

Chi phí: ~$300–400/tháng (2 servers)
```

### 2.3 Phase 3 — Horizontal Scale

```
Khi DAU > 3,000, scale theo chiều ngang:

Load Balancer (1):
  Nginx / HAProxy
  Floating IP

Application Servers (3):
  CPU: 8 cores each
  RAM: 16 GB each
  Stateless — scale out/in dễ dàng

Data Server (1, nâng cấp):
  CPU: 16 cores
  RAM: 64 GB
  Disk: 2 TB SSD RAID
  MongoDB: 3 nodes trên server này (cân nhắc tách ra riêng Phase 4)

AI Orchestrator (dedicate 1 server):
  CPU: 8 cores
  RAM: 16 GB
  Disk: 50 GB
  Lý do: OpenAI API calls có latency cao, isolate để không ảnh hưởng services khác

Chi phí: ~$800–1,200/tháng
```

---

## 3. Database Capacity

### 3.1 MongoDB Storage Growth

```
Collection          | Record size avg | Growth rate     | 18-month estimate
─────────────────── | ─────────────── | ─────────────── | ──────────────────
farmers             | 2 KB            | 10,000 records  | 20 MB
farms               | 3 KB            | 25,000 records  | 75 MB
user_contexts       | 5 KB            | 10,000 records  | 50 MB
chat_sessions       | 1 KB            | 500,000 records | 500 MB
chat_messages       | 2 KB            | 5,000,000 recs  | 10 GB
disease_alerts      | 3 KB            | 100,000 records | 300 MB  (TTL 90d)
irrigation_plans    | 1 KB            | 200,000 records | 200 MB
coffee_prices       | 0.5 KB          | 50,000 records  | 25 MB
sensor_readings     | 0.5 KB          | 5,000,000 recs  | 2.5 GB  (TTL 90d)
─────────────────── | ─────────────── | ─────────────── | ──────────────────
TOTAL estimate                                           | ~14 GB raw data
With indexes (~30% overhead):                           | ~18 GB
With WiredTiger compression (~60% reduction):           | ~7 GB on disk ✅

→ 500 GB SSD đủ dùng đến Phase 3 với room để mở rộng
```

### 3.2 Qdrant Storage Growth

```
Knowledge Base (stable — không grow nhanh như user data):
  Phase 1: ~200 docs × 50 chunks × 3072 dims × 1 byte (INT8) = 31 MB
  Phase 3: ~1,000 docs × 50 chunks × 3072 dims × 1 byte     = 154 MB
  Sparse vectors:                                             + ~50 MB
  Payload (on-disk):                                          + ~100 MB
  ──────────────────────────────────────────────────────────────────────
  Total Qdrant storage Phase 3:                              ~304 MB ✅
  Total Qdrant RAM (quantized index):                        ~200 MB ✅
```

### 3.3 Redis Memory

```
Use case             | Size per key | Keys estimate   | Total
──────────────────── | ──────────── | ─────────────── | ──────
Query embedding cache | 12 KB       | 5,000 popular   | 60 MB
Session cache         | 2 KB        | 1,000 concurrent| 2 MB
Price cache           | 0.5 KB      | 10 types        | 5 KB
Rate limit counters   | 0.1 KB      | 10,000          | 1 MB
Farmer context cache  | 5 KB        | 1,000 concurrent| 5 MB
Idempotency keys      | 0.1 KB      | 50,000          | 5 MB
──────────────────── | ──────────── | ─────────────── | ──────
Total                                                  | ~74 MB

→ Redis maxmemory: 512 MB đủ dùng Phase 3
  allkeys-lru policy đảm bảo không bao giờ OOM
```

---

## 4. Network & Bandwidth

### 4.1 Inbound Traffic

```
Source               | Average size | Rate (Phase 3 peak) | Bandwidth
──────────────────── | ──────────── | ─────────────────── | ─────────
Text queries         | 500 B        | 100 req/s           | 0.4 Mbps
Image uploads        | 2 MB         | 5 req/s             | 80 Mbps  ← dominant
IoT sensor data      | 200 B        | 30 readings/s       | 0.05 Mbps
Zalo webhook         | 1 KB         | 20 req/s            | 0.16 Mbps
──────────────────── | ──────────── | ─────────────────── | ─────────
Total inbound peak:                                        | ~81 Mbps

→ 100 Mbps uplink đủ Phase 1–2
→ Phase 3: cân nhắc CDN cho image upload (MinIO presigned URL → bypass app server)
```

### 4.2 Outbound Traffic

```
Destination          | Average size | Rate (Phase 3 peak) | Bandwidth
──────────────────── | ──────────── | ─────────────────── | ─────────
AI responses (text)  | 1.5 KB       | 100 req/s           | 1.2 Mbps
Zalo push (text)     | 500 B        | 50 req/s            | 0.2 Mbps
FCM push             | 300 B        | 50 req/s            | 0.12 Mbps
OpenAI API calls     | 2 KB prompt  | 100 req/s           | 1.6 Mbps
──────────────────── | ──────────── | ─────────────────── | ─────────
Total outbound peak:                                       | ~4 Mbps
```

---

## 5. OpenAI API Cost Projection

```
Component            | Unit cost      | Phase 1      | Phase 3 (DAU 6,000)
──────────────────── | ────────────── | ──────────── | ────────────────────
GPT-4o input         | $2.50/1M tok   | $15/mo       | $900/mo
GPT-4o output        | $10.00/1M tok  | $30/mo       | $1,800/mo
text-embedding-3-lg  | $0.13/1M tok   | $5/mo        | $50/mo
gpt-4o-mini (intent) | $0.15/1M tok   | $2/mo        | $60/mo
──────────────────── | ────────────── | ──────────── | ────────────────────
Total OpenAI/month                    | ~$52/mo      | ~$2,810/mo

Cost per farmer/month:
  Phase 1: $52 / 60 DAU = $0.87/farmer/month
  Phase 3: $2,810 / 6,000 DAU = $0.47/farmer/month (economy of scale)

Optimization levers:
  1. Cache popular queries (40% hit rate → -40% cost)
  2. Use gpt-4o-mini for simple intents (-70% input cost)
  3. Shorten system prompt (-20% tokens)
  4. Phase 4: Fine-tuned smaller model (-60% total cost)
```

---

## 6. SLO Targets per Phase

| Metric | Phase 1 | Phase 2 | Phase 3 |
|--------|---------|---------|---------|
| API Availability | 99.0% | 99.5% | 99.5% |
| Chat response P50 | < 3s | < 2s | < 2s |
| Chat response P95 | < 8s | < 5s | < 3s |
| Notification delivery | < 30s | < 15s | < 10s |
| IoT ingestion lag | < 5s | < 3s | < 2s |
| DB query P95 | < 200ms | < 100ms | < 50ms |
| Qdrant search P95 | < 500ms | < 300ms | < 200ms |

---

## 7. Scale Triggers — Khi nào cần hành động

| Metric | Ngưỡng cảnh báo | Hành động |
|--------|----------------|----------|
| CPU usage avg > 70% / 15 phút | Liên tục 1 tuần | Upgrade server hoặc add node |
| RAM usage > 80% | Liên tục 3 ngày | Add RAM hoặc optimize service |
| MongoDB disk > 70% | — | Add disk hoặc enable TTL aggressive |
| Qdrant RAM > 80% | — | Tăng quantization hoặc add node |
| Chat P95 > 5s | Liên tục 1 giờ | Investigate bottleneck (DB? OpenAI? RAG?) |
| OpenAI cost > $2,000/tháng | — | Bật caching aggressive, xem xét fine-tuning |
| DLQ > 1,000 messages | — | Điều tra consumer errors, scale nếu cần |

---

## 8. Disaster Recovery Targets

| Scenario | RPO | RTO | Recovery Action |
|---------|-----|-----|----------------|
| App service crash | 0 | < 1 min | Docker restart policy |
| Single server down | < 1h | < 30 min | Restore từ backup |
| MongoDB Primary fail | 0 | < 10s | Automatic RS failover |
| Data corruption | < 24h | < 2h | mongorestore từ daily backup |
| Full server loss | < 24h | < 4h | Provision new server + restore |

---

*Tài liệu review hàng quý và khi DAU tăng 2x so với lần review trước.*

**© 2026 Bazan AI Project — Confidential**

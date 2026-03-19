# Developer Onboarding Guide — Bazan AI
## Hướng dẫn cho Developer mới tham gia dự án

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Engineering Lead |
| **Đối tượng** | Developer mới — Backend (.NET), AI/ML (Python), Mobile (Flutter) |
| **Liên quan** | Coding Standards, Git Workflow, Testing Strategy |

---

## Tuần 1: Checklist Day 1

```
□ Nhận laptop + thiết bị từ IT
□ Tạo tài khoản: GitHub (bazanai org), Slack, Jira, Seq, Grafana
□ SSH key → gửi cho DevOps để cấp quyền staging server
□ Clone repositories:
    git clone https://github.com/bazanai/infrastructure.git
    git clone https://github.com/bazanai/bazan-ai-backend.git
    git clone https://github.com/bazanai/bazan-ai-mobile.git

□ Đọc trong ngày 1:
    - README.md (repo root)
    - docs/architecture/system-architecture.md
    - ADR-001 đến ADR-005
    - Coding Standards & Style Guide

□ Setup môi trường local (xem Section 2)
□ Chạy được stack local trước cuối ngày 2
□ 1-on-1 với Engineering Lead (60 phút) — ngày 1
□ Meet với Product Lead (30 phút) — ngày 2
□ Meet với AI/ML Lead nếu relevant (30 phút) — ngày 3
```

---

## 1. Tổng quan dự án trong 5 phút

```
Bazan AI = AI chatbot tư vấn nông nghiệp cà phê cho nông dân Tây Nguyên

Stack chính:
  Backend:   .NET 8 / C# — Clean Architecture — 10 microservices
  AI/ML:     Python 3.12 / FastAPI — RAG với Qdrant
  Mobile:    Flutter (iOS + Android)
  Database:  MongoDB (primary), Qdrant (vector), Redis (cache)
  Messaging: RabbitMQ + MassTransit
  AI:        OpenAI GPT-4o + Semantic Kernel
  DevOps:    GitHub Actions + Docker Compose

Môi trường:
  Local:    Docker Compose trên máy developer
  Staging:  Server staging.bazanai.vn (auto-deploy từ develop branch)
  Prod:     Server api.bazanai.vn (manual approval)
```

---

## 2. Setup Môi trường Local

### 2.1 Prerequisites

```bash
# Bắt buộc
- Docker Desktop (hoặc OrbStack trên Mac) >= 24.0
- Docker Compose v2 (docker compose, không phải docker-compose)
- Git >= 2.40
- Make

# Cho Backend (.NET developer)
- .NET 8 SDK
- Visual Studio 2022 / Rider / VS Code + C# extension

# Cho AI/ML (Python developer)
- Python 3.12 (dùng pyenv để manage version)
- uv hoặc pip

# Cho Mobile (Flutter developer)
- Flutter 3.19+
- Android Studio / Xcode
- Android emulator hoặc thiết bị thật

# Optional nhưng hữu ích
- k9s (Kubernetes CLI UI — dùng cho staging K8s)
- mongosh (MongoDB shell)
- redis-cli
```

### 2.2 Clone và Setup

```bash
# 1. Clone infrastructure repo
git clone https://github.com/bazanai/infrastructure.git
cd infrastructure

# 2. Copy và điền .env
cp infra/.env.example infra/.env
# Lấy staging values từ 1Password vault "Bazan AI Dev"
# Hoặc hỏi DevOps Lead

# 3. Generate MongoDB keyfile (lần đầu)
make gen-keyfile

# 4. Khởi động toàn bộ stack
make up

# 5. Đợi tất cả healthy (khoảng 60 giây)
docker compose ps

# 6. Initialize MongoDB Replica Set (lần đầu)
make init-rs

# 7. Verify
make health
# Expected: {"status":"healthy", "services": {"identity-svc":"healthy", ...}}
```

### 2.3 Verify Stack hoạt động

```bash
# API Gateway
curl http://localhost:8080/healthz

# MongoDB
docker exec bazan-mongo-1 mongosh --eval "rs.status().myState"
# Expected: 1 (PRIMARY)

# Qdrant
curl http://localhost:6333/healthz

# RabbitMQ UI (mở browser)
open http://localhost:15672
# Login: admin / (password từ .env)

# Seq logs (mở browser)
open http://localhost:5341

# Grafana (mở browser)
open http://localhost:3000
# Login: admin / (password từ .env)
```

### 2.4 Run một service cụ thể từ source (hot reload)

```bash
# Thay vì chạy container image, run từ source để dev
# Ví dụ: Identity Service

# Stop container version
docker compose stop identity-svc

# Run từ source (terminal riêng)
cd src/Services/BazanAI.Identity/BazanAI.Identity.API
dotnet watch run --no-launch-profile

# Kết nối với MongoDB trong Docker
# Connection string tự lấy từ appsettings.Development.json
# Hoặc set env variable
export MongoDB__ConnectionString="mongodb://admin:password@localhost:27017/bazan_identity?replicaSet=rs0&authSource=admin"
```

---

## 3. Kiến trúc Code — Nhập môn

### 3.1 Solution Structure

```
BazanAI.sln
├── src/
│   ├── Services/
│   │   ├── BazanAI.Gateway/          ← YARP API Gateway
│   │   ├── BazanAI.Orchestrator/     ← AI Agent (Semantic Kernel)
│   │   ├── BazanAI.Identity/         ← Auth & Farmer profile
│   │   │   ├── Domain/               ← Entities, ValueObjects, Events
│   │   │   ├── Application/          ← Commands, Queries (MediatR)
│   │   │   ├── Infrastructure/       ← MongoDB, RabbitMQ implementations
│   │   │   └── API/                  ← Controllers, Program.cs
│   │   ├── BazanAI.Agronomy/         ← 6-factor farming logic
│   │   ├── BazanAI.Market/           ← Price & ROI
│   │   ├── BazanAI.Conversations/    ← Chat history & RLHF
│   │   └── ... (các services khác)
│   ├── Python/
│   │   └── bazan-knowledge-rag/      ← Python RAG service
│   └── SharedKernel/
│       ├── BazanAI.SharedKernel/     ← Domain primitives
│       └── BazanAI.Infrastructure.Common/  ← Shared infra
└── tests/
    ├── Unit/
    ├── Integration/
    └── E2E/
```

### 3.2 Luồng request điển hình

```
Mobile App
  → POST /api/v1/auth/login
  → API Gateway (YARP) — validate JWT, route request
  → Identity Service
     → RegisterFarmerCommandHandler (MediatR)
        → FarmerAggregate.Register()
        → FarmerRepository.SaveAsync() [MongoDB]
        → EventPublisher.Publish(FarmerRegisteredEvent) [RabbitMQ]
  → Response 201 Created
```

### 3.3 Thêm feature mới — Quy trình

```
1. Tạo Command/Query trong Application layer
   src/Services/BazanAI.{Service}/{Service}.Application/Commands/MyFeature/

2. Implement Handler

3. Thêm Controller endpoint trong API layer

4. Viết Unit Test cho Handler

5. Viết Integration Test nếu có DB interaction

6. Update OpenAPI spec trong docs/api/openapi/

7. PR → Code Review → Merge
```

---

## 4. Workflows Quan trọng

### 4.1 Tạo feature mới

```bash
# 1. Luôn bắt đầu từ develop
git checkout develop
git pull origin develop

# 2. Tạo feature branch
git checkout -b feature/US-042-disease-alert-zalo

# 3. Code + commit thường xuyên
git add .
git commit -m "feat(notification): add Zalo channel for disease alerts"

# 4. Push và tạo PR
git push origin feature/US-042-disease-alert-zalo
# Mở PR trên GitHub → target: develop

# 5. PR phải pass CI trước khi review
# 6. Cần 2 reviewers approve
# 7. Merge → auto deploy lên staging
```

### 4.2 Debug một lỗi trên staging

```bash
# 1. Xem logs gần đây
ssh deploy@staging.bazanai.vn
cd /opt/bazanai
docker logs bazan-agronomy-svc --tail 100 --follow

# 2. Hoặc dùng Seq (thoải mái hơn)
# http://staging.bazanai.vn:5341 (qua SSH tunnel)
ssh -L 5341:localhost:5341 deploy@staging.bazanai.vn

# 3. Search theo CorrelationId
# Lấy CorrelationId từ X-Correlation-Id header trong response

# 4. Check RabbitMQ queue depth
ssh -L 15672:localhost:15672 deploy@staging.bazanai.vn
# Mở http://localhost:15672
```

### 4.3 Run tests

```bash
# Unit tests
dotnet test tests/Unit/ -v

# Integration tests (cần Docker)
dotnet test tests/Integration/ -v

# Python tests (RAG service)
cd src/Python/bazan-knowledge-rag
pytest tests/ -v --asyncio-mode=auto

# All tests
make test

# Test với coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory coverage
reportgenerator -reports:coverage/**/*.xml -targetdir:coverage/html
open coverage/html/index.html
```

---

## 5. Tài nguyên cần biết

### 5.1 Internal Documentation

| Tài liệu | Link | Đọc khi nào |
|---------|------|------------|
| System Architecture | `docs/architecture/system-architecture.md` | Ngày 1 |
| ADR-001 to ADR-005 | `docs/architecture/ADR-*.md` | Ngày 1–2 |
| Coding Standards | `docs/team/coding-standards.md` | Ngày 1 |
| Git Workflow | `docs/team/git-workflow.md` | Ngày 1 |
| RAG Pipeline Design | `docs/ai/rag-pipeline-design.md` | Tuần 2 (AI dev) |
| API Specs | `docs/api/openapi/` | Khi làm việc với API |

### 5.2 External Resources Quan trọng

| Resource | URL | Tại sao cần |
|---------|-----|------------|
| .NET 8 Docs | learn.microsoft.com | Backend development |
| Semantic Kernel | learn.microsoft.com/semantic-kernel | AI orchestration |
| MediatR | github.com/jbogard/MediatR | CQRS pattern |
| MassTransit | masstransit.io | RabbitMQ integration |
| Qdrant Docs | qdrant.tech/documentation | Vector DB |
| FastAPI | fastapi.tiangolo.com | Python RAG service |

### 5.3 Slack Channels

| Channel | Mục đích |
|---------|---------|
| `#engineering` | Thảo luận kỹ thuật chung |
| `#bazan-alerts` | Monitoring alerts (đọc, không spam) |
| `#deployment` | Thông báo deploy |
| `#incident` | Khi có incident |
| `#ai-quality` | Chất lượng AI, feedback |
| `#random` | Giao lưu |

---

## 6. Conventions Quan trọng

```
Commits: Conventional Commits
  feat(scope):    feature mới
  fix(scope):     bug fix
  refactor:       refactor không thay đổi behavior
  test:           thêm/sửa tests
  docs:           chỉ documentation
  chore:          build, CI, dependencies

Branch naming:
  feature/US-{jira-id}-{short-description}
  bugfix/BUG-{jira-id}-{short-description}
  hotfix/HOTFIX-{jira-id}-{short-description}

PR Title: [US-{id}] Mô tả ngắn gọn (tiếng Anh)

Logging: Luôn dùng structured logging
  logger.LogInformation("Message {Param1} {Param2}", param1, param2);
  KHÔNG dùng: logger.LogInformation($"Message {param1}");
```

---

## 7. First Week Milestones

| Ngày | Milestone |
|------|----------|
| 1 | Đọc xong architecture docs, setup môi trường |
| 2 | Stack local chạy được, viết xong first commit (fix small thing) |
| 3 | Hiểu luồng request end-to-end (trace 1 API call từ đầu đến cuối) |
| 5 | Complete first real task (small feature hoặc bug fix) |

**Nếu gặp khó khăn:** Đừng tự loay hoay > 30 phút. Hỏi ngay trên `#engineering`.

---

**© 2026 Bazan AI — Internal Document**

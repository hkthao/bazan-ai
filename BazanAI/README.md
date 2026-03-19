# Bazan AI (BazanAI) ☕🤖
### Agentic Microservices Platform for Coffee Farmers in Vietnam's Central Highlands

[![CI](https://github.com/hkthao/bazan-ai/actions/workflows/ci.yml/badge.svg)](https://github.com/hkthao/bazan-ai/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Stack: .NET 8](https://img.shields.io/badge/Backend-.NET%208-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Stack: FastAPI](https://img.shields.io/badge/AI_Service-FastAPI-green)](https://fastapi.tiangolo.com/)

**Bazan AI** là một nền tảng Microservices thông minh (Agentic AI) được thiết kế chuyên biệt để hỗ trợ nông dân trồng cà phê tại vùng Tây Nguyên Việt Nam. Hệ thống kết hợp dữ liệu từ cảm biến IoT hiện trường, dự báo thời tiết và tri thức nông nghiệp chuyên sâu để cung cấp các giải pháp tư vấn tự động, chính xác và bảo mật.

---

## 🏗 Kiến trúc Hệ thống (System Architecture)

Bazan AI tuân thủ nguyên tắc **Clean Architecture** (Domain-Driven Design) với kiến trúc Microservices phân tán, đảm bảo khả năng mở rộng và bảo mật dữ liệu cấp doanh nghiệp (Enterprise Grade).

```ascii
                      [ Mobile App / Zalo Mini App ]
                                     |
                          [ API Gateway (YARP) ]
                                     |
         +---------------------------+---------------------------+
         |                           |                           |
  [ Identity Svc ]            [ Agronomy Svc ]            [ Market Svc ]
  [ Conversation Svc ]        [ Orchestrator ]            [ Weather Svc ]
  [ IoT Ingestion ]           [ Media Svc ]               [ Scheduler ]
         |                           |                           |
         +-------------+-------------+-------------+-------------+
                       |                           |
               [ RabbitMQ Bus ]             [ Shared Kernel ]
                       |                           |
         +-------------+-------------+-------------+-------------+
         |                           |                           |
   [ MongoDB RS ]              [ Qdrant Vector ]            [ Redis ]
```

### Các Dịch vụ Lõi:
- **Identity:** Quản lý nông dân, trang trại và xác thực (OTP/JWT).
- **Agronomy:** Chuẩn đoán sâu bệnh, lập lịch tưới tiêu và phân tích đất.
- **Orchestrator:** "Bộ não" AI sử dụng **Microsoft Semantic Kernel** để điều phối Agent.
- **Knowledge RAG:** Dịch vụ Python/FastAPI thực hiện Retrieval-Augmented Generation trên Qdrant.
- **IoT Ingestion:** Tiếp nhận dữ liệu cảm biến thời gian thực qua giao thức MQTT.

---

## 🛠 Tech Stack

- **Backend:** .NET 8 (C#), Python 3.12 (FastAPI).
- **Messaging:** RabbitMQ (MassTransit) cho giao tiếp bất đồng bộ (Event-driven).
- **Data Stores:** MongoDB (Replica Set), Qdrant (Vector DB), Redis (Caching).
- **AI/ML:** Microsoft Semantic Kernel, OpenAI text-embedding-3-large.
- **Storage:** MinIO (S3 compatible) cho ảnh và tài liệu tri thức.
- **Observability:** OpenTelemetry, Serilog, Seq, Prometheus, Grafana.
- **Gateway:** YARP (Yet Another Reverse Proxy) hỗ trợ Rate Limiting & Auth.

---

## 📂 Cấu trúc Dự án (Project Structure)

```
BazanAI/
├── src/
│   ├── Services/        # Các Microservices .NET (.NET 8)
│   ├── Python/          # Các dịch vụ AI/RAG (FastAPI)
│   └── SharedKernel/    # Thư viện dùng chung (Domain Primitives, Events)
├── infra/               # Docker, Scripts khởi tạo DB, RabbitMQ config
├── docs/                # Tài liệu chi tiết (ADR, Diagrams, Roadmap)
├── tests/               # Unit, Integration và E2E tests
├── .github/             # GitHub Actions & Issue Templates
├── Makefile             # Task runner cho các lệnh phổ biến
└── BazanAI.sln         # Solution chính (.NET)
```

---

## 🚀 Bắt đầu (Getting Started)

### Tiền đề (Prerequisites)
- Docker & Docker Compose
- .NET 8 SDK
- Python 3.12+
- Make (tùy chọn)

### Khởi chạy Nhanh
1. **Clone repository:**
   ```bash
   git clone https://github.com/hkthao/bazan-ai.git
   cd bazan-ai
   ```

2. **Khởi động Hạ tầng:**
   ```bash
   make up
   ```

3. **Khởi tạo Database (MongoDB Replica Set):**
   ```bash
   make migrate
   ```

4. **Kiểm tra trạng thái:**
   ```bash
   docker ps
   ```

---

## 📅 Lộ trình Phát triển (Product Roadmap)

Dự án được chia thành 12 Sprint phát triển. Chi tiết xem tại: [docs/product-business/product-roadmap.md](BazanAI/docs/product-business/product-roadmap.md)

- **Giai đoạn 1:** Nền tảng & Bảo mật (Sprint 1-3)
- **Giai đoạn 2:** Chất lượng dữ liệu & IoT (Sprint 4-6)
- **Giai đoạn 3:** AI Agent & Tri thức số (Sprint 7-9)
- **Giai đoạn 4:** Thị trường & Pilot Launch (Sprint 10-12)

---

## 🤝 Đóng góp & Quy chuẩn (Contribution)

Chúng tôi hoan nghênh mọi đóng góp. Vui lòng tuân thủ các quy chuẩn sau:
- **Coding Standards:** Tham khảo [docs/team/coding-standards.md](BazanAI/docs/team/coding-standards.md).
- **GitHub Issues:** Sử dụng các [Issue Templates](.github/ISSUE_TEMPLATE/) khi tạo Task/Bug/Feature.
- **Git Workflow:** `main` branch là stable, phát triển trên `develop` hoặc feature branches.

---

## 📜 Giấy phép (License)
Dự án được phát hành dưới giấy phép [MIT](LICENSE).

**© 2026 Bazan AI — Empowering Coffee Farmers with AI.**

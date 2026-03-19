# Product Roadmap — Bazan AI
## Agentic Microservices Platform for Coffee Farmers

| Thông tin | Giá trị |
|-----------|---------|
| **Phiên bản** | 1.1.0 (Enterprise Grade) |
| **Ngày cập nhật** | 2026-03-19 |
| **Trạng thái** | Draft - Approved for Phase 1 |
| **Tiêu chuẩn** | IBM/Enterprise Architecture |

---

## 1. Tầm nhìn Chiến lược (Strategic Vision)
Xây dựng một hệ thống hỗ trợ nông nghiệp thông minh, bảo mật và bền bỉ, giúp nông dân Tây Nguyên tối ưu hóa quy trình canh tác và kinh tế thông qua dữ liệu thực địa và trí tuệ nhân tạo (Agentic AI).

---

## 2. Các Giai đoạn Phát triển (Phases)

### Giai đoạn 1: Nền tảng & Bảo mật (Foundation & Security)
*   Thiết lập hạ tầng Microservices, xác thực và quyền sở hữu dữ liệu (Data Sovereignty).
*   **KPI:** Hệ thống lõi sẵn sàng, 100% dữ liệu nhạy cảm được mã hóa.

### Giai đoạn 2: Chất lượng Dữ liệu & IoT (Data Quality & IoT)
*   Kết nối cảm biến hiện trường, làm sạch dữ liệu rác (Data Validation) và hỗ trợ Offline-first.
*   **KPI:** Độ chính xác dữ liệu cảm biến > 95%, hoạt động được ở vùng sóng yếu.

### Giai đoạn 3: Trí tuệ Nhân tạo & Tri thức (AI & Knowledge)
*   Kích hoạt Agentic AI với Semantic Kernel và RAG (Retrieval-Augmented Generation).
*   **KPI:** Phản hồi AI chính xác > 85% dựa trên tài liệu kỹ thuật nông nghiệp.

### Giai đoạn 4: Kinh tế & Thông báo (Economics & Notifications)
*   Tích hợp dữ liệu thị trường, tính toán ROI và thông báo đa kênh (Zalo/FCM/SMS).
*   **KPI:** Cung cấp thông tin giá thời gian thực với độ trễ < 5 phút.

---

## 3. Kế hoạch Sprint Chi tiết (12 Sprints)

### Sprint 1: Infrastructure & Shared Kernel
*   Thiết lập CI/CD Pipelines, Shared Kernel (.NET/Python).
*   Cấu hình API Gateway (YARP) cơ bản.

### Sprint 2: Identity, Privacy & Data Sovereignty
*   Xác thực OTP (Zalo/SMS), JWT.
*   Mã hóa PII (AES-256) và thiết lập cơ chế Consent Management (Quyền đồng ý của nông dân).

### Sprint 3: Resilient Messaging & Notifications
*   Triển khai Outbox Pattern cho RabbitMQ để đảm bảo tin nhắn không bị mất.
*   Tích hợp Zalo OA API & Template Engine.

### Sprint 4: IoT Ingestion & Data Quality Assurance (DQA)
*   MQTT Broker & Sensor Topic Router.
*   Lớp lọc dữ liệu nhiễu (Validation Layer), lọc GPS (Kalman Filter).
*   Cơ chế Dead Letter Queue (DLQ) cho dữ liệu lỗi.

### Sprint 5: Offline-first Architecture
*   Chiến lược Local Persistence (SQLite) trên thiết bị di động.
*   Sync Manager: Đồng bộ hóa thông minh khi có mạng (Background Sync).

### Sprint 6: Agronomy Engine V1
*   Thuật toán tưới tiêu, bón phân.
*   Lập lịch canh tác dựa trên giống cây (Robusta/Arabica).

### Sprint 7: Knowledge Base & Vector Indexing
*   RAG Pipeline: PDF Parsing, Chunking & Embedding.
*   Lưu trữ Vector vào Qdrant (Hybrid Search).

### Sprint 8: AI Orchestrator (Semantic Kernel)
*   Định nghĩa Plugins cho Agronomy, Weather, Market.
*   Cơ chế Intent Classification (Phân loại ý định).

### Sprint 9: Real-time Agentic Chat
*   SignalR Hub cho streaming phản hồi AI.
*   Cơ chế phản hồi người dùng (Up/Down vote) cho RLHF.

### Sprint 10: Market & Price Monitoring
*   Sync giá cà phê ICE Futures & Đại lý địa phương.
*   Công cụ tính ROI & Tìm kiếm người mua (Geo-query).

### Sprint 11: Proactive Alerts & Scheduler
*   Hangfire Jobs cho báo cáo tuần.
*   Cảnh báo dịch bệnh vùng dựa trên dữ liệu tổng hợp.

### Sprint 12: Enterprise Observability & Pilot
*   Giám sát SLA, Seq, Grafana.
*   Diễn tập khôi phục sau sự cố (Disaster Recovery).
*   **Pilot Launch:** Triển khai thử nghiệm 50 hộ nông dân.

---

## 4. Các Trụ cột Enterprise (Enterprise Pillars)

### 4.1 Data Validation Layer
Mọi dữ liệu từ sensor/người dùng phải đi qua lớp kiểm chứng (Schema validation, Range check, Logic check) trước khi được AI sử dụng để tránh "Garbage In, Garbage Out".

### 4.2 Privacy by Design
Dữ liệu vườn của nông dân là tài sản riêng. Hệ thống mặc định không chia sẻ dữ liệu chi tiết nếu không có sự đồng ý tường minh.

### 4.3 Resilience & Offline-first
Hệ thống phải chịu được sự gián đoạn kết nối tại Tây Nguyên bằng cách xử lý ngầm (Asynchronous) và đồng bộ trễ.

---

## 5. Chỉ số Đo lường (KPIs)
*   **Latency:** < 2s cho luồng AI SignalR.
*   **Reliability:** 99.9% cho các dịch vụ Identity & IoT Ingestion.
*   **Accuracy:** > 90% độ chính xác cho các cảnh báo bệnh/tưới tiêu.
*   **User Retention:** > 60% nông dân quay lại sử dụng sau tuần đầu tiên.

---
**© 2026 Bazan AI — Confidential Business Document**

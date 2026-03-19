name: "🤖 Technical Task (AI Agent Only)"
about: "Dành riêng cho AI thực hiện code chi tiết cho các tầng Domain, Application, hoặc Infrastructure."
title: "[TECH] <Tên tác vụ kỹ thuật>"
labels: ["technical", "internal-ai"]
assignees: ""

---

## 🏗 Context & Service Layer
- **Service:** <Tên Microservice - ví dụ: Identity Service>
- **Layer:** <Domain / Application / Infrastructure / API>
- **Issue Reference:** <Tham chiếu User Story hoặc Bug liên quan>

## 🛠 Implementation Details (Files & Logic)
1. **Create/Update Files:**
   - [ ] `src/Services/.../<FileName>.cs`
   - [ ] `src/Services/.../<CommandHandler>.cs`
2. **Logic Description:**
   - <Mô tả logic chính cần thực hiện>
   - <Lưu ý đặc biệt cho AI về Clean Architecture (GEMINI.md)>

## 📦 Dependencies
- **Libraries:** <Ví dụ: MediatR, MassTransit, MongoDB.Driver>
- **Internal Services:** <Dependency đến các Service khác qua RabbitMQ/HTTP>

## 🧪 Definition of Done (DoD)
- [ ] Code tuân thủ coding standards trong `GEMINI.md`.
- [ ] Unit Test đã pass (xUnit/Pytest).
- [ ] Swagger/OpenAPI spec đã được cập nhật (nếu là API).
- [ ] Đã kiểm tra không có log dữ liệu PII/Sensitive.

## 📝 Code Review Checklist for AI
- Logic đúng với AC đề ra chưa?
- Có vi phạm ranh giới (Boundaries) giữa Domain và Infrastructure không?
- Đã xử lý các trường hợp null/empty chưa?

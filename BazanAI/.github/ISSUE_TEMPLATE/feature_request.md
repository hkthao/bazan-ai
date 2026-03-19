name: "🚀 Feature Request"
about: "Đề xuất tính năng mới hoặc thay đổi nghiệp vụ dưới dạng User Story."
title: "[FEAT] <Tiêu đề tính năng>"
labels: ["enhancement", "product"]
assignees: ""

---

## 📝 User Story
- **As a:** <Vai trò người dùng - ví dụ: Nông dân, Chuyên gia nông nghiệp>
- **I want to:** <Hành động muốn thực hiện>
- **So that:** <Giá trị nhận được>

## ✅ Acceptance Criteria (AC)
- [ ] <Tiêu chí 1: Điều kiện để tính năng được coi là hoàn thành>
- [ ] <Tiêu chí 2>
- [ ] <Luồng xử lý lỗi/Edge cases>

## 🛠 Technical Notes (Microservices Context)
- **Affected Services:** <Ví dụ: Identity Service, Agronomy Service>
- **Communication:** <Ví dụ: RabbitMQ Event (FarmerRegistered), SignalR Hub>
- **Contract Changes:** <Mô tả nếu có thay đổi trong API hoặc Message Schema>

## 🎨 UI/UX Requirements (Optional)
- <Mô tả giao diện hoặc đính kèm ảnh mockup/figma nếu có>

## 🧪 Testing Strategy
- [ ] Unit Test cho Domain Logic.
- [ ] Integration Test cho RabbitMQ flow.
- [ ] Kiểm thử thủ công trên thiết bị thực (nếu liên quan đến IoT/Mobile).

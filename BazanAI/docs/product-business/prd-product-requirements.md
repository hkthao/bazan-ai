# Product Requirements Document (PRD) — Bazan AI
## Nền tảng AI Tư vấn Nông nghiệp Cà phê Tây Nguyên

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Product Owner** | CEO / Co-founder |
| **Tác giả** | Product Lead · Principal Architect |
| **Người review** | CTO, Business Lead, WASI Collaboration |
| **Trạng thái** | Approved — Phase 1 |

---

## 1. Tóm tắt điều hành (Executive Summary)

Bazan AI là nền tảng trí tuệ nhân tạo hướng đến **nông dân trồng cà phê tại Tây Nguyên, Việt Nam** — khu vực sản xuất 40% sản lượng cà phê cả nước với hơn 600.000 hộ canh tác.

**Vấn đề cốt lõi:** Phần lớn nông dân cà phê thiếu tiếp cận kịp thời đến tri thức kỹ thuật chất lượng cao. Cán bộ khuyến nông không đủ để phục vụ số lượng lớn hộ nông dân, tài liệu WCR và WASI bằng tiếng Anh hoặc ngôn ngữ học thuật khó tiếp cận. Hậu quả: 25–40% năng suất tiềm năng bị mất do canh tác chưa tối ưu.

**Giải pháp:** Bazan AI đưa tri thức nông nghiệp đẳng cấp quốc tế đến tay từng nông dân qua giao diện chat tự nhiên bằng tiếng Việt, cá nhân hóa theo điều kiện từng vườn, sẵn sàng 24/7 ngay trên điện thoại.

**Cơ hội thị trường:**
- 600.000 hộ nông dân cà phê Tây Nguyên
- Giá trị thị trường cà phê Việt Nam: ~4.2 tỷ USD/năm
- Cải thiện 10% năng suất → tăng thu nhập ~400 USD/hộ/năm

---

## 2. Mục tiêu sản phẩm

### 2.1 Objectives & Key Results (OKRs)

**Phase 1 (Tháng 1–3) — Validate product-market fit:**

| Objective | Key Result | Target |
|-----------|-----------|--------|
| Nông dân thấy giá trị thực | Thumbs-up rate | ≥ 70% |
| Engagement thực sự | DAU/MAU ratio | ≥ 30% |
| Chất lượng AI đủ tin cậy | Expert accuracy score | ≥ 4.0/5.0 |
| Prove retention | D30 retention | ≥ 40% |

**Phase 2 (Tháng 4–6) — Scale to product:**

| Objective | Key Result | Target |
|-----------|-----------|--------|
| Grow user base | Registered farmers | 1,000 |
| Revenue validation | Paying HTX partners | ≥ 3 |
| Platform robustness | API uptime | ≥ 99.5% |
| Word-of-mouth | Referral rate | ≥ 20% |

### 2.2 Non-goals (không làm trong Phase 1)

- Không hỗ trợ cây trồng khác ngoài cà phê
- Không có marketplace mua bán cà phê
- Không build IoT hardware
- Không có features cho phân phối, xuất khẩu

---

## 3. User Personas

### Persona 1: Anh An — Nông dân tự canh (Primary)

```
Tuổi: 42 | Vườn: 1.5 ha | Đắk Lắk
Giống: Robusta + TR4 | Kinh nghiệm: 15 năm
Điện thoại: Samsung Galaxy A, Android 11
Mạng: 3G (vườn), 4G (nhà)

Đau điểm:
  - Không biết phân biệt bệnh gỉ sắt và thiếu dinh dưỡng
  - Mua phân bón theo thói quen, không theo khoa học
  - Giá cà phê xuống thì không biết nên giữ hay bán

Mục tiêu dùng Bazan AI:
  - Nhận diện bệnh nhanh để phun thuốc kịp thời
  - Biết lịch bón phân đúng giai đoạn
  - Theo dõi giá và nhận cảnh báo khi giá tốt
```

### Persona 2: Chị Lan — Nông dân kết nối HTX (Secondary)

```
Tuổi: 38 | Vườn: 3 ha | Lâm Đồng
Giống: Arabica | Chứng nhận: VietGAP (đang xin)
Điện thoại: iPhone 12 | Mạng: 4G ổn định

Đau điểm:
  - Cần ghi chép nhật ký canh tác cho chứng nhận VietGAP
  - Muốn ROI analysis để báo cáo HTX
  - Cần tư vấn chuyên sâu hơn nông dân thông thường

Mục tiêu dùng Bazan AI:
  - Ghi nhật ký canh tác có cấu trúc
  - Tính toán chi phí-lợi nhuận
  - Hỏi kỹ thuật chuyên sâu (bón phân sinh học, chứng nhận)
```

### Persona 3: Anh Minh — Cán bộ HTX (Tertiary)

```
Tuổi: 35 | Quản lý: 120 hộ thành viên | Đắk Nông
Học vấn: Đại học nông nghiệp
Điện thoại: iPhone 14 | Mạng: 4G, Wifi văn phòng

Đau điểm:
  - Không thể tư vấn 120 hộ cùng lúc
  - Cần dashboard xem nhanh tình trạng của tất cả vườn
  - Báo cáo mùa vụ tốn thời gian

Mục tiêu dùng Bazan AI:
  - Theo dõi cảnh báo sâu bệnh toàn HTX
  - Gửi thông báo hàng loạt cho thành viên
  - Export báo cáo mùa vụ tự động
```

---

## 4. User Stories (Priority Ordered)

### Epic 1: AI Chat Core

```
US-001 [Must-have] Hỏi câu hỏi kỹ thuật
  As a: Nông dân
  I want: Gửi câu hỏi về canh tác bằng tiếng Việt thông thường
  So that: Nhận câu trả lời chính xác, có nguồn tham khảo

  Acceptance Criteria:
  - Response time P95 < 3 giây
  - Câu trả lời kèm citation từ tài liệu WCR/WASI
  - Hiển thị typing indicator khi AI đang xử lý
  - Có thể hỏi tiếp trong cùng session (multi-turn)

US-002 [Must-have] Chụp ảnh hỏi bệnh
  As a: Nông dân
  I want: Chụp ảnh lá/quả bệnh và hỏi trực tiếp
  So that: Được chẩn đoán nhanh ngay tại vườn

  Acceptance Criteria:
  - Upload ảnh từ camera hoặc gallery
  - AI phân tích và trả lời trong < 5 giây
  - Hiển thị confidence score của chẩn đoán
  - Yêu cầu thêm ảnh nếu không rõ

US-003 [Must-have] Xem lịch sử hội thoại
  As a: Nông dân
  I want: Xem lại câu trả lời từ tuần trước
  So that: Không phải hỏi lại cùng câu hỏi

  Acceptance Criteria:
  - Lịch sử chat lưu tối thiểu 2 năm
  - Tìm kiếm trong lịch sử
  - Hoạt động offline (cache 20 sessions gần nhất)
```

### Epic 2: Thông tin Thị trường

```
US-010 [Must-have] Xem giá cà phê
  As a: Nông dân
  I want: Biết giá cà phê hôm nay
  So that: Quyết định bán hay giữ

  Acceptance Criteria:
  - Cập nhật ít nhất 3 lần/ngày
  - Hiển thị xu hướng 30 ngày (biểu đồ đơn giản)
  - So sánh với giá hòa vốn ước tính của nông dân

US-011 [High] Nhận cảnh báo giá
  As a: Nông dân
  I want: Nhận thông báo Zalo khi giá cà phê tăng > 5%
  So that: Không bỏ lỡ cơ hội bán giá tốt

US-012 [High] Tính ROI mùa vụ
  As a: Nông dân
  I want: Nhập chi phí và được tính lãi/lỗ tự động
  So that: Biết mùa vụ này có hiệu quả không
```

### Epic 3: Cảnh báo & Thông báo

```
US-020 [Must-have] Nhận cảnh báo bệnh vùng
  As a: Nông dân
  I want: Nhận thông báo khi vùng tôi đang có dịch bệnh
  So that: Phun phòng ngừa trước khi bệnh đến

US-021 [Must-have] Nhận cảnh báo thời tiết
  As a: Nông dân
  I want: Cảnh báo khi sắp có đợt hạn hoặc mưa lớn
  So that: Điều chỉnh lịch tưới kịp thời

US-022 [High] Lịch canh tác
  As a: Nông dân
  I want: Xem lịch canh tác 30 ngày tới cho vườn mình
  So that: Biết lịch tưới, bón phân, phun thuốc
```

### Epic 4: Onboarding & Profile

```
US-030 [Must-have] Đăng ký bằng số điện thoại
  As a: Nông dân
  I want: Đăng ký chỉ cần số điện thoại (không email/username)
  So that: Bắt đầu ngay không cần nhớ mật khẩu

US-031 [Must-have] Nhập thông tin vườn
  As a: Nông dân
  I want: Nhập loại đất, giống cà phê, diện tích
  So that: AI tư vấn phù hợp với vườn của tôi

US-032 [High] Kết nối Zalo
  As a: Nông dân
  I want: Kết nối tài khoản Zalo
  So that: Nhận thông báo qua Zalo không cần mở app
```

---

## 5. Functional Requirements

### 5.1 AI Chat Engine

| ID | Requirement | Priority | Notes |
|----|------------|---------|-------|
| F-001 | Xử lý câu hỏi tiếng Việt có dấu và không dấu | P0 | Dấu restoration |
| F-002 | Multi-turn conversation với memory | P0 | Session context |
| F-003 | Streaming response (token by token) | P0 | SignalR |
| F-004 | Citation từ knowledge base | P0 | RAG required |
| F-005 | Phân loại intent tự động | P0 | 10 intent classes |
| F-006 | Xử lý ảnh bệnh (Computer Vision) | P1 | Phase 2 |
| F-007 | Voice input (STT) | P2 | Phase 3 |
| F-008 | Offline response cache | P1 | Tier 1 data |

### 5.2 Market Data

| ID | Requirement | Priority |
|----|------------|---------|
| F-020 | Giá Robusta/Arabica cập nhật 3h/lần | P0 |
| F-021 | Biểu đồ giá 7/30/90 ngày | P1 |
| F-022 | Danh sách đại lý thu mua gần vườn | P1 |
| F-023 | ROI calculator đơn giản | P1 |
| F-024 | Price alert Zalo/FCM | P0 |

### 5.3 Notification System

| ID | Requirement | Priority |
|----|------------|---------|
| F-030 | Push notification qua FCM | P0 |
| F-031 | Zalo OA notification | P0 |
| F-032 | SMS fallback | P1 |
| F-033 | Disease alert theo GPS vùng | P0 |
| F-034 | Weather alert theo GPS vườn | P0 |

---

## 6. Non-Functional Requirements

| Category | Requirement | Target |
|---------|------------|--------|
| Performance | AI response P95 | < 3 giây |
| Availability | API uptime | 99.5% |
| Scalability | Concurrent users | 1,000 (Phase 3) |
| Security | Data encryption | AES-256 at-rest, TLS 1.3 |
| Compliance | PDPA Vietnam | Nghị định 13/2023 |
| Offline | Core features offline | Chat history, schedule |
| Accessibility | Min font size | 16px (aging farmers) |
| Localization | Ngôn ngữ | Tiếng Việt (100%), Tiếng Anh (admin) |

---

## 7. Out of Scope — Phase 1

Những tính năng được yêu cầu nhưng **chưa** build trong Phase 1:
- Voice assistant (sẽ làm Phase 3)
- Marketplace mua bán cà phê
- Drone mapping tích hợp
- Blockchain traceability
- Hỗ trợ cây trồng khác (tiêu, sầu riêng)
- Multi-language (chưa cần ngoài tiếng Việt)

---

## 8. Success Metrics

| Metric | Phase 1 Target | Đo bằng |
|--------|---------------|---------|
| Registered farmers | 100 | Backend analytics |
| DAU | 60 | Analytics |
| D7 retention | ≥ 50% | Cohort analysis |
| D30 retention | ≥ 40% | Cohort analysis |
| Messages per session | ≥ 4 | Backend |
| Thumbs-up rate | ≥ 70% | In-app feedback |
| Expert accuracy | ≥ 4.0/5.0 | WASI weekly review |
| NPS | ≥ 40 | Monthly survey |

---

## 9. Assumptions & Risks

| Assumption | Risk nếu sai |
|-----------|-------------|
| Nông dân dùng Zalo | Cần xây thêm kênh SMS/email |
| 3G đủ để stream AI | Cần aggressive caching |
| WASI sẵn sàng collaborate | Thiếu expert knowledge |
| OpenAI API ổn định | Cần fallback hoặc alternative LLM |
| Nông dân sẵn sàng chia sẻ GPS vườn | Cần xây dựng trust trước |

---

**© 2026 Bazan AI — Confidential**

# User Story Map — Farmer Journey
## Bazan AI — Bản đồ hành trình người dùng nông dân

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Product Lead · UX Researcher |
| **Liên quan** | PRD, User Persona, UX Research Report |

---

## 1. Story Map Structure

```
ACTIVITIES (Backbone)
└── TASKS (Walking Skeleton)
    └── USER STORIES (Details by Release)
```

---

## 2. Full Story Map

### ACTIVITY 1: Khám phá & Đăng ký

```
TASKS:
├── Biết đến Bazan AI
│   ├── [P1] Nghe giới thiệu từ cán bộ khuyến nông
│   ├── [P1] Thấy bạn bè trong HTX dùng
│   └── [P2] Thấy quảng cáo Zalo/Facebook
│
├── Đánh giá có nên dùng
│   ├── [P1] Xem video demo 2 phút
│   ├── [P1] Thử hỏi 1 câu miễn phí (không cần đăng ký)
│   └── [P2] Đọc đánh giá từ nông dân khác
│
└── Đăng ký tài khoản
    ├── [P0] Nhập số điện thoại → nhận OTP → xác nhận
    ├── [P0] Nhập tên và tỉnh/huyện
    ├── [P0] Nhập thông tin vườn (giống, diện tích, đất)
    └── [P1] Kết nối Zalo để nhận thông báo
```

### ACTIVITY 2: Hỏi AI về kỹ thuật canh tác

```
TASKS:
├── Đặt câu hỏi
│   ├── [P0] Gõ câu hỏi tiếng Việt bình thường
│   ├── [P0] Gõ không dấu vẫn hiểu
│   ├── [P1] Chụp ảnh lá/quả bệnh kèm câu hỏi
│   └── [P2] Ghi âm giọng nói (STT)
│
├── Nhận câu trả lời
│   ├── [P0] Xem câu trả lời stream từng chữ (không chờ đợi)
│   ├── [P0] Xem nguồn tài liệu (WCR, WASI)
│   ├── [P0] Xem bước hành động cụ thể ("Bước tiếp theo:")
│   └── [P1] Chia sẻ câu trả lời sang Zalo
│
├── Tương tác tiếp
│   ├── [P0] Hỏi tiếp trong cùng cuộc trò chuyện
│   ├── [P0] Đánh giá 👍/👎 câu trả lời
│   └── [P1] Lưu câu trả lời quan trọng (bookmark)
│
└── Không có mạng
    ├── [P1] Xem cache câu trả lời cũ
    ├── [P1] Ghi chép để hỏi sau khi có mạng
    └── [P1] Thấy indicator "Đang xem dữ liệu đã lưu"
```

### ACTIVITY 3: Theo dõi giá cà phê & ROI

```
TASKS:
├── Xem giá hiện tại
│   ├── [P0] Xem giá Robusta/Arabica hôm nay
│   ├── [P0] So sánh với giá hôm qua
│   └── [P1] Xem xu hướng 30 ngày (biểu đồ)
│
├── Nhận tư vấn bán/giữ
│   ├── [P1] Hỏi AI "Nên bán ngay hay chờ?"
│   ├── [P1] Xem phân tích hold vs sell
│   └── [P1] Tìm đại lý thu mua giá tốt gần vườn
│
├── Nhận cảnh báo giá
│   ├── [P0] Thiết lập ngưỡng cảnh báo (ví dụ: giá > 70,000đ)
│   ├── [P0] Nhận Zalo notification khi giá đạt ngưỡng
│   └── [P1] Nhận báo cáo giá hàng tuần
│
└── Tính toán kinh tế
    ├── [P1] Nhập chi phí mùa vụ → xem lãi/lỗ
    ├── [P1] Xem giá hòa vốn theo vườn mình
    └── [P2] So sánh ROI với nông dân cùng vùng (ẩn danh)
```

### ACTIVITY 4: Nhận cảnh báo & Phản ứng kịp thời

```
TASKS:
├── Nhận cảnh báo bệnh
│   ├── [P0] Notification Zalo: "Vùng bạn đang có dịch gỉ sắt"
│   ├── [P0] Xem chi tiết: bệnh gì, mức độ, cách phòng
│   └── [P1] Hỏi AI ngay từ notification
│
├── Nhận cảnh báo thời tiết
│   ├── [P0] Cảnh báo hạn kéo dài → tư vấn tưới bổ sung
│   ├── [P0] Cảnh báo mưa lớn → cảnh báo bệnh nấm
│   └── [P1] Lịch tưới tự điều chỉnh theo dự báo
│
└── Nhận nhắc nhở lịch canh tác
    ├── [P1] Nhắc tưới đến kỳ (dựa trên sensor hoặc lịch)
    ├── [P1] Nhắc bón phân theo giai đoạn
    └── [P1] Nhắc phun thuốc phòng ngừa định kỳ
```

### ACTIVITY 5: Theo dõi & Ghi chép vườn

```
TASKS:
├── Xem tổng quan vườn
│   ├── [P1] Dashboard: tình trạng độ ẩm, thời tiết, cảnh báo
│   ├── [P1] Xem dữ liệu cảm biến real-time (nếu có)
│   └── [P2] Bản đồ vườn với health indicator
│
├── Ghi chép nhật ký
│   ├── [P2] Ghi hoạt động canh tác (đã tưới, đã bón)
│   ├── [P2] Ghi quan sát bất thường
│   └── [P2] Export nhật ký cho VietGAP
│
└── Xem lịch sử và xu hướng
    ├── [P1] Xem lịch sử chat theo chủ đề
    ├── [P2] Xem xu hướng sức khỏe vườn theo tháng
    └── [P2] So sánh mùa vụ này với năm ngoái
```

---

## 3. Release Planning

| Release | Activities included | Target users | Timeline |
|---------|-------------------|-------------|---------|
| **MVP (v0.1)** | Activity 1 + 2 (core chat) | 20 pilot farmers | Tháng 1 |
| **v1.0** | + Activity 3 (market) + 4 (alerts) | 100 farmers | Tháng 3 |
| **v1.5** | + Activity 5 (tracking) | 500 farmers | Tháng 6 |
| **v2.0** | + Voice + IoT + HTX dashboard | 2,000 farmers | Tháng 9 |

---

## 4. Journey Map — Emotional Arc

```
Giai đoạn     Khám phá    Onboard    Hỏi lần đầu    Dùng hàng ngày    Trung thành

Cảm xúc       Tò mò       Không chắc   Ấn tượng!      Thoải mái         Tin tưởng
              😐          😕           😮              😊                ❤️

Thoughts     "Có tốt     "Khó gõ      "Ôi AI         "Tiện quá,        "Không thể
             không?"     lắm"         biết mình      hỏi lúc nào      thiếu rồi"
                                      vườn"          cũng được"

Pain points  Không biết   OTP chậm    Câu trả lời    Offline đôi       Muốn thêm
             app này      Nhập vườn   dài quá         khi không load   tính năng
             là gì        mất 5 phút                                    IoT

Opportunities Demo đơn    Guided      Short answer   Cache tốt hơn     Referral
              giản        onboarding  option          Offline mode      program
```

---

## 5. Critical Path (P0 cho MVP)

Đây là minimum viable journey để farmer có giá trị ngay lần đầu:

```
1. Tải app (< 30 giây)
2. Đăng ký bằng SĐT (< 2 phút)
3. Nhập thông tin vườn (< 3 phút)
4. Gõ câu hỏi đầu tiên
5. Nhận câu trả lời AI có nguồn tham khảo (< 3 giây)
6. Đánh giá 👍 nếu hữu ích
   TOTAL: < 10 phút từ tải app đến "aha moment"
```

---

**© 2026 Bazan AI — Confidential**

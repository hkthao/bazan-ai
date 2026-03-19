# Chính sách Bảo vệ Dữ liệu Cá nhân — Bazan AI
## Tuân thủ Nghị định 13/2023/NĐ-CP (PDPA Việt Nam)

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày hiệu lực** | 2026-04-01 |
| **Phân loại** | Public — hiển thị cho nông dân |
| **Liên quan** | Data Subject Request Process, Security Architecture |

---

## 1. Ai là Chủ thể dữ liệu & Bên Kiểm soát?

**Bên Kiểm soát Dữ liệu:** Công ty TNHH Bazan AI  
**Chủ thể Dữ liệu:** Nông dân sử dụng ứng dụng Bazan AI  
**Liên hệ:** privacy@bazanai.vn | Hotline: 1800 XXXX XX

---

## 2. Dữ liệu chúng tôi thu thập

### 2.1 Dữ liệu bạn cung cấp trực tiếp

| Loại dữ liệu | Ví dụ | Mục đích | Cơ sở pháp lý |
|-------------|-------|---------|--------------|
| Thông tin định danh | Họ tên, số điện thoại | Tạo tài khoản, xác thực OTP | Sự đồng ý (Điều 11 NĐ13) |
| Thông tin liên lạc | Zalo ID, số điện thoại | Gửi thông báo, cảnh báo | Sự đồng ý |
| Thông tin địa lý | Tỉnh, huyện, vị trí GPS vườn | Tư vấn theo địa bàn, cảnh báo thời tiết | Sự đồng ý |
| Thông tin canh tác | Loại đất, giống cà phê, diện tích | Cá nhân hóa AI advice | Sự đồng ý |
| Ảnh chụp | Ảnh lá/quả bệnh | Chẩn đoán bệnh bằng AI | Sự đồng ý |
| Nội dung trò chuyện | Câu hỏi và câu trả lời AI | Lưu lịch sử, cải thiện AI | Sự đồng ý |

### 2.2 Dữ liệu tự động thu thập

| Loại | Mục đích | Thời gian lưu |
|------|---------|--------------|
| Device info (OS, app version) | Troubleshooting, tương thích | 90 ngày |
| Usage analytics (tính năng dùng, thời gian) | Cải thiện sản phẩm (ẩn danh hóa) | 1 năm |
| Error logs | Sửa lỗi kỹ thuật | 30 ngày |
| Dữ liệu cảm biến IoT (nếu kết nối) | Tư vấn tưới tiêu, cảnh báo | 90 ngày |

### 2.3 Dữ liệu KHÔNG bao giờ thu thập

- Thông tin tài chính (số thẻ ngân hàng, tài khoản)
- Dữ liệu y tế cá nhân
- Dữ liệu của trẻ em dưới 16 tuổi
- Thông tin chính trị, tôn giáo

---

## 3. Cách chúng tôi sử dụng dữ liệu

### 3.1 Mục đích chính

1. **Cung cấp dịch vụ AI tư vấn nông nghiệp** — Câu hỏi của bạn được gửi đến AI để phân tích và trả lời
2. **Gửi thông báo và cảnh báo** — Cảnh báo bệnh, thời tiết, giá cà phê qua Zalo/FCM/SMS
3. **Cá nhân hóa tư vấn** — Thông tin vườn giúp AI đưa ra lời khuyên phù hợp hơn
4. **Cải thiện AI** — Phản hồi 👍/👎 (đã ẩn danh hóa) được dùng để cải thiện chất lượng

### 3.2 AI Processing — Bên thứ ba tham gia

> **Quan trọng:** Câu hỏi của bạn được xử lý bởi **OpenAI GPT-4o** (Mỹ). Chúng tôi đã:
> - Ký Data Processing Agreement với OpenAI
> - Bật tùy chọn **Zero Data Retention** — OpenAI không dùng data để train model
> - Loại bỏ thông tin nhận dạng cá nhân trước khi gửi (không gửi tên, SĐT)
> - Chỉ gửi thông tin kỹ thuật về vườn (loại cây, đất, triệu chứng)

---

## 4. Thời gian lưu trữ

| Dữ liệu | Thời gian lưu | Lý do |
|--------|--------------|-------|
| Hồ sơ tài khoản | Đến khi xóa tài khoản + 30 ngày | Xử lý yêu cầu cuối |
| Lịch sử hội thoại | 2 năm | Liên tục tư vấn theo lịch sử |
| Dữ liệu cảm biến | 90 ngày | Xu hướng ngắn hạn |
| Phản hồi đã ẩn danh | 3 năm | Cải thiện AI |
| Log kỹ thuật | 30 ngày | Troubleshooting |
| Backup | 1 năm | Khôi phục thảm họa |

---

## 5. Quyền của bạn (Điều 9 Nghị định 13/2023)

Theo pháp luật Việt Nam, bạn có quyền:

| Quyền | Nội dung | Cách thực hiện | Thời gian xử lý |
|-------|---------|---------------|----------------|
| **Quyền biết** | Biết dữ liệu nào đang được thu thập | Xem Chính sách này | Ngay lập tức |
| **Quyền đồng ý/không đồng ý** | Rút lại sự đồng ý bất kỳ lúc nào | Cài đặt app → Quyền riêng tư | 30 ngày |
| **Quyền truy cập** | Xem toàn bộ data của mình | privacy@bazanai.vn | 15 ngày |
| **Quyền chỉnh sửa** | Sửa thông tin sai | Trong app → Hồ sơ | Ngay lập tức |
| **Quyền xóa** | Xóa tài khoản và toàn bộ data | privacy@bazanai.vn hoặc app | 30 ngày |
| **Quyền hạn chế** | Ngừng xử lý một số loại data | privacy@bazanai.vn | 15 ngày |
| **Quyền khiếu nại** | Khiếu nại lên Cục An toàn thông tin | Gửi đơn đến ATTT | Theo quy định |

---

## 6. Bảo mật dữ liệu

Chúng tôi áp dụng các biện pháp kỹ thuật theo tiêu chuẩn ngành:

- Mã hóa AES-256 cho dữ liệu lưu trữ
- TLS 1.3 cho truyền tải
- Số điện thoại và tên được mã hóa trong database
- Tọa độ GPS chỉ lưu ở độ chính xác ~1km (không chính xác đến vườn cụ thể)
- Hệ thống phát hiện xâm nhập 24/7
- Audit log cho mọi truy cập vào dữ liệu cá nhân

---

## 7. Thông báo vi phạm dữ liệu

Trong trường hợp vi phạm dữ liệu cá nhân ảnh hưởng đến bạn:
- Chúng tôi sẽ thông báo **trong vòng 72 giờ** theo Điều 23 Nghị định 13/2023
- Thông qua: Zalo + email (nếu có) + thông báo trong app
- Nội dung thông báo: loại dữ liệu bị ảnh hưởng, tác động tiềm tàng, biện pháp bạn cần thực hiện

---

## 8. Liên hệ

**Phụ trách Bảo vệ Dữ liệu (DPO):**  
Email: privacy@bazanai.vn  
Địa chỉ: [Địa chỉ công ty]  
Giờ làm việc: Thứ 2 – Thứ 6, 8h – 17h

**Cơ quan quản lý:**  
Cục An toàn Thông tin, Bộ Thông tin và Truyền thông  
Website: attt.gov.vn

---

**© 2026 Bazan AI — Cập nhật lần cuối: 2026-03-19**

---
---

# Thỏa thuận Xử lý Dữ liệu Cá nhân
## Personal Data Processing Agreement (DPA)

| Trường | Nội dung |
|--------|---------|
| **Loại tài liệu** | Thỏa thuận pháp lý — Internal template |
| **Áp dụng cho** | Nông dân đăng ký Bazan AI |
| **Cơ sở pháp lý** | Nghị định 13/2023/NĐ-CP |

---

## Điều 1 — Các bên tham gia

**Bên Kiểm soát Dữ liệu:** Công ty TNHH Bazan AI ("Bazan AI")  
**Chủ thể Dữ liệu:** Nông dân đăng ký và sử dụng ứng dụng ("Nông dân")

---

## Điều 2 — Phạm vi dữ liệu cá nhân được xử lý

Bazan AI xử lý các loại dữ liệu sau của Nông dân:

**Dữ liệu thông thường:**
- Họ và tên, số điện thoại
- Tỉnh/huyện/xã cư trú và vị trí vườn (GPS)
- Thông tin canh tác (giống cà phê, loại đất, diện tích)
- Lịch sử hội thoại với AI
- Phản hồi và đánh giá dịch vụ

**Dữ liệu nhạy cảm (theo Điều 2.4 Nghị định 13/2023):**  
Không thu thập dữ liệu thuộc nhóm nhạy cảm.

---

## Điều 3 — Mục đích xử lý và cơ sở pháp lý

| Mục đích | Cơ sở pháp lý (NĐ 13/2023) |
|---------|---------------------------|
| Cung cấp dịch vụ tư vấn AI | Điều 17.b — Thực hiện hợp đồng |
| Gửi thông báo kỹ thuật | Điều 17.b — Thực hiện hợp đồng |
| Cải thiện AI (dữ liệu ẩn danh) | Điều 17.a — Sự đồng ý |
| Phân tích hành vi sử dụng | Điều 17.a — Sự đồng ý |

---

## Điều 4 — Quyền và nghĩa vụ của Nông dân

**Nông dân có quyền:**
1. Rút lại sự đồng ý bất kỳ lúc nào mà không ảnh hưởng đến tính hợp pháp của việc xử lý trước đó
2. Yêu cầu truy cập, chỉnh sửa, hoặc xóa dữ liệu cá nhân
3. Phản đối việc xử lý dữ liệu cho mục đích tiếp thị
4. Khiếu nại lên Cục An toàn Thông tin

**Nông dân có nghĩa vụ:**
1. Cung cấp thông tin chính xác
2. Thông báo kịp thời khi có thay đổi thông tin

---

## Điều 5 — Nghĩa vụ của Bazan AI

1. Chỉ xử lý dữ liệu đúng mục đích đã khai báo
2. Thực hiện biện pháp bảo mật phù hợp tiêu chuẩn ngành
3. Thông báo vi phạm dữ liệu trong 72 giờ (theo Điều 23 NĐ13)
4. Không bán dữ liệu cá nhân của Nông dân cho bên thứ ba
5. Xóa dữ liệu theo yêu cầu trong 30 ngày
6. Ký DPA với các nhà cung cấp xử lý dữ liệu (OpenAI, Zalo, v.v.)

---

## Điều 6 — Chuyển dữ liệu ra nước ngoài

Bazan AI chuyển dữ liệu ra nước ngoài theo Điều 25 NĐ13/2023 đến:

| Bên nhận | Quốc gia | Mục đích | Biện pháp bảo vệ |
|---------|---------|---------|-----------------|
| OpenAI, LLC | Hoa Kỳ | Xử lý câu hỏi AI | DPA + Zero Data Retention |
| Google Firebase | Hoa Kỳ | Push notification | Google DPA |
| Zalo (VNG Corp) | Việt Nam | Push notification | DPA |

---

## Điều 7 — Điều khoản kết thúc

Thỏa thuận này có hiệu lực từ ngày Nông dân hoàn tất đăng ký và chấm dứt khi Nông dân xóa tài khoản (sau 30 ngày grace period).

---

**© 2026 Bazan AI — Template v1.0**

---
---

# GDPR/PDPA Data Subject Request Process
## Quy trình xử lý Yêu cầu của Chủ thể Dữ liệu

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | Data Privacy Policy, Security Architecture |

---

## 1. Kênh tiếp nhận yêu cầu

| Kênh | Cách thức | Ai nhận |
|------|---------|--------|
| **Email** | privacy@bazanai.vn | DPO (tự động ticket) |
| **In-app** | Cài đặt → Quyền riêng tư → Yêu cầu | CS team |
| **Văn bản** | Gửi đến địa chỉ công ty | DPO |

---

## 2. Quy trình xử lý từng loại yêu cầu

### 2.1 Yêu cầu Truy cập dữ liệu (Right of Access)

```
Bước 1 (Ngày 1): Tiếp nhận → gửi email xác nhận với ticket number
Bước 2 (Ngày 2): Xác minh danh tính (OTP qua số điện thoại đăng ký)
Bước 3 (Ngày 3-10): Thu thập toàn bộ data
  - MongoDB: farmers, user_contexts, chat_sessions, chat_messages
  - Qdrant: không có PII trực tiếp
  - MinIO: ảnh farm-photos/{farmerId}/
  - Redis: session data (ngắn hạn)
Bước 4 (Ngày 11-14): Tổng hợp thành file JSON đọc được
Bước 5 (Ngày 15): Gửi link download (encrypted, TTL 7 ngày) qua email/Zalo
```

**Script export:**
```python
async def export_farmer_data(farmer_id: str) -> dict:
    return {
        "profile": await farmers_repo.get_full_profile(farmer_id),
        "farms": await farms_repo.get_all(farmer_id),
        "conversations": await conv_repo.export_all(farmer_id),
        "feedback_given": await conv_repo.export_feedback(farmer_id),
        "sensor_data_summary": await iot_repo.get_summary(farmer_id),
        "notifications_sent": await notif_repo.get_history(farmer_id),
        "exported_at": datetime.utcnow().isoformat(),
        "data_controller": "Bazan AI Co., Ltd"
    }
```

### 2.2 Yêu cầu Xóa dữ liệu (Right to Erasure)

```
Bước 1: Xác minh danh tính (như truy cập)
Bước 2: Kiểm tra các ràng buộc pháp lý:
  - Có nghĩa vụ pháp lý giữ lại không? (ví dụ: kiện tụng đang diễn ra)
  - Nếu không → tiến hành xóa
Bước 3: Xóa trong 30 ngày:
  - MongoDB: hard delete farmer + cascade all related documents
  - MinIO: xóa folder farm-photos/{farmerId}/
  - Redis: flush session keys
  - Qdrant: không có PII trực tiếp (conversation summaries không link farmerId)
  - Audit log: GIỮ LẠI nhưng pseudonymize farmerId (GDPR Art.17(3))
  - Backup: đánh dấu để xóa trong lần rotate backup tiếp theo
Bước 4: Gửi xác nhận xóa
```

```python
async def delete_farmer_data(farmer_id: str, reason: str):
    # Pseudonymize trong audit logs trước
    await audit_repo.pseudonymize_farmer(farmer_id)

    # Hard delete theo thứ tự (foreign key compliance)
    await iot_repo.delete_all(farmer_id)
    await notif_repo.delete_history(farmer_id)
    await conv_repo.delete_all_sessions(farmer_id)
    await farms_repo.delete_all(farmer_id)
    await user_contexts_repo.delete(farmer_id)
    await farmers_repo.delete(farmer_id)

    # MinIO
    await storage.delete_folder(f"farm-photos/{farmer_id}/")

    # Log deletion request (không log PII)
    logger.info("Data deletion completed",
                request_id=reason, deleted_at=datetime.utcnow())
```

### 2.3 Yêu cầu Chỉnh sửa (Right to Rectification)

```
Phần lớn có thể tự sửa trong app:
  ✅ Tên, thông tin vườn, vị trí → Hồ sơ trong app
  ✅ Kết nối Zalo → Cài đặt

Phần cần support:
  - Số điện thoại thay đổi → CS verify → update và re-authenticate
  - Sửa lịch sử chat → Không cho phép (integrity requirement)
```

### 2.4 Phản đối xử lý (Right to Object)

```
Nông dân có thể phản đối:
  ✅ Tiếp thị → Toggle trong app (tắt notification marketing)
  ✅ Cải thiện AI → Tắt trong Cài đặt → không dùng feedback để train
  ❌ Xử lý cốt lõi (tư vấn AI) → Không cho phép (phải xóa tài khoản nếu muốn)
```

---

## 3. Tracking & Reporting

```python
# Mọi DSR phải được track
class DataSubjectRequest:
    request_id: str        # DPO-2026-0001
    farmer_id: str         # Pseudonymized in logs
    request_type: str      # access | erasure | rectification | object
    received_at: datetime
    verified_at: datetime | None
    completed_at: datetime | None
    status: str            # pending | verified | processing | completed | rejected
    rejection_reason: str | None

# SLA tracking — alert nếu sắp vi phạm
# Nghị định 13: 72h cho notification, 15-30 ngày cho các quyền khác
```

---

**© 2026 Bazan AI — Confidential**

# ADR-006 — Mobile Strategy: Offline-First với Flutter

| Trường | Nội dung |
|--------|---------|
| **ID** | ADR-006 |
| **Tiêu đề** | Chiến lược Offline-First cho Mobile App Bazan AI |
| **Trạng thái** | Accepted |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Principal Software Architect · Mobile Lead |
| **Người review** | UX Researcher, Backend Lead |
| **Liên quan** | ADR-003 (RabbitMQ), ADR-005 (YARP), System Architecture Document |

---

## 1. Bối cảnh

Nông dân Tây Nguyên làm việc tại vườn — nơi sóng điện thoại thất thường, đặc biệt tại các vùng sâu của Đắk Nông, Kon Tum. Khảo sát sơ bộ cho thấy:

- **65%** nông dân dùng mạng 3G hoặc yếu hơn khi ở vườn
- **30%** vườn nằm trong vùng tín hiệu không ổn định (< -90 dBm)
- **Thời điểm cần thông tin nhất** (phun thuốc, tưới nước buổi sáng) trùng với lúc ít dùng điện thoại nhất

Nếu app chỉ hoạt động khi có mạng, nông dân không thể:
- Xem lịch canh tác đã tư vấn hôm trước
- Xem cảnh báo bệnh đã nhận
- Ghi chép quan sát tại vườn để hỏi AI sau

### 1.1 Yêu cầu

| Yêu cầu | Mức độ | Chi tiết |
|---------|--------|---------|
| **Xem offline** | Must-have | Lịch canh tác, cảnh báo, lịch sử hội thoại gần nhất |
| **Ghi chép offline** | Must-have | Ghi quan sát, chụp ảnh để hỏi sau khi có mạng |
| **Sync tự động** | Must-have | Tự đồng bộ khi có kết nối, không cần thao tác |
| **Conflict resolution** | Should-have | Xử lý xung đột khi data thay đổi cả 2 phía |
| **Offline AI lite** | Nice-to-have | Trả lời câu hỏi đơn giản từ knowledge cache local |

---

## 2. Các phương án

### Phương án A — Flutter + Drift (SQLite) + Background Sync

**Mô tả:** Flutter làm cross-platform mobile, Drift (SQLite wrapper) lưu data local, background isolate sync định kỳ.

**Ưu điểm:**
- Drift type-safe, query compile-time checked
- Background sync không cần user action
- Flutter một codebase cho Android + iOS
- SQLite mature, battle-tested on mobile

**Nhược điểm:**
- Tự implement sync logic (conflict resolution, delta sync)
- Drift migration phải quản lý thủ công

---

### Phương án B — Flutter + Hive (NoSQL) + Manual Sync

**Mô tả:** Hive key-value store thay SQLite, sync thủ công khi user mở app.

**Không chọn vì:** Query phức tạp (filter lịch sử session, tìm kiếm cảnh báo) rất khó với key-value store. Sync thủ công UX kém.

---

### Phương án C — Flutter + Firebase Firestore Offline

**Mô tả:** Dùng Firestore với offline persistence built-in.

**Không chọn vì:** Vendor lock-in Firebase. Data nông dân (GPS, sensor) không muốn qua Google server. Chi phí Firestore theo document read/write.

---

## 3. Quyết định

**Chọn Phương án A — Flutter + Drift + Background Sync Worker.**

---

## 4. Thiết kế

### 4.1 Data phân tầng

```
TIER 1 — Always available offline (sync mỗi 15 phút khi có mạng):
  • Lịch canh tác 30 ngày tới
  • 20 phiên hội thoại gần nhất
  • Cảnh báo bệnh chưa đọc
  • Giá cà phê 7 ngày qua
  • Thông tin vườn của nông dân

TIER 2 — Sync khi mở app (nếu có mạng):
  • Hội thoại cũ hơn 20 phiên
  • Báo cáo ROI

TIER 3 — Chỉ online:
  • AI chat real-time (SignalR streaming)
  • Ảnh độ phân giải cao
  • Dữ liệu thị trường live
```

### 4.2 Sync Protocol

```dart
// Outbox pattern cho writes khi offline
class OutboxEntry {
  final String id;
  final String entityType;   // 'observation', 'feedback', 'farm_update'
  final Map<String, dynamic> payload;
  final DateTime createdAt;
  SyncStatus status;         // pending, syncing, synced, failed
}

// Background sync worker
class SyncWorker {
  static const syncInterval = Duration(minutes: 15);

  Future<void> sync() async {
    if (!await connectivityService.hasConnection()) return;

    // 1. Push: gửi outbox entries lên server
    final pending = await outboxRepo.getPending();
    for (final entry in pending) {
      try {
        await apiClient.push(entry);
        await outboxRepo.markSynced(entry.id);
      } catch (e) {
        await outboxRepo.markFailed(entry.id, e.toString());
      }
    }

    // 2. Pull: lấy data mới về
    final lastSync = await prefs.getLastSyncTime();
    final delta = await apiClient.getDelta(since: lastSync);
    await localDb.applyDelta(delta);
    await prefs.setLastSyncTime(DateTime.now());
  }
}
```

### 4.3 Conflict Resolution

```
Chiến lược: Server wins cho data AI/agronomy, Client wins cho observations

Server wins:
  - Lịch canh tác (AI generate)
  - Cảnh báo bệnh
  - Giá thị trường

Client wins:
  - Ghi chép quan sát của nông dân
  - Ảnh chụp tại vườn
  - Feedback đánh giá câu trả lời
```

### 4.4 Offline Indicator UX

```
- Banner nhỏ màu amber khi offline: "Đang xem dữ liệu đã lưu"
- Chat input disabled khi offline, hiện message: "Cần kết nối mạng để hỏi AI"
- Nút "Ghi chép để hỏi sau" khi offline — lưu vào outbox
- Badge số trên icon sync khi có pending entries
```

---

## 5. Hậu quả

**Tích cực:** App hoạt động tốt kể cả vùng không có sóng. Sync tự động, nông dân không cần hiểu khái niệm "đồng bộ".

**Tiêu cực:** Tăng complexity: outbox pattern, delta sync, conflict resolution. Storage device tăng ~50MB cho local cache. Cần test kỹ các edge case mạng chập chờn.

---

## 6. Tiêu chí đánh giá lại

- Sync conflict > 5% sessions → Xem xét CRDT (Conflict-free Replicated Data Types)
- Local storage > 200MB → Evict data cũ tích cực hơn
- Background sync drain pin > 5%/giờ → Tăng interval hoặc dùng platform push notification để trigger sync

---

**© 2026 Bazan AI Project — Confidential**

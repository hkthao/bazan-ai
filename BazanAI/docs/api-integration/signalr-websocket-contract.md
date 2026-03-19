# WebSocket / SignalR Contract — Chat Streaming
## Bazan AI — AI Chat Real-time Communication

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Protocol** | SignalR over WebSocket (ASP.NET Core) |
| **Endpoint** | `wss://api.bazanai.vn/hub/chat` |
| **Ngày tạo** | 2026-03-19 |
| **Liên quan** | ADR-004 (Semantic Kernel), ADR-005 (YARP), Sequence Diagrams |

---

## 1. Tổng quan

Bazan AI dùng **SignalR** để stream token AI về client theo thời gian thực. Client kết nối WebSocket một lần và nhận từng token ngay khi model sinh ra — tạo trải nghiệm chat tự nhiên, không phải đợi toàn bộ câu trả lời.

```
Client                     Gateway (YARP)        AI Orchestrator (SignalR Hub)
  │                              │                          │
  │── WS Connect ──────────────► │                          │
  │   /hub/chat?token={jwt}      │                          │
  │                              ├── Proxy WebSocket ──────► │
  │                              │                          │
  │◄─ Connected ─────────────────────────────────────────── │
  │   connectionId               │                          │
  │                              │                          │
  │── SendMessage ──────────────────────────────────────── ► │
  │   {sessionId, message}       │                          │
  │                              │                          │
  │◄─ ReceiveToken("Dựa") ──────────────────────────────── │
  │◄─ ReceiveToken(" vào") ─────────────────────────────── │
  │◄─ ReceiveToken(" mô") ──────────────────────────────── │
  │   ... (streaming) ...        │                          │
  │◄─ StreamComplete({sources})─────────────────────────── │
  │                              │                          │
  │── SubmitFeedback ──────────────────────────────────────► │
  │   {messageId, rating}        │                          │
```

---

## 2. Connection

### 2.1 URL & Auth

```
Endpoint: wss://api.bazanai.vn/hub/chat

Authentication:
  Option 1 (preferred): Query string
    wss://api.bazanai.vn/hub/chat?access_token={jwt}
  
  Option 2: Custom header (khi platform hỗ trợ)
    Authorization: Bearer {jwt}
```

> **Lưu ý:** WebSocket không hỗ trợ custom header trực tiếp trên browser. Dùng query string và Gateway sẽ extract token trước khi upgrade.

### 2.2 Client SDK (Flutter)

```dart
// lib/services/chat_hub_service.dart
import 'package:signalr_netcore/signalr_client.dart';

class ChatHubService {
  late HubConnection _hubConnection;
  final String _baseUrl = 'wss://api.bazanai.vn/hub/chat';

  Future<void> connect(String accessToken) async {
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          '$_baseUrl?access_token=$accessToken',
          options: HttpConnectionOptions(
            transport: HttpTransportType.WebSockets,
            skipNegotiation: true,        // Bỏ qua negotiate — chỉ WS
          ),
        )
        .withAutomaticReconnect(
          retryDelays: [0, 2000, 10000, 30000]  // ms
        )
        .configureLogging(Logger('ChatHub'))
        .build();

    // Đăng ký handlers trước khi connect
    _hubConnection.on('ReceiveToken', _onReceiveToken);
    _hubConnection.on('StreamComplete', _onStreamComplete);
    _hubConnection.on('StreamError', _onStreamError);
    _hubConnection.on('TypingIndicator', _onTypingIndicator);

    // Reconnect events
    _hubConnection.onreconnecting(({error}) {
      print('Reconnecting... $error');
    });
    _hubConnection.onreconnected(({connectionId}) {
      print('Reconnected: $connectionId');
    });

    await _hubConnection.start();
    print('Connected: ${_hubConnection.connectionId}');
  }

  Future<void> disconnect() async {
    await _hubConnection.stop();
  }
}
```

### 2.3 Connection States

```
Disconnected → Connecting → Connected → Active session
                                    ↓
                               Reconnecting (auto)
                                    ↓
                             Connected (restored)
```

**Reconnect behavior:**
- Tự động reconnect với exponential backoff: 0ms, 2s, 10s, 30s
- Sau 4 lần thất bại → emit `Disconnected` event, client tự xử lý
- Session ID được giữ nguyên sau reconnect — không mất context

---

## 3. Client → Server Methods

### 3.1 `SendMessage`

Gửi câu hỏi của nông dân và bắt đầu stream câu trả lời AI.

**Signature:**
```csharp
// Server-side hub method
public async Task SendMessage(SendMessageRequest request)
```

**Flutter call:**
```dart
await _hubConnection.invoke(
  'SendMessage',
  args: [
    {
      'sessionId': 'sess_20260319_AN_001',
      'farmerId': '64f1a2b3c4d5e6f7a8b9c0d1',
      'message': 'Cây cà phê của tôi bị vàng lá',
      'imageUrl': null,            // Optional: URL ảnh đã upload qua Media Service
      'clientTimestamp': '2026-03-19T07:31:00.000Z'
    }
  ],
);
```

**Request payload:**

| Field | Type | Required | Description |
|-------|------|---------|-------------|
| `sessionId` | string | Yes | Session đã tạo qua Conversation Service |
| `farmerId` | string | Yes | Farmer ObjectId |
| `message` | string | Yes | Nội dung câu hỏi (max 2000 ký tự) |
| `imageUrl` | string? | No | Pre-signed URL ảnh từ Media Service |
| `clientTimestamp` | ISO 8601 | Yes | Timestamp phía client |

**Lưu ý quan trọng:**
- Không gọi `SendMessage` mới khi stream đang active (chờ `StreamComplete`)
- Server ignore nếu gọi trong lúc đang stream

---

### 3.2 `CancelStream`

Hủy stream đang chạy (nông dân nhấn nút "Dừng").

```dart
await _hubConnection.invoke('CancelStream', args: [
  { 'sessionId': 'sess_20260319_AN_001' }
]);
```

Server sẽ hủy OpenAI streaming call và emit `StreamCancelled`.

---

### 3.3 `SubmitFeedback`

Gửi đánh giá ngay sau khi nhận `StreamComplete`.

```dart
await _hubConnection.invoke('SubmitFeedback', args: [
  {
    'messageId': 'msg_002_sess_20260319',
    'rating': 5,
    'thumbs': 'up',
    'comment': 'Đúng rồi thầy!'
  }
]);
```

---

### 3.4 `Ping`

Keepalive — gọi mỗi 25 giây để tránh timeout.

```dart
Timer.periodic(Duration(seconds: 25), (_) {
  if (_hubConnection.state == HubConnectionState.Connected) {
    _hubConnection.invoke('Ping');
  }
});
```

---

## 4. Server → Client Events

### 4.1 `ReceiveToken`

Token đơn lẻ từ LLM streaming. Được gọi liên tục cho đến `StreamComplete`.

**Payload:**
```json
{
  "token": "Dựa",
  "sessionId": "sess_20260319_AN_001",
  "sequenceNumber": 1
}
```

**Flutter handler:**
```dart
void _onReceiveToken(List<Object?>? args) {
  final payload = args?[0] as Map<String, dynamic>;
  final token = payload['token'] as String;
  
  setState(() {
    _currentResponse += token;
  });
}
```

**Tần suất:** ~15–30 tokens/giây (phụ thuộc OpenAI throughput)

---

### 4.2 `StreamComplete`

Báo hiệu stream kết thúc, kèm metadata trích dẫn nguồn.

**Payload:**
```json
{
  "sessionId": "sess_20260319_AN_001",
  "messageId": "msg_002_sess_20260319",
  "fullResponse": "Dựa vào mô tả của bạn, cây đang bị bệnh gỉ sắt...",
  "sources": [
    {
      "docId": "wcr-leaf-rust-2023",
      "title": "WCR Leaf Rust Management Guide 2023",
      "pageStart": 12,
      "pageEnd": 14
    }
  ],
  "tokenUsage": {
    "promptTokens": 2340,
    "completionTokens": 287
  },
  "latency_ms": 1847,
  "timestamp": "2026-03-19T07:31:02Z"
}
```

**Flutter handler:**
```dart
void _onStreamComplete(List<Object?>? args) {
  final payload = args?[0] as Map<String, dynamic>;
  
  setState(() {
    _isStreaming = false;
    _currentMessageId = payload['messageId'];
    _sources = (payload['sources'] as List)
        .map((s) => DocumentSource.fromJson(s))
        .toList();
  });
  
  // Hiện nút đánh giá
  _showFeedbackWidget();
}
```

---

### 4.3 `StreamError`

Lỗi trong quá trình streaming.

**Payload:**
```json
{
  "sessionId": "sess_20260319_AN_001",
  "errorCode": "llm_timeout",
  "message": "Hệ thống đang bận, vui lòng thử lại sau",
  "retryable": true,
  "retryAfter_seconds": 5
}
```

**Error codes:**

| Code | Mô tả | Retryable |
|------|-------|-----------|
| `llm_timeout` | OpenAI không phản hồi trong 15s | Yes |
| `llm_rate_limit` | Vượt rate limit OpenAI | Yes (5s) |
| `context_too_long` | Lịch sử chat quá dài | No — trim và retry |
| `service_unavailable` | Service nội bộ down | Yes (30s) |
| `invalid_session` | SessionId không tồn tại | No |
| `concurrent_stream` | Đang có stream khác | No — đợi complete |

---

### 4.4 `StreamCancelled`

Xác nhận hủy stream thành công.

```json
{
  "sessionId": "sess_20260319_AN_001",
  "cancelledAt": "2026-03-19T07:31:05Z"
}
```

---

### 4.5 `TypingIndicator`

Báo hiệu AI đang xử lý (trước khi token đầu tiên đến).

```json
{
  "sessionId": "sess_20260319_AN_001",
  "stage": "retrieving",
  "message": "Đang tìm kiếm tài liệu..."
}
```

**Stage values:**

| Stage | Hiển thị cho user |
|-------|------------------|
| `classifying` | "Đang phân tích câu hỏi..." |
| `retrieving` | "Đang tìm kiếm tài liệu..." |
| `analyzing` | "Đang phân tích dữ liệu vườn..." |
| `generating` | "Đang soạn câu trả lời..." |

---

### 4.6 `Pong`

Response của `Ping`.

```json
{ "serverTime": "2026-03-19T07:31:00Z" }
```

---

## 5. Error Handling & Reconnect

### 5.1 Client-side Logic

```dart
class ChatHubService {
  StreamController<String> tokenStream = StreamController();
  StreamController<Map> completeStream = StreamController();
  
  String _pendingMessage = '';
  bool _isStreaming = false;

  void _setupReconnectHandlers() {
    _hubConnection.onreconnecting(({error}) {
      // Hiện "Đang kết nối lại..."
      _notifyReconnecting();
    });

    _hubConnection.onreconnected(({connectionId}) {
      // Ẩn banner reconnecting
      _notifyReconnected();
      
      // Nếu đang stream bị ngắt, inform user
      if (_isStreaming) {
        tokenStream.add('\n\n[Kết nối bị gián đoạn. Vui lòng hỏi lại.]');
        _isStreaming = false;
      }
    });

    _hubConnection.onclose(({error}) {
      // Đã thử reconnect 4 lần không được
      _notifyDisconnected(error);
    });
  }
}
```

### 5.2 Offline Queue

```dart
// Khi offline, queue message để gửi khi có lại kết nối
class OfflineMessageQueue {
  final List<PendingMessage> _queue = [];

  void enqueue(String message, String sessionId) {
    _queue.add(PendingMessage(
      message: message,
      sessionId: sessionId,
      queuedAt: DateTime.now(),
    ));
    // Hiện badge "1 tin nhắn đang chờ"
  }

  Future<void> flush(ChatHubService hub) async {
    for (final msg in List.from(_queue)) {
      try {
        await hub.sendMessage(msg.sessionId, msg.message);
        _queue.remove(msg);
      } catch (_) {
        break; // Dừng nếu vẫn lỗi
      }
    }
  }
}
```

---

## 6. Giới hạn & Constraints

| Constraint | Giá trị | Lý do |
|-----------|---------|-------|
| Max message length | 2,000 ký tự | Token budget |
| Max concurrent sessions per farmer | 1 | Tránh race condition |
| Stream timeout | 30 giây | Nếu không có token sau 30s → error |
| Max idle connection | 5 phút | Server close inactive connections |
| Ping interval | 25 giây | Tránh load balancer timeout (30s) |
| Max image size (qua Media Service) | 10 MB | Trước khi gửi URL vào SendMessage |

---

**© 2026 Bazan AI Project — Confidential**

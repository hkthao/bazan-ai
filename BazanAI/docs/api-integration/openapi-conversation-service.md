# OpenAPI 3.1 — Conversation Management Service

| Trường | Nội dung |
|--------|---------|
| **Phiên bản API** | v1.0.0 |
| **Base URL** | `https://api.bazanai.vn/api/v1` |
| **Service Port** | 5002 (internal) |
| **Ngày tạo** | 2026-03-19 |
| **Liên quan** | ADR-001 (MongoDB), RAG Pipeline Design, RLHF Protocol |

---

## Tổng quan

Conversation Service lưu trữ toàn bộ lịch sử chat, quản lý context session, và thu thập feedback phục vụ RLHF pipeline. **Tất cả endpoints yêu cầu JWT authentication.**

---

## Sessions

---

#### `POST /sessions`

Khởi tạo phiên hội thoại mới.

**Request Body:**
```json
{
  "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
  "channel": "mobile_app",
  "deviceInfo": {
    "os": "Android",
    "appVersion": "2.1.0"
  },
  "contextSnapshot": {
    "coffeeVariety": "Robusta",
    "growthStage": "fruit_development",
    "currentSoilMoisture_pct": 65.3,
    "currentCoffeePrice_vnd": 68500
  }
}
```

**Fields:**
- `channel`: enum `mobile_app | zalo_bot | web | sms`
- `contextSnapshot`: snapshot trạng thái vườn lúc bắt đầu session

**Response `201 Created`:**
```json
{
  "sessionId": "sess_20260319_AN_001",
  "id": "66a1b2c3d4e5f6a7b8c9d0e1",
  "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
  "startedAt": "2026-03-19T07:30:00Z",
  "status": "active"
}
```

---

#### `GET /sessions/{sessionId}`

Lấy thông tin phiên hội thoại.

**Response `200 OK`:**
```json
{
  "sessionId": "sess_20260319_AN_001",
  "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
  "startedAt": "2026-03-19T07:30:00Z",
  "endedAt": null,
  "channel": "mobile_app",
  "messageCount": 8,
  "tokenUsage": {
    "promptTokens": 2340,
    "completionTokens": 876,
    "totalCost_usd": 0.034
  },
  "summary": null,
  "satisfactionRating": null,
  "status": "active"
}
```

---

#### `POST /sessions/{sessionId}/close`

Kết thúc phiên và trigger summarization.

**Request Body:**
```json
{
  "satisfactionRating": 5,
  "closedBy": "user"
}
```

**Response `200 OK`:**
```json
{
  "sessionId": "sess_20260319_AN_001",
  "endedAt": "2026-03-19T07:45:00Z",
  "duration_sec": 900,
  "summarizationQueued": true
}
```

---

#### `DELETE /sessions/{sessionId}`

Xoá phiên (GDPR data subject request).

**Headers:** `X-Service-Api-Key: {internal_key}` (chỉ internal)

**Response `204 No Content`**

---

#### `GET /sessions`

Lấy danh sách phiên của nông dân. Hỗ trợ phân trang.

**Query Params:**
- `farmerId` (bắt buộc): ObjectId
- `page` (default: 1)
- `limit` (default: 20, max: 100)
- `channel`: filter theo kênh
- `from`, `to`: filter theo ngày (ISO 8601)

**Response `200 OK`:**
```json
{
  "data": [
    {
      "sessionId": "sess_20260319_AN_001",
      "startedAt": "2026-03-19T07:30:00Z",
      "endedAt": "2026-03-19T07:45:00Z",
      "messageCount": 8,
      "satisfactionRating": 5,
      "summary": "Nông dân hỏi về triệu chứng lá vàng...",
      "channel": "mobile_app"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 45,
    "totalPages": 3
  }
}
```

---

## Messages

---

#### `POST /sessions/{sessionId}/messages`

Lưu một tin nhắn vào session.

**Request Body:**
```json
{
  "role": "user",
  "content": "Cây cà phê của tôi bị vàng lá từ tuần trước",
  "contentType": "text",
  "attachments": [],
  "contextUsed": null,
  "latency_ms": null
}
```

**Fields:**
- `role`: enum `user | assistant | system`
- `contentType`: enum `text | text_with_image | audio`
- `attachments`: array các đính kèm (ảnh, file)
- `contextUsed`: metadata RAG (chỉ điền cho role=assistant)

**Response `201 Created`:**
```json
{
  "messageId": "msg_001_sess_20260319",
  "id": "67b2c3d4e5f6a7b8c9d0e1f2",
  "sessionId": "sess_20260319_AN_001",
  "timestamp": "2026-03-19T07:31:00Z"
}
```

---

#### `POST /sessions/{sessionId}/messages` (assistant)

Lưu tin nhắn AI với đầy đủ metadata.

**Request Body (role=assistant):**
```json
{
  "role": "assistant",
  "content": "Dựa vào mô tả của bạn...",
  "contentType": "text",
  "contextUsed": {
    "ragDocuments": [
      {
        "docId": "wcr-leaf-rust-2023",
        "chunkId": "wcr-leaf-rust-2023_chunk_003",
        "relevanceScore": 0.92,
        "sourceTitle": "WCR Leaf Rust Management 2023"
      }
    ],
    "sensorDataUsed": true,
    "weatherDataUsed": false,
    "intentClassified": "disease_diagnosis",
    "toolsInvoked": ["diagnose_farm_disease", "search_agronomy_knowledge"]
  },
  "latency_ms": 1847,
  "tokenUsage": {
    "promptTokens": 2340,
    "completionTokens": 287
  }
}
```

---

#### `GET /sessions/{sessionId}/messages`

Lấy danh sách tin nhắn trong session. Hỗ trợ phân trang.

**Query Params:**
- `page` (default: 1), `limit` (default: 50, max: 200)
- `role`: filter theo role

**Response `200 OK`:**
```json
{
  "data": [
    {
      "messageId": "msg_001_sess_20260319",
      "role": "user",
      "content": "Cây cà phê của tôi bị vàng lá từ tuần trước",
      "contentType": "text",
      "timestamp": "2026-03-19T07:31:00Z",
      "feedback": null
    },
    {
      "messageId": "msg_002_sess_20260319",
      "role": "assistant",
      "content": "Dựa vào mô tả của bạn...",
      "contentType": "text",
      "timestamp": "2026-03-19T07:31:02Z",
      "latency_ms": 1847,
      "feedback": {
        "rating": 5,
        "thumbs": "up",
        "submittedAt": "2026-03-19T07:32:00Z"
      }
    }
  ],
  "pagination": { "page": 1, "limit": 50, "total": 8 }
}
```

---

#### `GET /sessions/{sessionId}/context-window`

Lấy context window tối ưu cho AI Agent (đã trim theo token budget).

**Query Params:**
- `maxTokens` (default: 2000): Token budget cho history
- `preserveFirst` (default: 3): Số tin đầu luôn giữ lại

**Response `200 OK`:**
```json
{
  "messages": [...],
  "totalTokensEstimated": 1840,
  "trimmedCount": 3,
  "hasSummary": true,
  "summaryText": "[Tóm tắt 3 tin nhắn trước: Nông dân hỏi về bệnh gỉ sắt...]"
}
```

---

## Feedback

---

#### `POST /messages/{messageId}/feedback`

Nông dân đánh giá chất lượng câu trả lời AI.

**Request Body:**
```json
{
  "rating": 5,
  "thumbs": "up",
  "comment": "Đúng rồi thầy ơi, thuốc đó tôi đã thấy ở đại lý"
}
```

**Fields:**
- `rating`: 1–5 (bắt buộc)
- `thumbs`: enum `up | down`
- `comment`: max 500 ký tự

**Response `200 OK`:**
```json
{
  "messageId": "msg_002_sess_20260319",
  "feedbackSaved": true,
  "usedForTraining": false
}
```

---

## RLHF Export (Internal)

---

#### `GET /feedback/export`

Export dữ liệu feedback cho RLHF pipeline. Chỉ internal.

**Headers:** `X-Service-Api-Key: {internal_key}`

**Query Params:**
- `minRating` (default: 4): Chỉ lấy feedback chất lượng cao
- `from`, `to`: khoảng thời gian
- `usedForTraining`: `false` (chỉ lấy chưa dùng)
- `limit` (max: 10000)

**Response `200 OK`:**
```json
{
  "data": [
    {
      "messageId": "msg_002_sess_20260319",
      "sessionId": "sess_20260319_AN_001",
      "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
      "userQuery": "Cây cà phê của tôi bị vàng lá...",
      "assistantResponse": "Dựa vào mô tả của bạn...",
      "contextUsed": { "ragDocuments": [...] },
      "feedback": { "rating": 5, "thumbs": "up" },
      "farmerContext": {
        "coffeeVariety": "Robusta",
        "soilType": "basalt",
        "province": "Đắk Lắk"
      }
    }
  ],
  "total": 1240,
  "exportedAt": "2026-03-19T03:00:00Z"
}
```

---

#### `POST /feedback/mark-trained`

Đánh dấu các messages đã được dùng để train.

**Headers:** `X-Service-Api-Key: {internal_key}`

**Request Body:**
```json
{ "messageIds": ["msg_001", "msg_002", "msg_003"] }
```

**Response `200 OK`:**
```json
{ "marked": 3 }
```

**© 2026 Bazan AI Project — Confidential**

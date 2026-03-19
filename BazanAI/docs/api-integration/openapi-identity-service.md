# OpenAPI 3.1 — Identity & User Context Service

| Trường | Nội dung |
|--------|---------|
| **Phiên bản API** | v1.0.0 |
| **Base URL** | `https://api.bazanai.vn/api/v1` |
| **Service Port** | 5001 (internal) |
| **Ngày tạo** | 2026-03-19 |
| **Liên quan** | ADR-001 (MongoDB), ADR-005 (YARP), Database ERD |

---

## Tổng quan

Identity Service quản lý toàn bộ vòng đời xác thực và hồ sơ nông dân. Đây là service duy nhất phát JWT token cho toàn hệ thống.

**Authentication:** Tất cả endpoint (trừ `/auth/*`) yêu cầu `Authorization: Bearer {token}`.

---

## Endpoints

### Auth Group

---

#### `POST /auth/register`

Đăng ký tài khoản mới và gửi OTP xác thực.

**Request Body:**
```json
{
  "phoneNumber": "+84901234567",
  "fullName": "Nguyễn Văn An",
  "province": "Đắk Lắk",
  "district": "Cư M'gar"
}
```

**Validation:**
- `phoneNumber`: Bắt buộc, regex `^\+84[0-9]{9}$`
- `fullName`: Bắt buộc, 2–100 ký tự
- `province`: Bắt buộc

**Response `201 Created`:**
```json
{
  "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
  "message": "OTP đã được gửi đến số điện thoại của bạn",
  "otpExpiresAt": "2026-03-19T07:35:00Z"
}
```

**Response `409 Conflict`:**
```json
{
  "error": "phone_already_registered",
  "message": "Số điện thoại này đã được đăng ký"
}
```

---

#### `POST /auth/verify-otp`

Xác thực OTP và nhận JWT token.

**Request Body:**
```json
{
  "phoneNumber": "+84901234567",
  "otp": "123456"
}
```

**Response `200 OK`:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 86400,
  "tokenType": "Bearer",
  "farmer": {
    "id": "64f1a2b3c4d5e6f7a8b9c0d1",
    "fullName": "Nguyễn Văn An",
    "phoneNumber": "+84901234567",
    "isProfileComplete": false
  }
}
```

**Response `400 Bad Request`:**
```json
{
  "error": "otp_invalid_or_expired",
  "message": "Mã OTP không hợp lệ hoặc đã hết hạn"
}
```

---

#### `POST /auth/login`

Đăng nhập và gửi OTP (luồng tương tự register cho số đã có).

**Request Body:**
```json
{ "phoneNumber": "+84901234567" }
```

**Response `200 OK`:**
```json
{
  "message": "OTP đã được gửi",
  "otpExpiresAt": "2026-03-19T07:35:00Z"
}
```

---

#### `POST /auth/refresh`

Làm mới access token.

**Request Body:**
```json
{ "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..." }
```

**Response `200 OK`:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 86400
}
```

---

#### `POST /auth/logout`

Thu hồi refresh token.

**Headers:** `Authorization: Bearer {token}`

**Response `204 No Content`**

---

### Farmers Group

---

#### `GET /farmers/{farmerId}`

Lấy thông tin cơ bản của nông dân.

**Headers:** `Authorization: Bearer {token}`

**Path Params:** `farmerId` — ObjectId string

**Response `200 OK`:**
```json
{
  "id": "64f1a2b3c4d5e6f7a8b9c0d1",
  "fullName": "Nguyễn Văn An",
  "phoneNumber": "+84901234567",
  "zaloId": "zalo_uid_123456",
  "preferredLanguage": "vi",
  "literacyLevel": "intermediate",
  "location": {
    "province": "Đắk Lắk",
    "district": "Cư M'gar",
    "commune": "Quảng Tiến",
    "coordinates": { "lat": 12.6702, "lng": 108.0388 },
    "altitude_m": 650
  },
  "createdAt": "2023-11-01T00:00:00Z",
  "updatedAt": "2026-03-19T08:00:00Z"
}
```

---

#### `PUT /farmers/{farmerId}/profile`

Cập nhật thông tin hồ sơ nông dân.

**Headers:** `Authorization: Bearer {token}`

**Request Body:**
```json
{
  "fullName": "Nguyễn Văn An",
  "zaloId": "zalo_uid_123456",
  "preferredLanguage": "vi",
  "literacyLevel": "intermediate",
  "location": {
    "province": "Đắk Lắk",
    "district": "Cư M'gar",
    "coordinates": { "lat": 12.6702, "lng": 108.0388 },
    "altitude_m": 650
  }
}
```

**Response `200 OK`:** Farmer object đã cập nhật

---

#### `GET /farmers/{farmerId}/farms`

Lấy danh sách vườn của nông dân.

**Response `200 OK`:**
```json
{
  "data": [
    {
      "id": "65a2b3c4d5e6f7a8b9c0d1e2",
      "name": "Vườn chính Cư M'gar",
      "totalArea_ha": 1.5,
      "numberOfTrees": 2250,
      "coffeeVarieties": ["Robusta", "TR4"],
      "soilType": "basalt",
      "soilPH": 6.2,
      "irrigationSystem": "drip",
      "certifications": ["VietGAP"],
      "location": {
        "coordinates": { "lat": 12.6705, "lng": 108.0390 },
        "altitude_m": 655
      }
    }
  ],
  "total": 1
}
```

---

#### `POST /farmers/{farmerId}/farms`

Thêm vườn mới.

**Request Body:**
```json
{
  "name": "Vườn phụ",
  "totalArea_ha": 0.8,
  "numberOfTrees": 1200,
  "coffeeVarieties": ["Robusta"],
  "soilType": "basalt",
  "irrigationSystem": "sprinkler",
  "location": {
    "coordinates": { "lat": 12.6800, "lng": 108.0500 },
    "altitude_m": 620
  }
}
```

**Response `201 Created`:** Farm object

---

#### `PUT /farmers/{farmerId}/farms/{farmId}`

Cập nhật thông tin vườn.

**Response `200 OK`:** Farm object đã cập nhật

---

#### `GET /farmers/{farmerId}/context`

Lấy toàn bộ UserContext cho AI Agent (endpoint nội bộ, Gateway forward header `X-Farmer-Id`).

**Headers:** `X-Service-Api-Key: {internal_key}`

**Response `200 OK`:** Full UserContext document (xem MongoDB Schema trong System Architecture Doc)

---

#### `PATCH /farmers/{farmerId}/context`

Cập nhật một phần UserContext (thường gọi bởi internal services).

**Headers:** `X-Service-Api-Key: {internal_key}`

**Request Body:**
```json
{
  "path": "behaviorProfile.preferredQueryTopics",
  "value": ["fertilizer", "pest_control", "price"]
}
```

**Response `200 OK`:** `{ "updated": true }`

---

## Error Schema chung

```json
{
  "error": "error_code_snake_case",
  "message": "Mô tả lỗi thân thiện với người dùng",
  "details": {},
  "traceId": "req-uuid-v4",
  "timestamp": "2026-03-19T07:30:00Z"
}
```

**HTTP Status codes:**

| Code | Ý nghĩa |
|------|---------|
| 200 | Success |
| 201 | Created |
| 204 | No content |
| 400 | Bad request / Validation error |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not found |
| 409 | Conflict |
| 429 | Rate limited |
| 500 | Internal server error |

---

## JWT Payload

```json
{
  "sub": "64f1a2b3c4d5e6f7a8b9c0d1",
  "name": "Nguyễn Văn An",
  "phone": "+84901234567",
  "role": "farmer",
  "farm_ids": ["65a2b3c4d5e6f7a8b9c0d1e2"],
  "province": "Dak Lak",
  "iat": 1710835200,
  "exp": 1710921600,
  "iss": "bazan-ai",
  "aud": "bazan-ai-services"
}
```

**© 2026 Bazan AI Project — Confidential**

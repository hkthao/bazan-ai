# Zalo OA Integration Guide — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Liên quan** | ADR-003, Notification Service, Prompt Engineering Playbook |

---

## 1. Tổng quan

Zalo OA (Official Account) là kênh thông báo chính của Bazan AI, chiếm >80% nông dân Tây Nguyên dùng Zalo hàng ngày. Hệ thống dùng Zalo OA để:
- Gửi cảnh báo bệnh, thời tiết, giá cà phê
- Nhận câu hỏi qua Zalo (Zalo Bot webhook)
- Gửi lịch canh tác định kỳ

---

## 2. Authentication

### 2.1 OA Access Token

```
OA Access Token — dùng để gọi Zalo API
Loại:       Long-lived token (90 ngày)
Header:     access_token: {OA_ACCESS_TOKEN}
```

Lưu token trong `.env` và rotate trước khi hết hạn qua Hangfire job:

```csharp
// Jobs/ZaloTokenRefreshJob.cs
public class ZaloTokenRefreshJob(IZaloApiClient zalo, ISecretsService secrets)
{
    // Chạy mỗi 80 ngày (trước khi token 90 ngày hết hạn)
    [Cron("0 0 */80 * *")]
    public async Task Execute()
    {
        var newToken = await zalo.RefreshAccessTokenAsync(
            appId:     secrets.Get("ZALO_APP_ID"),
            appSecret: secrets.Get("ZALO_APP_SECRET"),
            refreshToken: secrets.Get("ZALO_REFRESH_TOKEN")
        );
        await secrets.Set("ZALO_OA_ACCESS_TOKEN", newToken.AccessToken);
    }
}
```

---

## 3. Gửi tin nhắn

### 3.1 Text Message (cảnh báo cơ bản)

```
POST https://openapi.zalo.me/v3.0/oa/message/cs
Header: access_token: {OA_ACCESS_TOKEN}
```

```json
{
  "recipient": { "user_id": "{zalo_user_id_of_farmer}" },
  "message": {
    "text": "⚠ Bazan AI: Vườn TR4 của bạn có nguy cơ bệnh gỉ sắt cao do mưa kéo dài.\nKhuyến nghị: Phun Carbendazim 50SC trong 48h.\nXem chi tiết: https://app.bazanai.vn/alert/abc123"
  }
}
```

**Giới hạn text:** 2,000 ký tự/tin nhắn.

---

### 3.2 Structured Message — Cảnh báo bệnh

```json
{
  "recipient": { "user_id": "{zalo_user_id}" },
  "message": {
    "attachment": {
      "type": "template",
      "payload": {
        "template_type": "list",
        "elements": [
          {
            "title": "⚠ Phát hiện bệnh gỉ sắt",
            "subtitle": "Mức độ: Cao • Vườn Cư M'gar",
            "image_url": "https://cdn.bazanai.vn/icons/leaf-rust.png",
            "buttons": [
              {
                "title": "Xem phác đồ",
                "type": "oa.open.url",
                "payload": {
                  "url": "https://app.bazanai.vn/alert/abc123"
                }
              },
              {
                "title": "Hỏi thêm AI",
                "type": "oa.query.show",
                "payload": "Cách xử lý bệnh gỉ sắt"
              }
            ]
          }
        ]
      }
    }
  }
}
```

---

### 3.3 Notification Message (không cần user follow OA)

Gửi qua số điện thoại đã đăng ký (yêu cầu Zalo verify số điện thoại khi đăng ký Bazan AI):

```json
{
  "phone": "84901234567",
  "templateid": "bazan_disease_alert_v1",
  "template_data": {
    "disease_name": "bệnh gỉ sắt",
    "severity": "cao",
    "farm_name": "Vườn chính",
    "action": "Phun Carbendazim 50SC"
  },
  "tracking_id": "alert-uuid"
}
```

---

## 4. Webhook — Nhận tin nhắn từ nông dân

### 4.1 Webhook Setup

```
URL: https://api.bazanai.vn/webhooks/zalo
Method: POST
Verify token: {ZALO_WEBHOOK_VERIFY_TOKEN}
```

### 4.2 Incoming Message Payload

```json
{
  "app_id": "123456789",
  "timestamp": 1710835200000,
  "event_name": "user_send_text",
  "follower": {
    "id": "zalo_uid_123456",
    "name": "Nguyễn Văn An"
  },
  "message": {
    "text": "bệnh gỉ sắt phun thuốc gì thầy",
    "msg_id": "zalo-msg-id"
  }
}
```

### 4.3 Xử lý Webhook

```csharp
// Controllers/ZaloWebhookController.cs
[HttpPost("/webhooks/zalo")]
public async Task<IActionResult> HandleWebhook([FromBody] ZaloWebhookPayload payload)
{
    // 1. Verify signature
    var signature = Request.Headers["X-ZaloOA-Signature"].FirstOrDefault();
    if (!_zaloService.VerifySignature(payload, signature))
        return Unauthorized();

    // 2. Lookup farmer bằng zaloId
    var farmer = await _farmerRepo.FindByZaloIdAsync(payload.Follower.Id);
    if (farmer == null)
    {
        // Nông dân chưa liên kết Zalo → hướng dẫn đăng ký
        await _zaloService.SendTextAsync(payload.Follower.Id,
            "Chào bạn! Vui lòng tải app Bazan AI để đăng ký và liên kết Zalo.");
        return Ok();
    }

    // 3. Tạo session và route sang AI Orchestrator
    var sessionId = await _conversationService.CreateSessionAsync(
        farmerId: farmer.Id,
        channel: "zalo_bot"
    );

    // 4. Gửi câu hỏi vào AI (sync — Zalo timeout 5s)
    var response = await _orchestratorService.GetShortResponseAsync(
        sessionId:  sessionId,
        farmerId:   farmer.Id,
        message:    payload.Message.Text,
        maxTokens:  200   // Ngắn hơn vì Zalo text limit
    );

    // 5. Reply ngay
    await _zaloService.SendTextAsync(payload.Follower.Id, response);

    return Ok();
}
```

---

## 5. Message Templates

### Template Map

| Event | Template | Channel |
|-------|---------|---------|
| DiseaseAlertGenerated | `bazan_disease_alert_v1` | Zalo + FCM |
| CoffeePriceUpdated (>5%) | `bazan_price_alert_v1` | Zalo |
| WeatherAlertTriggered | `bazan_weather_alert_v1` | Zalo + SMS |
| IrrigationReminderCreated | `bazan_irrigation_v1` | FCM (silent) |
| HarvestWindowOpened | `bazan_harvest_v1` | Zalo |

---

**© 2026 Bazan AI Project — Confidential**

---
---

# External API Dependencies — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |

---

## Danh sách đầy đủ

| # | Service | Provider | Usage | Cost model | SLA | Fallback |
|---|---------|---------|-------|-----------|-----|---------|
| 1 | **GPT-4o Chat** | OpenAI | AI generation | $2.50/1M in + $10/1M out | 99.9% | Retry 3x, degrade to error message |
| 2 | **text-embedding-3-large** | OpenAI | Document & query embedding | $0.13/1M tokens | 99.9% | Redis cache, queue for retry |
| 3 | **gpt-4o-mini** | OpenAI | Intent classification, re-ranking | $0.15/1M in | 99.9% | Rule-based fallback |
| 4 | **Zalo OA API** | Zalo | Push notification, bot webhook | Free (OA approved) | 99.5% | FCM fallback |
| 5 | **Firebase FCM** | Google | Mobile push notification | Free tier đủ dùng | 99.5% | SMS fallback |
| 6 | **OpenWeatherMap** | OpenWeather | Weather forecast | $40/mo (Pro) | 99.9% | Cache 3h, skip if down |
| 7 | **Vietnam Meteorology API** | VNMHA | Local weather accuracy | Free (government) | 95% | OpenWeatherMap primary |
| 8 | **ICE Futures API** | ICE | Coffee price data | $200/mo | 99.5% | Cache 5min, last known price |
| 9 | **Twilio SMS** | Twilio | SMS fallback notification | $0.0079/SMS | 99.95% | Skip if both Zalo+FCM down |
| 10 | **Computer Vision API** | Azure CV / Custom | Disease detection from images | Pay-per-call ~$1.50/1K | 99.9% | Return low-confidence, ask for more info |

---

## Dependency Matrix — Ai cần gì

| Service | OpenAI | Zalo | FCM | Weather | ICE | Twilio | CV API |
|---------|--------|------|-----|---------|-----|--------|--------|
| AI Orchestrator | ✅ | — | — | — | — | — | — |
| Knowledge RAG | ✅ | — | — | — | — | — | — |
| Notification Svc | — | ✅ | ✅ | — | — | ✅ | — |
| Weather Svc | — | — | — | ✅ | — | — | — |
| Market Svc | — | — | — | — | ✅ | — | — |
| Agronomy Svc | — | — | — | — | — | — | ✅ |

---

## Circuit Breaker Config per dependency

```csharp
// Polly policy per external API
var openAiPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
    .WrapAsync(Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

---

## Cost Alert Thresholds

| Service | Alert at | Action |
|---------|---------|--------|
| OpenAI total | > $2,000/month | Review caching, optimize prompts |
| ICE Futures | > $250/month | Check for redundant calls |
| Twilio SMS | > $50/month | Check fallback logic is correct |
| Azure CV | > $100/month | Check for unnecessary calls |

---

**© 2026 Bazan AI Project — Confidential**

---
---

# API Versioning Strategy — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |

---

## 1. Chiến lược

Bazan AI dùng **URL path versioning**: `/api/v1/`, `/api/v2/`.

**Lý do:** Đơn giản nhất cho mobile client — URL rõ ràng, không cần xử lý header. Phù hợp với team nhỏ Phase 1.

---

## 2. Quy tắc tạo version mới

**Cần tạo version mới (breaking change) khi:**
- Xóa endpoint hoặc field trong response
- Đổi tên field bắt buộc
- Đổi kiểu dữ liệu của field
- Thay đổi logic nghiệp vụ ảnh hưởng kết quả

**KHÔNG cần version mới (non-breaking):**
- Thêm field mới vào response (client bỏ qua)
- Thêm endpoint mới
- Thêm query parameter optional
- Thay đổi logic nội bộ không ảnh hưởng contract

---

## 3. Vòng đời version

```
v1 (current) → v2 (new)
     │
     ├── Announce deprecation: 3 tháng trước khi sunset
     ├── Deprecation header: Deprecation: true, Sunset: {date}
     ├── Support period: 6 tháng sau khi v2 release
     └── Sunset: tắt v1, trả về 410 Gone
```

---

## 4. Implementation (.NET 8)

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;  // Thêm header: api-supported-versions
});

// Controller
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/farmers")]
public class FarmersController : ControllerBase
{
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetV1(string id) { ... }

    [HttpGet("{id}")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> GetV2(string id) { ... }
}
```

---

## 5. Deprecation Headers

```http
HTTP/1.1 200 OK
Deprecation: true
Sunset: Sat, 19 Sep 2026 00:00:00 GMT
Link: <https://api.bazanai.vn/api/v2/farmers>; rel="successor-version"
```

---

**© 2026 Bazan AI Project — Confidential**

---
---

# SDK & Client Library Guide — Mobile Team

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |

---

## 1. HTTP Client (Flutter / Dart)

### 1.1 Setup

```dart
// lib/core/api/bazan_api_client.dart
import 'package:dio/dio.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';

class BazanApiClient {
  static const _baseUrl = 'https://api.bazanai.vn/api/v1';
  late final Dio _dio;

  BazanApiClient(String accessToken) {
    _dio = Dio(BaseOptions(
      baseUrl: _baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $accessToken',
        'X-App-Version': '2.1.0',
        'X-Platform': Platform.isAndroid ? 'android' : 'ios',
      },
    ));

    _dio.interceptors.addAll([
      _TokenRefreshInterceptor(this),
      _CorrelationIdInterceptor(),
      _RetryInterceptor(retries: 3),
      if (kDebugMode) PrettyDioLogger(),
    ]);
  }
}
```

### 1.2 Token Refresh Interceptor

```dart
class _TokenRefreshInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      try {
        final newToken = await _refreshToken();
        // Retry original request với token mới
        final opts = Options(headers: {'Authorization': 'Bearer $newToken'});
        final response = await _dio.request(
          err.requestOptions.path,
          options: opts,
        );
        handler.resolve(response);
      } catch (_) {
        // Refresh failed → logout
        AuthService.logout();
        handler.next(err);
      }
    } else {
      handler.next(err);
    }
  }
}
```

### 1.3 Correlation ID Interceptor

```dart
class _CorrelationIdInterceptor extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    options.headers['X-Correlation-Id'] = const Uuid().v4();
    handler.next(options);
  }
}
```

---

## 2. API Service Classes

```dart
// lib/features/agronomy/data/agronomy_api_service.dart
class AgronomyApiService {
  final BazanApiClient _client;

  Future<DiagnosisResult> diagnose({
    required String farmId,
    required String symptomDescription,
    String? imageUrl,
  }) async {
    final response = await _client.post('/agronomy/diagnose', data: {
      'farmId': farmId,
      'symptomDescription': symptomDescription,
      if (imageUrl != null) 'imageUrl': imageUrl,
    });
    return DiagnosisResult.fromJson(response.data);
  }

  Future<List<Activity>> getSchedule(String farmId) async {
    final response = await _client.get('/agronomy/schedule/$farmId');
    return (response.data['activities'] as List)
        .map((a) => Activity.fromJson(a))
        .toList();
  }
}
```

---

## 3. Offline-aware Repository Pattern

```dart
// lib/features/agronomy/data/agronomy_repository.dart
class AgronomyRepository {
  final AgronomyApiService _api;
  final AgronomyLocalService _local;    // Drift SQLite
  final ConnectivityService _connectivity;

  Future<List<Activity>> getSchedule(String farmId) async {
    if (await _connectivity.hasConnection()) {
      try {
        final fresh = await _api.getSchedule(farmId);
        await _local.saveSchedule(farmId, fresh);  // Update cache
        return fresh;
      } catch (e) {
        // Network error → fallback to cache
        return _local.getSchedule(farmId);
      }
    } else {
      // Offline → return cached
      return _local.getSchedule(farmId);
    }
  }
}
```

---

## 4. Error Handling

```dart
// lib/core/errors/api_error.dart
class ApiError {
  final String code;
  final String message;
  final int statusCode;

  static ApiError fromDioException(DioException e) {
    if (e.response != null) {
      final data = e.response!.data as Map<String, dynamic>;
      return ApiError(
        code: data['error'] ?? 'unknown_error',
        message: data['message'] ?? 'Có lỗi xảy ra',
        statusCode: e.response!.statusCode ?? 0,
      );
    }

    if (e.type == DioExceptionType.connectionTimeout) {
      return ApiError(
        code: 'connection_timeout',
        message: 'Kết nối mạng chậm, vui lòng thử lại',
        statusCode: 0,
      );
    }

    return ApiError(code: 'network_error', message: 'Không có kết nối mạng', statusCode: 0);
  }

  bool get isRetryable => statusCode == 429 || statusCode >= 500;
  bool get requiresAuth => statusCode == 401;
  bool get isForbidden => statusCode == 403;
}
```

---

## 5. Environment Config

```dart
// lib/core/config/env_config.dart
abstract class EnvConfig {
  static const baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://api.bazanai.vn/api/v1',
  );

  static const signalrHubUrl = String.fromEnvironment(
    'SIGNALR_HUB_URL',
    defaultValue: 'wss://api.bazanai.vn/hub/chat',
  );
}

// Dart define khi build:
// flutter run --dart-define=API_BASE_URL=https://staging.bazanai.vn/api/v1
```

---

## 6. Environment URLs

| Environment | API Base URL | SignalR Hub |
|------------|-------------|------------|
| Development | `http://localhost:8080/api/v1` | `ws://localhost:8080/hub/chat` |
| Staging | `https://staging.bazanai.vn/api/v1` | `wss://staging.bazanai.vn/hub/chat` |
| Production | `https://api.bazanai.vn/api/v1` | `wss://api.bazanai.vn/hub/chat` |

---

**© 2026 Bazan AI Project — Confidential**

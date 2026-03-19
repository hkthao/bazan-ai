# ADR-005 — API Gateway: YARP (Yet Another Reverse Proxy)

| Trường | Nội dung |
|--------|---------|
| **ID** | ADR-005 |
| **Tiêu đề** | Lựa chọn YARP làm API Gateway cho Bazan AI |
| **Trạng thái** | Accepted |
| **Ngày tạo** | 2026-03-19 |
| **Ngày cập nhật** | 2026-03-19 |
| **Tác giả** | Principal Software Architect |
| **Người review** | Tech Lead, DevOps Lead |
| **Liên quan** | ADR-001 (MongoDB), ADR-003 (RabbitMQ), ADR-004 (Semantic Kernel) |

---

## 1. Bối cảnh

Bazan AI là nền tảng Agentic Microservices gồm **10+ services độc lập** (.NET 8, Python FastAPI) phục vụ nông dân cà phê Tây Nguyên qua nhiều kênh: Mobile App (Flutter), Web, Zalo Bot, SMS. Hệ thống cần một **điểm vào duy nhất (single entry point)** để:

- Định tuyến request đến đúng upstream service
- Xác thực JWT token tập trung, tránh mỗi service phải tự validate
- Kiểm soát lưu lượng (rate limiting) — đặc biệt quan trọng vì nông dân vùng nông thôn có thể gửi request lặp do mạng không ổn định
- Aggregate response cho mobile client (BFF pattern) để giảm số lượng round-trip trên mạng 3G/4G

Không có API Gateway, client phải biết địa chỉ từng microservice — điều này vi phạm nguyên tắc encapsulation của microservices và làm phức tạp việc deploy, scale, và bảo mật.

### 1.1 Yêu cầu đặt ra

| Yêu cầu | Mức độ | Mô tả |
|---------|--------|-------|
| **JWT Validation** | Must-have | Validate token tập trung, forward claims đến upstream |
| **Request Routing** | Must-have | Path-based routing đến 10+ services |
| **Rate Limiting** | Must-have | Sliding window, phân biệt free/premium tier |
| **Load Balancing** | Must-have | Round-robin giữa các instance của cùng service |
| **Health Checks** | Must-have | Passive + active health check cho upstream |
| **WebSocket / SignalR** | Must-have | Proxy persistent connection cho AI chat streaming |
| **HTTPS Termination** | Must-have | TLS offloading tại Gateway |
| **Circuit Breaker** | Should-have | Polly integration khi upstream không phản hồi |
| **Request Aggregation** | Should-have | BFF pattern gom nhiều call thành 1 response |
| **Observability** | Should-have | Correlation ID, structured logging, metrics |
| **gRPC Proxy** | Nice-to-have | Forward gRPC calls giữa internal services |
| **Hot Reload Config** | Nice-to-have | Cập nhật route mà không restart service |

### 1.2 Ràng buộc kỹ thuật

- Toàn bộ backend viết bằng **.NET 8 / C#** — ưu tiên giải pháp trong hệ sinh thái
- Team chưa có kinh nghiệm vận hành Kubernetes — cần giải pháp chạy tốt trên **Docker Compose**
- Budget giai đoạn MVP giới hạn — ưu tiên **open-source, không tốn license fee**
- Cần **tích hợp sâu với Polly** (đã dùng trong các service khác) cho resilience patterns

---

## 2. Các phương án đã xem xét

### Phương án A — YARP (Yet Another Reverse Proxy)

**Mô tả:** Thư viện reverse proxy do Microsoft phát triển, chạy như một ASP.NET Core middleware. Config bằng C# code hoặc JSON, hỗ trợ hot reload.

**Ưu điểm:**
- Native .NET 8 — cùng stack với toàn bộ backend, không cần học thêm ngôn ngữ/runtime mới
- Tích hợp trực tiếp với ASP.NET Core middleware pipeline: JWT validation, rate limiting, logging dùng chung cơ chế với các service khác
- Polly integration sẵn có cho circuit breaker, retry, timeout
- Hỗ trợ WebSocket và gRPC proxy — cần thiết cho SignalR chat streaming
- Hot reload config từ `appsettings.json` mà không cần restart container
- Open source, MIT license, được Microsoft maintain tích cực
- Chạy trong Docker container bình thường — không cần Kubernetes hay sidecar

**Nhược điểm:**
- Ít ecosystem plugin hơn Kong/Traefik (phải tự viết một số middleware)
- Không có built-in Web UI để quản lý route (phải dùng code hoặc file JSON)
- Ít tài liệu tiếng Việt

**Phù hợp nhất với:** Team .NET, cần tùy chỉnh logic routing/auth phức tạp trong code

---

### Phương án B — Kong Gateway (Open Source)

**Mô tả:** API Gateway mạnh mẽ dựa trên Nginx + Lua, có hệ sinh thái plugin phong phú.

**Ưu điểm:**
- Plugin system đồ sộ (rate limiting, OAuth2, logging) chỉ cần enable
- Web UI (Kong Manager) để quản lý route trực quan
- Được dùng rộng rãi trong production lớn, tài liệu nhiều

**Nhược điểm:**
- Runtime là Lua/Nginx — hoàn toàn khác stack .NET, team phải học thêm
- Tích hợp với ASP.NET Core JWT validation phức tạp hơn (cần custom plugin)
- Docker image nặng (~200MB vs ~100MB của YARP)
- Kong DB mode cần thêm PostgreSQL hoặc Cassandra, tăng complexity infrastructure
- DBless mode hạn chế tính năng
- License: Kong Enterprise (một số feature cần trả phí)

**Không chọn vì:** Thêm tech stack ngoài .NET làm tăng cognitive load cho team nhỏ giai đoạn MVP

---

### Phương án C — Traefik

**Mô tả:** Cloud-native reverse proxy, tự động discover service qua Docker labels.

**Ưu điểm:**
- Auto-discovery container qua Docker labels — zero config routing khi dùng Docker Compose
- Dashboard đẹp, built-in
- Let's Encrypt HTTPS tự động
- Nhẹ, hiệu năng cao

**Nhược điểm:**
- Middleware pipeline hạn chế — không thể viết custom C# logic trong request pipeline
- JWT validation phải dùng external middleware (Forward Auth), thêm một hop network
- Không native WebSocket proxy ổn định cho SignalR với sticky sessions
- Không tích hợp Polly — phải implement resilience ở tầng khác
- Config bằng TOML/YAML — không đồng nhất với phần còn lại của project (C# + JSON)

**Không chọn vì:** Thiếu khả năng custom C# middleware và JWT integration kém hơn YARP

---

### Phương án D — Nginx (custom config)

**Mô tả:** Web server/reverse proxy truyền thống, config bằng nginx.conf.

**Ưu điểm:**
- Cực kỳ nhẹ và nhanh
- Mọi DevOps đều biết
- Tài liệu đồ sộ

**Nhược điểm:**
- Không có JWT validation built-in — phải kết hợp với `auth_request` module và external service
- Rate limiting cơ bản, không đủ cho multi-tier (free/premium)
- Không hỗ trợ dynamic config tốt — mỗi thay đổi route cần reload
- Không tích hợp OpenTelemetry, phải dùng module bên thứ ba
- Không hỗ trợ gRPC proxy tốt

**Không chọn vì:** Thiếu native JWT validation và khả năng tích hợp với .NET observability stack

---

## 3. Quyết định

**Chọn Phương án A — YARP** làm API Gateway cho Bazan AI.

---

## 4. Lý do

### 4.1 Đồng nhất tech stack

Toàn bộ backend Bazan AI viết bằng .NET 8. Dùng YARP nghĩa là Gateway cũng là một ASP.NET Core app — team có thể dùng chung:

- **Serilog** cho structured logging với correlation ID
- **OpenTelemetry** cho distributed tracing xuyên suốt Gateway → Services
- **Polly** cho circuit breaker, retry khi upstream service down
- **FluentValidation / MediatR** nếu cần custom logic phức tạp
- **Microsoft.AspNetCore.RateLimiting** cho rate limiting sliding window

Không phải học thêm runtime mới, không phải maintain Lua script hay TOML config song song với C# codebase.

### 4.2 JWT validation tập trung trong .NET middleware

```csharp
// Gateway Program.cs — JWT validation + claim forwarding
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = "bazan-ai",
            ValidateAudience = true,
            ValidAudience = "bazan-ai-services",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JwtSettings:SecretKey"]!))
        };
    });

// Transform: forward farmer context claims đến upstream
builder.Services.AddReverseProxy()
    .AddTransforms(ctx => {
        ctx.AddRequestTransform(async reqCtx => {
            if (reqCtx.HttpContext.User.Identity?.IsAuthenticated == true) {
                var farmerId = reqCtx.HttpContext.User.FindFirst("sub")?.Value;
                var farmIds  = reqCtx.HttpContext.User.FindFirst("farm_ids")?.Value;
                reqCtx.ProxyRequest.Headers.TryAddWithoutValidation("X-Farmer-Id", farmerId);
                reqCtx.ProxyRequest.Headers.TryAddWithoutValidation("X-Farm-Ids", farmIds);
            }
        });
    });
```

Upstream services nhận claims qua header mà không cần validate JWT lần nữa — giảm latency và tránh lặp logic.

### 4.3 WebSocket / SignalR proxy

Bazan AI dùng SignalR để stream token AI về client theo thời gian thực. YARP hỗ trợ WebSocket proxy native:

```json
{
  "Routes": {
    "orchestrator-chat": {
      "ClusterId": "orchestrator-cluster",
      "Match": { "Path": "/hub/{**catch-all}" }
    }
  },
  "Clusters": {
    "orchestrator-cluster": {
      "Destinations": {
        "primary": { "Address": "http://ai-orchestrator:5010/" }
      }
    }
  }
}
```

YARP tự negotiate WebSocket upgrade và proxy bidirectional stream — không cần cấu hình thêm.

### 4.4 Hot reload routing config

Route config có thể thay đổi runtime mà không restart container:

```csharp
// Đọc route từ appsettings.json — tự reload khi file thay đổi
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
```

Quan trọng khi cần rollout service mới (ví dụ: deploy `weather-svc:v2` bên cạnh `v1`) mà không ảnh hưởng traffic hiện tại.

### 4.5 Polly resilience tích hợp

```csharp
builder.Services.AddHttpClient("agronomy-client")
    .AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30)))
    .AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

Khi `agronomy-svc` down, Gateway trả về `503` ngay thay vì để request timeout — quan trọng trên mạng nông thôn latency cao.

---

## 5. Thiết kế triển khai

### 5.1 Route Configuration

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "ClusterId": "identity-cluster",
        "Match": { "Path": "/api/v1/auth/{**catch-all}" },
        "Metadata": { "RequireAuth": "false" }
      },
      "farmers-route": {
        "ClusterId": "identity-cluster",
        "AuthorizationPolicy": "RequireAuthenticatedUser",
        "Match": { "Path": "/api/v1/farmers/{**catch-all}" }
      },
      "agronomy-route": {
        "ClusterId": "agronomy-cluster",
        "AuthorizationPolicy": "RequireAuthenticatedUser",
        "Match": { "Path": "/api/v1/agronomy/{**catch-all}" }
      },
      "market-route": {
        "ClusterId": "market-cluster",
        "AuthorizationPolicy": "RequireAuthenticatedUser",
        "Match": { "Path": "/api/v1/market/{**catch-all}" }
      },
      "conversations-route": {
        "ClusterId": "conversation-cluster",
        "AuthorizationPolicy": "RequireAuthenticatedUser",
        "Match": { "Path": "/api/v1/sessions/{**catch-all}" }
      },
      "media-route": {
        "ClusterId": "media-cluster",
        "AuthorizationPolicy": "RequireAuthenticatedUser",
        "Match": { "Path": "/api/v1/media/{**catch-all}" }
      },
      "signalr-route": {
        "ClusterId": "orchestrator-cluster",
        "AuthorizationPolicy": "RequireAuthenticatedUser",
        "Match": { "Path": "/hub/{**catch-all}" }
      },
      "health-route": {
        "ClusterId": "identity-cluster",
        "Match": { "Path": "/health" },
        "Metadata": { "RequireAuth": "false" }
      }
    },
    "Clusters": {
      "identity-cluster": {
        "LoadBalancingPolicy": "RoundRobin",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:10",
            "Timeout": "00:00:05",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          }
        },
        "Destinations": {
          "primary": { "Address": "http://identity-svc:5001/" }
        }
      },
      "agronomy-cluster": {
        "LoadBalancingPolicy": "RoundRobin",
        "HealthCheck": {
          "Active": { "Enabled": true, "Interval": "00:00:10", "Path": "/health" }
        },
        "Destinations": {
          "primary": { "Address": "http://agronomy-svc:5004/" }
        }
      }
    }
  }
}
```

### 5.2 Rate Limiting — Multi-tier

```csharp
builder.Services.AddRateLimiter(options => {
    // Free tier: 60 requests / phút
    options.AddSlidingWindowLimiter("free-tier", opt => {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.SegmentsPerWindow = 6;
        opt.QueueLimit = 10;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Premium tier (HTX, cán bộ kỹ thuật): 300 requests / phút
    options.AddSlidingWindowLimiter("premium-tier", opt => {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 300;
        opt.SegmentsPerWindow = 6;
        opt.QueueLimit = 50;
    });

    // AI Orchestrator: giới hạn riêng vì tốn OpenAI token
    options.AddSlidingWindowLimiter("ai-tier", opt => {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 20;
        opt.SegmentsPerWindow = 4;
    });

    options.OnRejected = async (ctx, _) => {
        ctx.HttpContext.Response.StatusCode = 429;
        await ctx.HttpContext.Response.WriteAsJsonAsync(new {
            error = "rate_limit_exceeded",
            message = "Quá nhiều yêu cầu, vui lòng thử lại sau.",
            retryAfter = 60
        });
    };
});
```

### 5.3 Middleware Pipeline Order

```
Request
  │
  ├─ CorrelationIdMiddleware      — sinh X-Correlation-Id nếu chưa có
  ├─ RequestLoggingMiddleware     — log method, path, IP, user-agent
  ├─ HTTPS Redirection
  ├─ Rate Limiting                — check rate limit dựa trên tier
  ├─ Authentication (JWT)         — validate Bearer token
  ├─ Authorization                — check RequireAuthenticatedUser policy
  ├─ Request Transform            — inject X-Farmer-Id, X-Farm-Ids headers
  ├─ YARP Proxy                   — forward đến upstream cluster
  └─ Response Logging             — log status code, duration_ms
```

### 5.4 Correlation ID Middleware

```csharp
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue(CorrelationHeader, out var id))
            id = Guid.NewGuid().ToString("N");

        ctx.Response.Headers[CorrelationHeader] = id.ToString();

        using (LogContext.PushProperty("CorrelationId", id.ToString()))
            await next(ctx);
    }
}
```

### 5.5 Health Check Aggregation

```csharp
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://identity-svc:5001/health"),    "identity-svc")
    .AddUrlGroup(new Uri("http://agronomy-svc:5004/health"),    "agronomy-svc")
    .AddUrlGroup(new Uri("http://market-svc:5005/health"),      "market-svc")
    .AddUrlGroup(new Uri("http://conversation-svc:5002/health"),"conversation-svc")
    .AddUrlGroup(new Uri("http://ai-orchestrator:5010/health"), "ai-orchestrator")
    .AddUrlGroup(new Uri("http://knowledge-rag:5003/health"),   "knowledge-rag");

// Expose tại /healthz — dùng cho Docker HEALTHCHECK và load balancer
app.MapHealthChecks("/healthz", new HealthCheckOptions {
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 5.6 Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["BazanAI.Gateway/BazanAI.Gateway.csproj", "BazanAI.Gateway/"]
RUN dotnet restore "BazanAI.Gateway/BazanAI.Gateway.csproj"
COPY . .
WORKDIR "/src/BazanAI.Gateway"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
RUN addgroup -S bazan && adduser -S bazan -G bazan
USER bazan
COPY --from=build /app/publish .
HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
    CMD wget -qO- http://localhost:8080/healthz || exit 1
ENTRYPOINT ["dotnet", "BazanAI.Gateway.dll"]
```

---

## 6. Hậu quả & Trade-offs

### 6.1 Tích cực

- **Giảm cognitive load:** Team chỉ cần biết .NET — không học thêm Lua, TOML, hay Nginx config
- **Custom middleware dễ dàng:** Bất kỳ logic nào (ví dụ: inject farmer context vào header, ban IP, A/B routing) đều viết bằng C# thuần
- **Debug dễ:** Toàn bộ log từ Gateway, services đều qua Serilog → Seq, cùng format, cùng correlation ID
- **Không license fee:** MIT license, không giới hạn tính năng theo tier
- **Hot reload:** Thêm service mới không cần restart Gateway

### 6.2 Tiêu cực & Cách giảm thiểu

| Trade-off | Mức độ ảnh hưởng | Cách giảm thiểu |
|-----------|-----------------|-----------------|
| Không có Web UI quản lý route | Thấp | Dùng file JSON + VS Code, đủ cho team nhỏ |
| Ít plugin hơn Kong | Thấp | Tự viết middleware C# — nhanh hơn học Lua plugin |
| Single point of failure nếu không scale | Trung bình | Chạy 2+ instance trong Docker Compose hoặc K8s sau này |
| Không có caching tầng Gateway | Thấp | Redis cache ở tầng service, Gateway không cần cache |

### 6.3 Rủi ro

**Rủi ro 1:** YARP là thư viện tương đối mới (GA từ 2021), API có thể thay đổi trong các major release.

Giảm thiểu: Pin version cụ thể (`Microsoft.ReverseProxy 2.x`), viết integration test cho gateway behavior, theo dõi YARP changelog.

**Rủi ro 2:** Khi scale lên K8s (Phase 4), có thể cần migrate sang Ingress Controller (Nginx Ingress, Traefik).

Giảm thiểu: Giữ business logic (auth, rate limit) trong YARP code, routing config trong JSON — dễ migrate hơn là nhúng logic vào Nginx config. Ngoài ra, YARP chạy tốt trong K8s như một Deployment thông thường — không nhất thiết phải replace.

---

## 7. Tiêu chí đánh giá lại quyết định

Xem xét lại ADR này nếu xảy ra một trong các trường hợp sau:

- Số lượng service vượt **30+** và việc quản lý route bằng JSON trở nên phức tạp → xem xét Kong hoặc AWS API Gateway
- Team mở rộng lên **3+ ngôn ngữ backend** (Go, Java, v.v.) → Gateway trung lập ngôn ngữ sẽ phù hợp hơn
- Có yêu cầu **API monetization** (usage-based billing, developer portal) → Kong Enterprise có tính năng này sẵn
- Latency P99 của Gateway vượt **50ms** sau khi tối ưu → profile và đánh giá lại

---

## 8. Tham khảo

- [YARP Documentation — Microsoft](https://microsoft.github.io/reverse-proxy/)
- [YARP GitHub Repository](https://github.com/microsoft/reverse-proxy)
- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Polly Documentation](https://www.pollydocs.org/)
- [Kong vs YARP Comparison — InfoQ 2024](https://www.infoq.com/articles/api-gateway-comparison/)
- ADR-001 — MongoDB Primary Database
- ADR-003 — RabbitMQ Messaging
- ADR-004 — Semantic Kernel Orchestration

---

*Tài liệu này tuân theo template ADR của Michael Nygard (2011) và được điều chỉnh theo tiêu chuẩn IBM Architecture Framework.*

**© 2026 Bazan AI Project — Confidential**

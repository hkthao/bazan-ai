# Security Architecture Document — Bazan AI
## Kiến trúc Bảo mật Toàn diện

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Principal Architect · Security Lead |
| **Người review** | DevOps Lead, Backend Lead, Legal |
| **Phân loại** | Confidential — Internal Only |
| **Liên quan** | Threat Model, API Security Checklist, Secrets Management, Network Topology |

---

## 1. Security Principles

Bazan AI áp dụng **Defense in Depth** — không tin vào một lớp bảo vệ duy nhất:

```
LAYER 1: Perimeter (Nginx, Firewall, DDoS basic)
LAYER 2: Gateway (JWT validation, Rate limiting, CORS)
LAYER 3: Application (Authorization, Input validation, OWASP)
LAYER 4: Data (Encryption at-rest, Field-level PII masking)
LAYER 5: Infrastructure (Network isolation, Secrets management)
LAYER 6: Monitoring (Anomaly detection, Audit logs, Alerting)
```

**Zero-Trust Principles:**
- Không tin tưởng bất kỳ request nào mặc định, kể cả internal
- Xác thực và ủy quyền ở mọi lớp
- Least privilege — mỗi service chỉ có quyền tối thiểu cần thiết
- Assume breach — thiết kế như thể attacker đã vào được bên trong

---

## 2. Authentication Architecture

### 2.1 Farmer Authentication Flow

```
Farmer → POST /auth/login (phone number)
    │
    ▼
Identity Service
    ├── Rate limit: 5 OTP requests/15 phút per phone
    ├── Blacklist check (banned phones)
    └── Generate 6-digit OTP → Zalo/SMS (TTL: 5 phút)
    │
Farmer → POST /auth/verify-otp
    │
    ▼
Identity Service
    ├── OTP validation (Redis, time-constant comparison)
    ├── Increment failed attempts (lockout after 5 fails)
    └── Generate JWT pair:
        Access Token:  RS256, 24h TTL
        Refresh Token: opaque UUID, 30d TTL, stored hashed in MongoDB
    │
Client → lưu token trong:
    Android: EncryptedSharedPreferences (AES-256-GCM)
    iOS:     Keychain (kSecAttrAccessibleWhenUnlockedThisDeviceOnly)
```

### 2.2 JWT Security

```csharp
// Sử dụng RS256 (asymmetric) thay vì HS256 (symmetric)
// Lý do: Public key có thể share cho services verify mà không cần share secret

var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, farmer.Id),
        new Claim("role", "farmer"),
        new Claim("farm_ids", string.Join(",", farmer.FarmIds)),
        new Claim("province", farmer.Province),
        // KHÔNG đưa sensitive data vào token (phone, GPS, etc.)
    }),
    Expires = DateTime.UtcNow.AddHours(24),
    Issuer = "bazan-ai",
    Audience = "bazan-ai-services",
    SigningCredentials = new SigningCredentials(
        _rsaPrivateKey,           // Private key — chỉ Identity Service có
        SecurityAlgorithms.RsaSha256
    )
};

// Services verify bằng public key (không cần private key)
tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = _rsaPublicKey,  // Public key — share cho tất cả services
    ValidateIssuer = true,
    ValidIssuer = "bazan-ai",
    ValidateAudience = true,
    ValidAudience = "bazan-ai-services",
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(5)
};
```

### 2.3 Service-to-Service Authentication

```csharp
// Phase 1: API Key trong header
// X-Service-Api-Key: {hashed_api_key}

// Mỗi service có API key riêng, lưu trong .env
// Gateway validate trước khi forward
// Key rotation: hàng quý

// Phase 2 (K8s): mTLS qua Istio
// Service A → Certificate → Istio sidecar → Service B
// Không cần code thay đổi — Istio handle transparent
```

---

## 3. Authorization Model

### 3.1 Role-Based Access Control (RBAC)

| Role | Mô tả | Quyền |
|------|-------|-------|
| `farmer` | Nông dân thường | Đọc/ghi data của chính mình |
| `farmer_premium` | HTX member | Mọi quyền của farmer + team data |
| `agronomist` | Cán bộ kỹ thuật | Đọc tất cả, ghi knowledge base |
| `admin` | Quản trị viên | Full access |
| `service` | Internal service | Các endpoint nội bộ |

### 3.2 Resource-Level Authorization

```csharp
// Farmer chỉ được access data của chính mình
// Kiểm tra tại Application layer, không chỉ Gateway

public class FarmerAuthorizationHandler
    : AuthorizationHandler<SamefarmerRequirement, Farm>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx,
        SameFarmerRequirement requirement,
        Farm resource)
    {
        var farmerId = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var farmIds  = ctx.User.FindFirst("farm_ids")?.Value?.Split(',');

        // Farm phải thuộc về farmer đang request
        if (farmIds != null && farmIds.Contains(resource.FarmId))
            ctx.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

---

## 4. Data Security

### 4.1 Encryption at Rest

| Data store | Encryption | Key management |
|-----------|-----------|---------------|
| MongoDB | WiredTiger encryption (AES-256) | Key trong `.env` → Phase 2: HashiCorp Vault |
| Qdrant storage | OS-level encryption (dm-crypt) | Cloud provider managed |
| MinIO | SSE-S3 (AES-256) | MinIO KMS |
| Redis | No encryption (non-persistent sensitive data) | Memonly — không lưu disk |
| Backup files | GPG AES-256 | Passphrase trong Vault |

### 4.2 Encryption in Transit

```nginx
# nginx.conf — TLS Configuration
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:
            ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
ssl_prefer_server_ciphers off;

# HSTS
add_header Strict-Transport-Security "max-age=63072000" always;

# OCSP Stapling
ssl_stapling on;
ssl_stapling_verify on;
```

**Internal services:** HTTP (Phase 1 — Docker internal network).
**Phase 2+:** mTLS giữa tất cả services qua Istio service mesh.

### 4.3 PII Field-Level Protection

```csharp
// Các trường PII nhạy cảm được mã hóa trong MongoDB
// Sử dụng MongoDB Client-Side Field Level Encryption (CSFLE)

// Phase 1 (đơn giản hơn): Application-level encryption
public class PiiEncryptionService(IKeyProvider keyProvider)
{
    public string Encrypt(string plaintext)
    {
        var key = keyProvider.GetDataEncryptionKey();
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        // ... AES-256-GCM encryption
        return Convert.ToBase64String(ciphertext);
    }
}

// Áp dụng cho: phoneNumber, fullName, zaloId
// GPS coordinates: round đến 0.01° trước khi lưu (~1km precision)
// Không encrypt: coffeeVariety, soilType, province (không phải PII)
```

---

## 5. Input Validation & Injection Prevention

### 5.1 API Input Validation

```csharp
// FluentValidation cho mọi Command
public class RegisterFarmerCommandValidator : AbstractValidator<RegisterFarmerCommand>
{
    public RegisterFarmerCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+84[0-9]{9}$")
            .WithMessage("Số điện thoại không hợp lệ");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-ZÀ-ỹ\s]+$")  // Chỉ chữ và khoảng trắng
            .WithMessage("Tên không hợp lệ");

        RuleFor(x => x.Province)
            .NotEmpty()
            .Must(p => ValidProvinces.Contains(p))
            .WithMessage("Tỉnh không hợp lệ");
    }
}
```

### 5.2 MongoDB Injection Prevention

```csharp
// ✅ ĐÚNG: Dùng strongly-typed LINQ, không raw query
var farmer = await collection
    .Find(f => f.PhoneNumber == phoneNumber)
    .FirstOrDefaultAsync();

// ❌ SAI: Raw filter với user input
var filter = $"{{phoneNumber: '{phoneNumber}'}}";  // Injection risk!

// Không bao giờ dùng BsonDocument với user input unsanitized
```

### 5.3 AI Prompt Injection Defense

```python
# Bazan AI nhận câu hỏi từ nông dân — cần protect khỏi prompt injection

INJECTION_PATTERNS = [
    r"ignore previous instructions",
    r"disregard (your|all) (instructions|constraints)",
    r"you are now (DAN|GPT|an AI without restrictions)",
    r"reveal (your|the) (system prompt|instructions)",
    r"act as (if you have no|without any) restrictions",
    r"jailbreak",
    r"bypass.*filter",
]

def detect_prompt_injection(user_input: str) -> bool:
    normalized = user_input.lower()
    for pattern in INJECTION_PATTERNS:
        if re.search(pattern, normalized, re.IGNORECASE):
            logger.warning("Prompt injection detected",
                           pattern=pattern, input_preview=user_input[:100])
            return True
    return False

async def process_query(raw_query: str, farmer_id: str) -> str:
    if detect_prompt_injection(raw_query):
        return "Tôi chỉ có thể hỗ trợ về cà phê và nông nghiệp Tây Nguyên bạn nhé!"
    # ... normal processing
```

---

## 6. Security Headers

```csharp
// Program.cs — Security middleware
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"]  = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]          = "DENY";
    ctx.Response.Headers["X-XSS-Protection"]         = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"]          = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"]       = "geolocation=(), microphone=(), camera=()";
    ctx.Response.Headers["Content-Security-Policy"]  =
        "default-src 'self'; " +
        "connect-src 'self' wss://api.bazanai.vn; " +
        "img-src 'self' data: https://cdn.bazanai.vn;";
    await next();
});
```

---

## 7. Security Incident Classification

| Level | Mô tả | Response |
|-------|-------|---------|
| **Critical** | Data breach, unauthorized access to farmer PII | Immediate — notify farmers + authorities within 72h |
| **High** | Service compromise, injection attack | 4 giờ response, forensic analysis |
| **Medium** | Failed brute force, anomalous access pattern | 24 giờ investigation |
| **Low** | Scanner probes, failed login attempts | Log và monitor |

---

## 8. Security Review Cadence

| Activity | Tần suất | Owner |
|---------|---------|-------|
| Dependency vulnerability scan (Trivy/Snyk) | Hàng tuần (CI) | DevOps |
| Code security review (SAST) | Mỗi PR | Backend Lead |
| Infrastructure config review | Hàng quý | DevOps + Architect |
| Penetration test | 6 tháng/lần | External vendor |
| Threat model review | Hàng năm hoặc khi architecture thay đổi | Architect |
| API security audit (OWASP) | Hàng quý | Security Lead |

---

**© 2026 Bazan AI Project — Confidential — Do not distribute**

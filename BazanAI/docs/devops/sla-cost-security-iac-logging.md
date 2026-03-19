# SLA Definition Document — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | Monitoring Runbook, Capacity Planning, On-call Policy |

---

## 1. Service Level Objectives (SLOs)

### 1.1 Availability

| Service | SLO | Measurement | Error Budget/tháng |
|---------|-----|-------------|-------------------|
| API Gateway | 99.5% | Synthetic ping mỗi 1 phút | 3.6 giờ |
| AI Chat (SignalR) | 99.0% | Uptime check mỗi 5 phút | 7.2 giờ |
| Notification delivery | 99.0% | Zalo + FCM success rate | 7.2 giờ |
| Knowledge RAG | 99.0% | Health check | 7.2 giờ |
| Market Price data | 99.5% | Data freshness < 6 giờ | 3.6 giờ |

### 1.2 Latency SLOs

| Endpoint category | P50 | P95 | P99 |
|------------------|-----|-----|-----|
| Auth (login/register) | < 200ms | < 500ms | < 1s |
| Profile & Farm CRUD | < 100ms | < 300ms | < 500ms |
| AI Chat first token | < 500ms | < 2s | < 5s |
| AI Chat full response | < 1.5s | < 3s | < 8s |
| Market price lookup | < 50ms | < 200ms | < 500ms |
| Disease diagnosis | < 1s | < 3s | < 8s |
| Notification delivery | < 5s | < 15s | < 30s |

### 1.3 Data SLOs

| Metric | SLO |
|--------|-----|
| Chat history availability | 99.9% |
| Market price freshness | < 6 giờ từ lần cập nhật |
| Sensor data latency (ingest) | < 5 giây |
| Knowledge base freshness | Tài liệu mới trong < 24 giờ |

---

## 2. Service Level Indicators (SLIs)

```python
# Cách đo từng SLO

# Availability SLI
availability = successful_requests / total_requests
# Đo qua Prometheus: rate(http_requests_total{status!~"5.."}[5m]) / rate(http_requests_total[5m])

# Latency SLI
latency_p95 = histogram_quantile(0.95, rate(http_request_duration_ms_bucket[5m]))

# Error budget remaining
error_budget_remaining = (
    (slo_target - (1 - availability)) / (1 - slo_target) * 100
)
# Nếu < 0% → freeze non-critical deployments
```

---

## 3. SLA (External commitment với HTX/Partners)

> **Lưu ý:** SLO là internal target. SLA là cam kết ra bên ngoài — thường thấp hơn SLO ~1-2%.

| Tầng | SLA Availability | Compensation |
|------|-----------------|-------------|
| Free (Farmers) | 99.0% | Không — free tier |
| Premium (HTX) | 99.5% | Gia hạn 1 tháng nếu vi phạm |
| Enterprise | 99.9% | Hoàn tiền 10% tháng vi phạm |

---

## 4. Error Budget Policy

```
Khi error budget còn > 50%: Deploy bình thường, focus features
Khi error budget còn 25–50%: Chỉ deploy sau full test
Khi error budget còn < 25%: Freeze deployments, focus reliability
Khi error budget = 0%: Emergency — no new deploys, all hands on reliability
```

**Grafana dashboard:** `Error Budget Burn Rate` — cảnh báo khi burn rate > 2x trong 1 giờ.

---

**© 2026 Bazan AI Project — Confidential**

---
---

# Cost Optimization Report — Bazan AI
## Phân tích & Tối ưu chi phí vận hành

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | Capacity Planning, AI Model Card, Infrastructure as Code |

---

## 1. Chi phí breakdown (Phase 2 — 2,000 nông dân)

| Hạng mục | Cost/tháng | % tổng |
|---------|-----------|--------|
| **OpenAI API (GPT-4o)** | $750 | 54% |
| **Server infrastructure** | $300 | 22% |
| **OpenAI Embeddings** | $15 | 1% |
| **OpenWeatherMap Pro** | $40 | 3% |
| **ICE Futures API** | $200 | 14% |
| **Twilio SMS** | $20 | 1% |
| **GitHub Actions CI/CD** | $30 | 2% |
| **Domain + SSL** | $10 | 1% |
| **MinIO backup storage** | $15 | 1% |
| **Tổng** | **$1,380** | |

---

## 2. Top Cost Reduction Opportunities

### 2.1 OpenAI — Largest lever (54% tổng chi phí)

**Opportunity A: Query Embedding Cache**
- Current: Mỗi query = 1 API call
- Optimized: Redis cache, 40% hit rate
- Saving: ~$6/tháng (nhỏ vì embedding rẻ)

**Opportunity B: Reduce GPT-4o calls**
- Hiện tại: Mọi intent đều gọi GPT-4o đầy đủ
- Tối ưu 1: Dùng `gpt-4o-mini` cho intent classification (~10% cost)
- Tối ưu 2: Cache câu trả lời cho câu hỏi phổ biến (Redis, TTL 1h)
  - Top 20% câu hỏi chiếm 60% traffic → cache hit = 60% x 60% = 36% reduction
- Saving: ~$270/tháng

**Opportunity C: Shorten prompts**
- System prompt hiện tại: ~1,200 tokens
- Tối ưu: Gọn lại còn ~800 tokens (không mất quality)
- Saving: 400 tokens × $2.50/1M × 50,000 calls = ~$50/tháng

**Opportunity D: Phase 3 — Self-hosted LLM**
- Fine-tuned Llama 3.1 8B trên 1× RTX 4090
- Cost: ~$90/tháng (GPU rental on-demand)
- vs OpenAI: $750/tháng
- Saving: ~$660/tháng (87%)
- Timeline: Phase 3 (tháng 13+)

### 2.2 ICE Futures API (14% chi phí)

- Current: $200/tháng subscription
- Alternative: Scrape giá từ các sàn miễn phí + cross-validate
- Risk: Reliability thấp hơn
- Decision: Giữ ICE Futures cho Phase 1–2, evaluate Phase 3

### 2.3 Server Infrastructure (22% chi phí)

- Phase 1: 1 server $80–120/tháng → OK
- Phase 2: 2 servers $300/tháng → OK
- Phase 3: Xem xét Reserved Instances (tiết kiệm 30–40%)
  - 1-year reservation: ~$180/tháng thay vì $300/tháng

---

## 3. Implementation Roadmap

| Action | Saving/tháng | Effort | Phase |
|--------|-------------|--------|-------|
| gpt-4o-mini cho intent | $75 | Thấp | Phase 1 |
| Response caching (Redis) | $200 | Trung bình | Phase 2 |
| Shorten system prompt | $50 | Thấp | Phase 1 |
| Reserved instances | $120 | Thấp | Phase 2 |
| Self-hosted LLM | $660 | Cao | Phase 3 |
| **Tổng tiết kiệm** | **$1,105** | | |

---

## 4. Cost Monitoring

```python
# Grafana alert — chi phí OpenAI
alert: OpenAICostProjection
expr: |
  increase(bazan_openai_tokens_total{model="gpt-4o"}[24h]) * 0.000010 > 30
# Alert nếu daily cost > $30 (projected $900/tháng)
annotations:
  summary: "OpenAI projected cost > $900/tháng — review caching"
```

---

**© 2026 Bazan AI Project — Confidential**

---
---

# Security Patching & Vulnerability Management — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | CI/CD Pipeline, Security Architecture Document |

---

## 1. Patch Schedule

| Component | Patch Frequency | Owner | Method |
|-----------|----------------|-------|--------|
| .NET NuGet packages | Hàng tuần (CI scan) | Backend Lead | Dependabot PR |
| Python pip packages | Hàng tuần | AI Team | Dependabot PR |
| Docker base images | Hàng tháng | DevOps | Manual update |
| MongoDB | Khi có security release | DevOps | Rolling restart |
| Ubuntu OS | Hàng tháng | DevOps | `apt upgrade` |

---

## 2. Vulnerability Scanning

### 2.1 CI Pipeline (mỗi PR)

```yaml
# Trong ci.yml
- name: Trivy scan Docker images
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: 'registry.bazanai.vn/bazanai/identity-svc:latest'
    severity: 'CRITICAL,HIGH'
    exit-code: '1'      # Fail CI nếu có CRITICAL/HIGH vuln

- name: Snyk SCA scan
  uses: snyk/actions/dotnet@master
  env:
    SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
  with:
    args: --severity-threshold=high
```

### 2.2 Weekly Automated Scan

```bash
# Chạy thứ 2 hàng tuần 8AM
# .github/workflows/security-scan.yml

jobs:
  weekly-scan:
    runs-on: ubuntu-latest
    steps:
      - name: Trivy full scan
        run: |
          trivy fs . --severity CRITICAL,HIGH \
            --format json --output trivy-report.json

      - name: Upload to Slack if findings
        run: |
          CRITICALS=$(cat trivy-report.json | jq '[.Results[].Vulnerabilities[] | select(.Severity=="CRITICAL")] | length')
          if [ "$CRITICALS" -gt "0" ]; then
            curl -X POST $SLACK_WEBHOOK \
              -d "{\"text\": \"🚨 $CRITICALS CRITICAL vulnerabilities found — review trivy-report\"}"
          fi
```

---

## 3. Vulnerability Response SLA

| Severity | Response | Fix Deadline |
|---------|---------|-------------|
| Critical (CVSS ≥ 9.0) | Immediate — page on-call | 24 giờ |
| High (CVSS 7.0–8.9) | Alert team trong 4h | 7 ngày |
| Medium (CVSS 4.0–6.9) | Add to backlog | Next sprint |
| Low (CVSS < 4.0) | Log, review quarterly | Quarterly |

---

## 4. Dependency Update Process

```bash
# Dependabot config (.github/dependabot.yml)
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10
    reviewers: ["backend-lead"]
    labels: ["dependencies", "security"]

  - package-ecosystem: "pip"
    directory: "/src/Python/bazan-knowledge-rag"
    schedule:
      interval: "weekly"

  - package-ecosystem: "docker"
    directory: "/infra"
    schedule:
      interval: "monthly"
```

---

**© 2026 Bazan AI Project — Confidential**

---
---

# Infrastructure as Code Guide — Bazan AI
## Quản lý Infrastructure bằng Code

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | CI/CD Pipeline, Network Topology, Disaster Recovery |

---

## 1. IaC Philosophy

```
Mọi thứ phải là code, mọi thứ phải được version control.
Không có manual config trên production server.
Reproduce toàn bộ stack từ zero bằng 3 lệnh.
```

---

## 2. Stack hiện tại (Phase 1–2)

```
docker-compose.yml         → Application stack definition
.env.example               → Config template
infra/                     → All infrastructure config
├── mongo-init/            → MongoDB initialization scripts
├── rabbitmq/              → RabbitMQ config & definitions
├── qdrant/                → Qdrant config
├── monitoring/            → Prometheus + Grafana config
├── nginx/                 → Nginx config + SSL
└── scripts/               → Utility scripts
```

---

## 3. Server Provisioning Script

```bash
#!/bin/bash
# scripts/provision-server.sh
# Chạy 1 lần trên Ubuntu 22.04 fresh install

set -euo pipefail

# System updates
apt-get update && apt-get upgrade -y

# Docker installation
curl -fsSL https://get.docker.com | sh
usermod -aG docker ubuntu
systemctl enable docker

# Docker Compose v2
apt-get install -y docker-compose-plugin

# Essential tools
apt-get install -y \
  git curl wget htop iotop \
  ufw fail2ban \
  awscli  # For MinIO client setup

# UFW Firewall
ufw --force enable
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp    comment 'SSH'
ufw allow 80/tcp    comment 'HTTP'
ufw allow 443/tcp   comment 'HTTPS'
ufw allow 1883/tcp  comment 'MQTT IoT'

# Fail2ban SSH protection
systemctl enable fail2ban
systemctl start fail2ban

# Create deploy user (non-root)
useradd -m -s /bin/bash deploy
usermod -aG docker deploy

# SSH key for GitHub Actions
mkdir -p /home/deploy/.ssh
echo "${DEPLOY_PUBLIC_KEY}" >> /home/deploy/.ssh/authorized_keys
chmod 600 /home/deploy/.ssh/authorized_keys
chown -R deploy:deploy /home/deploy/.ssh

# Application directory
mkdir -p /opt/bazanai
chown deploy:deploy /opt/bazanai

# MinIO client
wget https://dl.min.io/client/mc/release/linux-amd64/mc -O /usr/local/bin/mc
chmod +x /usr/local/bin/mc

echo "✅ Server provisioned successfully"
```

---

## 4. Environment Management

```bash
# Environments
infra/
├── .env.example           → Template (committed to git)
├── .env.staging           → Staging (NOT committed — in secrets manager)
└── .env.production        → Production (NOT committed — in secrets manager)

# Secrets flow:
# GitHub Secrets → CI/CD → SSH to server → create /opt/bazanai/.env

# .env.example (committed)
MONGO_ROOT_USER=admin
MONGO_ROOT_PASSWORD=CHANGE_ME_STRONG_PASSWORD
QDRANT_API_KEY=CHANGE_ME
REDIS_PASSWORD=CHANGE_ME
RABBITMQ_USER=bazan
RABBITMQ_PASS=CHANGE_ME
JWT_SECRET=CHANGE_ME_AT_LEAST_32_CHARS
OPENAI_API_KEY=sk-CHANGE_ME
ZALO_OA_ACCESS_TOKEN=CHANGE_ME
FCM_SERVER_KEY=CHANGE_ME
OPENWEATHER_API_KEY=CHANGE_ME
ICE_FUTURES_API_KEY=CHANGE_ME
MINIO_ACCESS_KEY=CHANGE_ME
MINIO_SECRET_KEY=CHANGE_ME
GRAFANA_ADMIN_PASSWORD=CHANGE_ME
SEQ_ADMIN_PASSWORD_HASH=CHANGE_ME
```

---

## 5. Makefile — Single Source of Truth

```makefile
# Makefile — tất cả lệnh thường dùng

.PHONY: up down logs restart build test deploy-staging health clean

# Start all services
up:
	docker compose up -d
	@echo "✅ Bazan AI stack started"

# Stop all
down:
	docker compose down

# View logs (all services)
logs:
	docker compose logs -f

# Restart specific service
restart:
	@read -p "Service name: " svc; docker compose restart $$svc

# Build all images locally
build:
	docker compose build --no-cache

# Run all tests
test:
	dotnet test BazanAI.sln --configuration Release
	cd src/Python/bazan-knowledge-rag && pytest tests/ -v

# Deploy to staging
deploy-staging:
	@echo "Deploying to staging..."
	git push origin develop

# Health check
health:
	curl -s http://localhost:8080/healthz | python3 -m json.tool

# MongoDB backup now
backup-now:
	./scripts/backup-mongodb.sh manual-$(shell date +%Y%m%d%H%M)

# MongoDB restore
restore:
	@read -p "Backup date (YYYYMMDD): " date; \
	./scripts/restore-mongodb.sh $$date

# View resource usage
stats:
	docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"

# Prune unused docker resources
clean:
	docker system prune -f
	docker volume prune -f
	@echo "✅ Docker cleaned"

# Generate MongoDB keyfile
gen-keyfile:
	openssl rand -base64 741 > infra/mongo-init/keyfile
	chmod 400 infra/mongo-init/keyfile
	@echo "✅ MongoDB keyfile generated"

# Initialize replica set (first time)
init-rs:
	sleep 10
	docker exec bazan-mongo-1 mongosh \
		-u root -p $(MONGO_ROOT_PASSWORD) \
		--authenticationDatabase admin \
		--file /docker-entrypoint-initdb.d/init-replica.js
```

---

## 6. Phase 4 — Terraform (Preview)

```hcl
# infra/terraform/main.tf
# Phase 4: Kubernetes trên cloud

terraform {
  required_providers {
    aws = { source = "hashicorp/aws", version = "~> 5.0" }
  }
  backend "s3" {
    bucket = "bazanai-terraform-state"
    key    = "production/terraform.tfstate"
    region = "ap-southeast-1"
  }
}

resource "aws_eks_cluster" "bazan" {
  name     = "bazan-production"
  role_arn = aws_iam_role.eks_cluster.arn
  version  = "1.29"

  vpc_config {
    subnet_ids = module.vpc.private_subnets
  }
}

resource "aws_eks_node_group" "app_nodes" {
  cluster_name  = aws_eks_cluster.bazan.name
  node_group_name = "app-nodes"
  instance_types = ["t3.xlarge"]

  scaling_config {
    desired_size = 3
    min_size     = 2
    max_size     = 10
  }
}
```

---

**© 2026 Bazan AI Project — Confidential**

---
---

# Log Management & Structured Logging Guide — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | Monitoring Runbook, CI/CD Pipeline, Incident Response |

---

## 1. Logging Stack

```
.NET Services → Serilog → Seq (http://server:5341)
Python RAG    → structlog → Seq via Serilog HTTP sink
RabbitMQ      → Management logs → Docker logs
MongoDB       → MongoDB logs → Docker logs
Nginx         → access.log, error.log → Docker logs
```

---

## 2. Serilog Configuration (.NET)

```csharp
// SharedKernel/Observability/SerilogExtensions.cs
public static class SerilogExtensions
{
    public static WebApplicationBuilder AddBazanLogging(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Host.UseSerilog((ctx, lc) => lc
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", serviceName)
            .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
            .Enrich.WithMachineName()
            .WriteTo.Console(new JsonFormatter())  // Stdout cho Docker
            .WriteTo.Seq(
                serverUrl: ctx.Configuration["Seq:ServerUrl"]!,
                apiKey: ctx.Configuration["Seq:ApiKey"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
        );
        return builder;
    }
}
```

---

## 3. Log Schema Chuẩn

Mọi log entry phải có các trường sau:

```json
{
  "@t": "2026-03-19T07:31:00.123Z",
  "@l": "Information",
  "@m": "AI chat response streamed successfully",

  "Application": "bazan-ai-orchestrator",
  "Environment": "Production",
  "MachineName": "bazan-server-01",

  "CorrelationId": "req-uuid-v4",
  "SessionId": "sess_20260319_AN_001",
  "FarmerId": "64f1a2b3c4d5e6f7a8b9c0d1",

  "EventType": "chat_response_complete",
  "Duration_ms": 1847,
  "TokensUsed": 576,
  "IntentClassified": "disease_diagnosis",
  "CitationsCount": 2
}
```

---

## 4. Correlation ID Middleware

```csharp
// Propagate Correlation ID qua toàn bộ request chain
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers[Header].FirstOrDefault()
                         ?? Guid.NewGuid().ToString("N");

        ctx.Response.Headers[Header] = correlationId;
        ctx.Items[Header] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Thêm vào tất cả HTTP client calls đi ra
            using var activity = Activity.Current;
            activity?.SetTag("correlation_id", correlationId);

            await next(ctx);
        }
    }
}
```

---

## 5. Log Levels & Usage

| Level | Khi nào dùng | Ví dụ |
|-------|------------|-------|
| **Verbose** | Dev only, bị tắt production | Raw SQL queries |
| **Debug** | Dev + staging, bị tắt production | Cache hit/miss details |
| **Information** | Business events quan trọng | Session started, price updated |
| **Warning** | Không expected nhưng handled | Retry triggered, cache miss rate cao |
| **Error** | Exception được handle, operation failed | DB timeout, API external error |
| **Fatal** | App không thể tiếp tục | Config missing, port bind failed |

```csharp
// ✅ Đúng: log event, không phải message
logger.LogInformation(
    "Disease diagnosis completed for {FarmId}: {Disease} (confidence: {Confidence})",
    farmId, diseaseCode, confidence
);

// ❌ Sai: string interpolation mất structured logging
logger.LogInformation($"Disease diagnosis completed for {farmId}");

// ✅ Đúng: log exception với context
logger.LogError(ex,
    "Failed to call OpenAI API for session {SessionId}. Attempt {RetryCount}/{MaxRetries}",
    sessionId, retryCount, maxRetries
);
```

---

## 6. Log Retention Policy

| Log type | Retention | Storage |
|----------|----------|---------|
| Application logs (Seq) | 30 ngày | Seq internal storage (~5GB) |
| Error logs | 90 ngày | Seq với filter |
| Security/Audit logs | 1 năm | MinIO archive |
| Access logs (Nginx) | 14 ngày | Docker volume |
| MongoDB logs | 7 ngày | Docker volume (auto-rotate) |

**Seq retention config:**
```json
{
  "Storage": {
    "RetainedFileCount": 30,
    "RawPayloadMaxBytes": 5368709120
  }
}
```

---

## 7. Sensitive Data Masking

```csharp
// Không bao giờ log:
// - Số điện thoại đầy đủ
// - JWT token
// - Password/OTP
// - GPS coordinates chính xác (chỉ log tỉnh/huyện)

// ✅ Đúng
logger.LogInformation("Farmer {FarmerId} from {Province} logged in",
    farmerId, province);

// ❌ Sai
logger.LogInformation("Farmer {Phone} logged in with OTP {Otp}",
    phoneNumber, otp);

// Masking middleware
public static string MaskPhone(string phone)
    => phone.Length > 5 ? $"{phone[..3]}****{phone[^2..]}" : "***";
```

---

## 8. Seq Useful Queries

```sql
-- Tất cả errors hôm nay
@l = 'Error' AND @t >= Today()

-- Slow requests > 2 giây
Duration_ms > 2000 AND @l = 'Information'

-- Theo dõi 1 request
CorrelationId = 'specific-id-here'

-- AI safety violations
EventType = 'safety_violation'

-- Rate limit hits
EventType = 'rate_limit_exceeded'

-- Failed consumer retries (DLQ bound)
EventType = 'consume_failed' AND RetryCount = 3

-- OpenAI errors
Application = 'bazan-ai-orchestrator' AND @l = 'Error'

-- Nông dân cụ thể
FarmerId = '64f1a2b3c4d5e6f7a8b9c0d1' AND @t >= @t - 24h
```

---

**© 2026 Bazan AI Project — Confidential**

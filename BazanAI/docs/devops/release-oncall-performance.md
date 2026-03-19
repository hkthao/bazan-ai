# Release Management Process — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | CI/CD Pipeline, SLA Definition |

---

## 1. Release Types

| Type | Trigger | Frequency | Approval |
|------|---------|-----------|---------|
| **Hotfix** | Critical bug/security | As needed | 1 senior review |
| **Patch** | Bug fixes, minor improvements | Weekly (Thursday) | 1 senior review |
| **Minor** | New features, non-breaking | Bi-weekly | 2 reviews + QA |
| **Major** | Breaking changes, architecture | Monthly | Full team + stakeholder |

---

## 2. Release Checklist

### Pre-release (T-2 ngày)

```
□ PR reviewed và approved đủ số người
□ CI pipeline passed (build + test + security scan)
□ AI eval regression passed (nếu có AI change)
□ CHANGELOG.md cập nhật (theo Keep a Changelog format)
□ VERSION bumped theo SemVer
□ Staging đã chạy ổn định > 24h
□ Database migration script ready (nếu có schema change)
□ Rollback plan documented
□ On-call engineer confirmed available
```

### Release day (Thứ Năm, 10AM)

```
□ MongoDB snapshot: ./scripts/backup-mongodb.sh pre-release-vX.Y.Z
□ Announce trong Slack #deployment: "🚀 Deploying vX.Y.Z in 10 minutes"
□ Trigger production pipeline (via GitHub Actions manual dispatch)
□ Monitor Grafana dashboard trong 30 phút sau deploy
□ Run production smoke test
□ Check error rate, latency không tăng bất thường
□ Confirm trong Slack: "✅ vX.Y.Z deployed successfully"
```

### Post-release (T+1 ngày)

```
□ Review Grafana metrics 24h sau deploy
□ Check user feedback không có spike tiêu cực
□ Update Release Notes trên internal wiki
□ Close release/* branch
□ Merge hotfixes vào develop nếu có
```

---

## 3. CHANGELOG Format

```markdown
# Changelog — Bazan AI

## [2.1.0] — 2026-04-03

### Added
- IoT Sensor Ingestion Service (MQTT broker, soil moisture/pH/temperature)
- Disease detection từ ảnh qua Azure Custom Vision
- Zalo Bot webhook cho câu hỏi trực tiếp

### Changed
- Hybrid Search RRF weights tối ưu: dense 0.70, sparse 0.30
- System prompt cập nhật v2.3 — thêm safety constraints cho thuốc

### Fixed
- Lỗi encoding tiếng Việt trong export RLHF dataset
- Race condition khi nhiều session cùng farmer_id mở đồng thời

### Security
- Update MongoDB driver 3.0.1 → 3.1.0 (CVE-2026-XXXX)

## [2.0.0] — 2026-03-19
...
```

---

## 4. Hotfix Process

```bash
# Khi có critical bug trên production

# 1. Tạo hotfix branch từ main
git checkout main
git pull
git checkout -b hotfix/fix-otp-not-sending

# 2. Fix và commit
git commit -m "fix: OTP gửi lỗi khi số điện thoại có dấu cách"

# 3. PR vào main (bypass bi-weekly schedule)
# Cần 1 senior review, CI pass

# 4. Merge và tag
git tag -a v2.0.1 -m "Hotfix: OTP sending"
git push origin main --tags

# 5. Deploy ngay (không cần chờ Thursday)
# GitHub Actions tự trigger khi push main

# 6. Merge hotfix về develop
git checkout develop
git merge hotfix/fix-otp-not-sending
git push origin develop
```

---

**© 2026 Bazan AI Project — Confidential**

---
---

# On-call Rotation & Escalation Policy — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | Incident Response Playbook, Monitoring Runbook |

---

## 1. On-call Schedule

### Rotation (Phase 1 — Team nhỏ)

**Tuần 1–2:** Backend Lead (primary) + DevOps Lead (secondary)
**Tuần 3–4:** Senior Backend Dev (primary) + Backend Lead (secondary)
**Repeat**

**Công cụ:** PagerDuty rotation schedule
**Giờ on-call:** 24/7 cho P0/P1, 8AM–10PM cho P2/P3

---

## 2. Severity Levels & SLA

| Level | Mô tả | Response Time | Resolution Time | Ai on-call |
|-------|-------|--------------|----------------|-----------|
| **P0** | Toàn bộ platform down | 5 phút | 2 giờ | Primary + Secondary + Lead |
| **P1** | Core feature down (AI chat, auth) | 15 phút | 4 giờ | Primary on-call |
| **P2** | Feature degraded (slow, partial) | 1 giờ | 8 giờ | Primary on-call |
| **P3** | Minor issue, no user impact | Next business day | 3 ngày | Assigned dev |
| **P4** | Improvement/non-urgent | Sprint planning | Next sprint | Product team |

---

## 3. Escalation Matrix

```
Alert xuất hiện
      │
      ▼ (5 phút)
Primary On-call — không acknowledge?
      │
      ▼ (10 phút thêm)
Secondary On-call
      │
      ▼ (15 phút thêm)
Engineering Lead / Backend Lead
      │
      ▼ (30 phút thêm — chỉ P0)
CTO / Founder
```

### Contact Info (lưu trong PagerDuty)

```yaml
primary_oncall:
  name: "Backend Lead"
  phone: "+84 9XX XXX XXX"
  slack: "@backend-lead"
  pagerduty: "backend-lead@bazanai.vn"

secondary_oncall:
  name: "DevOps Lead"
  phone: "+84 9XX XXX XXX"
  slack: "@devops-lead"

engineering_lead:
  name: "Principal Architect"
  phone: "+84 9XX XXX XXX"

external_contacts:
  mongodb_support: "support.mongodb.com (M10+ cluster support)"
  openai_status: "status.openai.com"
  zalo_oa_support: "oa.zalo.me/support"
```

---

## 4. On-call Responsibilities

**Khi nhận alert:**
```
1. Acknowledge trong PagerDuty ngay (< 5 phút)
2. Join #incident Slack channel
3. Post initial assessment: "Đang điều tra. Vấn đề: [mô tả sơ bộ]"
4. Làm theo runbook tương ứng
5. Update mỗi 30 phút nếu chưa resolve
6. Viết incident report sau khi resolve (xem template)
```

**Quyền hạn trong on-call:**
- Restart bất kỳ service nào
- Rollback deploy ngay lập tức (không cần approval)
- Bật/tắt feature flag
- Tăng rate limit tạm thời
- Escalate lên level cao hơn bất kỳ lúc nào

---

## 5. Incident Report Template

```markdown
# Incident Report — [INC-YYYY-MM-DD-001]

**Severity:** P1
**Duration:** 45 phút (2026-03-19 14:32 → 15:17)
**Impact:** ~300 farmers không gửi được câu hỏi AI

## Timeline
- 14:32 — Alert: APIErrorRateHigh triggered (error rate 8.3%)
- 14:35 — Primary on-call acknowledged
- 14:40 — Root cause identified: ai-orchestrator OOM
- 14:45 — Restarted ai-orchestrator
- 14:50 — Error rate xuống 0
- 15:17 — Monitoring 30 phút, confirmed stable

## Root Cause
ai-orchestrator memory leak khi conversation history quá dài (> 50 turns).
Memory usage tăng từ 600MB lên 2GB, OOMKilled.

## Fix
1. Immediate: Restart container
2. Short-term: Giảm max conversation turns từ 50 → 20
3. Long-term: Fix memory leak trong sliding window trimmer (PR #234)

## Lessons Learned
- Memory limit của ai-orchestrator quá thấp (1GB) → tăng lên 2GB
- Cần alert khi memory > 80% (đã thêm vào Grafana)

## Action Items
- [ ] @backend-lead: Fix memory leak PR #234 (deadline: 2026-03-26)
- [ ] @devops-lead: Tăng memory limit (done)
- [ ] @devops-lead: Thêm memory alert (done)
```

---

## 6. On-call Compensation

- **Weekday on-call allowance:** 300,000 VND/ngày
- **Weekend/holiday:** 500,000 VND/ngày
- **P0 resolved within SLA:** Thưởng thêm 500,000 VND
- **Time off after long incident:** 1 ngày nghỉ bù cho incident > 4 giờ đêm

---

**© 2026 Bazan AI Project — Confidential**

---
---

# Performance Testing Plan — Bazan AI
## Kế hoạch kiểm thử hiệu năng với 10.000 nông dân concurrent

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Công cụ** | k6, Grafana k6 Cloud, Artillery |
| **Liên quan** | Capacity Planning, SLA Definition, CI/CD Pipeline |

---

## 1. Test Objectives

| Mục tiêu | Metric | Target |
|---------|--------|--------|
| API throughput | Requests/second | ≥ 200 RPS sustained |
| Chat latency | P95 end-to-end | < 3 giây |
| MongoDB query | P95 | < 100ms |
| Qdrant search | P95 | < 500ms |
| Peak load handling | 1,000 concurrent users | No errors |
| Endurance | 2 giờ sustained load | Memory stable |
| Spike tolerance | 3x normal load | Graceful degradation |

---

## 2. Test Scenarios

### Scenario 1: Baseline Load (k6)

```javascript
// tests/performance/baseline_load.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '5m',  target: 50  },   // Ramp up
    { duration: '20m', target: 100 },   // Sustained baseline
    { duration: '5m',  target: 0   },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'],   // P95 < 3s
    http_req_failed:   ['rate<0.01'],    // Error rate < 1%
    errors:            ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://staging.bazanai.vn';

export function setup() {
  // Login để lấy token
  const loginRes = http.post(`${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ phoneNumber: '+84901234567' }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  return { token: loginRes.json('accessToken') };
}

export default function(data) {
  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${data.token}`,
  };

  // Simulate typical farmer session
  const scenarios = [
    () => {
      // Get market prices (40% of requests)
      const res = http.get(`${BASE_URL}/api/v1/market/prices/current`, { headers });
      check(res, { 'market price 200': r => r.status === 200 });
    },
    () => {
      // Get schedule (30% of requests)
      const res = http.get(
        `${BASE_URL}/api/v1/agronomy/schedule/65a2b3c4d5e6f7a8b9c0d1e2`,
        { headers }
      );
      check(res, { 'schedule 200': r => r.status === 200 });
    },
    () => {
      // Get pest alerts (20% of requests)
      const res = http.get(
        `${BASE_URL}/api/v1/agronomy/pest-alerts?province=Đắk Lắk`,
        { headers }
      );
      check(res, { 'alerts 200': r => r.status === 200 });
    },
    () => {
      // Get farmer context (10% of requests)
      const res = http.get(
        `${BASE_URL}/api/v1/farmers/64f1a2b3c4d5e6f7a8b9c0d1`,
        { headers }
      );
      check(res, { 'farmer 200': r => r.status === 200 });
    },
  ];

  // Weighted random selection
  const rand = Math.random();
  if (rand < 0.40) scenarios[0]();
  else if (rand < 0.70) scenarios[1]();
  else if (rand < 0.90) scenarios[2]();
  else scenarios[3]();

  sleep(Math.random() * 2 + 1);  // Think time: 1-3 giây
}
```

### Scenario 2: Peak Load (3x baseline)

```javascript
export const options = {
  stages: [
    { duration: '2m',  target: 100  },
    { duration: '5m',  target: 300  },  // 3x spike
    { duration: '10m', target: 300  },  // Sustain peak
    { duration: '3m',  target: 0    },
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'],  // Relaxed — 5s acceptable at peak
    http_req_failed:   ['rate<0.05'],   // Max 5% errors during spike
  },
};
```

### Scenario 3: Endurance Test (2 giờ)

```javascript
export const options = {
  stages: [
    { duration: '10m', target: 100 },
    { duration: '100m', target: 100 },  // Sustained 100 VU
    { duration: '10m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.01'],
  },
};
// Mục tiêu: Memory usage phải stable (không memory leak)
// Monitor: container_memory_usage_bytes trong Grafana
```

### Scenario 4: AI Chat Load (WebSocket/SignalR)

```javascript
// Artillery config cho WebSocket testing
// tests/performance/ai_chat_load.yml
config:
  target: "wss://staging.bazanai.vn"
  phases:
    - duration: 60
      arrivalRate: 5      # 5 users/giây ramp up
    - duration: 300
      arrivalRate: 20     # 20 concurrent chat sessions
  defaults:
    headers:
      Authorization: "Bearer {{ token }}"

scenarios:
  - name: "AI Chat Session"
    engine: "ws"
    flow:
      - connect: "/hub/chat?access_token={{ token }}"
      - send:
          channel: "SendMessage"
          data:
            sessionId: "test-session-{{ $randomNumber() }}"
            farmerId: "64f1a2b3c4d5e6f7a8b9c0d1"
            message: "Cây cà phê bị vàng lá phải làm gì?"
      - wait:
          channel: "StreamComplete"
          timeout: 15000
      - think: 5
      - close
```

---

## 3. MongoDB Query Performance Tests

```python
# tests/performance/db_benchmark.py
import asyncio, time
from motor.motor_asyncio import AsyncIOMotorClient

async def benchmark_farmer_lookup(client, iterations=1000):
    """P95 target: < 50ms"""
    db = client.bazan_identity
    latencies = []

    for _ in range(iterations):
        start = time.monotonic()
        await db.farmers.find_one({"_id": ObjectId("64f1a2b3c4d5e6f7a8b9c0d1")})
        latencies.append((time.monotonic() - start) * 1000)

    latencies.sort()
    p95 = latencies[int(0.95 * len(latencies))]
    print(f"Farmer lookup P95: {p95:.1f}ms (target: <50ms)")
    assert p95 < 50, f"FAIL: {p95:.1f}ms > 50ms"

async def benchmark_geospatial_query(client, iterations=500):
    """P95 target: < 200ms"""
    db = client.bazan_identity
    latencies = []

    for _ in range(iterations):
        start = time.monotonic()
        await db.coffee_buyers.find({
            "location": {
                "$nearSphere": {
                    "$geometry": {"type": "Point", "coordinates": [108.03, 12.67]},
                    "$maxDistance": 10000
                }
            }
        }).limit(10).to_list(10)
        latencies.append((time.monotonic() - start) * 1000)

    latencies.sort()
    p95 = latencies[int(0.95 * len(latencies))]
    print(f"Geospatial query P95: {p95:.1f}ms (target: <200ms)")
    assert p95 < 200
```

---

## 4. Test Schedule

| Test | Môi trường | Khi nào chạy | Duration |
|------|-----------|-------------|---------|
| Baseline load | Staging | Trước mỗi Minor/Major release | 30 phút |
| Peak load (3x) | Staging | Trước Major release | 20 phút |
| Endurance | Staging | Hàng tháng | 2 giờ |
| AI chat load | Staging | Khi có thay đổi Orchestrator | 15 phút |
| DB benchmark | Staging | Khi có schema/index change | 10 phút |
| Full regression | Staging | Hàng quý | 4 giờ |

---

## 5. Performance Regression Gate (CI)

Chạy baseline load test ngắn (5 phút) trên mỗi PR vào `main`:

```yaml
# .github/workflows/ci.yml
perf-gate:
  runs-on: ubuntu-latest
  if: github.base_ref == 'main'
  steps:
    - name: Run performance gate
      run: |
        k6 run \
          --env BASE_URL=https://staging.bazanai.vn \
          --vus 20 --duration 5m \
          tests/performance/baseline_load.js

        # k6 exits with code 99 if thresholds fail
```

---

**© 2026 Bazan AI Project — Confidential**

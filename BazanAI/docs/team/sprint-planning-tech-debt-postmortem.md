# Sprint Planning Template — Bazan AI
## Mẫu Lập kế hoạch Sprint (2 tuần)

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Áp dụng** | Toàn bộ Engineering + Product team |
| **Liên quan** | Developer Onboarding, KPI Dashboard |

---

## Sprint Template

```
SPRINT {N} — {Start Date} → {End Date}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

SPRINT GOAL (1 câu):
"Sau sprint này, nông dân có thể [VALUE được deliver]."

Ví dụ: "Sau sprint này, nông dân nhận được cảnh báo bệnh tự động qua Zalo."

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TEAM CAPACITY:
  [Name 1]: X points (hết {n} ngày PTO)
  [Name 2]: X points
  [Name 3]: X points
  Total available: X points
  Buffer (20% cho unplanned): -X points
  Net capacity: X points

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
SPRINT BACKLOG:

┌──────────┬─────────────────────────────────────┬───────┬────────────┬──────────┐
│ Ticket   │ Title                               │ Story │ Assignee   │ Status   │
│          │                                     │ Points│            │          │
├──────────┼─────────────────────────────────────┼───────┼────────────┼──────────┤
│ US-042   │ Zalo notification for disease alert │  5    │ Dev A      │ To Do    │
│ US-043   │ IoT soil moisture threshold alert   │  3    │ Dev B      │ To Do    │
│ BUG-012  │ OTP rate limit reset bug            │  2    │ Dev A      │ To Do    │
│ TECH-008 │ Refactor MongoDB repo base class    │  3    │ Dev C      │ To Do    │
│ US-044   │ Market price chart (30 days)        │  5    │ Dev D      │ To Do    │
├──────────┼─────────────────────────────────────┼───────┼────────────┼──────────┤
│          │ TOTAL                               │  18   │            │          │
└──────────┴─────────────────────────────────────┴───────┴────────────┴──────────┘

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
DEFINITION OF DONE (mọi ticket phải đủ):

□ Code implemented và reviewed (≥ 2 approvals vào develop)
□ Unit tests viết và pass (coverage không giảm)
□ Integration tests (nếu có DB interaction)
□ CI/CD pipeline pass
□ Deployed lên staging và smoke test pass
□ OpenAPI spec updated (nếu API changes)
□ CHANGELOG.md updated (nếu user-facing feature)
□ Product Owner acceptance (demo đủ yêu cầu)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
RISKS & DEPENDENCIES:

Risk 1: WASI review cho knowledge base update chưa confirm
  → Owner: AI Lead | Mitigation: Email nhắc WASI thứ 2

Risk 2: OpenAI API có thể thay đổi pricing mid-sprint
  → Owner: CTO | Mitigation: Budget buffer 20%

Dependency: US-044 phụ thuộc US-043 (data từ sensor)
  → Plan: US-043 vào nửa đầu sprint, US-044 vào nửa sau
```

---

## Story Point Scale (Fibonacci)

| Points | Effort | Ví dụ |
|--------|--------|-------|
| 1 | < 2 giờ | Fix typo, update config value |
| 2 | 2–4 giờ | Small bug fix, add field to existing API |
| 3 | 4–8 giờ | New API endpoint đơn giản |
| 5 | 1–2 ngày | Feature trung bình, có DB + tests |
| 8 | 2–3 ngày | Feature phức tạp, nhiều layer |
| 13 | 3–5 ngày | Epic cần tách nhỏ hơn |
| ? | Chưa rõ | Cần spike/research trước |

**Rule:** Story points > 8 → **phải tách** thành tickets nhỏ hơn trước khi vào sprint.

---

## Daily Standup Format (15 phút)

```
Mỗi người trả lời 3 câu:
1. Hôm qua làm gì? (done)
2. Hôm nay sẽ làm gì? (plan)
3. Có blocker nào không? (impediments)

Standup KHÔNG phải:
  ❌ Status report cho manager
  ❌ Technical deep-dive
  ❌ Problem solving session
  → Những thứ này → sau standup, giữa những người liên quan

Tool: Slack thread #daily-standup (async) hoặc Huddle (sync)
```

---

## Sprint Retrospective Format

```
Thời gian: 60 phút, cuối mỗi sprint (thứ Sáu cuối sprint)

Cấu trúc (Start/Stop/Continue):

START — Làm điều này nhiều hơn:
  [Tất cả ghi sticky notes, 5 phút]

STOP — Ngừng làm điều này:
  [Tất cả ghi sticky notes, 5 phút]

CONTINUE — Tiếp tục làm điều này:
  [Tất cả ghi sticky notes, 5 phút]

Vote — Top 3 action items (15 phút)
  Mỗi người có 3 votes
  Top items → assign owner + deadline

Action items từ sprint trước → Review đã done chưa?

Output: Sprint Retro Notes trong Confluence
```

---

## Velocity Tracking

```
Sprint | Committed | Delivered | Velocity | Notes
-------|-----------|-----------|----------|-------
  1   |    20     |    15     |   15     | Onboarding overhead
  2   |    18     |    18     |   18     | First stable sprint
  3   |    20     |    22     |   22     | Carried over US-044
  4   |    22     |    20     |   20     | BUG-018 unplanned

Rolling avg (last 3): (18+22+20)/3 = 20 points

Planning rule: Commit = 90% of rolling average velocity
  → Next sprint commit: 18 points
```

---

**© 2026 Bazan AI — Internal Document**

---
---

# Technical Debt Register — Bazan AI
## Sổ theo dõi Nợ kỹ thuật

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Owner** | Engineering Lead |
| **Review** | Cuối mỗi sprint |

---

## Scoring Matrix

**Interest (chi phí nếu không trả):**
- High: Chặn feature mới, gây bugs thường xuyên, security risk
- Medium: Làm chậm development, khó maintain
- Low: Khó chịu nhưng không blocking

**Effort to fix:**
- Small: < 1 ngày | Medium: 1–3 ngày | Large: 3–5 ngày | XL: > 1 tuần

---

## Active Technical Debt Items

```
┌──────┬────────────────────────────────────────────┬──────────┬────────┬─────────┬───────────┐
│  ID  │ Description                                │ Interest │ Effort │  Owner  │ Target    │
├──────┼────────────────────────────────────────────┼──────────┼────────┼─────────┼───────────┤
│TD-001│ HTTP (không TLS) giữa các internal services│ High     │ Large  │ DevOps  │ Phase 2   │
│      │ Cần implement mTLS với Istio               │          │        │         │ (K8s)     │
├──────┼────────────────────────────────────────────┼──────────┼────────┼─────────┼───────────┤
│TD-002│ AI Orchestrator không có circuit breaker   │ High     │ Medium │ Backend │ Sprint 8  │
│      │ cho OpenAI calls — timeout cascade         │          │        │ Lead    │           │
├──────┼────────────────────────────────────────────┼──────────┼────────┼─────────┼───────────┤
│TD-003│ FarmerRepository không có pagination       │ Medium   │ Small  │ Dev A   │ Sprint 7  │
│      │ → GetAll() sẽ OOM khi 10K farmers          │          │        │         │           │
├──────┼────────────────────────────────────────────┼──────────┼────────┼─────────┼───────────┤
│TD-004│ Knowledge Base metadata extraction         │ Medium   │ Medium │ AI Lead │ Sprint 9  │
│      │ dùng LLM call cho mỗi chunk — chậm        │          │        │         │           │
│      │ Nên cache hoặc dùng rule-based             │          │        │         │           │
├──────┼────────────────────────────────────────────┼──────────┼────────┼─────────┼───────────┤
│TD-005│ Test coverage Infrastructure layer < 40%  │ Medium   │ Large  │ Dev B   │ Sprint 10 │
│      │ → Bug trong Repository khó detect          │          │        │         │           │
├──────┼────────────────────────────────────────────┼──────────┼────────┼─────────┼───────────┤
│TD-006│ .env secrets — Phase 1 dùng plain text     │ High     │ Large  │ DevOps  │ Phase 2   │
│      │ → Cần migrate sang HashiCorp Vault          │          │        │         │           │
├──────┼────────────────────────────────────────────┼──────────┼────────┼─────────┼───────────┤
│TD-007│ MongoDB indexes chưa đủ cho analytics      │ Low      │ Small  │ Dev A   │ Sprint 8  │
│      │ queries (RLHF export, market reports)       │          │        │         │           │
├──────┼────────────────────────────────────────────┼──────────┼────────┼─────────┼───────────┤
│TD-008│ Notification Service không retry Zalo      │ Medium   │ Small  │ Dev C   │ Sprint 7  │
│      │ delivery failure — silently drop           │          │        │         │           │
└──────┴────────────────────────────────────────────┴──────────┴────────┴─────────┴───────────┘
```

---

## Policy

```
Tech debt allocation:
  20% of each sprint capacity reserved for tech debt
  High-interest items: prioritize within 2 sprints of identification
  Medium: schedule within the quarter
  Low: backlog, review quarterly

Adding new tech debt:
  Developer tạo TD-{next_id} với full description
  Engineering Lead approve và assign score
  Add to sprint backlog within 1 sprint

"No new debt" rule:
  Trước khi commit workaround, phải tạo TD ticket
  Không được merge "TODO: fix later" không có ticket
```

---

## Resolved Items (Hall of Shame → Hall of Fame)

```
┌──────┬────────────────────────────────┬──────────────┬────────────────┐
│  ID  │ Description                    │ Resolved In  │ Time to Resolve │
├──────┼────────────────────────────────┼──────────────┼────────────────┤
│ OLD1 │ SHA1 token hashing → SHA256    │ Sprint 2     │ 3 tuần         │
│ OLD2 │ N+1 query trong FarmerList     │ Sprint 3     │ 1 sprint       │
│ OLD3 │ Missing CancellationToken      │ Sprint 4     │ 2 sprints      │
└──────┴────────────────────────────────┴──────────────┴────────────────┘
```

---

**© 2026 Bazan AI — Internal Document**

---
---

# Post-mortem Template — Bazan AI
## Mẫu Báo cáo Phân tích Sự cố

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Triết lý** | Blameless Post-mortem — tìm nguyên nhân hệ thống, không đổ lỗi cá nhân |

---

## Template

```markdown
# Post-mortem: [INC-YYYY-MM-DD-NNN]
## [Tên ngắn gọn mô tả sự cố]

**Incident ID:** INC-2026-03-19-001
**Severity:** P1 (P0/P1/P2/P3)
**Duration:** {start_time} → {end_time} ({total_minutes} phút)
**Incident Commander:** {name}
**Author(s):** {names}
**Review Date:** {date} (tối đa 1 tuần sau incident)
**Status:** Draft / Under Review / Final

---

## 1. Tóm tắt điều hành (Executive Summary)

[2-3 câu mô tả ngắn gọn: chuyện gì xảy ra, tác động đến ai, và đã làm gì để khắc phục]

Ví dụ:
> Ngày 19/03/2026, ai-orchestrator bị OOM kill sau 45 phút do memory leak khi xử lý conversation history dài. Khoảng 300 nông dân không gửi được câu hỏi AI trong thời gian này. Service được khôi phục sau 45 phút bằng cách restart container và giảm max conversation turns.

---

## 2. Tác động (Impact)

| Metric | Giá trị |
|--------|---------|
| Thời gian downtime | 45 phút |
| Người dùng bị ảnh hưởng | ~300 farmers (DAU lúc đó) |
| Tính năng bị ảnh hưởng | AI Chat (toàn bộ), Disease diagnosis |
| Tính năng KHÔNG bị ảnh hưởng | Market price, Notifications |
| Revenue impact | $0 (chưa có paying users) |
| Reputation impact | 15 nông dân complain trên Zalo group |

---

## 3. Timeline (UTC+7)

```
14:32 — Alert: APIErrorRateHigh triggered (error rate 8.3%)
         Source: Grafana → Slack #bazan-alerts → PagerDuty

14:35 — On-call engineer (Dev A) acknowledge alert

14:38 — Initial diagnosis: ai-orchestrator container không healthy
         docker ps: bazan-ai-orchestrator | Exited (137) ← OOMKilled

14:40 — Root cause suspected: memory leak
         docker stats (before restart) → RAM 2.1GB / 2GB limit

14:45 — Immediate mitigation: restart container
         docker compose restart ai-orchestrator

14:50 — Error rate xuống 0%, service healthy

14:52 — Identified: conversation với >50 turns bị leak
         Memory usage trace → sliding window không trim đúng

14:55 — Short-term fix deployed: max_turns = 20 (thay 50)
         docker compose restart ai-orchestrator

15:17 — Confirmed stable sau 30 phút monitoring

15:20 — Incident declared resolved, notify team
```

---

## 4. Root Cause Analysis

### Primary Root Cause

**Memory leak trong ConversationHistoryBuilder** khi conversation > 50 turns:

```csharp
// BUG: Method trả về new list mỗi lần nhưng không dispose previous
// History tích lũy theo cấp số nhân
public ChatHistory BuildHistory(List<Message> messages)
{
    var history = new ChatHistory(); // Không reuse
    foreach (var msg in messages)
        history.Add(msg.Role, msg.Content); // Add all, không trim

    // BUG: Token counting sai → không trim khi cần
    return history; // Caller giữ reference → GC không collect
}
```

### Contributing Factors

1. **Memory limit quá thấp (1GB → cần 2GB):** Config không được test với long conversations
2. **Không có alert cho memory > 80%:** Chúng ta không biết memory đang tăng
3. **Max conversation turns (50) quá cao:** Không có upper bound test

### What Went Well

```
✅ PagerDuty alert trigger đúng và nhanh (3 phút sau sự cố bắt đầu)
✅ On-call response time tốt (3 phút acknowledge)
✅ Root cause xác định nhanh (10 phút)
✅ Mitigation bằng restart hiệu quả ngay lập tức
✅ Market price và notifications vẫn hoạt động (blast radius limited)
```

### What Could Be Better

```
❌ Không có memory alert → không biết trước khi crash
❌ Memory limit config chưa được load test với real usage patterns
❌ Max turns parameter không có documentation về tại sao chọn 50
❌ Không có graceful degradation khi ai-orchestrator down
   → Nông dân nhận 500 error, không phải "Dịch vụ tạm thời không khả dụng"
```

---

## 5. Action Items

```
┌──────┬────────────────────────────────────────────────┬─────────┬────────────┐
│  #   │ Action Item                                    │  Owner  │  Deadline  │
├──────┼────────────────────────────────────────────────┼─────────┼────────────┤
│  1   │ Fix memory leak trong ConversationHistoryBuilder│ Dev A   │ 2026-03-26 │
│      │ (PR #234 — urgent)                             │         │ [DONE]     │
├──────┼────────────────────────────────────────────────┼─────────┼────────────┤
│  2   │ Tăng memory limit ai-orchestrator 1GB → 2GB   │ DevOps  │ 2026-03-20 │
│      │                                                │         │ [DONE]     │
├──────┼────────────────────────────────────────────────┼─────────┼────────────┤
│  3   │ Thêm Grafana alert: container memory > 80%    │ DevOps  │ 2026-03-22 │
├──────┼────────────────────────────────────────────────┼─────────┼────────────┤
│  4   │ Thêm load test với 50-turn conversations       │ Dev B   │ 2026-04-02 │
│      │ trong performance testing suite               │         │            │
├──────┼────────────────────────────────────────────────┼─────────┼────────────┤
│  5   │ Implement graceful degradation khi orchestrator│ Dev A   │ 2026-04-09 │
│      │ down: trả về 503 với retry-after header       │         │            │
│      │ và message thân thiện cho farmer              │         │            │
├──────┼────────────────────────────────────────────────┼─────────┼────────────┤
│  6   │ Document max_turns rationale trong ADR-004    │ Backend │ 2026-03-26 │
│      │                                                │ Lead    │            │
└──────┴────────────────────────────────────────────────┴─────────┴────────────┘
```

---

## 6. Lessons Learned

**Kỹ thuật:**
- Long-running stateful services cần memory profiling trong CI
- Memory limits phải được set dựa trên load test, không phải ước tính

**Process:**
- Load testing với real usage patterns (long conversations) quan trọng hơn chúng ta nghĩ
- Cần graceful degradation cho mọi core service

**Monitoring:**
- "No alert = no problem" là suy nghĩ nguy hiểm
- Resource utilization alerts phải có trước khi launch production

---

## 7. Metadata

```
Người tham gia xử lý incident:
  - Dev A (on-call, incident commander)
  - DevOps Lead (memory config fix)
  - Backend Lead (code review PR #234)

Người review post-mortem này:
  - Engineering Lead
  - CTO

Distribution: #engineering Slack, Confluence
```

---

## Checklist khi viết Post-mortem

```
□ Viết trong vòng 5 ngày sau incident
□ Timeline đủ chi tiết (mỗi bước có timestamp)
□ Root cause analysis: tìm hệ thống, không đổ lỗi người
□ "What went well" section: không chỉ criticize
□ Action items: có owner cụ thể + deadline cụ thể
□ Review bởi ít nhất 1 senior engineer
□ Share rộng rãi trong team (learning culture)
□ Follow up action items trong sprint tiếp theo
```

---

**© 2026 Bazan AI — Internal Document**
```

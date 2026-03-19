# Monitoring & Alerting Runbook — Bazan AI
## Hướng dẫn vận hành Giám sát hệ thống

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | DevOps Lead |
| **Người review** | Principal Architect, Backend Lead |
| **Liên quan** | Incident Response Playbook, SLA Definition, Capacity Planning |

---

## 1. Stack Observability

```
Logs:     Serilog → Seq (http://server:5341)
Metrics:  .NET metrics → Prometheus → Grafana (http://server:3000)
Traces:   OpenTelemetry → Grafana Tempo
Uptime:   Grafana synthetic monitoring (ping checks)
Alerts:   Grafana Alerting → Slack #bazan-alerts + PagerDuty (critical)
```

---

## 2. Dashboards

### 2.1 Dashboard: System Overview

**URL:** `http://server:3000/d/bazan-overview`

**Panels:**

| Panel | Query | Ngưỡng cảnh báo |
|-------|-------|----------------|
| API Request Rate | `rate(http_server_requests_total[5m])` | — |
| API Error Rate | `rate(http_server_requests_total{status=~"5.."}[5m])` | > 1% → Warning |
| P50/P95/P99 Latency | `histogram_quantile(0.95, http_request_duration_ms_bucket)` | P95 > 3s → Warning |
| Active Sessions | `bazan_active_sessions` | — |
| AI Token Usage | `rate(bazan_openai_tokens_total[1h])` | > 100K/h → Warning |
| Service Health | `up{job=~"bazan-.*"}` | Any = 0 → Critical |

### 2.2 Dashboard: Database Performance

**Panels:** MongoDB op latency, connection pool usage, replication lag, Qdrant search P95, Redis hit rate, RabbitMQ queue depth.

### 2.3 Dashboard: AI Quality (từ RLHF signals)

**Panels:** Thumbs-up rate (7d rolling), Re-ask rate, Avg response tokens, Hallucination alerts count.

---

## 3. Alert Rules

### 3.1 Critical Alerts (PagerDuty + Slack)

```yaml
# alerting_rules.yml

groups:
  - name: bazan_critical
    rules:

      - alert: ServiceDown
        expr: up{job=~"bazan-.*"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Service {{ $labels.job }} DOWN"
          runbook: "https://wiki.bazan/runbook/service-down"

      - alert: MongoDBPrimaryDown
        expr: mongodb_rs_members_health{state="PRIMARY"} == 0
        for: 30s
        labels:
          severity: critical
        annotations:
          summary: "MongoDB Primary không phản hồi — failover đang diễn ra"

      - alert: APIErrorRateHigh
        expr: |
          rate(http_server_requests_total{status=~"5.."}[5m]) /
          rate(http_server_requests_total[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Tỉ lệ lỗi 5xx > 5% trong 5 phút"

      - alert: DiskSpaceCritical
        expr: node_filesystem_avail_bytes / node_filesystem_size_bytes < 0.10
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Disk còn lại < 10%: {{ $labels.mountpoint }}"

      - alert: RabbitMQConsumerGone
        expr: rabbitmq_queue_consumers{queue="bazan.notification.dispatch"} == 0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Không có consumer cho notification queue — alerts không được gửi"

      - alert: SafetyIncident
        expr: increase(bazan_safety_violation_total[1h]) > 0
        labels:
          severity: critical
        annotations:
          summary: "AI trả lời có nội dung không an toàn — cần review ngay"
```

### 3.2 Warning Alerts (Slack only)

```yaml
      - alert: P95LatencyHigh
        expr: histogram_quantile(0.95, rate(http_request_duration_ms_bucket[5m])) > 3000
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "P95 latency > 3s trên {{ $labels.service }}"

      - alert: MongoDBReplicationLag
        expr: mongodb_rs_replication_lag_seconds > 30
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Replication lag MongoDB > 30s"

      - alert: QdrantMemoryHigh
        expr: container_memory_usage_bytes{name="bazan-qdrant"} / 2e9 > 0.80
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "Qdrant dùng > 80% memory budget (2GB)"

      - alert: RabbitMQQueueDepth
        expr: rabbitmq_queue_messages{queue="bazan.notification.dispatch"} > 10000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Notification queue tồn đọng > 10,000 messages"

      - alert: DeadLetterQueueGrowing
        expr: rabbitmq_queue_messages{queue="bazan.deadletter"} > 100
        for: 15m
        labels:
          severity: warning
        annotations:
          summary: "DLQ có > 100 messages — cần investigate consumer errors"

      - alert: OpenAICostSpike
        expr: increase(bazan_openai_tokens_total[1h]) > 500000
        labels:
          severity: warning
        annotations:
          summary: "Token OpenAI tăng đột biến: {{ $value }} tokens/h (bình thường ~50K)"

      - alert: FeedbackRateDrop
        expr: |
          rate(bazan_feedback_total{thumbs="up"}[2h]) /
          rate(bazan_feedback_total[2h]) < 0.60
        for: 2h
        labels:
          severity: warning
        annotations:
          summary: "Thumbs-up rate < 60% — chất lượng AI đang giảm"
```

---

## 4. Alert Routing

```yaml
# Grafana Alerting Contact Points

contact_points:
  - name: slack-warnings
    type: slack
    settings:
      url: ${SLACK_WEBHOOK_URL}
      channel: "#bazan-alerts"
      title: "⚠️ [{{ .Status | title }}] {{ .CommonAnnotations.summary }}"

  - name: pagerduty-critical
    type: pagerduty
    settings:
      integrationKey: ${PAGERDUTY_KEY}
      severity: critical

policies:
  - matchers:
      - severity = critical
    contact_point: pagerduty-critical
    continue: true  # Cũng gửi Slack

  - matchers:
      - severity = critical
    contact_point: slack-warnings

  - matchers:
      - severity = warning
    contact_point: slack-warnings
    group_wait: 5m      # Gộp alerts trong 5 phút
    group_interval: 30m  # Không repeat quá 30 phút
    repeat_interval: 4h
```

---

## 5. Runbook từng Alert

### ServiceDown

```
1. SSH vào server
2. Kiểm tra container: docker ps | grep {service}
3. Xem logs: docker logs bazan-{service} --tail 100
4. Restart: docker compose restart {service}
5. Nếu không hết: docker compose up -d --force-recreate {service}
6. Escalate nếu vẫn down sau 5 phút
```

### MongoDBPrimaryDown

```
1. Kiểm tra RS status:
   docker exec bazan-mongo-1 mongosh --eval "rs.status()"
2. Nếu đang failover → chờ 30s, RS tự bầu Primary mới
3. Nếu không tự phục hồi:
   docker compose restart mongo1 mongo2 mongo3
4. Nếu data corruption:
   → Kích hoạt Disaster Recovery Plan
```

### RabbitMQConsumerGone

```
1. Kiểm tra notification service:
   docker ps | grep notification
2. Nếu container down → restart
3. Nếu container up nhưng không consume:
   docker logs bazan-notification-svc --tail 50
4. Check RabbitMQ UI: http://server:15672 (qua SSH tunnel)
   → Tab Consumers → xem queue bazan.notification.dispatch
```

### DiskSpaceCritical

```
1. Xem dung lượng theo folder:
   du -sh /var/lib/docker/volumes/* | sort -rh | head 20
2. Cleanup Docker:
   docker system prune -f
3. Cleanup MongoDB logs:
   docker exec bazan-mongo-1 mongosh --eval "db.runCommand({logRotate: 1})"
4. Cleanup Seq logs cũ:
   (Seq tự rotate, kiểm tra retention setting)
5. Nếu vẫn đầy → add disk volume
```

---

## 6. Health Check Endpoints

| Service | Endpoint | Expected |
|---------|---------|---------|
| API Gateway | `/healthz` | `{"status":"healthy"}` |
| Identity | `http://identity-svc:5001/health` | 200 OK |
| Agronomy | `http://agronomy-svc:5004/health` | 200 OK |
| Qdrant | `http://qdrant:6333/healthz` | 200 OK |
| RabbitMQ | `rabbitmq-diagnostics ping` | pong |
| MongoDB | `db.runCommand("ping").ok` | 1 |
| Redis | `redis-cli ping` | PONG |

---

## 7. Structured Log Queries (Seq)

**Câu truy vấn thường dùng:**

```sql
-- Lỗi trong 1 giờ qua
Level = "Error" AND @Timestamp > Now() - 1h

-- Slow queries > 1 giây
Duration_ms > 1000 AND Application = "bazan-agronomy-svc"

-- Trace một request cụ thể
CorrelationId = "req-uuid-here"

-- AI safety violations
EventType = "safety_violation"

-- Failed message consumers
EventType = "consume_failed" AND RetryCount = 3
```

---

**© 2026 Bazan AI Project — Confidential**

# Disaster Recovery Plan — Bazan AI
## Kế hoạch Khôi phục Thảm họa

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | DevOps Lead · Principal Architect |
| **Review định kỳ** | 6 tháng / lần hoặc sau mỗi incident |
| **Liên quan** | Incident Response Playbook, Database Backup Plan, Monitoring Runbook |

---

## 1. RTO / RPO Targets

| Tier | Scenario | RPO | RTO | Priority |
|------|---------|-----|-----|---------|
| **Tier 0** | Toàn bộ platform down | < 1 giờ | < 2 giờ | P0 — Mọi người on-call |
| **Tier 1** | MongoDB Primary failure | 0 (Replica Set) | < 10 giây | P1 — Auto recovery |
| **Tier 1** | Single service crash | 0 | < 1 phút | P1 — Docker restart |
| **Tier 2** | Data corruption | < 24 giờ (daily backup) | < 2 giờ | P2 — Manual restore |
| **Tier 2** | Toàn bộ server mất | < 24 giờ | < 4 giờ | P2 — Re-provision |
| **Tier 3** | Knowledge Base corruption | < 1 tuần | < 8 giờ | P3 — Re-index |

---

## 2. Backup Strategy

### 2.1 MongoDB

```bash
# Chạy 3AM hàng ngày qua Hangfire

mongodump \
  --uri="mongodb://backup_user:${PASS}@mongo2:27017/?authSource=admin" \
  --readPreference=secondary \
  --oplog \
  --gzip \
  --out="/tmp/backup/mongodb/$(date +%Y%m%d)"

# Upload lên MinIO
mc cp --recursive /tmp/backup/mongodb/$(date +%Y%m%d) \
  minio/bazan-backups/mongodb/$(date +%Y/%m/%d)/

# Retention: 30 ngày daily, 3 tháng weekly (mỗi CN), 1 năm monthly (ngày 1 hàng tháng)
mc rm --recursive --force --older-than 30d minio/bazan-backups/mongodb/daily/
```

**Verification:** Mỗi thứ 6, restore sang `bazan-staging` và chạy smoke test.

### 2.2 Qdrant

```bash
# Snapshot Qdrant collections
curl -X POST http://qdrant:6333/collections/agronomy_knowledge/snapshots

# List và download snapshot mới nhất
SNAPSHOT=$(curl -s http://qdrant:6333/collections/agronomy_knowledge/snapshots \
  | jq -r '.result[-1].name')

curl -o "/tmp/qdrant_${SNAPSHOT}.snapshot" \
  "http://qdrant:6333/collections/agronomy_knowledge/snapshots/${SNAPSHOT}"

# Upload MinIO
mc cp /tmp/qdrant_${SNAPSHOT}.snapshot \
  minio/bazan-backups/qdrant/$(date +%Y/%m/%d)/
```

### 2.3 MinIO (Object Storage)

```bash
# Sync MinIO buckets sang remote backup
mc mirror \
  minio/knowledge-docs \
  minio-backup/bazan-knowledge-docs

# Chạy hàng ngày, giữ 90 ngày versioning
```

### 2.4 Secrets & Config

```bash
# Backup .env và config files (encrypted)
tar -czf /tmp/config_$(date +%Y%m%d).tar.gz \
  infra/.env \
  infra/rabbitmq/definitions.json \
  infra/mongo-init/

# Encrypt với GPG trước khi upload
gpg --symmetric --cipher-algo AES256 /tmp/config_$(date +%Y%m%d).tar.gz

mc cp /tmp/config_$(date +%Y%m%d).tar.gz.gpg \
  minio/bazan-backups/config/
```

---

## 3. Recovery Procedures

### 3.1 Scenario: Single Service Crash (Tier 1)

```bash
# Tự động qua Docker restart policy
# container_name: bazan-agronomy-svc
# restart: unless-stopped

# Manual restart nếu cần:
docker compose restart agronomy-svc

# Kiểm tra:
docker logs bazan-agronomy-svc --tail 50
curl http://localhost:5004/health
```

**RTO thực tế:** 20–60 giây

---

### 3.2 Scenario: MongoDB Primary Failure (Tier 1)

```bash
# Bước 1: Quan sát — RS tự failover
docker exec bazan-mongo-1 mongosh --eval "rs.status()"
# Đợi 10–30 giây

# Bước 2: Xác nhận Primary mới
docker exec bazan-mongo-2 mongosh \
  -u root -p ${MONGO_ROOT_PASSWORD} \
  --authenticationDatabase admin \
  --eval "rs.isMaster().primary"

# Bước 3: Restart node cũ để join lại RS
docker compose restart mongo1

# Bước 4: Verify replication đã đồng bộ
docker exec bazan-mongo-2 mongosh --eval "rs.printReplicationInfo()"
```

**RTO thực tế:** < 30 giây (auto failover)

---

### 3.3 Scenario: Data Corruption (Tier 2)

```bash
# Bước 1: Stop tất cả services để tránh write thêm vào data corrupt
docker compose stop

# Bước 2: Tìm backup gần nhất trước thời điểm corruption
mc ls minio/bazan-backups/mongodb/$(date +%Y/%m/) | tail -5

# Bước 3: Download backup
BACKUP_DATE="20260319"
mc cp --recursive \
  minio/bazan-backups/mongodb/2026/03/${BACKUP_DATE}/ \
  /tmp/restore/

# Bước 4: Stop MongoDB containers, xóa data volume
docker compose stop mongo1 mongo2 mongo3
docker volume rm bazanai_mongo-data-1 bazanai_mongo-data-2 bazanai_mongo-data-3

# Bước 5: Start MongoDB fresh
docker compose up -d mongo1 mongo2 mongo3

# Bước 6: Initialize RS (nếu cần)
sleep 10
docker exec bazan-mongo-1 mongosh \
  -u root -p ${MONGO_ROOT_PASSWORD} \
  --authenticationDatabase admin \
  --file /docker-entrypoint-initdb.d/init-replica.js

# Bước 7: Restore
mongorestore \
  --uri="mongodb://root:${PASS}@localhost:27017/?replicaSet=rs0&authSource=admin" \
  --oplogReplay \
  --gzip \
  --drop \
  /tmp/restore/${BACKUP_DATE}/

# Bước 8: Verify
docker exec bazan-mongo-1 mongosh --eval \
  "use bazan_identity; db.farmers.countDocuments()"

# Bước 9: Restart application services
docker compose up -d
```

**RTO thực tế:** 1–2 giờ

---

### 3.4 Scenario: Full Server Loss (Tier 2)

```bash
# Giả sử server mới đã được provision với Ubuntu 22.04

# Bước 1: Cài Docker & Docker Compose
curl -fsSL https://get.docker.com | sh
docker compose version

# Bước 2: Clone repo hoặc extract deployment package
git clone https://github.com/bazanai/infrastructure.git
cd infrastructure

# Bước 3: Restore secrets
mc cp minio-remote/bazan-backups/config/config_latest.tar.gz.gpg /tmp/
gpg --decrypt /tmp/config_latest.tar.gz.gpg | tar -xz -C infra/

# Bước 4: Pull Docker images
docker compose pull

# Bước 5: Start infrastructure first
docker compose up -d mongo1 mongo2 mongo3 redis rabbitmq minio qdrant

# Bước 6: Restore MongoDB từ backup
./scripts/restore-mongodb.sh $(date +%Y%m%d)

# Bước 7: Restore MinIO buckets
mc mirror minio-remote/bazan-knowledge-docs minio/knowledge-docs

# Bước 8: Restore Qdrant snapshots
./scripts/restore-qdrant.sh

# Bước 9: Start application services
docker compose up -d

# Bước 10: Smoke test
./scripts/smoke-test.sh

# Bước 11: Update DNS (nếu IP thay đổi)
# Cập nhật A record tại DNS provider
```

**RTO thực tế:** 3–4 giờ

---

### 3.5 Scenario: Qdrant Knowledge Base Corruption (Tier 3)

```bash
# Knowledge base có thể rebuild từ MinIO (PDF files vẫn còn)
# Không ảnh hưởng chat history hay farmer data

# Bước 1: Restore Qdrant từ snapshot (nếu có)
curl -X POST \
  "http://qdrant:6333/collections/agronomy_knowledge/snapshots/recover" \
  -H "Content-Type: application/json" \
  -d '{"location": "file:///qdrant/snapshots/agronomy_knowledge-backup.snapshot"}'

# Bước 2: Nếu không có snapshot — re-index từ MinIO PDFs
# Trigger via API (sẽ mất 6-8 giờ cho 450 docs)
curl -X POST http://localhost:5003/api/v1/knowledge/reindex \
  -H "X-Service-Api-Key: ${INTERNAL_KEY}" \
  -d '{"collection": "all", "force": true}'

# Bước 3: Monitor progress
watch -n 30 'curl -s http://qdrant:6333/collections/agronomy_knowledge | jq .result.points_count'
```

**RTO thực tế:** 1–8 giờ (tùy còn snapshot không)

---

## 4. DR Testing Calendar

| Test | Tần suất | Môi trường | Owner |
|------|---------|-----------|-------|
| MongoDB failover (kill Primary) | Hàng tháng | Staging | DevOps |
| Full restore từ backup | Hàng quý | DR environment | DevOps |
| Full server rebuild | 6 tháng/lần | DR environment | DevOps + Arch |
| Smoke test sau restore | Mỗi lần restore | Any | DevOps |

---

## 5. Communication Plan khi DR

```
Nội bộ team (Slack #incident):
  "🚨 DR Activated — Scenario: {scenario}
   Started: {time}
   Lead: {name}
   ETA: {estimated recovery time}
   Status updates mỗi 30 phút"

Nếu downtime > 1 giờ → thông báo cho HTX/farmers:
  Zalo broadcast: "Hệ thống Bazan AI đang bảo trì khẩn cấp.
                   Dự kiến hoạt động trở lại lúc {time}.
                   Xin lỗi vì sự bất tiện."
```

---

**© 2026 Bazan AI Project — Confidential**

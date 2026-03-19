# ADR-001 — Primary Database: MongoDB Replica Set

| Trường | Nội dung |
|--------|---------|
| **ID** | ADR-001 |
| **Tiêu đề** | Lựa chọn MongoDB làm Primary Database cho Bazan AI |
| **Trạng thái** | Accepted |
| **Ngày tạo** | 2026-03-19 |
| **Ngày cập nhật** | 2026-03-19 |
| **Tác giả** | Principal Software Architect |
| **Người review** | Backend Lead, DevOps Lead, Data Engineer |
| **Liên quan** | ADR-002 (Qdrant), ADR-003 (RabbitMQ), ADR-005 (YARP), System Architecture Document |

---

## 1. Bối cảnh

Bazan AI cần lưu trữ nhiều loại dữ liệu có đặc điểm rất khác nhau, phát sinh từ đặc thù của hệ thống nông nghiệp thông minh phục vụ nông dân Tây Nguyên:

### 1.1 Đặc điểm dữ liệu

**Dữ liệu nông dân và vườn** có cấu trúc không đồng nhất theo từng cá nhân:
- Nông dân A có 3 vườn, 4 loại cảm biến, 2 chứng nhận
- Nông dân B có 1 vườn, không cảm biến, 0 chứng nhận
- Nông dân C có vườn xen canh hồ tiêu — có thêm trường `intercrop_data` không tồn tại ở nông dân khác

Nếu dùng RDBMS, cần hàng chục bảng với nhiều nullable column hoặc EAV pattern — cả hai đều làm tăng complexity và giảm performance.

**Dữ liệu hội thoại AI** có cấu trúc lồng nhau tự nhiên:
```
Session
  └─ Messages[]
       ├─ Content (text / text+image / audio)
       ├─ Attachments[]
       │    └─ AnalysisResult (nested, chỉ có nếu là ảnh)
       ├─ ContextUsed (RAG docs, sensor data flags)
       └─ Feedback (nullable, chỉ khi nông dân đánh giá)
```

**Metadata AI hành vi người dùng** tích lũy dần, schema mở rộng liên tục:
- Ban đầu: `preferredQueryTopics`, `sessionFrequency`
- Tháng 3: thêm `adaptivePromptVersion`
- Tháng 6: thêm `voiceInputPreference`, `offlineSyncBehavior`
- Không thể dự đoán trước toàn bộ trường — đặc thù của AI personalization

**Dữ liệu cảm biến IoT** thay đổi cấu hình theo từng vườn:
- Vườn dùng soil moisture + temperature (2 sensors)
- Vườn khác dùng thêm pH + lux + rain gauge (5 sensors)
- Mỗi loại sensor có schema reading riêng

### 1.2 Yêu cầu phi chức năng

| Yêu cầu | Metric |
|---------|--------|
| Availability | 99.5% uptime |
| Read latency | P95 < 50ms cho profile lookup |
| Write throughput | Hỗ trợ 1,000 sensor readings/giây (Phase 3) |
| Geospatial query | Tìm vườn trong bán kính GPS |
| Schema flexibility | Thêm field mới không cần migration |
| Horizontal scale | Read scale-out qua Secondary nodes |
| Disaster recovery | RPO < 1 giờ, RTO < 30 phút |

### 1.3 Ràng buộc

- Team quen với .NET 8 — driver MongoDB C# được Microsoft support tốt
- Budget Phase 1 không đủ cho managed database service (Atlas) — self-hosted
- Deployment trên Docker Compose (Phase 1–3), Kubernetes (Phase 4)
- Không có DBA chuyên nghiệp — cần database dễ vận hành

---

## 2. Các phương án đã xem xét

### Phương án A — MongoDB (Replica Set)

**Mô tả:** Document database lưu dữ liệu dưới dạng BSON document. Replica Set gồm 1 Primary + 2 Secondary đảm bảo HA và read scale-out.

**Ưu điểm:**
- Schema-less: thêm field mới vào document không cần `ALTER TABLE`, không downtime
- Native geospatial indexing: `2dsphere` index cho query theo GPS coordinates
- Document model phù hợp tự nhiên với dữ liệu lồng nhau (session → messages → attachments)
- Aggregation Pipeline mạnh mẽ: tính ROI, thống kê mùa vụ trong database
- Change Streams: subscribe thay đổi document real-time → trigger event đến RabbitMQ
- .NET driver (MongoDB.Driver) được MongoDB Inc. maintain tích cực
- Self-hosted miễn phí (Community Edition, SSPL license)
- Replica Set: tự động failover < 10 giây khi Primary down

**Nhược điểm:**
- Không có ACID transaction đa collection theo mặc định (có từ v4.0 nhưng có overhead)
- Join phức tạp hơn SQL — cần denormalize có chủ ý
- SSPL license: không thể redistribute như service (không ảnh hưởng vì self-hosted)
- Require `keyFile` để Replica Set secure — thêm bước setup

---

### Phương án B — PostgreSQL với JSONB

**Mô tả:** RDBMS truyền thống với extension JSONB cho phép lưu semi-structured data trong column.

**Ưu điểm:**
- ACID transaction đầy đủ, mature nhất trong ngành
- SQL quen thuộc, tooling phong phú
- JSONB column cho phép schema linh hoạt khi cần
- PostGIS extension cho geospatial — mạnh hơn MongoDB 2dsphere
- Full-text search tích hợp

**Nhược điểm:**
- JSONB là giải pháp "patch" — trộn lẫn relational và document làm codebase phức tạp hơn
- Khi 80% dữ liệu là JSONB, dùng PostgreSQL mất đi hầu hết lợi thế relational
- Schema migration (Flyway/Liquibase) bắt buộc mỗi khi thêm column — không phù hợp với AI metadata thay đổi liên tục
- Vertical scale chủ yếu (read replica khó hơn MongoDB Secondary)
- Team .NET ít kinh nghiệm PostgreSQL hơn MongoDB

**Không chọn vì:** Lợi thế relational bị mất khi schema linh hoạt là yêu cầu chính. JSONB là compromise tệ nhất của cả hai thế giới cho use case này.

---

### Phương án C — Microsoft SQL Server / Azure SQL

**Mô tả:** RDBMS enterprise của Microsoft, tích hợp tốt với .NET ecosystem.

**Ưu điểm:**
- Tích hợp native với .NET, Entity Framework Core
- ACID transaction tốt nhất trong class
- JSON support từ SQL Server 2016
- Quen thuộc với nhiều .NET developer

**Nhược điểm:**
- License: SQL Server Standard ~$3,700/năm, Enterprise cao hơn — quá đắt cho startup
- Geospatial: SQL Server Spatial tốt nhưng ít tài liệu hơn PostGIS
- Schema migration bắt buộc — xem nhược điểm A
- Không native document model — cùng vấn đề JSONB
- Không phù hợp với Docker Compose self-hosted (cần license Windows hoặc Linux container)

**Không chọn vì:** Chi phí license không phù hợp Phase 1 MVP. Giải quyết bài toán schema flexibility kém hơn MongoDB.

---

### Phương án D — CassandraDB

**Mô tả:** Distributed wide-column database, tối ưu cho write-heavy workload và time-series.

**Ưu điểm:**
- Write throughput cực cao — lý tưởng cho IoT sensor data
- Horizontal scale tốt nhất trong các phương án
- Multi-datacenter replication native

**Nhược điểm:**
- Query rất hạn chế: không có JOIN, không có aggregation, index phải khai báo trước
- Không có secondary index linh hoạt — mỗi query pattern cần table riêng (denormalize nặng)
- Không phù hợp cho đọc theo nhiều chiều (profile lookup, chat history, geospatial)
- Complexity vận hành cao — cần Cassandra DBA
- Không phù hợp giai đoạn early product khi data model còn thay đổi liên tục

**Không chọn vì:** Over-engineered cho Phase 1. Write throughput yêu cầu (~1,000 readings/giây) MongoDB Replica Set hoàn toàn đáp ứng được.

---

### Phương án E — DynamoDB (AWS)

**Mô tả:** Managed NoSQL key-value + document database của AWS.

**Ưu điểm:**
- Fully managed — không cần vận hành
- Auto-scaling, serverless pricing
- SLA 99.999% availability

**Nhược điểm:**
- Vendor lock-in AWS — không thể self-host, không có exit plan
- Pricing model phức tạp và khó dự đoán (read/write capacity units)
- Query flexibility kém — phải thiết kế access pattern từ đầu, khó thay đổi
- Geospatial query không native (cần DynamoDB Geo library bên ngoài)
- Không phù hợp với Docker Compose development workflow

**Không chọn vì:** Vendor lock-in và pricing model không phù hợp với startup giai đoạn đầu cần kiểm soát cost.

---

## 3. Quyết định

**Chọn Phương án A — MongoDB Replica Set (3 nodes)** làm Primary Database cho toàn bộ hệ thống Bazan AI.

**Cấu hình:**
- 1 Primary + 2 Secondary
- WiredTiger storage engine
- Authentication với keyFile
- Replica Set name: `rs0`

---

## 4. Lý do

### 4.1 Document model là lựa chọn tự nhiên

Dữ liệu `UserContext` của nông dân là một entity phức hợp, phản ánh đúng nhất bằng một document duy nhất:

```json
{
  "_id": "...",
  "farmerId": "...",
  "location": { "type": "Point", "coordinates": [108.03, 12.67] },
  "farmContext": {
    "coffeeVarieties": ["Robusta", "TR4"],
    "treeAgeDistribution": { "0-3yr": 300, "4-8yr": 1500 }
  },
  "sensorMetadata": {
    "devices": [ { "deviceId": "...", "type": "soil_moisture" } ],
    "lastSensorReadings": { "soilMoisture_pct": 65.3 }
  },
  "behaviorProfile": { "preferredQueryTopics": ["fertilizer", "pest_control"] },
  "aiPersonalization": { "adaptivePromptVersion": "v2.3" }
}
```

Nếu dùng PostgreSQL, entity này cần ít nhất 6 bảng với nhiều foreign key join. Mỗi lần AI Agent cần toàn bộ context để sinh câu trả lời, phải execute 6 JOIN query thay vì 1 document fetch. Với P95 latency target 50ms cho profile lookup, document fetch trong MongoDB (indexed `_id`) đạt < 5ms, trong khi multi-table JOIN PostgreSQL cần 15–40ms.

### 4.2 Geospatial query native

Bazan AI cần nhiều query địa lý:
- Tìm vườn trong vòng 5km của một tọa độ GPS
- Tìm đại lý thu mua gần nhất
- Group nông dân theo vùng để gửi cảnh báo thời tiết

```javascript
// MongoDB 2dsphere index — syntax đơn giản, hiệu năng cao
db.farmers.createIndex({ "location.coordinates": "2dsphere" })

db.farmers.find({
  "location.coordinates": {
    $nearSphere: {
      $geometry: { type: "Point", coordinates: [108.03, 12.67] },
      $maxDistance: 5000  // meters
    }
  }
})
```

Tương đương trong PostgreSQL cần PostGIS extension với syntax phức tạp hơn và cần DBA để optimize.

### 4.3 Change Streams cho Event-driven Architecture

MongoDB Change Streams cho phép subscribe vào thay đổi document real-time — là cầu nối tự nhiên giữa database và RabbitMQ:

```csharp
// Identity Service — tự động publish event khi farmer location thay đổi
var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<Farmer>>()
    .Match(change =>
        change.OperationType == ChangeStreamOperationType.Update &&
        change.UpdateDescription.UpdatedFields.Contains("location"));

using var cursor = await collection.WatchAsync(pipeline);

await cursor.ForEachAsync(change => {
    var farmerId = change.FullDocument.Id;
    var newCoords = change.FullDocument.Location.Coordinates;

    await eventPublisher.PublishAsync(new UserLocationUpdatedEvent {
        FarmerId = farmerId,
        NewCoordinates = newCoords
    });
});
```

Không cần polling database hay outbox pattern phức tạp cho các event đơn giản.

### 4.4 Schema Evolution không cần migration

Trong 6 tháng đầu, schema của AI behavior profile sẽ thay đổi liên tục khi team học được pattern dùng của nông dân. Với MongoDB:

```csharp
// Thêm field mới vào document — không cần migration
await collection.UpdateOneAsync(
    filter: Builders<UserContext>.Filter.Eq(u => u.FarmerId, farmerId),
    update: Builders<UserContext>.Update.Set(
        "aiPersonalization.voiceInputPreference", "vietnamese_central_highlands"
    )
);
// Document cũ không có field này → trả về null khi đọc → handle bằng null coalescing
```

Với PostgreSQL, cần: viết migration script → test → deploy → rollback plan → execute. Mỗi lần thêm column là một deployment risk.

### 4.5 Aggregation Pipeline cho Predictive Analytics

Tính ROI và thống kê mùa vụ không cần load dữ liệu ra application layer:

```javascript
// Tính ROI trung bình theo giống cà phê — chạy hoàn toàn trong MongoDB
db.chatSessions.aggregate([
  { $match: { "contextSnapshot.sessionIntent": "roi_calculation" } },
  { $lookup: {
      from: "farmers",
      localField: "farmerId",
      foreignField: "_id",
      as: "farmer"
  }},
  { $unwind: "$farmer" },
  { $group: {
      _id: "$farmer.farmContext.coffeeVarieties",
      avgSatisfaction: { $avg: "$satisfactionRating" },
      totalSessions: { $sum: 1 }
  }}
])
```

### 4.6 Replica Set — HA và Read Scale-out

```
Primary   ──writes──► Secondary 1
                ├──reads (optional)──► Secondary 2
                │
                └── Automatic failover < 10 giây khi Primary down

Read preference: secondaryPreferred
→ Query analytics, report chạy trên Secondary
→ Không ảnh hưởng write performance của Primary
```

---

## 5. Thiết kế triển khai

### 5.1 Replica Set Configuration (Docker Compose)

```yaml
mongo1:
  image: mongo:7.0
  command: ["--replSet", "rs0", "--bind_ip_all",
            "--keyFile", "/etc/mongo-keyfile"]
  volumes:
    - mongo-data-1:/data/db
    - ./infra/mongo-init/keyfile:/etc/mongo-keyfile:ro
  environment:
    MONGO_INITDB_ROOT_USERNAME: ${MONGO_ROOT_USER}
    MONGO_INITDB_ROOT_PASSWORD: ${MONGO_ROOT_PASSWORD}
  healthcheck:
    test: echo 'db.runCommand("ping").ok' | mongosh localhost:27017/test --quiet
    interval: 10s
    timeout: 10s
    retries: 5

mongo2:
  image: mongo:7.0
  command: ["--replSet", "rs0", "--bind_ip_all",
            "--keyFile", "/etc/mongo-keyfile"]
  # ... same pattern

mongo3:
  image: mongo:7.0
  command: ["--replSet", "rs0", "--bind_ip_all",
            "--keyFile", "/etc/mongo-keyfile"]
  # ... same pattern
```

### 5.2 Replica Set Initialization Script

```javascript
// infra/mongo-init/init-replica.js
// Chạy một lần sau khi 3 container healthy

rs.initiate({
  _id: "rs0",
  members: [
    { _id: 0, host: "mongo1:27017", priority: 2 },  // Preferred Primary
    { _id: 1, host: "mongo2:27017", priority: 1 },
    { _id: 2, host: "mongo3:27017", priority: 1 },
  ]
});

// Đợi PRIMARY sẵn sàng
let status = rs.status();
while (status.myState !== 1) {
  sleep(1000);
  status = rs.status();
}
print("Replica Set initialized. Primary:", status.members.find(m => m.state === 1).name);
```

### 5.3 Database & User Separation

Mỗi service có database riêng và user riêng với quyền tối thiểu:

```javascript
// infra/mongo-init/create-users.js
const dbUserMap = [
  { db: "bazan_identity",      user: "identity_svc",      roles: ["readWrite"] },
  { db: "bazan_conversations", user: "conversation_svc",  roles: ["readWrite"] },
  { db: "bazan_agronomy",      user: "agronomy_svc",      roles: ["readWrite"] },
  { db: "bazan_market",        user: "market_svc",        roles: ["readWrite"] },
  { db: "bazan_knowledge",     user: "knowledge_svc",     roles: ["readWrite"] },
  { db: "bazan_iot",           user: "iot_svc",           roles: ["readWrite"] },
  { db: "bazan_weather",       user: "weather_svc",       roles: ["readWrite"] },
  // Analytics user chỉ đọc, trên secondary preferred
  { db: "bazan_identity",      user: "analytics_reader",  roles: ["read"] },
  { db: "bazan_conversations", user: "analytics_reader",  roles: ["read"] },
];

dbUserMap.forEach(({ db, user, roles }) => {
  const password = process.env[`${user.toUpperCase()}_PASSWORD`];
  db.getSiblingDB(db).createUser({ user, pwd: password, roles });
});
```

### 5.4 Connection String Pattern

```csharp
// appsettings.json — Identity Service
{
  "MongoDB": {
    "ConnectionString": "mongodb://identity_svc:{PASSWORD}@mongo1:27017,mongo2:27017,mongo3:27017/bazan_identity?replicaSet=rs0&authSource=bazan_identity&readPreference=primaryPreferred&connectTimeoutMS=5000&serverSelectionTimeoutMS=5000",
    "Database": "bazan_identity"
  }
}

// Infrastructure/Persistence/MongoDbContext.cs
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var clientSettings = MongoClientSettings
            .FromConnectionString(settings.Value.ConnectionString);

        // Đăng ký serializer cho các custom type
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer());

        // Serilog integration cho slow query logging
        clientSettings.ClusterConfigurator = cb => {
            cb.Subscribe<CommandStartedEvent>(e => {
                if (e.Command.Contains("filter"))
                    Log.Debug("MongoDB command: {CommandName}", e.CommandName);
            });
            cb.Subscribe<CommandSucceededEvent>(e => {
                if (e.Duration > TimeSpan.FromMilliseconds(100))
                    Log.Warning("Slow MongoDB query: {CommandName} took {Duration}ms",
                        e.CommandName, e.Duration.TotalMilliseconds);
            });
        };

        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(settings.Value.Database);
    }

    public IMongoCollection<T> GetCollection<T>(string name)
        => _database.GetCollection<T>(name);
}
```

### 5.5 Index Strategy

```csharp
// Infrastructure/Persistence/Configurations/IndexConfigurator.cs
public static class IndexConfigurator
{
    public static async Task EnsureIndexesAsync(IMongoDatabase db)
    {
        // ── bazan_identity ──────────────────────────────────────
        var farmers = db.GetCollection<Farmer>("farmers");

        // Geospatial — tìm vườn gần GPS
        await farmers.Indexes.CreateOneAsync(
            new CreateIndexModel<Farmer>(
                Builders<Farmer>.IndexKeys.Geo2DSphere(f => f.Location.Coordinates),
                new CreateIndexOptions { Name = "idx_location_2dsphere" }
            ));

        // Compound — filter theo soil type + coffee variety cho RAG context
        await farmers.Indexes.CreateOneAsync(
            new CreateIndexModel<Farmer>(
                Builders<Farmer>.IndexKeys
                    .Ascending(f => f.FarmContext.SoilType)
                    .Ascending(f => f.FarmContext.CoffeeVarieties),
                new CreateIndexOptions { Name = "idx_soil_variety" }
            ));

        // Unique — phone number
        await farmers.Indexes.CreateOneAsync(
            new CreateIndexModel<Farmer>(
                Builders<Farmer>.IndexKeys.Ascending(f => f.PhoneNumber),
                new CreateIndexOptions { Unique = true, Name = "idx_phone_unique" }
            ));

        // ── bazan_conversations ──────────────────────────────────
        var sessions = db.GetCollection<ChatSession>("chatSessions");

        // Lookup lịch sử session theo farmer, mới nhất lên đầu
        await sessions.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatSession>(
                Builders<ChatSession>.IndexKeys
                    .Ascending(s => s.FarmerId)
                    .Descending(s => s.StartedAt),
                new CreateIndexOptions { Name = "idx_farmer_session_time" }
            ));

        var messages = db.GetCollection<ChatMessage>("chatMessages");

        // Lookup messages trong session theo thứ tự thời gian
        await messages.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys
                    .Ascending(m => m.SessionId)
                    .Ascending(m => m.Timestamp),
                new CreateIndexOptions { Name = "idx_session_messages_time" }
            ));

        // Export RLHF data — lọc theo rating
        await messages.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys
                    .Ascending(m => m.FarmerId)
                    .Ascending("feedback.rating"),
                new CreateIndexOptions {
                    Name = "idx_farmer_feedback",
                    Sparse = true   // Chỉ index document có feedback
                }
            ));

        // ── bazan_agronomy ───────────────────────────────────────
        var alerts = db.GetCollection<DiseaseAlert>("diseaseAlerts");

        // Tìm alert đang active theo vùng địa lý
        await alerts.Indexes.CreateOneAsync(
            new CreateIndexModel<DiseaseAlert>(
                Builders<DiseaseAlert>.IndexKeys
                    .Ascending(a => a.IsResolved)
                    .Ascending(a => a.DiseaseCode)
                    .Ascending(a => a.Severity),
                new CreateIndexOptions { Name = "idx_active_alerts" }
            ));

        // TTL index — tự xóa alert đã resolve sau 90 ngày
        await alerts.Indexes.CreateOneAsync(
            new CreateIndexModel<DiseaseAlert>(
                Builders<DiseaseAlert>.IndexKeys.Ascending(a => a.ResolvedAt),
                new CreateIndexOptions {
                    Name = "idx_ttl_resolved_alerts",
                    ExpireAfter = TimeSpan.FromDays(90),
                    Sparse = true
                }
            ));
    }
}
```

### 5.6 CQRS với Read Preference

```csharp
// Read operations → secondaryPreferred (không load Primary)
// Write operations → primary (mặc định)

public class FarmerRepository : IFarmerRepository
{
    private readonly IMongoCollection<Farmer> _collection;
    private readonly IMongoCollection<Farmer> _readCollection;

    public FarmerRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Farmer>("farmers");

        // Read collection dùng Secondary
        _readCollection = _collection.WithReadPreference(
            ReadPreference.SecondaryPreferred);
    }

    // Queries → secondary
    public async Task<Farmer?> FindByIdAsync(string id, CancellationToken ct)
        => await _readCollection
            .Find(f => f.Id == id)
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<Farmer>> FindNearLocationAsync(
        double lat, double lng, double radiusMeters, CancellationToken ct)
        => await _readCollection
            .Find(Builders<Farmer>.Filter.NearSphere(
                f => f.Location.Coordinates,
                longitude: lng, latitude: lat,
                maxDistance: radiusMeters))
            .ToListAsync(ct);

    // Commands → primary (default)
    public async Task<string> CreateAsync(Farmer farmer, CancellationToken ct)
    {
        await _collection.InsertOneAsync(farmer, null, ct);
        return farmer.Id;
    }

    public async Task UpdateAsync(Farmer farmer, CancellationToken ct)
        => await _collection.ReplaceOneAsync(
            f => f.Id == farmer.Id, farmer,
            new ReplaceOptions { IsUpsert = false }, ct);
}
```

---

## 6. Vận hành & Backup

### 6.1 Backup Strategy

```bash
#!/bin/bash
# infra/scripts/backup-mongodb.sh
# Chạy hàng ngày lúc 3AM qua Hangfire Scheduler

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups/mongodb/${TIMESTAMP}"

# mongodump từ Secondary (tránh load Primary)
mongodump \
  --uri="mongodb://backup_user:${BACKUP_PASSWORD}@mongo2:27017/?authSource=admin" \
  --readPreference=secondary \
  --oplog \
  --gzip \
  --out="${BACKUP_DIR}"

# Upload lên MinIO
mc cp --recursive "${BACKUP_DIR}" \
  minio/bazan-backups/mongodb/${TIMESTAMP}/

# Xóa backup local sau khi upload
rm -rf "${BACKUP_DIR}"

# Xóa backup MinIO cũ hơn 30 ngày
mc rm --recursive --force --older-than 30d \
  minio/bazan-backups/mongodb/

echo "Backup completed: ${TIMESTAMP}"
```

### 6.2 Restore Procedure

```bash
# Restore từ backup gần nhất
BACKUP_DATE="20260319_030000"

# Download từ MinIO
mc cp --recursive \
  minio/bazan-backups/mongodb/${BACKUP_DATE}/ \
  /tmp/restore/

# Restore vào Replica Set
mongorestore \
  --uri="mongodb://admin:${ADMIN_PASSWORD}@mongo1:27017/?replicaSet=rs0&authSource=admin" \
  --oplogReplay \
  --gzip \
  --drop \
  /tmp/restore/${BACKUP_DATE}/

# RPO target: < 1 giờ (backup hàng ngày + oplog replay)
# RTO target: < 30 phút
```

### 6.3 Health Check & Monitoring

```javascript
// Prometheus metrics qua mongodb_exporter
// Các metric quan trọng cần alert:

Metric                          | Alert threshold
--------------------------------|------------------------
mongodb_up                      | == 0 → Critical
mongodb_rs_members_health       | < 2 healthy → Warning
mongodb_rs_replication_lag_sec  | > 30s → Warning
mongodb_connections_current     | > 80% maxConnections → Warning
mongodb_op_latencies_latency_ms | P99 > 500ms → Warning (writes)
mongodb_wiredtiger_cache_used_% | > 90% → Warning
mongodb_opcounters_total{type=query} | spike 10x baseline → Investigate
```

### 6.4 Maintenance Window

| Task | Tần suất | Thời điểm | Tác động |
|------|---------|----------|---------|
| Compact collection | Hàng tháng | Chủ nhật 2AM | Không (chạy trên Secondary trước) |
| Index rebuild | Khi cần | Chủ nhật 2AM | Đọc chậm hơn trong lúc build |
| Version upgrade | Hàng quý | Kế hoạch trước 2 tuần | Rolling upgrade, zero downtime |
| Backup verification | Hàng tuần | Thứ 6 3AM | Restore sang môi trường test |

---

## 7. Hậu quả & Trade-offs

### 7.1 Tích cực

- **Developer velocity cao:** Thêm field mới, thay đổi schema → commit code ngay, không cần migration
- **Query đơn giản cho use case chính:** Fetch toàn bộ farmer context = 1 document read
- **Geospatial out-of-the-box:** Không cần extension, setup phức tạp
- **Event-driven dễ implement:** Change Streams → RabbitMQ không cần outbox pattern phức tạp
- **Read scale-out miễn phí:** Secondary nodes xử lý analytics query, Primary tập trung write

### 7.2 Tiêu cực & Cách giảm thiểu

| Trade-off | Mức độ | Cách giảm thiểu |
|-----------|--------|----------------|
| Không có ACID multi-collection transaction | Trung bình | Saga pattern cho distributed transaction (RabbitMQ choreography). Chấp nhận eventual consistency cho các flow không critical. |
| Join phức tạp hơn SQL | Thấp | Denormalize có chủ ý: embed data thường đọc cùng nhau. Dùng `$lookup` sparingly, chỉ khi cần. |
| Schema-less dễ dẫn đến inconsistency | Trung bình | Enforce schema validation trong MongoDB (`$jsonSchema`). C# model là source of truth. |
| SSPL license | Thấp | Self-hosted nội bộ, không redistribute → không vi phạm SSPL. Hoặc upgrade Atlas nếu cần managed. |
| Không có stored procedure | Thấp | Business logic ở Application layer — đây là best practice cho microservices. |

### 7.3 MongoDB Schema Validation (Guard rail)

```javascript
// Enforce tối thiểu structure cho farmers collection
db.createCollection("farmers", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["farmerId", "phoneNumber", "location", "createdAt"],
      properties: {
        farmerId:    { bsonType: "string" },
        phoneNumber: { bsonType: "string", pattern: "^\\+84[0-9]{9}$" },
        location: {
          bsonType: "object",
          required: ["type", "coordinates"],
          properties: {
            type:        { enum: ["Point"] },
            coordinates: { bsonType: "array", minItems: 2, maxItems: 2 }
          }
        },
        createdAt: { bsonType: "date" }
      }
    }
  },
  validationLevel: "moderate",   // Validate insert + update, không block nếu doc cũ không hợp lệ
  validationAction: "warn"       // Log warning thay vì reject (Phase 1 — strict hơn sau)
})
```

---

## 8. Tiêu chí đánh giá lại quyết định

Xem xét lại ADR này nếu:

- **Multi-collection ACID transaction** trở thành yêu cầu thường xuyên và Saga pattern gây quá nhiều complexity → Xem xét PostgreSQL cho service cụ thể đó
- **Reporting & Analytics** phức tạp cần SQL JOIN nhiều chiều → Thêm PostgreSQL read replica hoặc ClickHouse riêng cho analytics, giữ MongoDB cho operational data
- **Số lượng nông dân vượt 100,000** và write throughput vượt giới hạn Replica Set → Xem xét MongoDB Sharding hoặc đánh giá lại data partitioning strategy
- **Atlas migration** trở nên hợp lý về giá (managed service loại bỏ ops burden khi team scale)

---

## 9. Tham khảo

- [MongoDB Replica Set Architecture](https://www.mongodb.com/docs/manual/replication/)
- [MongoDB Change Streams](https://www.mongodb.com/docs/manual/changeStreams/)
- [MongoDB .NET Driver Documentation](https://www.mongodb.com/docs/drivers/csharp/)
- [WiredTiger Storage Engine](https://www.mongodb.com/docs/manual/core/wiredtiger/)
- [SSPL License Analysis — Percona](https://www.percona.com/blog/mongodb-sspl-license/)
- ADR-002 — Qdrant Vector Database
- ADR-003 — RabbitMQ Messaging
- System Architecture Document — Section 5: Data Infrastructure

---

*Tài liệu này tuân theo template ADR của Michael Nygard (2011), điều chỉnh theo tiêu chuẩn IBM Architecture Framework.*

**© 2026 Bazan AI Project — Confidential**

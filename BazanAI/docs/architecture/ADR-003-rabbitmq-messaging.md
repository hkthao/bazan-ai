# ADR-003 — Messaging: RabbitMQ với MassTransit

| Trường | Nội dung |
|--------|---------|
| **ID** | ADR-003 |
| **Tiêu đề** | Lựa chọn RabbitMQ + MassTransit làm Event Bus cho Bazan AI |
| **Trạng thái** | Accepted |
| **Ngày tạo** | 2026-03-19 |
| **Ngày cập nhật** | 2026-03-19 |
| **Tác giả** | Principal Software Architect |
| **Người review** | Backend Lead, DevOps Lead |
| **Liên quan** | ADR-001 (MongoDB), ADR-004 (Semantic Kernel), ADR-005 (YARP), System Architecture Document |

---

## 1. Bối cảnh

Bazan AI gồm 10+ microservices cần giao tiếp với nhau theo nhiều pattern:

### 1.1 Các use case giao tiếp async

| Use case | Producer | Consumer(s) | Tính chất |
|---------|---------|------------|---------|
| Nông dân đăng ký | Identity Service | Notification Service | Fire-and-forget |
| Phát hiện bệnh | Agronomy Engine | Notification Service, Conversation Service | Fan-out |
| Giá cà phê thay đổi | Market Service | Notification Service | Conditional (>5% change) |
| Cảm biến IoT gửi data | IoT Ingestion | Agronomy Engine, Identity Service | High-frequency |
| Cảnh báo thời tiết | Weather Service | Agronomy Engine, Notification Service | Fan-out |
| File PDF upload | Media Service | Knowledge RAG Service | Trigger indexing |
| Farmer location updated | Identity Service | Weather Service | Cascading |

### 1.2 Yêu cầu messaging

| Yêu cầu | Mức độ | Chi tiết |
|---------|--------|---------|
| **Durability** | Must-have | Message không mất khi service restart |
| **At-least-once delivery** | Must-have | Chấp nhận duplicate, không chấp nhận mất |
| **Dead Letter Queue** | Must-have | Message lỗi không block queue chính |
| **Retry với backoff** | Must-have | 3 lần, exponential backoff |
| **Fan-out routing** | Must-have | 1 event → nhiều consumer |
| **Topic-based routing** | Must-have | Consumer chỉ nhận event quan tâm |
| **Management UI** | Should-have | Debug queue, xem message |
| **Throughput** | Should-have | 1,000 IoT messages/giây (Phase 3) |
| **Message ordering** | Nice-to-have | Không critical cho Bazan AI |
| **Replay message** | Nice-to-have | Chỉ cần cho analytics, không ops |

### 1.3 Ràng buộc

- Team .NET — thư viện client phải có C# support tốt
- Self-hosted trên Docker Compose
- Không cần Kafka-level throughput (triệu message/giây)
- Team nhỏ — cần management UI để debug mà không cần CLI

---

## 2. Các phương án đã xem xét

### Phương án A — RabbitMQ + MassTransit

**Mô tả:** RabbitMQ là message broker AMQP mature, MassTransit là .NET abstraction layer cung cấp retry, DLQ, saga, consumer pattern thống nhất.

**Ưu điểm:**
- **MassTransit abstraction:** Consumer, publisher viết bằng C# thuần — không cần biết chi tiết AMQP exchange/queue/binding
- **Built-in retry + DLQ:** Cấu hình exponential backoff, dead letter queue bằng vài dòng C#
- **Topic Exchange:** 1 event → nhiều queue theo routing key pattern (`bazan.agronomy.#`)
- **Management UI:** Web UI port 15672, xem queue depth, rate, message, binding
- **Persistent messages:** Survives broker restart — durability đảm bảo
- **Mature & battle-tested:** 15+ năm production usage
- **Docker image nhẹ:** `rabbitmq:3.13-management-alpine` ~80MB
- **Open source:** Mozilla Public License

**Nhược điểm:**
- Không replay message cũ (Kafka có, RabbitMQ không)
- Message bị xóa sau khi consumer ACK — không có log stream
- Scale horizontal phức tạp (Shovel/Federation thay vì native cluster đơn giản)

---

### Phương án B — Apache Kafka

**Mô tả:** Distributed event streaming platform, lưu message như log bất biến, hỗ trợ replay.

**Ưu điểm:**
- Throughput cực cao (triệu message/giây)
- Message replay — consumer có thể đọc lại từ đầu
- Distributed log — audit trail tự nhiên
- Kafka Streams cho real-time processing

**Nhược điểm:**
- **Over-engineered cho Bazan AI Phase 1–3:** 1,000 IoT msg/giây << Kafka minimum viable use case
- **Operational complexity cao:** ZooKeeper (hoặc KRaft), partition management, consumer group rebalancing
- **Tốn tài nguyên:** Minimum 3 broker nodes cho production HA, mỗi node cần 4GB+ RAM
- **.NET client (Confluent):** Ít tích hợp với ASP.NET Core middleware pipeline hơn MassTransit
- **Delay message không native:** RabbitMQ hỗ trợ delayed exchange, Kafka cần workaround

**Không chọn vì:** Complexity và resource requirement vượt xa nhu cầu của Phase 1–3. Replay message là nice-to-have, không phải must-have.

---

### Phương án C — Azure Service Bus

**Mô tả:** Managed message broker của Azure, AMQP compatible.

**Ưu điểm:**
- Fully managed — không ops
- Dead letter queue native
- Session-based ordering
- MassTransit hỗ trợ Azure Service Bus transport

**Nhược điểm:**
- **Vendor lock-in:** Phụ thuộc Azure
- **Chi phí:** Standard tier ~$10/tháng + theo message count — khó dự đoán
- **Network latency:** Thêm một hop ra internet cho mỗi internal message
- **Development workflow:** Local dev cần Azure emulator hoặc kết nối thật — phức tạp hơn RabbitMQ local

**Không chọn vì:** Vendor lock-in và latency không phù hợp với internal microservices communication.

---

### Phương án D — Redis Pub/Sub / Redis Streams

**Mô tả:** Dùng Redis (đã có trong stack cho cache) làm message broker.

**Ưu điểm:**
- Tận dụng Redis đã có, không thêm infrastructure
- Redis Streams hỗ trợ consumer group, at-least-once delivery
- Latency rất thấp (in-memory)

**Nhược điểm:**
- **Redis Pub/Sub:** Fire-and-forget, message mất nếu consumer offline — không có durability
- **Redis Streams:** Tốt hơn nhưng không có topic routing linh hoạt, không có DLQ mature
- **Dùng Redis cho quá nhiều việc:** Cache + rate limit + session + messaging → single point of failure cho quá nhiều concern
- **Management UI kém:** Không có message browser, khó debug

**Không chọn vì:** Durability của Pub/Sub không đủ. Streams phức tạp hơn RabbitMQ cho fan-out routing. Separation of concerns quan trọng.

---

## 3. Quyết định

**Chọn Phương án A — RabbitMQ 3.13 + MassTransit 8.x** làm Event Bus cho Bazan AI.

**Virtual Host:** `bazan`
**Exchange chính:** `bazan.events` (type: `topic`)
**Exchange commands:** `bazan.commands` (type: `direct`)

---

## 4. Lý do

### 4.1 MassTransit làm cho RabbitMQ "invisible"

Developer không cần biết AMQP exchange, binding, routing key. Chỉ cần định nghĩa event và consumer:

```csharp
// Publisher — Identity Service
public class IdentityEventPublisher(IBus bus) : IEventPublisher
{
    public async Task PublishAsync<T>(T domainEvent, CancellationToken ct)
        where T : class, IDomainEvent
        => await bus.Publish(domainEvent, ct);
        // MassTransit tự tạo exchange, routing key, persistent message
}

// Consumer — Weather Service
public class UserLocationUpdatedConsumer(IWeatherService weatherService)
    : IConsumer<UserLocationUpdatedEvent>
{
    public async Task Consume(ConsumeContext<UserLocationUpdatedEvent> context)
    {
        await weatherService.UpdateFarmForecastZone(
            farmId:   context.Message.FarmId,
            lat:      context.Message.NewCoordinates.Latitude,
            lng:      context.Message.NewCoordinates.Longitude,
            altitude: context.Message.AltitudeM
        );
    }
}
```

Toàn bộ retry, DLQ, serialization, correlation ID được MassTransit xử lý tự động.

### 4.2 Retry + Dead Letter Queue không cần code thêm

```csharp
// Program.cs — Weather Service
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserLocationUpdatedConsumer>();
    x.AddConsumer<WeatherAlertTriggeredConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/bazan", h =>
        {
            h.Username(config["RabbitMQ:User"]);
            h.Password(config["RabbitMQ:Pass"]);
        });

        cfg.ReceiveEndpoint("bazan.weather.events", ep =>
        {
            // Retry 3 lần với exponential backoff
            ep.UseMessageRetry(r => r.Exponential(
                retryLimit:     3,
                minInterval:    TimeSpan.FromSeconds(1),
                maxInterval:    TimeSpan.FromSeconds(30),
                intervalDelta:  TimeSpan.FromSeconds(5)
            ));

            // Dead Letter Queue tự động sau 3 lần retry fail
            ep.UseDelayedRedelivery(r => r.Intervals(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromHours(2)
            ));

            ep.ConfigureConsumer<UserLocationUpdatedConsumer>(ctx);
            ep.ConfigureConsumer<WeatherAlertTriggeredConsumer>(ctx);
        });
    });
});
```

### 4.3 Topic Exchange cho Fan-out linh hoạt

```
Exchange: bazan.events (type: topic)

Routing key patterns:
  bazan.farmer.*          → Identity events
  bazan.agronomy.*        → Agronomy events
  bazan.market.*          → Market events
  bazan.iot.#             → IoT events (bất kỳ sub-topic nào)
  bazan.weather.*         → Weather events
  bazan.knowledge.*       → Knowledge events

Queue bindings:
  notification-dispatch   → bazan.agronomy.disease.#
                          → bazan.market.price.#
                          → bazan.weather.alert.#
                          → bazan.agronomy.irrigation.#

  agronomy-events         → bazan.weather.#
                          → bazan.iot.sensor.#
                          → bazan.farmer.location.#

  identity-events         → bazan.iot.sensor.#
                          (để update lastSensorReadings trong UserContext)
```

### 4.4 RabbitMQ Definitions — Pre-configured

```json
// infra/rabbitmq/definitions.json
{
  "vhosts": [{ "name": "bazan" }],
  "exchanges": [
    {
      "name":        "bazan.events",
      "vhost":       "bazan",
      "type":        "topic",
      "durable":     true,
      "auto_delete": false
    },
    {
      "name":        "bazan.commands",
      "vhost":       "bazan",
      "type":        "direct",
      "durable":     true
    },
    {
      "name":        "bazan.deadletter",
      "vhost":       "bazan",
      "type":        "fanout",
      "durable":     true
    }
  ],
  "queues": [
    {
      "name":    "bazan.farmer.events",
      "vhost":   "bazan",
      "durable": true,
      "arguments": {
        "x-dead-letter-exchange": "bazan.deadletter",
        "x-message-ttl": 86400000
      }
    },
    {
      "name":    "bazan.agronomy.events",
      "vhost":   "bazan",
      "durable": true,
      "arguments": { "x-dead-letter-exchange": "bazan.deadletter" }
    },
    {
      "name":    "bazan.notification.dispatch",
      "vhost":   "bazan",
      "durable": true,
      "arguments": {
        "x-dead-letter-exchange": "bazan.deadletter",
        "x-max-priority": 5
      }
    },
    {
      "name":    "bazan.iot.ingestion",
      "vhost":   "bazan",
      "durable": true,
      "arguments": {
        "x-dead-letter-exchange": "bazan.deadletter",
        "x-max-length": 100000,
        "x-overflow": "drop-head"
      }
    },
    {
      "name":    "bazan.deadletter",
      "vhost":   "bazan",
      "durable": true
    }
  ],
  "bindings": [
    { "source": "bazan.events", "vhost": "bazan",
      "destination": "bazan.farmer.events",
      "routing_key": "bazan.farmer.#" },
    { "source": "bazan.events", "vhost": "bazan",
      "destination": "bazan.agronomy.events",
      "routing_key": "bazan.agronomy.#" },
    { "source": "bazan.events", "vhost": "bazan",
      "destination": "bazan.notification.dispatch",
      "routing_key": "bazan.agronomy.disease.#" },
    { "source": "bazan.events", "vhost": "bazan",
      "destination": "bazan.notification.dispatch",
      "routing_key": "bazan.market.price.#" },
    { "source": "bazan.events", "vhost": "bazan",
      "destination": "bazan.notification.dispatch",
      "routing_key": "bazan.weather.alert.#" },
    { "source": "bazan.events", "vhost": "bazan",
      "destination": "bazan.iot.ingestion",
      "routing_key": "bazan.iot.#" }
  ]
}
```

### 4.5 IoT Queue — Bounded với Drop Policy

IoT sensor data là high-frequency nhưng có thể mất một số reading mà không ảnh hưởng nghiêm trọng (lấy mẫu tiếp theo vẫn đến). Queue được giới hạn 100,000 messages, overflow drop-head — tránh memory bloat khi consumer chậm.

```
x-max-length: 100000
x-overflow: drop-head   ← Xóa message cũ nhất khi đầy, không block producer
```

---

## 5. Thiết kế triển khai

### 5.1 Docker Compose

```yaml
rabbitmq:
  image: rabbitmq:3.13-management-alpine
  container_name: bazan-rabbitmq
  environment:
    RABBITMQ_DEFAULT_USER:  ${RABBITMQ_USER}
    RABBITMQ_DEFAULT_PASS:  ${RABBITMQ_PASS}
    RABBITMQ_DEFAULT_VHOST: bazan
  volumes:
    - rabbitmq-data:/var/lib/rabbitmq
    - ./infra/rabbitmq/definitions.json:/etc/rabbitmq/definitions.json:ro
    - ./infra/rabbitmq/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro
  ports:
    - "5672:5672"    # AMQP
    - "15672:15672"  # Management UI
  networks:
    - bazan-network
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
    interval: 10s
    timeout: 10s
    retries: 5
```

```ini
# infra/rabbitmq/rabbitmq.conf
management.load_definitions = /etc/rabbitmq/definitions.json
loopback_users.guest = false
log.console.level = warning
heartbeat = 60
```

### 5.2 Shared Consumer Base

```csharp
// SharedKernel/Messaging/RabbitMqConsumerBase.cs
public abstract class BazanConsumerBase<TMessage>(
    ILogger logger,
    IMetrics metrics
) : IConsumer<TMessage> where TMessage : class, IDomainEvent
{
    public async Task Consume(ConsumeContext<TMessage> context)
    {
        var eventType = typeof(TMessage).Name;
        var correlationId = context.CorrelationId?.ToString()
                         ?? context.Message.CorrelationId;

        using var _ = logger.BeginScope(new Dictionary<string, object> {
            ["CorrelationId"] = correlationId,
            ["EventType"]     = eventType,
            ["EventId"]       = context.Message.EventId
        });

        var sw = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("Processing {EventType}", eventType);
            await ConsumeInternal(context);
            metrics.Increment($"events.consumed.{eventType}.success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process {EventType}", eventType);
            metrics.Increment($"events.consumed.{eventType}.error");
            throw; // MassTransit sẽ retry
        }
        finally
        {
            metrics.Histogram(
                $"events.processing_ms.{eventType}",
                sw.ElapsedMilliseconds
            );
        }
    }

    protected abstract Task ConsumeInternal(ConsumeContext<TMessage> context);
}
```

### 5.3 Monitoring

**Prometheus metrics quan trọng:**

| Metric | Alert threshold | Mức độ |
|--------|----------------|--------|
| `rabbitmq_queue_messages` (notification.dispatch) | > 10,000 | Warning |
| `rabbitmq_queue_messages` (deadletter) | > 100 | Warning |
| `rabbitmq_connections` | > 200 | Warning |
| `rabbitmq_consumers` (bất kỳ queue) | == 0 | Critical |
| `app_masstransit_consume_duration_p99_ms` | > 5,000ms | Warning |
| `rabbitmq_node_mem_used` | > 80% mem_limit | Critical |

---

## 6. Hậu quả & Trade-offs

### 6.1 Tích cực

- **Developer experience tốt:** MassTransit ẩn AMQP complexity, consumer viết như regular C# class
- **Reliability sẵn có:** Retry, DLQ, correlation ID không cần viết code
- **Observability:** Management UI + Prometheus metrics cho full visibility
- **Flexibility:** Thêm consumer mới chỉ cần đăng ký binding — không thay đổi producer

### 6.2 Tiêu cực & Cách giảm thiểu

| Trade-off | Mức độ | Cách giảm thiểu |
|-----------|--------|----------------|
| Không replay message | Thấp | Dùng Change Streams (MongoDB) cho audit. Event sourcing chỉ khi cần. |
| Single broker — SPOF | Trung bình | Docker volume backup + restart policy. Phase 4: RabbitMQ cluster. |
| Message size limit (128MB default) | Thấp | Chỉ truyền IDs + metadata, không truyền payload lớn. File qua MinIO. |
| At-least-once → idempotent consumer | Trung bình | Consumer phải idempotent (check đã xử lý event chưa bằng EventId). |

### 6.3 Idempotency Pattern

```csharp
// Mỗi consumer critical phải check idempotency
public class DiseaseAlertConsumer(
    INotificationService notifications,
    IIdempotencyStore idempotency
) : BazanConsumerBase<DiseaseAlertGeneratedEvent>
{
    protected override async Task ConsumeInternal(
        ConsumeContext<DiseaseAlertGeneratedEvent> context)
    {
        var eventId = context.Message.EventId;

        // Đã xử lý event này rồi? (Redis SET NX)
        if (await idempotency.HasProcessedAsync(eventId))
        {
            logger.LogInformation("Duplicate event {EventId}, skipping", eventId);
            return;
        }

        await notifications.SendDiseaseAlertAsync(context.Message);
        await idempotency.MarkProcessedAsync(eventId, ttl: TimeSpan.FromDays(7));
    }
}
```

---

## 7. Tiêu chí đánh giá lại

- Dead letter queue > 1,000 messages liên tục → Điều tra consumer errors, không phải đổi broker
- IoT throughput vượt 10,000 messages/giây thường xuyên → Đánh giá Kafka cho IoT pipeline riêng, giữ RabbitMQ cho business events
- Cần message replay cho audit/compliance → Thêm Event Store riêng, không nhất thiết đổi broker
- RabbitMQ cluster cần thiết → Upgrade lên RabbitMQ Quorum Queue + 3-node cluster

---

## 8. Tham khảo

- [RabbitMQ Documentation](https://www.rabbitmq.com/docs)
- [MassTransit Documentation](https://masstransit.io/documentation)
- [RabbitMQ vs Kafka — Thoughtworks](https://www.thoughtworks.com/insights/blog/microservices/scaling-microservices-rabbitmq-kafka)
- [Topic Exchanges — RabbitMQ](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet)
- ADR-001 — MongoDB Primary Database
- ADR-004 — Semantic Kernel Orchestration
- System Architecture Document — Section 7: Messaging & Event Contracts

---

*Tài liệu theo template ADR của Michael Nygard (2011).*

**© 2026 Bazan AI Project — Confidential**

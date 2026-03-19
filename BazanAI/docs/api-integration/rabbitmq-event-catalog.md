# RabbitMQ Event Catalog — Bazan AI
## Toàn bộ Event Contracts trên Message Bus

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Liên quan** | ADR-003 (RabbitMQ), System Architecture Document |

---

## 1. Topology

```
Exchange: bazan.events   (type: topic,  durable: true)
Exchange: bazan.commands (type: direct, durable: true)
Exchange: bazan.deadletter (type: fanout, durable: true)

Routing key format: bazan.{domain}.{entity}.{action}
```

---

## 2. Base Event Schema

```json
{
  "eventId":       "uuid-v4",
  "eventType":     "bazan.farmer.location.updated",
  "version":       "1.0",
  "source":        "identity-service",
  "correlationId": "req-uuid-v4",
  "causationId":   "parent-event-id | null",
  "occurredAt":    "2026-03-19T07:30:00.000Z",
  "schemaVersion": "1.0",
  "payload":       {}
}
```

---

## 3. Identity Domain Events

### `bazan.farmer.registered`
**Source:** identity-service | **Consumers:** notification-svc, agronomy-svc

```json
{
  "eventType": "bazan.farmer.registered",
  "payload": {
    "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
    "fullName": "Nguyễn Văn An",
    "phoneNumber": "+84901234567",
    "province": "Đắk Lắk",
    "district": "Cư M'gar",
    "registeredAt": "2026-03-19T07:00:00Z"
  }
}
```

---

### `bazan.farmer.location.updated`
**Source:** identity-service | **Consumers:** weather-svc, agronomy-svc

```json
{
  "eventType": "bazan.farmer.location.updated",
  "payload": {
    "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
    "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
    "previousCoordinates": { "lat": 12.6700, "lng": 108.0385 },
    "newCoordinates": { "lat": 12.6702, "lng": 108.0388 },
    "altitude_m": 650,
    "updatedBy": "farmer_self"
  }
}
```

---

### `bazan.farmer.farm.added`
**Source:** identity-service | **Consumers:** agronomy-svc, weather-svc, scheduler-svc

```json
{
  "eventType": "bazan.farmer.farm.added",
  "payload": {
    "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
    "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
    "coffeeVarieties": ["Robusta", "TR4"],
    "soilType": "basalt",
    "totalArea_ha": 1.5,
    "coordinates": { "lat": 12.6702, "lng": 108.0388 },
    "altitude_m": 650
  }
}
```

---

## 4. Agronomy Domain Events

### `bazan.agronomy.disease.alert.generated`
**Source:** agronomy-svc | **Consumers:** notification-svc, conversation-svc

```json
{
  "eventType": "bazan.agronomy.disease.alert.generated",
  "payload": {
    "alertId": "alert-uuid",
    "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
    "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
    "disease": {
      "code": "leaf_rust",
      "name": "Bệnh gỉ sắt",
      "severity": "high",
      "confidence": 0.87
    },
    "affectedArea_pct": 30,
    "detectionMethod": "image_cv",
    "recommendedActions": [
      { "priority": 1, "action": "Phun Carbendazim 50SC 20ml/10L", "within_hours": 48 }
    ],
    "sourceMessageId": "msg-001",
    "generatedAt": "2026-03-19T07:32:00Z"
  }
}
```

---

### `bazan.agronomy.irrigation.reminder.created`
**Source:** agronomy-svc | **Consumers:** notification-svc

```json
{
  "eventType": "bazan.agronomy.irrigation.reminder.created",
  "payload": {
    "reminderId": "reminder-uuid",
    "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
    "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
    "trigger": "soil_moisture_low",
    "currentMoisture_pct": 28,
    "threshold_pct": 30,
    "recommendedDate": "2026-03-20",
    "waterVolume_m3": 562
  }
}
```

---

### `bazan.agronomy.harvest.window.opened`
**Source:** agronomy-svc | **Consumers:** notification-svc, market-svc

```json
{
  "eventType": "bazan.agronomy.harvest.window.opened",
  "payload": {
    "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
    "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
    "windowStart": "2026-10-15",
    "windowEnd": "2026-11-15",
    "estimatedYield_kg": 4500,
    "maturityScore": 0.82,
    "recommendation": "Thu hoạch trong 2 tuần tới để đạt chất lượng tốt nhất"
  }
}
```

---

## 5. Market Domain Events

### `bazan.market.price.updated`
**Source:** market-svc | **Consumers:** notification-svc, agronomy-svc

```json
{
  "eventType": "bazan.market.price.updated",
  "payload": {
    "priceId": "price-uuid",
    "coffeeType": "Robusta",
    "grade": "Grade 2, 5% black & broken",
    "price_vnd_per_kg": 68500,
    "price_usd_per_mt": 2680,
    "change_pct": 5.2,
    "changeDirection": "up",
    "source": "ICE_Futures",
    "marketRegion": "Vietnam_Central_Highlands",
    "recordedAt": "2026-03-19T09:00:00Z",
    "shouldAlert": true
  }
}
```

---

### `bazan.market.price.alert.triggered`
**Source:** market-svc | **Consumers:** notification-svc

```json
{
  "eventType": "bazan.market.price.alert.triggered",
  "payload": {
    "coffeeType": "Robusta",
    "price_vnd_per_kg": 68500,
    "change_pct": 5.2,
    "changeDirection": "up",
    "affectedFarmerIds": ["64f1a2b3c4d5e6f7a8b9c0d1"],
    "triggerReason": "price_change_above_threshold"
  }
}
```

---

## 6. IoT Domain Events

### `bazan.iot.sensor.data.received`
**Source:** iot-ingestion-svc | **Consumers:** agronomy-svc, identity-svc

```json
{
  "eventType": "bazan.iot.sensor.data.received",
  "payload": {
    "deviceId": "SM_001_AN",
    "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
    "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
    "readings": {
      "soilMoisture_pct": 38.5,
      "soilTemp_c": 26.8,
      "airTemp_c": 31.2,
      "soilPH": 6.1
    },
    "batteryLevel_pct": 78,
    "recordedAt": "2026-03-19T08:00:00Z"
  }
}
```

---

### `bazan.iot.device.offline.detected`
**Source:** iot-ingestion-svc | **Consumers:** notification-svc (admin alert)

```json
{
  "eventType": "bazan.iot.device.offline.detected",
  "payload": {
    "deviceId": "SM_001_AN",
    "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
    "lastHeartbeat": "2026-03-19T06:00:00Z",
    "offlineDuration_hours": 2.3
  }
}
```

---

## 7. Weather Domain Events

### `bazan.weather.alert.triggered`
**Source:** weather-svc | **Consumers:** agronomy-svc, notification-svc

```json
{
  "eventType": "bazan.weather.alert.triggered",
  "payload": {
    "alertId": "weather-alert-uuid",
    "alertType": "dry_spell",
    "severity": "medium",
    "affectedFarms": ["65a2b3c4d5e6f7a8b9c0d1e2"],
    "affectedRegion": { "province": "Đắk Lắk", "district": "Cư M'gar" },
    "forecast": {
      "nextRainfall_days": 12,
      "expectedRainfall_mm": 8,
      "avgTemp_c": 31.5
    },
    "agronomicImpact": "Nguy cơ thiếu nước cao, khuyến nghị tưới bổ sung",
    "triggeredAt": "2026-03-19T06:00:00Z"
  }
}
```

---

## 8. Knowledge Domain Events

### `bazan.knowledge.variety.indexed`
**Source:** knowledge-rag | **Consumers:** agronomy-svc

```json
{
  "eventType": "bazan.knowledge.variety.indexed",
  "payload": {
    "varietyCode": "TR4",
    "varietyName": "Thiên Rang 4",
    "documentIds": ["qdrant-doc-id-1"],
    "sourceDocuments": ["wcr-tr4-2024.pdf"],
    "indexedAt": "2026-03-19T06:00:00Z"
  }
}
```

---

## 9. Conversation Domain Events

### `bazan.conversation.feedback.collected`
**Source:** conversation-svc | **Consumers:** scheduler-svc (RLHF export trigger)

```json
{
  "eventType": "bazan.conversation.feedback.collected",
  "payload": {
    "messageId": "msg-002",
    "sessionId": "sess_20260319_AN_001",
    "farmerId": "64f1a2b3c4d5e6f7a8b9c0d1",
    "rating": 5,
    "thumbs": "up",
    "usedForTraining": false
  }
}
```

---

## 10. Queue Bindings Summary

| Queue | Subscribes to routing keys | DLQ | Max Length |
|-------|---------------------------|-----|-----------|
| `bazan.farmer.events` | `bazan.farmer.#` | Yes | — |
| `bazan.agronomy.events` | `bazan.agronomy.#`, `bazan.weather.#`, `bazan.iot.sensor.#`, `bazan.farmer.location.#` | Yes | — |
| `bazan.market.events` | `bazan.market.#` | Yes | — |
| `bazan.notification.dispatch` | `bazan.agronomy.disease.#`, `bazan.market.price.alert.#`, `bazan.weather.alert.#`, `bazan.agronomy.irrigation.#`, `bazan.agronomy.harvest.#` | Yes | — |
| `bazan.iot.ingestion` | `bazan.iot.#` | Yes | 100,000 (drop-head) |
| `bazan.weather.events` | `bazan.weather.#`, `bazan.farmer.location.#` | Yes | — |
| `bazan.knowledge.events` | `bazan.knowledge.#` | Yes | — |
| `bazan.conversation.events` | `bazan.conversation.#` | Yes | — |
| `bazan.deadletter` | All failed messages | — | — |

---

**© 2026 Bazan AI Project — Confidential**

# MQTT Topic Schema — IoT Sensors
## Bazan AI — Giao thức Cảm biến IoT

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Protocol** | MQTT v5.0 (backward compat v3.1.1) |
| **Broker** | IoT Ingestion Service (port 1883 / TLS 8883) |
| **Ngày tạo** | 2026-03-19 |
| **Liên quan** | System Architecture, Sequence Diagrams, ADR-003 (RabbitMQ) |

---

## 1. Topic Hierarchy

```
bazan/
├── {farmId}/
│   ├── {sensorType}/
│   │   └── {deviceId}          ← Data topic (sensor publish here)
│   └── cmd/
│       └── {deviceId}          ← Command topic (server → device)
└── sys/
    ├── heartbeat/{deviceId}    ← Device health
    └── ota/{deviceId}          ← Firmware update
```

**Naming conventions:**
- `{farmId}`: MongoDB ObjectId của farm (24-char hex)
- `{sensorType}`: snake_case (xem bảng dưới)
- `{deviceId}`: format `{TYPE}_{3-digit-sequence}_{FARM_CODE}`, ví dụ `SM_001_AN`

---

## 2. Sensor Types

| sensorType | Mô tả | Unit | Range |
|-----------|-------|------|-------|
| `soil_moisture` | Độ ẩm đất | % | 0–100 |
| `soil_temperature` | Nhiệt độ đất | °C | -10–60 |
| `air_temperature` | Nhiệt độ không khí | °C | -10–60 |
| `soil_ph` | pH đất | pH | 3.0–9.0 |
| `rainfall` | Lượng mưa | mm | 0–500 |
| `lux` | Cường độ ánh sáng | lux | 0–100000 |
| `humidity` | Độ ẩm không khí | % | 0–100 |
| `wind_speed` | Tốc độ gió | m/s | 0–50 |
| `co2` | Nồng độ CO₂ | ppm | 300–5000 |

---

## 3. Data Topics — Sensor → Broker

### 3.1 Topic Pattern

```
bazan/{farmId}/{sensorType}/{deviceId}
```

**Ví dụ:**
```
bazan/65a2b3c4d5e6f7a8b9c0d1e2/soil_moisture/SM_001_AN
bazan/65a2b3c4d5e6f7a8b9c0d1e2/air_temperature/AT_001_AN
bazan/65a2b3c4d5e6f7a8b9c0d1e2/rainfall/RG_001_AN
```

### 3.2 Payload Schema (JSON)

```json
{
  "deviceId": "SM_001_AN",
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "sensorType": "soil_moisture",
  "value": 65.3,
  "unit": "%",
  "quality": "good",
  "battery_pct": 78,
  "rssi": -67,
  "timestamp": "2026-03-19T08:00:00Z",
  "firmware": "1.2.4"
}
```

**Fields:**

| Field | Type | Required | Description |
|-------|------|---------|-------------|
| `deviceId` | string | Yes | ID thiết bị |
| `farmId` | string | Yes | ID vườn |
| `sensorType` | string | Yes | Loại cảm biến |
| `value` | number | Yes | Giá trị đọc được |
| `unit` | string | Yes | Đơn vị |
| `quality` | enum | Yes | `good \| degraded \| error` |
| `battery_pct` | integer | Yes | Pin còn lại % |
| `rssi` | integer | Yes | Cường độ tín hiệu (dBm) |
| `timestamp` | ISO 8601 | Yes | Thời gian đọc tại sensor |
| `firmware` | string | No | Phiên bản firmware |
| `calibrationOffset` | number | No | Hiệu chỉnh calibration |

**Quality values:**
- `good`: Giá trị trong khoảng bình thường, tin cậy
- `degraded`: Giá trị có thể kém chính xác (ví dụ: pin yếu, nhiệt độ cực đoan)
- `error`: Cảm biến lỗi, giá trị không dùng được

### 3.3 Multi-sensor Payload (Gateway device)

Một thiết bị có nhiều sensor có thể gửi một lần:

```json
{
  "deviceId": "MULTI_001_AN",
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "deviceType": "multi_sensor_gateway",
  "readings": [
    { "sensorType": "soil_moisture",    "value": 65.3, "unit": "%" },
    { "sensorType": "soil_temperature", "value": 24.1, "unit": "°C" },
    { "sensorType": "soil_ph",          "value": 6.1,  "unit": "pH" }
  ],
  "battery_pct": 82,
  "rssi": -62,
  "timestamp": "2026-03-19T08:00:00Z"
}
```

**Topic:** `bazan/{farmId}/multi/{deviceId}`

---

## 4. Heartbeat Topics

### 4.1 Device → Broker (heartbeat)

**Topic:** `bazan/sys/heartbeat/{deviceId}`

**Payload:**
```json
{
  "deviceId": "SM_001_AN",
  "farmId": "65a2b3c4d5e6f7a8b9c0d1e2",
  "status": "online",
  "battery_pct": 78,
  "rssi": -67,
  "uptime_sec": 86400,
  "firmware": "1.2.4",
  "timestamp": "2026-03-19T08:00:00Z"
}
```

**Tần suất:** Mỗi 5 phút
**QoS:** 1 (at-least-once)
**Retain:** true — Server biết trạng thái cuối cùng khi subscribe

---

## 5. Command Topics — Broker → Device

### 5.1 Topic Pattern

```
bazan/{farmId}/cmd/{deviceId}
```

### 5.2 Command Payload

```json
{
  "commandId": "cmd-uuid-v4",
  "command": "set_interval",
  "params": {
    "intervalMinutes": 30
  },
  "issuedAt": "2026-03-19T08:00:00Z",
  "expiresAt": "2026-03-19T09:00:00Z"
}
```

**Supported commands:**

| command | params | Mô tả |
|---------|--------|-------|
| `set_interval` | `intervalMinutes: int` | Đổi tần suất gửi data |
| `reboot` | — | Khởi động lại thiết bị |
| `calibrate` | `offset: float` | Hiệu chỉnh giá trị cảm biến |
| `firmware_update` | `url: string, version: string` | Trigger OTA update |
| `get_config` | — | Yêu cầu device báo cáo config |

### 5.3 Command Response

Device gửi ACK về topic: `bazan/{farmId}/cmd_ack/{deviceId}`

```json
{
  "commandId": "cmd-uuid-v4",
  "status": "executed",
  "executedAt": "2026-03-19T08:00:05Z",
  "result": { "intervalMinutes": 30 }
}
```

---

## 6. QoS & Retention Policy

| Topic | QoS | Retain | Lý do |
|-------|-----|--------|-------|
| Data (`/{sensorType}/`) | 1 | false | At-least-once đủ, không cần retain |
| Heartbeat (`/sys/heartbeat/`) | 1 | **true** | Server cần biết trạng thái cuối |
| Command (`/cmd/`) | 1 | false | Device online mới nhận |
| Command ACK (`/cmd_ack/`) | 1 | false | Fire-and-forget |

---

## 7. Authentication

### 7.1 MQTT Username/Password

```
Username: {deviceId}
Password: {HMAC-SHA256(farmId + ":" + deviceId + ":" + sharedSecret)}
```

Server validate trong `MqttBrokerService.OnClientConnecting()`.

### 7.2 Client ID Format

```
BAZAN_{deviceId}_{timestamp_unix}
```

Ví dụ: `BAZAN_SM_001_AN_1710835200`

---

## 8. Thresholds & Alert Triggers

IoT Ingestion Service tự động publish alert lên RabbitMQ khi:

| Sensor | Threshold | Alert Event |
|--------|-----------|-------------|
| `soil_moisture` | < 30% | `IrrigationReminderCreated` |
| `soil_moisture` | > 90% | `DrainageWarningCreated` |
| `soil_ph` | < 5.5 hoặc > 7.0 | `SoilPhAlertCreated` |
| `air_temperature` | > 38°C | `HeatStressAlertCreated` |
| `air_temperature` | < 15°C | `FrostRiskAlertCreated` |
| `battery_pct` | < 20% | `DeviceBatteryLowAlert` |
| Không có heartbeat > 2h | — | `DeviceOfflineAlert` |

---

## 9. Payload Size Limits

| Loại | Giới hạn | Lý do |
|------|---------|-------|
| Single sensor payload | < 512 bytes | MQTT efficient |
| Multi-sensor payload | < 2 KB | Tránh fragmentation |
| Command payload | < 1 KB | |
| Topic length | < 128 chars | Broker limit |

---

**© 2026 Bazan AI Project — Confidential**

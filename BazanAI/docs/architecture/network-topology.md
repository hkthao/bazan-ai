# Network Topology Diagram — Bazan AI
## Thiết kế mạng & Phân vùng bảo mật

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Principal Software Architect · DevOps Lead |
| **Môi trường** | Phase 1–2: Single Server (Docker Compose) |
| **Liên quan** | ADR-005 (YARP), Security Architecture Document |

---

## 1. Tổng quan phân vùng mạng

```
INTERNET
    │
    │ HTTPS/443, WSS/443
    ▼
┌─────────────────────────────────────────────────────┐
│                  DMZ / Public Zone                   │
│                                                      │
│   ┌──────────────────────────────────────────┐       │
│   │           Nginx Reverse Proxy             │       │
│   │   (SSL Termination, DDoS basic filter)    │       │
│   │   Port 80 → redirect 443                  │       │
│   │   Port 443 → proxy_pass to API Gateway    │       │
│   └──────────────────┬───────────────────────┘       │
│                      │ HTTP/8080 (internal)           │
└──────────────────────┼──────────────────────────────┘
                       │
┌──────────────────────┼──────────────────────────────┐
│   APPLICATION ZONE   │   (Docker network: bazan-net) │
│                      ▼                               │
│   ┌─────────────────────────────────────────────┐    │
│   │          API Gateway (YARP)                  │    │
│   │          Port 8080 (internal)                │    │
│   │   JWT Validation · Rate Limiting · Routing   │    │
│   └──┬──────────┬──────────┬──────────┬─────────┘    │
│      │          │          │          │               │
│      ▼          ▼          ▼          ▼               │
│  ┌───────┐ ┌────────┐ ┌────────┐ ┌────────┐          │
│  │5001   │ │5002    │ │5004    │ │5005    │          │
│  │Identity│ │Conver- │ │Agronomy│ │Market  │          │
│  │Service│ │sation  │ │Engine  │ │Service │          │
│  └───────┘ └────────┘ └────────┘ └────────┘          │
│                                                       │
│  ┌───────┐ ┌────────┐ ┌────────┐ ┌────────┐          │
│  │5010   │ │5003    │ │5006    │ │5007    │          │
│  │AI     │ │RAG     │ │Notif   │ │IoT     │          │
│  │Orchest│ │Service │ │Service │ │Ingest  │          │
│  └───────┘ └────────┘ └────────┘ └────────┘          │
│                                                       │
│  ┌───────┐ ┌────────┐ ┌────────┐                     │
│  │5008   │ │5009    │ │5011    │                     │
│  │Weather│ │Media   │ │Schedul │                     │
│  │Service│ │Service │ │er      │                     │
│  └───────┘ └────────┘ └────────┘                     │
└─────────────────────────────────────────────────────┘
                       │
┌──────────────────────┼──────────────────────────────┐
│   DATA ZONE          │   (Docker network: bazan-data)│
│   (Không expose port ra ngoài APPLICATION ZONE)      │
│                      ▼                               │
│   ┌───────┐  ┌───────┐  ┌────────┐  ┌──────────┐    │
│   │Mongo  │  │Qdrant │  │Redis   │  │RabbitMQ  │    │
│   │27017  │  │6333   │  │6379    │  │5672      │    │
│   │(RS x3)│  │6334   │  │        │  │15672 *   │    │
│   └───────┘  └───────┘  └────────┘  └──────────┘    │
│                                                       │
│   ┌────────┐  ┌────────────────────────────────┐     │
│   │MinIO   │  │ * RabbitMQ Management UI        │     │
│   │9000    │  │   chỉ expose qua SSH tunnel     │     │
│   │9001 *  │  │   không expose ra internet      │     │
│   └────────┘  └────────────────────────────────┘     │
└─────────────────────────────────────────────────────┘
                       │
┌──────────────────────┼──────────────────────────────┐
│   OBSERVABILITY ZONE │   (Docker network: bazan-obs) │
│                      ▼                               │
│   ┌────────┐  ┌────────────┐  ┌────────┐            │
│   │Seq     │  │Prometheus  │  │Grafana │            │
│   │5341 *  │  │9090 *      │  │3000 *  │            │
│   └────────┘  └────────────┘  └────────┘            │
│                                                       │
│   * Chỉ accessible qua VPN / SSH tunnel              │
└─────────────────────────────────────────────────────┘
```

---

## 2. Port Map đầy đủ

### 2.1 Exposed ra Internet (qua Nginx)

| Port | Protocol | Service | Mô tả |
|------|---------|---------|-------|
| 80 | HTTP | Nginx | Redirect → 443 |
| 443 | HTTPS | Nginx | SSL termination, proxy → Gateway:8080 |
| 443 | WSS | Nginx | WebSocket upgrade cho SignalR /hub/* |
| 1883 | MQTT | IoT Ingestion | Sensor data (cân nhắc TLS/8883 Phase 2) |

### 2.2 Internal — Application Zone (không expose)

| Port | Service | Caller |
|------|---------|--------|
| 8080 | API Gateway | Nginx |
| 5001 | Identity Service | Gateway, Orchestrator |
| 5002 | Conversation Service | Gateway, Orchestrator |
| 5003 | Knowledge RAG | Orchestrator |
| 5004 | Agronomy Engine | Gateway, Orchestrator |
| 5005 | Market Service | Gateway, Orchestrator |
| 5006 | Notification Service | Event consumers only |
| 5007 | IoT Ingestion | Sensors (MQTT), Gateway |
| 5008 | Weather Service | Scheduler, Agronomy |
| 5009 | Media Service | Gateway, Orchestrator |
| 5010 | AI Orchestrator | Gateway |
| 5011 | Scheduler | Internal cron |

### 2.3 Internal — Data Zone

| Port | Service | Caller |
|------|---------|--------|
| 27017 | MongoDB Primary | All services |
| 27018 | MongoDB Secondary 1 | Read replicas |
| 27019 | MongoDB Secondary 2 | Read replicas |
| 6333 | Qdrant HTTP | RAG Service |
| 6334 | Qdrant gRPC | RAG Service |
| 6379 | Redis | Gateway, Orchestrator, Weather |
| 5672 | RabbitMQ AMQP | All services |
| 9000 | MinIO API | Media Service, RAG Service |

### 2.4 Admin — Chỉ qua SSH Tunnel

| Port | Service | Tunnel command |
|------|---------|---------------|
| 15672 | RabbitMQ UI | `ssh -L 15672:localhost:15672 admin@server` |
| 9001 | MinIO Console | `ssh -L 9001:localhost:9001 admin@server` |
| 9090 | Prometheus | `ssh -L 9090:localhost:9090 admin@server` |
| 3000 | Grafana | `ssh -L 3000:localhost:3000 admin@server` |
| 5341 | Seq Logs | `ssh -L 5341:localhost:5341 admin@server` |

---

## 3. Docker Network Isolation

```yaml
# docker-compose.yml — network definitions
networks:
  bazan-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16

  bazan-data:
    driver: bridge
    internal: true          # ← Không có external connectivity
    ipam:
      config:
        - subnet: 172.21.0.0/16

  bazan-obs:
    driver: bridge
    internal: true
    ipam:
      config:
        - subnet: 172.22.0.0/16

# Services chỉ thuộc network cần thiết:
services:
  mongo1:
    networks:
      - bazan-data          # Chỉ data network — không thể reach từ internet
  
  identity-svc:
    networks:
      - bazan-network       # Để nhận request từ Gateway
      - bazan-data          # Để connect MongoDB, Redis
  
  prometheus:
    networks:
      - bazan-network       # Để scrape /metrics của services
      - bazan-obs           # Để grafana query
```

---

## 4. TLS & Certificate

```
Phase 1 (MVP):
  Nginx → Let's Encrypt certificate (certbot auto-renew)
  Internal services: HTTP (không TLS) — chạy trong Docker network riêng
  MongoDB: TLS disabled nhưng authentication bắt buộc (keyFile)

Phase 2:
  Internal TLS: mTLS giữa services (hoặc service mesh lite như Linkerd)
  MQTT: TLS/8883 thay vì 1883
  MongoDB: TLS enabled

Phase 4 (Kubernetes):
  Cert-manager tự động cấp certificate
  Istio service mesh cho mTLS toàn bộ internal traffic
```

---

## 5. Firewall Rules (UFW / iptables)

```bash
# Chỉ mở port cần thiết ra internet
ufw allow 22/tcp    # SSH (nên thêm IP whitelist)
ufw allow 80/tcp    # HTTP → redirect HTTPS
ufw allow 443/tcp   # HTTPS
ufw allow 1883/tcp  # MQTT (sensors)
ufw default deny incoming
ufw default allow outgoing

# Tất cả port khác chỉ accessible qua Docker internal network
# hoặc SSH tunnel
```

---

## 6. Phase 4 — Kubernetes Network Architecture (Preview)

```
Internet → Cloud Load Balancer (TCP 443)
                │
                ▼
        Ingress Controller (Nginx/Traefik)
                │
         ┌──────┴──────┐
         │             │
    API Gateway     Static Assets
    (ClusterIP)     (CDN)
         │
    Kubernetes Services (ClusterIP)
    ├── identity-svc
    ├── agronomy-svc
    ├── orchestrator-svc
    └── ...
         │
    StatefulSets (Data)
    ├── MongoDB ReplicaSet (3 pods)
    ├── Qdrant (1 pod → cluster later)
    └── Redis (1 pod → sentinel later)

NetworkPolicy:
  - Data pods: only accept from App pods
  - App pods: only accept from Gateway
  - Gateway: only accept from Ingress
```

---

*Tài liệu cập nhật khi thay đổi port, thêm service, hoặc chuyển đổi môi trường.*

**© 2026 Bazan AI Project — Confidential**

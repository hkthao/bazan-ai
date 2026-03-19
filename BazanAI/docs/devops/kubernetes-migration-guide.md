# Kubernetes Migration Guide — Bazan AI
## Phase 4: Docker Compose → Kubernetes

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | DevOps Lead · Principal Architect |
| **Target** | Phase 4 (Tháng 13–18), khi DAU > 5,000 |
| **Liên quan** | Capacity Planning, Infrastructure as Code, Network Topology |

---

## 1. Migration Rationale

| Vấn đề với Docker Compose | Giải pháp K8s |
|--------------------------|--------------|
| Manual horizontal scaling | HPA auto-scales dựa trên CPU/custom metrics |
| Single server SPOF | Multi-node cluster, pod rescheduling |
| Manual rolling update | K8s rolling deployment built-in |
| Không có self-healing | Liveness/Readiness probes + auto restart |
| Resource capping thủ công | Resource requests/limits per pod |
| Không có traffic splitting | Istio / Nginx ingress canary |

**Trigger để migrate:**
- DAU vượt 5,000 nông dân liên tục 1 tháng
- Infrastructure cost > $500/tháng (K8s ROI dương)
- Team có ít nhất 1 người kinh nghiệm K8s

---

## 2. Target Architecture (K8s)

```
Internet
    │
AWS ALB / GCP Load Balancer
    │
Nginx Ingress Controller
    ├── api.bazanai.vn → API Gateway pods (3 replicas)
    └── wss://api.bazanai.vn/hub → Orchestrator pods (SignalR sticky)

Namespaces:
  bazan-app       → Application services
  bazan-data      → StatefulSets (MongoDB, Qdrant, Redis, RabbitMQ)
  bazan-obs       → Observability (Prometheus, Grafana, Seq)
  cert-manager    → TLS automation
  istio-system    → Service mesh (mTLS)
```

---

## 3. Helm Chart Structure

```
infra/k8s/
├── bazan-ai/                  # Main Helm chart
│   ├── Chart.yaml
│   ├── values.yaml            # Default values
│   ├── values.staging.yaml    # Staging overrides
│   ├── values.production.yaml # Production overrides
│   └── templates/
│       ├── _helpers.tpl
│       ├── namespace.yaml
│       ├── services/
│       │   ├── gateway/
│       │   │   ├── deployment.yaml
│       │   │   ├── service.yaml
│       │   │   └── hpa.yaml
│       │   ├── identity/
│       │   ├── agronomy/
│       │   ├── market/
│       │   ├── orchestrator/
│       │   ├── knowledge-rag/
│       │   └── notification/
│       ├── data/
│       │   ├── mongodb-statefulset.yaml
│       │   ├── qdrant-statefulset.yaml
│       │   ├── redis-statefulset.yaml
│       │   └── rabbitmq-statefulset.yaml
│       ├── ingress.yaml
│       ├── network-policies.yaml
│       └── secrets-external.yaml   # ExternalSecrets (from Vault)
└── charts/                    # Chart dependencies
    └── mongodb-community/     # MongoDB Operator
```

---

## 4. Key Manifests

### 4.1 Deployment — Identity Service

```yaml
# templates/services/identity/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: identity-svc
  namespace: bazan-app
spec:
  replicas: {{ .Values.identity.replicas | default 2 }}
  selector:
    matchLabels:
      app: identity-svc
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0      # Zero-downtime deploy
  template:
    metadata:
      labels:
        app: identity-svc
        version: {{ .Chart.AppVersion }}
    spec:
      containers:
        - name: identity-svc
          image: {{ .Values.registry }}/identity-svc:{{ .Values.imageTag }}
          ports:
            - containerPort: 5001
          resources:
            requests:
              cpu: "250m"
              memory: "512Mi"
            limits:
              cpu: "1000m"
              memory: "1Gi"
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: {{ .Values.environment }}
            - name: MongoDB__ConnectionString
              valueFrom:
                secretKeyRef:
                  name: bazan-secrets
                  key: mongodb-connection-string
          livenessProbe:
            httpGet:
              path: /health/live
              port: 5001
            initialDelaySeconds: 30
            periodSeconds: 10
            failureThreshold: 3
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 5001
            initialDelaySeconds: 10
            periodSeconds: 5
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
            - weight: 100
              podAffinityTerm:
                labelSelector:
                  matchLabels:
                    app: identity-svc
                topologyKey: kubernetes.io/hostname
```

### 4.2 HPA — Auto Scaling

```yaml
# templates/services/orchestrator/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: ai-orchestrator-hpa
  namespace: bazan-app
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: ai-orchestrator
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: External
      external:
        metric:
          name: bazan_active_sessions
        target:
          type: AverageValue
          averageValue: "50"  # Scale khi > 50 sessions per pod
```

### 4.3 MongoDB StatefulSet (via Operator)

```yaml
# templates/data/mongodb-community.yaml
apiVersion: mongodbcommunity.mongodb.com/v1
kind: MongoDBCommunity
metadata:
  name: bazan-mongodb
  namespace: bazan-data
spec:
  members: 3
  type: ReplicaSet
  version: "7.0.6"
  security:
    authentication:
      modes: ["SCRAM"]
  users:
    - name: identity_svc
      db: bazan_identity
      passwordSecretRef:
        name: identity-db-password
      roles:
        - name: readWrite
          db: bazan_identity
  statefulSet:
    spec:
      volumeClaimTemplates:
        - metadata:
            name: data-volume
          spec:
            accessModes: ["ReadWriteOnce"]
            storageClassName: gp3
            resources:
              requests:
                storage: 100Gi
```

### 4.4 Network Policy

```yaml
# templates/network-policies.yaml
# Chỉ Gateway được nhận traffic từ Ingress
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-ingress-to-gateway
  namespace: bazan-app
spec:
  podSelector:
    matchLabels:
      app: api-gateway
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              kubernetes.io/metadata.name: ingress-nginx

---
# Data namespace chỉ nhận từ app namespace
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: data-allow-app-only
  namespace: bazan-data
spec:
  podSelector: {}
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              kubernetes.io/metadata.name: bazan-app
```

---

## 5. SignalR Sticky Sessions

AI Orchestrator dùng SignalR — cần sticky sessions để WebSocket duy trì kết nối đúng pod.

```yaml
# Nginx Ingress annotation cho sticky sessions
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: bazan-ingress
  annotations:
    nginx.ingress.kubernetes.io/affinity: "cookie"
    nginx.ingress.kubernetes.io/session-cookie-name: "BAZANROUTE"
    nginx.ingress.kubernetes.io/session-cookie-hash: "sha1"
    nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"    # WebSocket timeout
    nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
    nginx.ingress.kubernetes.io/proxy-http-version: "1.1"
    nginx.ingress.kubernetes.io/configuration-snippet: |
      proxy_set_header Upgrade $http_upgrade;
      proxy_set_header Connection "upgrade";
spec:
  rules:
    - host: api.bazanai.vn
      http:
        paths:
          - path: /hub
            pathType: Prefix
            backend:
              service:
                name: ai-orchestrator
                port:
                  number: 5010
```

---

## 6. Migration Checklist

```
PRE-MIGRATION:
□ K8s cluster provisioned (EKS/GKE/self-managed)
□ kubectl, helm installed
□ Container registry accessible từ cluster
□ Secrets đã migrate sang K8s Secrets hoặc External Secrets (Vault)
□ MongoDB Community Operator installed
□ Cert-manager installed (Let's Encrypt)
□ Nginx Ingress Controller installed
□ DNS records updated (CNAME hoặc A record → cluster LB)
□ Docker Compose vẫn running song song (blue-green migration)

MIGRATION:
□ Deploy data layer (StatefulSets) trước
□ Seed data từ Docker Compose MongoDB → K8s MongoDB (replication)
□ Deploy application services (test với staging traffic)
□ Switch 5% traffic → K8s (canary)
□ Monitor 48h
□ Switch 100% traffic → K8s
□ Decommission Docker Compose sau 1 tuần ổn định

POST-MIGRATION:
□ HPA tested (scale up/down manually)
□ Rolling update tested (no downtime)
□ Pod failure tested (delete pod, verify auto-restart)
□ MongoDB failover tested (kill primary pod)
□ Disaster recovery updated (K8s backup với Velero)
```

---

## 7. Cost Estimate (Phase 4)

```
AWS EKS (ap-southeast-1):
  Control plane:          $73/tháng
  Worker nodes (3× t3.xlarge): $450/tháng
  Load Balancer:          $20/tháng
  EBS storage (1TB):      $100/tháng
  Data transfer:          $30/tháng
  ─────────────────────────────────
  Total K8s:              ~$673/tháng

vs Docker Compose (Phase 3): ~$800–1,200/tháng (3 servers)

K8s savings: $127–527/tháng + operational efficiency
Break-even: Tháng 2–3 sau migration
```

---

**© 2026 Bazan AI Project — Confidential**

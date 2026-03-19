# ADR-002 — Vector Database: Qdrant

| Trường | Nội dung |
|--------|---------|
| **ID** | ADR-002 |
| **Tiêu đề** | Lựa chọn Qdrant làm Vector Database cho RAG System |
| **Trạng thái** | Accepted |
| **Ngày tạo** | 2026-03-19 |
| **Ngày cập nhật** | 2026-03-19 |
| **Tác giả** | Principal Software Architect · AI/ML Lead |
| **Người review** | Backend Lead, Knowledge RAG Team |
| **Liên quan** | ADR-001 (MongoDB), ADR-004 (Semantic Kernel), RAG Pipeline Design, Embedding Strategy |

---

## 1. Bối cảnh

Bazan AI triển khai RAG (Retrieval-Augmented Generation) để cung cấp câu trả lời kỹ thuật cho nông dân dựa trên kho tài liệu nông nghiệp cà phê (~450 tài liệu, ~22,500 chunks Phase 1, tăng lên ~50,000 chunks Phase 3).

### 1.1 Yêu cầu kỹ thuật

| Yêu cầu | Metric | Ghi chú |
|---------|--------|---------|
| **Hybrid Search** | Dense + Sparse trong 1 query | Bắt buộc — xem Embedding Strategy |
| **Retrieval latency** | P95 < 500ms | Trong context tổng P95 < 3s |
| **Payload filtering** | Filter trước khi search | Lọc theo giống, vùng, giai đoạn |
| **Storage efficiency** | INT8 quantization | RAM budget 2GB/node |
| **Self-hosted** | Docker Compose | Không phụ thuộc cloud vendor |
| **Collection management** | Multi-collection | 4 collections độc lập |
| **Indexing throughput** | 1,000 chunks/phút | Offline indexing job |
| **gRPC interface** | Low-latency internal call | Python RAG Service → Qdrant |

### 1.2 Thách thức RAG đặc thù của Bazan AI

Câu hỏi nông dân xen lẫn **ngữ nghĩa** ("lá vàng cà phê bị bệnh gì") và **từ khóa kỹ thuật** ("NPK 16-16-8", "TR4", "Carbendazim 50SC"). Không một loại search nào đủ:

- **Dense-only:** Miss tên kỹ thuật không có trong training data
- **Sparse-only (BM25):** Miss paraphrase, cross-lingual (VI query → EN document)
- **Hybrid (Dense + Sparse + RRF):** Đáp ứng cả hai — là yêu cầu bắt buộc

Ngoài ra, mỗi nông dân có context khác nhau (giống cây, loại đất, vùng), nên search phải được **filter theo metadata** để tránh trả về tài liệu không liên quan đến điều kiện cụ thể của họ.

---

## 2. Các phương án đã xem xét

### Phương án A — Qdrant

**Mô tả:** Vector database open-source viết bằng Rust, hỗ trợ hybrid search native, self-hosted.

**Ưu điểm:**
- **Hybrid Search native:** Dense vector + Sparse vector (BM42) + RRF fusion trong một API call — không cần orchestrate thủ công
- **Payload filtering trước khi search:** Filter giảm search space trước khi tính cosine similarity → nhanh hơn filter sau
- **Scalar quantization (INT8):** Giảm 75% RAM, accuracy loss ~1%
- **Matryoshka dimension truncation:** Hỗ trợ giảm dims mà không re-embed
- **gRPC interface:** Latency thấp hơn REST cho Python RAG Service
- **Self-hosted, MIT license:** Không tốn chi phí, không vendor lock-in
- **Python client chính thức:** Tương thích tốt với FastAPI RAG Service
- **On-disk storage:** Vector lớn lưu trên disk, quantized index trên RAM

**Nhược điểm:**
- Ecosystem nhỏ hơn Pinecone/Weaviate
- Ít tài liệu tiếng Việt
- Chưa có managed service tốt nếu cần cloud (Qdrant Cloud còn non)

---

### Phương án B — Pinecone

**Mô tả:** Fully managed vector database, cloud-native.

**Ưu điểm:**
- Fully managed — zero ops
- Hybrid search hỗ trợ (sparse + dense)
- SLA 99.9%

**Nhược điểm:**
- **Vendor lock-in:** Không self-hosted, không thể migrate dễ dàng
- **Chi phí:** ~$70/tháng cho 1M vectors (starter) — không phù hợp Phase 1
- **Payload filtering:** Kém linh hoạt hơn Qdrant (filter sau search thay vì trước)
- **Không kiểm soát được data:** Tài liệu kỹ thuật nông nghiệp nhạy cảm với IP

**Không chọn vì:** Vendor lock-in + chi phí + dữ liệu không nằm trong tầm kiểm soát.

---

### Phương án C — Weaviate

**Mô tả:** Open-source vector database với GraphQL API, hỗ trợ hybrid search.

**Ưu điểm:**
- Hybrid search (BM25 + dense)
- GraphQL API linh hoạt
- Self-hosted được
- Module ecosystem (text2vec, generative)

**Nhược điểm:**
- **Hybrid search kém hơn Qdrant:** Dùng BM25 thay vì BM42, không RRF native
- **RAM hungry:** Không có INT8 quantization tốt như Qdrant ở phiên bản stable
- **GraphQL overhead:** Phức tạp hơn cần thiết cho use case đơn giản
- **Go runtime:** Khó debug hơn Rust (Qdrant) khi gặp vấn đề memory
- **Java-like config:** Schema definition phức tạp, verbose

**Không chọn vì:** Hybrid search kém hơn và RAM usage cao hơn Qdrant trong điều kiện tương đương.

---

### Phương án D — ChromaDB

**Mô tả:** Open-source vector database nhẹ, dễ dùng, thường dùng trong prototype.

**Ưu điểm:**
- Cực kỳ đơn giản để bắt đầu
- Python-native
- Embed function tích hợp

**Nhược điểm:**
- **Không có hybrid search:** Chỉ dense search — không đủ cho yêu cầu Bazan AI
- **Không có payload filtering trước search:** Filter sau → không scale
- **Không có INT8 quantization:** RAM sẽ là bottleneck
- **Single-node:** Không HA
- **Production readiness thấp:** Thiết kế cho prototype, không production

**Không chọn vì:** Thiếu hybrid search là dealbreaker. Không phù hợp production.

---

### Phương án E — pgvector (PostgreSQL Extension)

**Mô tả:** Extension thêm vector search vào PostgreSQL.

**Ưu điểm:**
- Tận dụng PostgreSQL đã có (nếu dùng)
- SQL familiar
- ACID transaction với vector data

**Nhược điểm:**
- **Không có hybrid search native:** Cần kết hợp `tsvector` (BM25) + `vector` thủ công — phức tạp, chậm
- **HNSW index kém hơn:** pgvector HNSW chậm hơn Qdrant ~2–5x ở scale 10K+ vectors
- **Không có quantization:** Storage lớn
- **PostgreSQL không trong stack:** Bazan AI dùng MongoDB, thêm PostgreSQL chỉ cho vector là over-engineering

**Không chọn vì:** Hybrid search phức tạp và performance kém hơn dedicated vector database.

---

## 3. Quyết định

**Chọn Phương án A — Qdrant v1.9+** làm Vector Database cho Bazan AI RAG System.

**Version:** `qdrant/qdrant:v1.9.0`
**Interface chính:** gRPC (port 6334) cho Python RAG Service
**Interface phụ:** HTTP REST (port 6333) cho admin, health check

---

## 4. Lý do

### 4.1 Hybrid Search là yêu cầu không thể nhân nhượng

Qdrant là vector database duy nhất trong danh sách có **RRF (Reciprocal Rank Fusion) native** kết hợp dense và sparse vector trong một API call:

```python
results = await qdrant_client.query_points(
    collection_name="agronomy_knowledge",
    prefetch=[
        Prefetch(query=dense_vector, using="dense", limit=20),
        Prefetch(
            query=SparseVector(indices=sparse.indices, values=sparse.values),
            using="sparse", limit=20
        ),
    ],
    query=FusionQuery(fusion=Fusion.RRF),  # Qdrant xử lý RRF internally
    limit=5,
    with_payload=True
)
```

Với Weaviate hay pgvector, phải tự implement RRF ở application layer — thêm code, thêm latency, thêm điểm lỗi.

### 4.2 Pre-filtering giảm latency và tăng relevance

```python
# Filter trước khi search — chỉ search trong subset phù hợp
payload_filter = Filter(must=[
    FieldCondition(key="coffee_varieties",
                   match=MatchAny(any=["Robusta", "TR4", "general"])),
    FieldCondition(key="language",
                   match=MatchAny(any=["vi", "bilingual"])),
    FieldCondition(key="growth_stages",
                   match=MatchAny(any=["fruit_development", "general"]))
])
```

Pre-filtering giảm search space từ 22,500 → ~5,000 chunks trước khi tính similarity → nhanh hơn 4x, kết quả liên quan hơn.

### 4.3 INT8 Quantization phù hợp với RAM budget

```
3072 dims × 22,500 chunks × 4 bytes (float32) = 277 MB
3072 dims × 22,500 chunks × 1 byte  (int8)    = 69 MB  ✅

Quantized index luôn trên RAM (always_ram=True)
Original float32 on-disk — dùng cho re-scoring chính xác khi cần
```

Server 4GB RAM hoàn toàn đủ cho Qdrant + 3 collections với quantization.

### 4.4 Self-hosted phù hợp với data governance

Tài liệu WCR và kỹ thuật canh tác là intellectual property có giá trị. Lưu embeddings trên Pinecone (cloud) có rủi ro về:
- Data residency (dữ liệu nằm ở data center nước ngoài)
- Vendor access (Pinecone có thể đọc data)
- Pricing thay đổi (không kiểm soát được cost)

Qdrant self-hosted trên server của Bazan AI giải quyết cả 3 vấn đề.

---

## 5. Thiết kế triển khai

### 5.1 Docker Compose

```yaml
qdrant:
  image: qdrant/qdrant:v1.9.0
  container_name: bazan-qdrant
  volumes:
    - qdrant-data:/qdrant/storage
    - ./infra/qdrant/config.yaml:/qdrant/config/production.yaml:ro
  environment:
    QDRANT__SERVICE__API_KEY: ${QDRANT_API_KEY}
    QDRANT__LOG_LEVEL: INFO
    QDRANT__STORAGE__ON_DISK_PAYLOAD: true
  ports:
    - "6333:6333"   # HTTP REST
    - "6334:6334"   # gRPC
  networks:
    - bazan-network
  healthcheck:
    test: ["CMD", "curl", "-sf", "http://localhost:6333/healthz"]
    interval: 10s
    timeout: 5s
    retries: 5
  deploy:
    resources:
      limits:
        memory: 2G
      reservations:
        memory: 1G
```

### 5.2 Collection Setup

```python
# infrastructure/qdrant/collections.py

COLLECTIONS = {
    "wcr_documents": {
        "dims": 3072,
        "description": "WCR research papers — full precision"
    },
    "agronomy_knowledge": {
        "dims": 3072,
        "description": "Kỹ thuật canh tác tổng hợp"
    },
    "pest_disease_library": {
        "dims": 3072,
        "description": "Sâu bệnh, triệu chứng, phác đồ"
    },
    "market_reports": {
        "dims": 1536,
        "description": "Báo cáo thị trường — Matryoshka 1536d"
    },
}

async def create_collection(client: QdrantClient, name: str, dims: int):
    await client.create_collection(
        collection_name=name,
        vectors_config={
            "dense": VectorParams(
                size=dims,
                distance=Distance.COSINE,
                on_disk=True,
                hnsw_config=HnswConfigDiff(
                    m=16,
                    ef_construct=200,
                    full_scan_threshold=10_000
                )
            )
        },
        sparse_vectors_config={
            "sparse": SparseVectorParams(
                index=SparseIndexParams(on_disk=False)  # Sparse nhỏ, giữ RAM
            )
        },
        quantization_config=ScalarQuantization(
            scalar=ScalarQuantizationConfig(
                type=ScalarType.INT8,
                quantile=0.99,
                always_ram=True
            )
        ),
        optimizers_config=OptimizersConfigDiff(
            indexing_threshold=20_000,
            memmap_threshold=50_000
        ),
        on_disk_payload=True  # Payload lưu disk, không tốn RAM
    )

    # Payload indexes cho fast filtering
    for field, schema in [
        ("coffee_varieties", PayloadSchemaType.KEYWORD),
        ("topics",           PayloadSchemaType.KEYWORD),
        ("region",           PayloadSchemaType.KEYWORD),
        ("growth_stages",    PayloadSchemaType.KEYWORD),
        ("language",         PayloadSchemaType.KEYWORD),
        ("document_type",    PayloadSchemaType.KEYWORD),
        ("source_year",      PayloadSchemaType.INTEGER),
    ]:
        await client.create_payload_index(
            collection_name=name,
            field_name=field,
            field_schema=schema
        )
```

### 5.3 Monitoring Metrics

```yaml
# prometheus.yml — scrape Qdrant metrics
- job_name: 'qdrant'
  static_configs:
    - targets: ['qdrant:6333']
  metrics_path: '/metrics'
```

**Metrics cần alert:**

| Metric | Ngưỡng | Mức độ |
|--------|--------|--------|
| `qdrant_collections_total` | Giảm đột ngột | Critical |
| `qdrant_points_count` | Giảm > 5% | Warning |
| `app_qdrant_search_duration_p95_ms` | > 500ms | Warning |
| `app_qdrant_search_duration_p99_ms` | > 1000ms | Critical |
| `qdrant_memory_usage_bytes` | > 1.8GB | Warning |
| `app_qdrant_index_errors_total` | > 0 | Warning |

---

## 6. Hậu quả & Trade-offs

### 6.1 Tích cực

- **Hybrid search chính xác:** Recall@5 target 0.85 — đạt được nhờ RRF fusion native
- **Latency thấp:** P95 < 200ms nhờ pre-filtering + quantized HNSW index on RAM
- **Storage tiết kiệm:** INT8 quantization giảm 75% RAM so với float32
- **Zero vendor lock-in:** MIT license, self-hosted, exportable data

### 6.2 Tiêu cực & Cách giảm thiểu

| Trade-off | Mức độ | Cách giảm thiểu |
|-----------|--------|----------------|
| Single-node, không HA native | Trung bình | Docker volume backup hàng ngày. Phase 4 xem xét Qdrant cluster. |
| Ecosystem nhỏ hơn Pinecone | Thấp | Tài liệu đủ dùng, Python client chính thức maintained. |
| Upgrade có thể breaking change | Thấp | Pin version trong Docker Compose, test trước khi upgrade. |
| Qdrant Cloud non (nếu cần managed) | Thấp | Có thể migrate sang Atlas Vector Search nếu cần — re-embed không cần thiết. |

---

## 7. Tiêu chí đánh giá lại

- Recall@5 < 0.80 sau 6 tháng → Đánh giá lại chunking, embedding model, RRF weights
- Qdrant memory vượt 1.8GB thường xuyên → Xem xét Qdrant cluster hoặc tách collection ra node riêng
- Team cần managed service (scale > 500K chunks) → Evaluate Qdrant Cloud hoặc Weaviate Cloud

---

## 8. Tham khảo

- [Qdrant Documentation](https://qdrant.tech/documentation/)
- [Qdrant Hybrid Search Guide](https://qdrant.tech/articles/hybrid-search/)
- [BM42 Sparse Vectors](https://qdrant.tech/articles/bm42/)
- [Scalar Quantization in Qdrant](https://qdrant.tech/documentation/guides/quantization/)
- ADR-001 — MongoDB Primary Database
- RAG Pipeline Design Document
- Embedding Strategy Document

---

*Tài liệu theo template ADR của Michael Nygard (2011).*

**© 2026 Bazan AI Project — Confidential**

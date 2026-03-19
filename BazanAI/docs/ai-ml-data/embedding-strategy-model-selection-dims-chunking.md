# Embedding Strategy Document
## Bazan AI — Chiến lược Vector Embedding cho RAG System

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | AI/ML Lead · Principal Architect |
| **Người review** | Knowledge RAG Team, DevOps Lead |
| **Trạng thái** | Draft — Pending Review |
| **Liên quan** | RAG Pipeline Design, ADR-002 (Qdrant), Prompt Engineering Playbook |

---

## Mục lục

1. [Tổng quan & Bài toán cần giải](#1-tổng-quan--bài-toán-cần-giải)
2. [Lựa chọn Embedding Model](#2-lựa-chọn-embedding-model)
3. [Dual-encoder Architecture — Dense + Sparse](#3-dual-encoder-architecture--dense--sparse)
4. [Chiến lược Embedding cho Tiếng Việt](#4-chiến-lược-embedding-cho-tiếng-việt)
5. [Document Embedding Pipeline](#5-document-embedding-pipeline)
6. [Query Embedding Pipeline](#6-query-embedding-pipeline)
7. [Dimensionality & Quantization](#7-dimensionality--quantization)
8. [Batching & Rate Limit Strategy](#8-batching--rate-limit-strategy)
9. [Caching Architecture](#9-caching-architecture)
10. [Embedding Drift & Reindex Strategy](#10-embedding-drift--reindex-strategy)
11. [Cost Analysis & Optimization](#11-cost-analysis--optimization)
12. [Evaluation & Benchmarking](#12-evaluation--benchmarking)
13. [Lộ trình nâng cấp Model](#13-lộ-trình-nâng-cấp-model)

---

## 1. Tổng quan & Bài toán cần giải

### 1.1 Embedding là gì trong Bazan AI?

Embedding là quá trình chuyển đổi **văn bản** (câu hỏi nông dân, đoạn tài liệu kỹ thuật) thành **vector số học** trong không gian nhiều chiều, sao cho các văn bản có nghĩa tương đồng sẽ có vector gần nhau về mặt khoảng cách.

```
"vàng lá cà phê thiếu magie"    → [0.023, -0.145, 0.891, ..., 0.034]  (3072 chiều)
"triệu chứng thiếu Mg ở cây"    → [0.019, -0.138, 0.887, ..., 0.041]  ← Gần nhau
"giá cà phê hôm nay bao nhiêu"  → [-0.412, 0.234, -0.091, ..., 0.882] ← Xa nhau
```

### 1.2 Các bài toán embedding trong hệ thống

| Bài toán | Input | Output | Dùng ở đâu |
|---------|-------|--------|-----------|
| **Document indexing** | Chunk văn bản (512 tokens) | Dense vector 3072d + Sparse vector | Qdrant upsert khi index tài liệu |
| **Query embedding** | Câu hỏi nông dân (đã rewrite) | Dense vector 3072d + Sparse vector | Qdrant search real-time |
| **Session summarization** | Tóm tắt cuộc hội thoại | Dense vector 1536d | MongoDB chatSessions.summaryEmbedding |
| **Semantic deduplication** | Hai tài liệu PDF | Cosine similarity score | Phát hiện tài liệu trùng lặp khi index |

### 1.3 Thách thức đặc thù của Bazan AI

```
┌─────────────────────────────────────────────────────────┐
│  THÁCH THỨC 1: Tiếng Việt có dấu phức tạp              │
│  "cà phê" ≠ "ca phe" ≠ "café" — cùng nghĩa             │
│  Nông dân gõ thiếu dấu trên điện thoại → cần normalize  │
├─────────────────────────────────────────────────────────┤
│  THÁCH THỨC 2: Domain cực kỳ chuyên biệt               │
│  "TR4", "gỉ sắt", "NPK 16-16-8" — ít xuất hiện         │
│  trong training data của general purpose model          │
├─────────────────────────────────────────────────────────┤
│  THÁCH THỨC 3: Code-switching Việt-Anh                  │
│  Tài liệu WCR bằng tiếng Anh, câu hỏi tiếng Việt       │
│  → Cross-lingual retrieval cần thiết                    │
├─────────────────────────────────────────────────────────┤
│  THÁCH THỨC 4: Tên kỹ thuật & Số liệu                  │
│  "Carbendazim 50SC", "20ml/10L", "pH 6.2"              │
│  → Sparse embedding bắt buộc, dense không đủ           │
├─────────────────────────────────────────────────────────┤
│  THÁCH THỨC 5: Chi phí API tích lũy theo thời gian      │
│  ~450 tài liệu × ~50 chunks × 3072 dims × $0.13/1M     │
│  → Cần caching, batching, quantization                  │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Lựa chọn Embedding Model

### 2.1 So sánh các phương án

| Model | Nhà cung cấp | Dims | Đa ngôn ngữ | MTEB VI | Cost/1M tokens | Latency |
|-------|-------------|------|------------|---------|----------------|---------|
| **text-embedding-3-large** | OpenAI | 3072 | Có (100+ ngôn ngữ) | ~0.72 | $0.13 | ~80ms |
| text-embedding-3-small | OpenAI | 1536 | Có | ~0.62 | $0.02 | ~50ms |
| text-embedding-ada-002 | OpenAI | 1536 | Có | ~0.58 | $0.10 | ~80ms |
| multilingual-e5-large | Microsoft | 1024 | Có (94 ngôn ngữ) | ~0.69 | Self-hosted | ~120ms |
| bge-m3 | BAAI | 1024 | Có (100+ ngôn ngữ) | ~0.74 | Self-hosted | ~150ms |
| Vietnamese-SBERT | Bactrian | 768 | VI chỉ | ~0.78 | Self-hosted | ~60ms |
| phobert-large | VinAI | 768 | VI chỉ | ~0.71 | Self-hosted | ~80ms |

> MTEB VI = điểm benchmark trên tập dữ liệu tiếng Việt (Massive Text Embedding Benchmark)

### 2.2 Quyết định: text-embedding-3-large (Phase 1)

**Chọn `text-embedding-3-large` làm dense embedding model** vì:

**Lý do chính:**

1. **Cross-lingual out-of-the-box** — Tài liệu WCR viết bằng tiếng Anh, câu hỏi nông dân bằng tiếng Việt. `text-embedding-3-large` có thể map cả hai vào cùng không gian vector mà không cần bước dịch thuật trung gian.

2. **API managed — không cần MLOps** — Phase 1 team nhỏ, chưa có infrastructure để self-host model GPU. Dùng OpenAI API loại bỏ hoàn toàn: GPU provisioning, model serving, auto-scaling, monitoring model health.

3. **Matryoshka Representation Learning** — `text-embedding-3-large` hỗ trợ truncate dimensions về 1536 hoặc 256 mà **không cần re-embed**. Cho phép thí nghiệm trade-off chất lượng/storage mà không tốn thêm API call.

4. **Đủ tốt cho Phase 1** — MTEB score 0.72 trên tiếng Việt là chấp nhận được cho MVP. Phase 2 sẽ đánh giá lại.

**Trade-off chấp nhận:**

- Phụ thuộc vào OpenAI API (uptime, pricing) — giảm thiểu bằng caching aggressive
- Chi phí tích lũy khi knowledge base mở rộng — xem phân tích Section 11
- Không kiểm soát được model update của OpenAI — giảm thiểu bằng Embedding Versioning (Section 10)

### 2.3 Matryoshka Dimensions — Kế hoạch sử dụng

```python
# text-embedding-3-large hỗ trợ truncate không mất thông tin
DIMENSION_STRATEGY = {
    # Collection chính — full quality
    "wcr_documents":        3072,   # Tài liệu quan trọng, ưu tiên accuracy
    "agronomy_knowledge":   3072,   # Tri thức cốt lõi
    "pest_disease_library": 3072,   # Cần chính xác cao cho diagnosis

    # Collection phụ — giảm storage
    "market_reports":       1536,   # Báo cáo ngắn hạn, accuracy đủ dùng
    "session_summaries":    1536,   # Tóm tắt session (MongoDB, không Qdrant)
}

# Cách request dimensions khác nhau:
response = openai_client.embeddings.create(
    model="text-embedding-3-large",
    input=text,
    dimensions=1536   # Chỉ định chiều muốn dùng
)
```

---

## 3. Dual-encoder Architecture — Dense + Sparse

### 3.1 Tại sao cần cả hai?

```
CÂU HỎI: "bón phân NPK 16-16-8 cho TR4 giai đoạn ra hoa bao nhiêu kg?"

Dense embedding (semantic):
  ✅ Hiểu "giai đoạn ra hoa" ~ "flowering stage" ~ "thời kỳ trổ hoa"
  ✅ Hiểu "bón phân" ~ "fertilization" ~ "nutrient application"
  ❌ Không phân biệt "NPK 16-16-8" vs "NPK 12-12-17" — cùng là phân NPK
  ❌ "TR4" gần như không có trong training data → vector không đặc trưng

Sparse embedding (BM42 keyword):
  ✅ Match chính xác "NPK 16-16-8", "TR4", "16-16-8"
  ✅ Match "kg", "bón phân", "ra hoa"
  ❌ "flowering stage" và "ra hoa" → không match nếu tài liệu dùng tiếng Anh
  ❌ Paraphrase không match: "cần bón bao nhiêu" vs "liều lượng"

Hybrid (Dense 70% + Sparse 30%):
  ✅ Cả hai điểm mạnh trên
  ✅ RRF fusion giảm thiểu điểm yếu của từng loại
```

### 3.2 BM42 — Sparse Embedding trong Qdrant

BM42 là sparse embedding model tích hợp sẵn trong Qdrant, cải tiến từ BM25 truyền thống:

```python
# Qdrant xử lý sparse embedding nội bộ — không cần tự tính
# Chỉ cần enable khi tạo collection

from qdrant_client.models import SparseVectorParams, SparseIndexParams

sparse_config = {
    "sparse": SparseVectorParams(
        index=SparseIndexParams(
            on_disk=False,        # Sparse index nhỏ, giữ trong RAM
            full_scan_threshold=5000
        )
    )
}

# Khi search, Qdrant tự generate sparse vector từ text
# Không cần gọi external API cho sparse embedding
# → Latency thấp, không tốn chi phí API
```

**So sánh BM42 vs BM25:**

| Tính năng | BM25 | BM42 |
|-----------|------|------|
| Term frequency | ✅ | ✅ |
| IDF weighting | ✅ | ✅ |
| Subword tokenization | ❌ | ✅ |
| Vietnamese word segmentation | ❌ | ✅ (partial) |
| Neural term importance | ❌ | ✅ |
| Tích hợp Qdrant native | ❌ | ✅ |

### 3.3 RRF Weight Tuning

```python
# Trọng số RRF được tuned qua thực nghiệm trên test set
# (xem Section 12 — Evaluation)

RRF_WEIGHTS = {
    # Collection chứa nhiều tài liệu tiếng Anh → tăng sparse
    # vì tên kỹ thuật tiếng Anh match tốt hơn qua keyword
    "wcr_documents": {
        "dense_weight":  0.65,
        "sparse_weight": 0.35,
        "k": 60
    },

    # Collection tiếng Việt → tăng dense
    # vì nông dân dùng paraphrase nhiều
    "agronomy_knowledge": {
        "dense_weight":  0.75,
        "sparse_weight": 0.25,
        "k": 60
    },

    # Bệnh & sâu hại → cân bằng
    # (tên bệnh quan trọng, nhưng triệu chứng cần semantic)
    "pest_disease_library": {
        "dense_weight":  0.70,
        "sparse_weight": 0.30,
        "k": 60
    },
}
```

---

## 4. Chiến lược Embedding cho Tiếng Việt

### 4.1 Preprocessing Pipeline

```python
class VietnameseTextPreprocessor:
    """
    Tiền xử lý văn bản tiếng Việt trước khi embed.
    Áp dụng cho cả document indexing và query embedding.
    """

    def preprocess(self, text: str, mode: Literal["document", "query"]) -> str:

        # Bước 1: Chuẩn hóa Unicode
        # "café" (Latin Extended) → "cà phê" (Vietnamese Unicode chuẩn)
        text = unicodedata.normalize("NFC", text)

        # Bước 2: Khôi phục dấu tiếng Việt (chỉ với query)
        if mode == "query":
            text = self._restore_diacritics(text)
            # "cay bi vang la" → "cây bị vàng lá"

        # Bước 3: Chuẩn hóa thuật ngữ địa phương
        text = self._normalize_regional_terms(text)
        # "mọt quả" → "mọt đục quả Hypothenemus hampei"

        # Bước 4: Bảo vệ token kỹ thuật (không tokenize nhỏ hơn)
        text = self._protect_technical_terms(text)
        # NPK 16-16-8, TR4, pH 6.2, Carbendazim 50SC

        # Bước 5: Chuẩn hóa số đo
        text = self._normalize_measurements(text)
        # "20ml/10l" → "20 ml / 10 lít"
        # "1,5ha" → "1.5 ha"

        # Bước 6: Loại bỏ nhiễu (chỉ với document)
        if mode == "document":
            text = self._remove_boilerplate(text)
            # Loại bỏ header/footer PDF, số trang, watermark

        return text.strip()

    def _protect_technical_terms(self, text: str) -> str:
        """
        Bọc các tên kỹ thuật trong markers để tokenizer không phá vỡ chúng.
        Quan trọng: "NPK 16-16-8" không được tách thành "NPK", "16", "16", "8"
        """
        patterns = [
            r'\bNPK\s*\d+-\d+-\d+\b',          # NPK 16-16-8
            r'\b(pH|EC)\s*[\d.]+\b',            # pH 6.2, EC 1.5
            r'\b\d+\s*ml\s*/\s*\d+\s*[lL]\b',  # 20ml/10L
            r'\bTR\d+\b',                        # TR4, TR5A, TR9
            r'\b[A-Z]{2,}\d+[A-Z]*\b',          # Catimor, SA21
        ]
        for pattern in patterns:
            text = re.sub(pattern,
                lambda m: m.group().replace(" ", "\u00a0"),  # Non-breaking space
                text, flags=re.IGNORECASE)
        return text

    def _restore_diacritics(self, text: str) -> str:
        """
        Khôi phục dấu tiếng Việt cho text gõ không dấu.
        Ưu tiên từ vựng nông nghiệp khi có nhiều khả năng.
        Dùng thư viện vncorenlp hoặc model transformer nhỏ.
        """
        if self._needs_diacritic_restoration(text):
            return vncorenlp_client.restore_diacritics(text)
        return text

    def _needs_diacritic_restoration(self, text: str) -> bool:
        """Phát hiện text thiếu dấu bằng tỉ lệ ký tự có dấu."""
        vietnamese_diacritics = set("àáảãạăắặẳẵặâấầẩẫậđèéẻẽẹêếềểễệìíỉĩịòóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựỳýỷỹỵ")
        vi_chars = sum(1 for c in text.lower() if c in vietnamese_diacritics)
        total_alpha = sum(1 for c in text if c.isalpha())
        if total_alpha == 0:
            return False
        # Nếu < 5% ký tự có dấu trong text tiếng Việt → cần khôi phục
        return (vi_chars / total_alpha) < 0.05
```

### 4.2 Cross-lingual Embedding — Tiếng Việt ↔ Tiếng Anh

```
CHIẾN LƯỢC: Không dịch, nhờ vào khả năng cross-lingual của model.

text-embedding-3-large được train trên 100+ ngôn ngữ đồng thời.
Kết quả: văn bản cùng nghĩa ở tiếng Việt và tiếng Anh
sẽ có vector gần nhau trong cùng không gian.

Ví dụ thực nghiệm (cosine similarity):
  "bệnh gỉ sắt cà phê" (VI)
  "coffee leaf rust disease" (EN)           → similarity: 0.89 ✅

  "thiếu magie ở lá" (VI)
  "magnesium deficiency in leaves" (EN)     → similarity: 0.86 ✅

  "tưới nước giai đoạn ra hoa" (VI)
  "irrigation during flowering stage" (EN)  → similarity: 0.84 ✅

ĐỐI VỚI TÊN KỸ THUẬT KHÔNG CÓ TRONG TRAINING DATA:
  "TR4" (giống cà phê Việt Nam)             → Sparse BM42 sẽ xử lý
  "WASI" (Viện nghiên cứu)                  → Sparse BM42 sẽ xử lý
  "NPK 16-16-8" (công thức phân bón)        → Sparse + Dense kết hợp
```

### 4.3 Language Detection & Routing

```python
async def embed_with_language_awareness(
    text: str,
    mode: Literal["document", "query"]
) -> EmbeddingResult:

    # Detect ngôn ngữ
    lang = detect_language(text)  # "vi", "en", "mixed"

    # Preprocess theo ngôn ngữ
    if lang in ("vi", "mixed"):
        processed = vietnamese_preprocessor.preprocess(text, mode)
    else:
        processed = english_preprocessor.preprocess(text, mode)

    # Embed — dùng cùng model cho cả VI và EN
    # text-embedding-3-large xử lý cross-lingual internally
    dense_vector = await openai_embedder.embed(
        text=processed,
        dimensions=get_dimensions_for_collection(collection)
    )

    # Sparse embedding qua Qdrant BM42 (handle internally)
    # Không cần gọi external API

    return EmbeddingResult(
        dense_vector=dense_vector,
        language=lang,
        preprocessed_text=processed,
        original_text=text
    )
```

---

## 5. Document Embedding Pipeline

### 5.1 Flow tổng thể

```
PDF / DOCX
    │
    ▼
[Text Extraction]
  PyMuPDF (fitz)
    │
    ├─► Trang có bảng → TableProcessor → text có cấu trúc
    ├─► Trang có hình → ImageSkipper (Phase 1) / OCR (Phase 3)
    └─► Trang text thuần → PassThrough
    │
    ▼
[Vietnamese Preprocessor]
    │
    ▼
[Recursive Chunker]
  chunk_size=512, overlap=64
    │
    ▼
[Metadata Extractor]         ←── OpenAI gpt-4o-mini (1 call / doc)
    │
    ▼
[Batch Embedder]             ←── OpenAI text-embedding-3-large
  batch_size=20, parallel=3
    │
    ├─► dense_vectors (List[List[float]])
    └─► [Qdrant BM42 sparse] (tự động khi upsert)
    │
    ▼
[Qdrant Upsert]
  upsert_batch(collection, points)
    │
    ▼
[Document Registry Update]  ←── MongoDB bazan_knowledge
```

### 5.2 Batch Embedding Implementation

```python
class BatchEmbedder:
    """
    Embed nhiều chunks song song, tôn trọng rate limit OpenAI.
    Rate limit text-embedding-3-large: 3,000 RPM / 1,000,000 TPM
    """

    BATCH_SIZE = 20         # Số chunks mỗi API call
    MAX_PARALLEL = 3        # Số API calls đồng thời
    MAX_RETRIES = 5
    BASE_DELAY = 1.0        # seconds

    async def embed_chunks(
        self,
        chunks: List[Chunk],
        dimensions: int = 3072
    ) -> List[EmbeddedChunk]:

        # Chia thành batches
        batches = [
            chunks[i:i + self.BATCH_SIZE]
            for i in range(0, len(chunks), self.BATCH_SIZE)
        ]

        # Semaphore để giới hạn parallel calls
        semaphore = asyncio.Semaphore(self.MAX_PARALLEL)

        async def embed_batch_with_semaphore(batch: List[Chunk]) -> List[EmbeddedChunk]:
            async with semaphore:
                return await self._embed_batch_with_retry(batch, dimensions)

        # Chạy song song các batches
        results = await asyncio.gather(
            *[embed_batch_with_semaphore(batch) for batch in batches]
        )

        return [item for sublist in results for item in sublist]

    async def _embed_batch_with_retry(
        self,
        batch: List[Chunk],
        dimensions: int
    ) -> List[EmbeddedChunk]:

        for attempt in range(self.MAX_RETRIES):
            try:
                response = await openai_client.embeddings.create(
                    model="text-embedding-3-large",
                    input=[chunk.text for chunk in batch],
                    dimensions=dimensions,
                    encoding_format="float"   # float32, không phải base64
                )

                return [
                    EmbeddedChunk(
                        chunk=chunk,
                        dense_vector=data.embedding,
                        token_count=response.usage.total_tokens // len(batch),
                        model=response.model,
                        dimensions=dimensions,
                        embedded_at=datetime.utcnow()
                    )
                    for chunk, data in zip(batch, response.data)
                ]

            except openai.RateLimitError:
                delay = self.BASE_DELAY * (2 ** attempt) + random.uniform(0, 1)
                logger.warning(f"Rate limit hit, retry {attempt+1} after {delay:.1f}s")
                await asyncio.sleep(delay)

            except openai.APIError as e:
                if attempt == self.MAX_RETRIES - 1:
                    raise EmbeddingError(f"Failed after {self.MAX_RETRIES} retries: {e}")
                await asyncio.sleep(self.BASE_DELAY * (2 ** attempt))

        raise EmbeddingError("Max retries exceeded")
```

### 5.3 Upsert vào Qdrant

```python
async def upsert_embedded_chunks(
    collection: str,
    embedded_chunks: List[EmbeddedChunk],
    sparse_text_field: str = "text"   # BM42 dùng field này để tạo sparse vector
) -> UpsertResult:

    points = [
        PointStruct(
            id=str(uuid.uuid5(uuid.NAMESPACE_DNS, chunk.chunk.chunk_id)),
            vector={
                "dense": chunk.dense_vector,
                # Sparse vector được Qdrant tự generate từ payload[sparse_text_field]
                # khi collection được config với FastEmbed BM42
            },
            payload={
                # Text cho BM42 sparse embedding
                "text": chunk.chunk.text,

                # Metadata
                "doc_id":       chunk.chunk.doc_id,
                "chunk_id":     chunk.chunk.chunk_id,
                "source_title": chunk.chunk.source_title,
                "page_start":   chunk.chunk.page_start,
                "page_end":     chunk.chunk.page_end,
                "token_count":  chunk.token_count,

                # Domain metadata (cho filtering)
                "coffee_varieties": chunk.chunk.metadata.coffee_varieties,
                "topics":           chunk.chunk.metadata.topics,
                "region":           chunk.chunk.metadata.region,
                "growth_stages":    chunk.chunk.metadata.growth_stages,
                "document_type":    chunk.chunk.metadata.document_type,
                "language":         chunk.chunk.metadata.language,
                "source_year":      chunk.chunk.metadata.source_year,

                # Embedding metadata (cho drift detection)
                "embedding_model":      chunk.model,
                "embedding_dimensions": chunk.dimensions,
                "embedded_at":          chunk.embedded_at.isoformat(),
                "schema_version":       "1.0",
            }
        )
        for chunk in embedded_chunks
    ]

    # Upsert theo batch 100 points
    for i in range(0, len(points), 100):
        batch = points[i:i + 100]
        await qdrant_client.upsert(
            collection_name=collection,
            points=batch,
            wait=True   # Đợi indexing hoàn thành trước khi return
        )

    return UpsertResult(
        total_points=len(points),
        collection=collection
    )
```

---

## 6. Query Embedding Pipeline

### 6.1 Flow real-time

```
Câu hỏi nông dân (raw)
    │
    ▼ < 10ms
[Language Detection]
    │
    ▼ < 20ms
[Diacritic Restoration] (nếu cần)
  vncorenlp local model
    │
    ▼ < 5ms
[Regional Term Normalization]
    │
    ▼ < 5ms
[Technical Term Protection]
    │
    ▼
[Cache Lookup] ──── HIT ──► cached_vector (Redis, TTL 1h)
    │ MISS
    ▼ ~80ms
[OpenAI Embedding API]
  text-embedding-3-large
  dimensions = collection-specific
    │
    ▼ < 5ms
[Cache Write] (Redis)
    │
    ▼
dense_query_vector
```

### 6.2 Query vs Document Embedding — Sự khác biệt quan trọng

```python
# text-embedding-3-large dùng CÙNG model cho cả query và document
# nhưng OpenAI internal sử dụng asymmetric embedding:
# - Query được encode khác document để tối ưu retrieval
# - KHÔNG cần prefix "query:" hay "passage:" như một số model khác

# ✅ ĐÚNG:
query_vector = await embed("vàng lá cà phê thiếu magie")
doc_vector   = await embed("Triệu chứng thiếu magie: lá vàng giữa gân xanh...")
# Hai vector này có thể so sánh cosine similarity trực tiếp

# ❌ SAI (với text-embedding-3-large):
# query_vector = await embed("query: vàng lá cà phê thiếu magie")
# Thêm prefix không cần thiết với model này

# ⚠️ LƯU Ý: Nếu đổi sang bge-m3 hoặc e5-large → cần thêm prefix
# Phải kiểm tra instruction của từng model cụ thể
```

### 6.3 Late Interaction — Dự phòng Phase 3

```python
# ColBERT / ColPali — Late interaction model
# Thay vì tính 1 vector cho cả đoạn văn,
# tính vector cho từng token → matching chính xác hơn
#
# Phase 1: Single vector per chunk (current approach)
# Phase 3: Xem xét ColBERT nếu recall@5 < 0.80 sau 6 tháng
#
# Chi phí: ColBERT tốn storage ~30x hơn single vector
# Chỉ áp dụng cho collection pest_disease_library nếu cần
```

---

## 7. Dimensionality & Quantization

### 7.1 Chiến lược giảm chiều

```python
DIMENSION_CONFIG = {
    # Full 3072 dims — Precision critical
    "wcr_documents":        {"dims": 3072, "quantization": "int8"},
    "agronomy_knowledge":   {"dims": 3072, "quantization": "int8"},
    "pest_disease_library": {"dims": 3072, "quantization": "int8"},

    # Giảm dims — Storage sensitive, recall ít quan trọng hơn
    "market_reports":       {"dims": 1536, "quantization": "int8"},

    # MongoDB chỉ lưu summaries — không cần search
    "session_summaries":    {"dims": 1536, "quantization": None},
}

# Matryoshka truncation — không cần re-embed:
# 3072 → 1536: giảm 50% storage, accuracy giảm ~3%
# 3072 → 256:  giảm 92% storage, accuracy giảm ~15%
```

### 7.2 INT8 Scalar Quantization

```python
# Cấu hình trong Qdrant collection
quantization_config = ScalarQuantization(
    scalar=ScalarQuantizationConfig(
        type=ScalarType.INT8,

        # Quantile=0.99: 99% giá trị được quantize tốt,
        # 1% outlier được clamp → accuracy loss rất nhỏ
        quantile=0.99,

        # always_ram=True: quantized vectors luôn trong RAM
        # Original float32 có thể on-disk
        # → Tradeoff: tốc độ search nhanh, storage tiết kiệm
        always_ram=True
    )
)

# Tác động:
# Storage: float32 (4 bytes) → int8 (1 byte) = giảm 75%
# RAM usage: 3072 dims × 4 bytes × 100K chunks = 1.2GB
#       →   3072 dims × 1 byte × 100K chunks = 300MB  ✅
# Accuracy loss: ~1% trên MTEB benchmark (chấp nhận được)
# Search speed: tăng ~4x vì ít bytes phải đọc
```

### 7.3 Storage Estimation

```
Knowledge Base dự kiến (Phase 1 → Phase 3):

Phase 1 (~200 docs × 50 chunks avg):
  10,000 chunks × 3072 dims × 4 bytes = 123 MB (float32)
  Sau INT8 quantization:               = 31 MB  ✅

Phase 2 (~450 docs × 50 chunks avg):
  22,500 chunks × 3072 dims × 4 bytes = 277 MB
  Sau INT8 quantization:               = 69 MB  ✅

Phase 3 (~1000 docs × 50 chunks avg):
  50,000 chunks × 3072 dims × 4 bytes = 614 MB
  Sau INT8 quantization:               = 154 MB ✅

→ Qdrant 2GB RAM instance đủ dùng đến Phase 3
→ Sparse vectors (BM42): ~50MB thêm, nhỏ không đáng kể
```

---

## 8. Batching & Rate Limit Strategy

### 8.1 OpenAI Rate Limits

```
text-embedding-3-large (tier mặc định):
  RPM (Requests Per Minute): 3,000
  TPM (Tokens Per Minute):   1,000,000
  Batch size tối đa:         2,048 inputs / request

Khi indexing 10,000 chunks (batch_size=20):
  Số requests: 10,000 / 20 = 500 requests
  Thời gian tối thiểu: 500 / 3000 RPM = 10 giây
  Token estimate: 10,000 × 400 tokens avg = 4,000,000 tokens
  → Cần ~4 phút nếu đạt TPM limit

Chiến lược:
  - Indexing chạy offline (background job)
  - MAX_PARALLEL = 3 để không spike RPM
  - Exponential backoff khi gặp 429
  - Indexing job ưu tiên thấp (chạy ban đêm)
```

### 8.2 Token Estimation trước khi gọi API

```python
import tiktoken

class TokenEstimator:
    """
    Ước tính token count trước khi gọi API để:
    1. Kiểm tra chunk không vượt giới hạn model (8192 tokens)
    2. Track chi phí theo thời gian thực
    3. Quyết định có cần compress chunk không
    """

    def __init__(self):
        # text-embedding-3-large dùng cl100k_base encoding
        self.encoder = tiktoken.get_encoding("cl100k_base")

    def count_tokens(self, text: str) -> int:
        return len(self.encoder.encode(text))

    def estimate_batch_cost(
        self,
        texts: List[str],
        price_per_million: float = 0.13
    ) -> dict:
        total_tokens = sum(self.count_tokens(t) for t in texts)
        cost_usd = (total_tokens / 1_000_000) * price_per_million
        return {
            "total_tokens": total_tokens,
            "cost_usd": round(cost_usd, 6),
            "cost_vnd": round(cost_usd * 25_000, 0)
        }

    def validate_chunk(self, text: str, max_tokens: int = 8192) -> bool:
        """text-embedding-3-large limit: 8192 tokens per input"""
        count = self.count_tokens(text)
        if count > max_tokens:
            logger.warning(f"Chunk exceeds {max_tokens} tokens: {count}")
            return False
        return True
```

---

## 9. Caching Architecture

### 9.1 Query Embedding Cache

```
Vấn đề: Nhiều nông dân hỏi câu giống nhau
  "giá cà phê hôm nay" — hỏi ~200 lần/ngày
  "bệnh gỉ sắt" — hỏi ~150 lần/ngày
  → Mỗi lần embed = 1 API call + latency + cost

Giải pháp: Cache embedding trong Redis
```

```python
class EmbeddingCache:
    """
    Cache query embeddings trong Redis.
    Key = hash của (preprocessed_text + model + dimensions)
    TTL = 1 giờ cho query thường, 24 giờ cho câu hỏi phổ biến
    """

    QUERY_TTL     = 3_600    # 1 giờ
    POPULAR_TTL   = 86_400   # 24 giờ
    POPULAR_THRESHOLD = 10   # Số lần query trong 1h để coi là "phổ biến"

    async def get_or_embed(
        self,
        text: str,
        model: str,
        dimensions: int
    ) -> Tuple[List[float], bool]:  # (vector, is_cache_hit)

        cache_key = self._make_key(text, model, dimensions)

        # Try cache first
        cached = await redis_client.get(cache_key)
        if cached:
            await self._increment_hit_count(cache_key)
            return json.loads(cached), True

        # Cache miss → call OpenAI API
        vector = await openai_embedder.embed_single(text, model, dimensions)

        # Determine TTL
        hit_count = await self._get_hit_count(cache_key)
        ttl = self.POPULAR_TTL if hit_count >= self.POPULAR_THRESHOLD \
              else self.QUERY_TTL

        # Store in cache
        await redis_client.setex(
            cache_key,
            ttl,
            json.dumps(vector)
        )

        return vector, False

    def _make_key(self, text: str, model: str, dimensions: int) -> str:
        content = f"{model}:{dimensions}:{text}"
        hash_val = hashlib.sha256(content.encode()).hexdigest()[:16]
        return f"embed:{hash_val}"
```

### 9.2 Cache Hit Rate dự kiến

```
Phân tích top câu hỏi (dựa trên user research):

Top 10 câu hỏi phổ biến (chiếm ~40% traffic):
  1. "giá cà phê hôm nay"           — 15%
  2. "lịch tưới mùa khô"            —  6%
  3. "bón phân giai đoạn ra hoa"    —  5%
  4. "bệnh gỉ sắt cách xử lý"       —  4%
  5. "thu hoạch khi nào"             —  3%
  ... (các câu còn lại)

Dự kiến cache hit rate: 35–45% sau 1 tuần vận hành
→ Tiết kiệm ~40% API calls
→ Giảm P50 latency từ ~80ms → ~5ms cho cached queries
```

### 9.3 Document Embedding Cache

```python
# Document embeddings KHÔNG cache trong Redis
# Lý do: Chúng đã được lưu vĩnh viễn trong Qdrant
# Chỉ cần avoid re-embedding document đã index

async def should_reindex(doc_id: str, current_hash: str) -> bool:
    """
    Kiểm tra xem document cần re-index không dựa trên:
    1. Content hash thay đổi (file đã cập nhật)
    2. Embedding model thay đổi (cần re-embed)
    3. Schema version thay đổi
    """
    registry = await doc_registry.find_by_id(doc_id)
    if not registry:
        return True   # Chưa index, cần index mới

    model_changed   = registry.embedding_model != CURRENT_MODEL
    content_changed = registry.content_hash != current_hash
    schema_changed  = registry.schema_version != CURRENT_SCHEMA_VERSION

    return model_changed or content_changed or schema_changed
```

---

## 10. Embedding Drift & Reindex Strategy

### 10.1 Vấn đề Embedding Drift

```
TÌNH HUỐNG: OpenAI cập nhật text-embedding-3-large
(hoặc team quyết định đổi sang model tốt hơn)

Vấn đề:
  - 50,000 chunks trong Qdrant được embed bằng model cũ
  - Query embedding mới dùng model mới
  - Vector space của hai model KHÔNG tương thích
  - → Search kết quả sai hoàn toàn

Giải pháp: Versioning + Gradual Migration
```

### 10.2 Embedding Version Tracking

```python
EMBEDDING_VERSIONS = {
    "v1": {
        "model":      "text-embedding-3-large",
        "dimensions": 3072,
        "deployed":   "2026-03-19",
        "status":     "active",
        "collections": ["wcr_documents", "agronomy_knowledge",
                        "pest_disease_library", "market_reports"]
    }
    # "v2": { "model": "bge-m3", ... }  # Khi upgrade
}

CURRENT_EMBEDDING_VERSION = "v1"

# Mỗi Qdrant point lưu version trong payload:
# "embedding_model": "text-embedding-3-large"
# "embedding_version": "v1"
# "embedded_at": "2026-03-19T06:00:00Z"
```

### 10.3 Reindex Strategy khi đổi Model

```
CHIẾN LƯỢC: Blue-Green Reindexing

Bước 1: Tạo collection mới (green) song song với collection cũ (blue)
  agronomy_knowledge_v2  ← re-embed toàn bộ với model mới
  agronomy_knowledge_v1  ← vẫn serving traffic

Bước 2: Re-embed dần dần (không block traffic)
  - Hangfire job: 1000 chunks/giờ
  - Ưu tiên collections hay được query nhất
  - Thời gian hoàn thành: ~50 giờ cho 50K chunks

Bước 3: Validation trước khi switch
  - Chạy evaluation set: recall@5 phải ≥ v1
  - Shadow testing: 5% query → cả v1 và v2, so sánh kết quả
  - Nếu v2 tốt hơn hoặc bằng → proceed

Bước 4: Atomic switch
  Đổi tên: agronomy_knowledge_v1 → agronomy_knowledge_v1_archive
            agronomy_knowledge_v2 → agronomy_knowledge

Bước 5: Giữ v1 collection thêm 7 ngày để rollback nếu cần
```

### 10.4 Scheduled Reindex

```python
# Hangfire job — chạy hàng tuần (Chủ nhật 2AM)
class ScheduledReindexJob:
    async def execute(self):
        # Tìm documents cần re-index
        stale_docs = await doc_registry.find_stale(
            older_than_days=90,              # Index lại sau 90 ngày
            or_model_version_older_than="v1" # Hoặc model đã cũ
        )

        if not stale_docs:
            logger.info("No stale documents found")
            return

        logger.info(f"Re-indexing {len(stale_docs)} stale documents")

        for doc in stale_docs:
            try:
                await indexing_pipeline.reindex_document(
                    doc_id=doc.doc_id,
                    minio_path=doc.minio_path,
                    collection=doc.collection,
                    force=True
                )
                # Throttle: không spam API
                await asyncio.sleep(0.5)
            except Exception as e:
                logger.error(f"Re-index failed for {doc.doc_id}: {e}")
                # Continue với document tiếp theo
```

---

## 11. Cost Analysis & Optimization

### 11.1 Chi phí Indexing (One-time + Incremental)

```
INDEXING COST ESTIMATE:

Phase 1 — Initial index:
  450 documents × 50 chunks avg = 22,500 chunks
  22,500 chunks × 400 tokens avg = 9,000,000 tokens
  Cost: 9,000,000 / 1,000,000 × $0.13 = $1.17 (~30,000 VND)
  ✅ Rất rẻ cho initial setup

Incremental (mỗi tháng ~10 tài liệu mới):
  10 docs × 50 chunks × 400 tokens = 200,000 tokens
  Cost: 200,000 / 1,000,000 × $0.13 = $0.026/tháng ≈ 650 VND/tháng
  ✅ Gần như không đáng kể
```

### 11.2 Chi phí Query Embedding (Ongoing)

```
QUERY COST ESTIMATE (1,000 nông dân):

Assumption:
  - 1,000 DAU (daily active users)
  - Avg 5 messages/session
  - Avg 15 tokens/message (query ngắn)
  - Cache hit rate: 40%

Daily tokens (without cache):
  1,000 users × 5 messages × 15 tokens = 75,000 tokens/ngày

After 40% cache:
  75,000 × 0.6 = 45,000 tokens/ngày cần embed

Daily cost: 45,000 / 1,000,000 × $0.13 = $0.006/ngày
Monthly cost: $0.18/tháng ≈ 4,500 VND/tháng cho 1,000 users
```

### 11.3 Optimization Roadmap

| Optimization | Savings | Effort | Phase |
|-------------|---------|--------|-------|
| Redis query cache (40% hit rate) | 40% API calls | Thấp | 1 |
| INT8 quantization trong Qdrant | 75% storage | Thấp | 1 |
| Matryoshka 1536d cho market_reports | 50% storage | Thấp | 1 |
| Batch indexing offline (3AM) | Rate limit cost | Thấp | 1 |
| Self-host bge-m3 (Phase 2+) | 100% API cost | Cao | 3 |
| Query result cache (Qdrant kết quả) | 30% Qdrant latency | Trung bình | 2 |

### 11.4 Cost Alert Threshold

```python
# Prometheus alert rule
COST_ALERT = """
alert: EmbeddingCostAnomaly
expr: |
  sum(increase(bazan_openai_tokens_total[1h])) > 500000
for: 5m
labels:
  severity: warning
annotations:
  summary: "Embedding token usage spike detected"
  description: "Used {{ $value }} tokens in last hour (normal: ~50,000)"
"""
# Alert nếu token usage tăng 10x so với baseline
# Có thể do: indexing job bị loop, query không được cache, bug gửi lặp
```

---

## 12. Evaluation & Benchmarking

### 12.1 Test Set cho Embedding Evaluation

```
Cấu trúc test set (200 cặp, do chuyên gia nông nghiệp tạo):

50 cặp — Disease diagnosis queries
  Query: "lá cà phê Robusta bị vàng, mặt dưới có bột cam"
  Expected chunks: [chunk_ids trong pest_disease_library về gỉ sắt]

50 cặp — Fertilizer queries (tiếng Việt)
  Query: "bón phân giai đoạn nuôi trái tháng 7"
  Expected chunks: [chunk_ids về fertilization, fruit development]

50 cặp — Cross-lingual queries (VI query → EN document)
  Query: "phương pháp chế biến ướt cà phê Arabica"
  Expected chunks: [chunk_ids từ WCR wet processing paper EN]

50 cặp — Technical term queries (keyword-heavy)
  Query: "NPK 16-16-8 TR4 liều lượng"
  Expected chunks: [chunk_ids chứa "NPK 16-16-8" VÀ "TR4"]
```

### 12.2 Metrics Computation

```python
async def evaluate_embedding_strategy(
    test_set: List[EvalCase],
    strategy: EmbeddingStrategy
) -> EvalReport:

    metrics = {
        "recall_at_1":  [],
        "recall_at_3":  [],
        "recall_at_5":  [],
        "mrr":          [],
        "ndcg_at_5":    [],
        "latency_ms":   [],
    }

    for case in test_set:
        start = time.monotonic()

        # Embed query với strategy đang test
        query_vector = await strategy.embed_query(case.query)

        # Search Qdrant
        results = await qdrant_client.query_points(
            collection_name=case.collection,
            query=query_vector,
            limit=5
        )

        latency_ms = (time.monotonic() - start) * 1000
        result_ids = [r.id for r in results.points]

        # Tính metrics
        for k in [1, 3, 5]:
            relevant_in_top_k = any(
                expected_id in result_ids[:k]
                for expected_id in case.expected_chunk_ids
            )
            metrics[f"recall_at_{k}"].append(int(relevant_in_top_k))

        metrics["mrr"].append(compute_mrr(result_ids, case.expected_chunk_ids))
        metrics["ndcg_at_5"].append(compute_ndcg(result_ids, case.expected_chunk_ids, k=5))
        metrics["latency_ms"].append(latency_ms)

    return EvalReport(
        recall_at_1=mean(metrics["recall_at_1"]),
        recall_at_3=mean(metrics["recall_at_3"]),
        recall_at_5=mean(metrics["recall_at_5"]),
        mrr=mean(metrics["mrr"]),
        ndcg_at_5=mean(metrics["ndcg_at_5"]),
        p50_latency=percentile(metrics["latency_ms"], 50),
        p95_latency=percentile(metrics["latency_ms"], 95),
    )
```

### 12.3 Target Metrics & Baselines

| Strategy | Recall@1 | Recall@3 | Recall@5 | MRR | P95 Latency |
|----------|---------|---------|---------|-----|------------|
| BM25 only (baseline) | 0.41 | 0.60 | 0.68 | 0.52 | 45ms |
| Dense only (ada-002) | 0.52 | 0.65 | 0.72 | 0.61 | 95ms |
| Dense only (3-large) | 0.58 | 0.71 | 0.78 | 0.67 | 85ms |
| **Hybrid RRF — Phase 1 target** | **0.65** | **0.78** | **0.85** | **0.74** | **<200ms** |
| Hybrid + Re-ranking — Phase 2 | 0.70 | 0.83 | 0.88 | 0.79 | <500ms |
| Hybrid + Fine-tuned — Phase 4 | 0.78 | 0.89 | 0.93 | 0.85 | <300ms |

---

## 13. Lộ trình nâng cấp Model

### 13.1 Phase 1 (Hiện tại) — OpenAI Managed

```
text-embedding-3-large (OpenAI API)
+ BM42 sparse (Qdrant native)
+ Redis query cache

Ưu điểm: Triển khai nhanh, không cần MLOps
Nhược điểm: Chi phí API tăng theo scale, phụ thuộc vendor
```

### 13.2 Phase 2 — Đánh giá Self-hosted

```
Khi nào xem xét self-host:
  □ Monthly embedding cost > $50
  □ Recall@5 < 0.80 sau 6 tháng (model không đủ tốt cho VI)
  □ Team có ML Engineer có kinh nghiệm model serving

Ứng viên self-hosted:
  1. bge-m3 (BAAI) — Recall VI tốt nhất hiện tại, 1024 dims
     Deployment: sentence-transformers + FastAPI + GPU t4
     Cost: ~$150/tháng GPU vs $5/tháng OpenAI

  2. multilingual-e5-large (Microsoft) — Production proven
     Deployment: tương tự bge-m3

  3. VinAI PhoBERT fine-tuned — VI-only, tốt nhất cho tiếng Việt thuần
     Nhược điểm: Không cross-lingual, WCR EN docs sẽ kém
```

### 13.3 Phase 4 — Fine-tuning cho Domain

```
Mục tiêu: Fine-tune embedding model trên corpus cà phê Việt Nam

Data cần thiết (tích lũy từ Phase 1-3):
  - 5,000+ cặp (query, relevant_chunk) từ RLHF feedback
  - 500+ cặp hard negatives (câu hỏi tương tự nhưng chunk khác nhau)

Framework:
  - MTEB fine-tuning pipeline
  - Sentence Transformers (supervised contrastive learning)
  - Base model: bge-m3 (multilingual base)

Kỳ vọng Recall@5 improvement: +5–8 điểm so với general model
```

---

*Tài liệu này được review mỗi quý hoặc khi thay đổi embedding model.*
*Mọi thay đổi model/dims/strategy phải qua: Evaluation → ADR → Blue-Green Reindex.*

**© 2026 Bazan AI Project — Confidential**

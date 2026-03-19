# RAG Pipeline Design Document
## Bazan AI — Knowledge Retrieval-Augmented Generation

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Principal Software Architect |
| **Người review** | AI/ML Lead, Backend Lead |
| **Trạng thái** | Draft — Pending Review |
| **Liên quan** | ADR-002 (Qdrant), ADR-004 (Semantic Kernel), System Architecture Document |

---

## Mục lục

1. [Tổng quan & Mục tiêu](#1-tổng-quan--mục-tiêu)
2. [Kiến trúc Pipeline tổng thể](#2-kiến-trúc-pipeline-tổng-thể)
3. [Indexing Pipeline — Đưa tri thức vào hệ thống](#3-indexing-pipeline--đưa-tri-thức-vào-hệ-thống)
4. [Retrieval Pipeline — Tìm kiếm tri thức](#4-retrieval-pipeline--tìm-kiếm-tri-thức)
5. [Generation Pipeline — Sinh câu trả lời](#5-generation-pipeline--sinh-câu-trả-lời)
6. [Hybrid Search — Thiết kế chi tiết](#6-hybrid-search--thiết-kế-chi-tiết)
7. [Qdrant Collection Schema](#7-qdrant-collection-schema)
8. [Chunking Strategy](#8-chunking-strategy)
9. [Metadata & Filtering](#9-metadata--filtering)
10. [Re-ranking & Context Compression](#10-re-ranking--context-compression)
11. [Evaluation Framework](#11-evaluation-framework)
12. [Giới hạn & Rủi ro](#12-giới-hạn--rủi-ro)

---

## 1. Tổng quan & Mục tiêu

### 1.1 Vấn đề cần giải quyết

Nông dân cà phê Tây Nguyên thường đặt câu hỏi mang tính **chuyên sâu, địa phương hóa**, và đòi hỏi kiến thức từ nhiều nguồn:

- "Cây TR4 của tôi bị vàng lá ở độ cao 700m, đất bazan, mùa khô — có phải thiếu magie không?"
- "Nên bón phân NPK hay hữu cơ cho giai đoạn nuôi trái tháng 8?"
- "WCR khuyến nghị gì về xử lý sau thu hoạch cho Robusta?"

Mô hình LLM (GPT-4o) có kiến thức tổng quát nhưng **thiếu hai loại thông tin:**
1. **Tri thức chuyên sâu nông nghiệp cà phê** — tài liệu WCR, kỹ thuật canh tác Tây Nguyên
2. **Dữ liệu real-time & cá nhân hóa** — giá thị trường, dữ liệu sensor của từng vườn

RAG (Retrieval-Augmented Generation) giải quyết vấn đề này bằng cách tìm kiếm đúng tài liệu liên quan, đưa vào context trước khi LLM sinh câu trả lời.

### 1.2 Mục tiêu thiết kế

| Mục tiêu | Metric mục tiêu |
|---------|----------------|
| **Độ chính xác retrieval** | Recall@5 ≥ 0.85 trên test set |
| **Độ liên quan** | NDCG@5 ≥ 0.80 |
| **Latency end-to-end** | P95 < 3 giây (retrieval + generation) |
| **Retrieval latency** | P95 < 500ms cho Qdrant hybrid search |
| **Không ảo giác (hallucination)** | < 5% câu trả lời có thông tin sai lệch |
| **Trả lời có nguồn trích dẫn** | 100% câu trả lời kèm document reference |
| **Hỗ trợ ngôn ngữ** | Tiếng Việt (chính), tiếng Anh (tài liệu gốc WCR) |

### 1.3 Phạm vi Knowledge Base

| Nguồn | Loại | Số lượng ước tính | Ngôn ngữ |
|-------|------|------------------|---------|
| WCR Research Papers | PDF | ~200 tài liệu | EN |
| Kỹ thuật canh tác Tây Nguyên | PDF, DOCX | ~80 tài liệu | VI |
| Bệnh & Sâu hại cà phê | PDF | ~50 tài liệu | VI, EN |
| Phân bón & Hóa chất nông nghiệp | PDF | ~40 tài liệu | VI |
| Báo cáo thị trường ICO/WCR | PDF | ~60 tài liệu/năm | EN |
| Quy trình chứng nhận (VietGAP, 4C) | PDF | ~20 tài liệu | VI |

---

## 2. Kiến trúc Pipeline tổng thể

```
╔══════════════════════════════════════════════════════════════╗
║                    INDEXING PIPELINE                         ║
║                    (Offline / On-demand)                     ║
║                                                              ║
║  MinIO          PDF Loader      Chunker      Embedder        ║
║  [PDF Files] ──► [Extract Text] ──► [Chunks] ──► [Vectors]  ║
║                       │                              │       ║
║                  [Metadata                      [Qdrant     ║
║                  Extractor]                      Upsert]    ║
╚══════════════════════════════════════════════════════════════╝

╔══════════════════════════════════════════════════════════════╗
║                    RETRIEVAL PIPELINE                        ║
║                    (Online / Real-time)                      ║
║                                                              ║
║  User Query                                                  ║
║      │                                                       ║
║      ├──► [Query Rewriter] ──► expanded_query               ║
║      │                                                       ║
║      ├──► [Dense Embedder] ──► query_vector (3072 dims)     ║
║      │                                                       ║
║      ├──► [Sparse Embedder] ──► sparse_vector (BM42)        ║
║      │                                                       ║
║      ├──► [Metadata Filter Builder] ──► payload_filter      ║
║      │                                                       ║
║      └──► Qdrant Hybrid Search (RRF Fusion)                 ║
║               │                                             ║
║               ├──► [Re-ranker] ──► top_k chunks             ║
║               │                                             ║
║               └──► [Context Compressor] ──► compressed_ctx  ║
╚══════════════════════════════════════════════════════════════╝

╔══════════════════════════════════════════════════════════════╗
║                    GENERATION PIPELINE                       ║
║                    (Online / Streaming)                      ║
║                                                              ║
║  compressed_ctx + user_query + farmer_context               ║
║      │                                                       ║
║      ▼                                                       ║
║  [Prompt Builder]                                           ║
║      │                                                       ║
║      ▼                                                       ║
║  GPT-4o (via Semantic Kernel)                               ║
║      │                                                       ║
║      ├──► [Stream tokens] ──► SignalR ──► Mobile Client     ║
║      │                                                       ║
║      └──► [Citation Extractor] ──► source references        ║
╚══════════════════════════════════════════════════════════════╝
```

---

## 3. Indexing Pipeline — Đưa tri thức vào hệ thống

### 3.1 Trigger

Indexing được kích hoạt bởi 3 nguồn:

```
1. Manual Upload
   Admin → Media Service → MinIO (knowledge-docs bucket)
       └──► RabbitMQ: NewDocumentUploaded event
               └──► Knowledge RAG Service: start indexing

2. Scheduled Re-index
   Scheduler Service → Hangfire Job (weekly, Sunday 2AM)
       └──► Knowledge RAG Service: /api/v1/knowledge/reindex

3. API Direct
   POST /api/v1/knowledge/index
   Body: { "minio_path": "knowledge-docs/wcr/2024/arabica-processing.pdf",
           "collection": "wcr_documents",
           "metadata": { "language": "en", "year": 2024 } }
```

### 3.2 Step-by-step Indexing Flow

```
Step 1: Download PDF từ MinIO
────────────────────────────
MinioClient.download_file(bucket="knowledge-docs", key=minio_path)
→ bytes (PDF binary)

Step 2: Extract Text
─────────────────────
PDFLoader(fitz/PyMuPDF).extract(bytes)
→ List[PageContent]
   {
     page_number: int,
     text: str,              # raw extracted text
     has_tables: bool,       # detected table regions
     has_images: bool,       # detected image regions
     language: "vi" | "en"  # detected per page
   }

Xử lý đặc biệt:
- Bảng (tables): chuyển sang text dạng "Cột A | Cột B | Cột C"
- Công thức hóa học: giữ nguyên string, không normalize
- Tiếng Việt: giữ dấu đầy đủ, không strip

Step 3: Chunking
─────────────────
RecursiveChunker.chunk(pages, strategy="recursive")
→ List[Chunk]
   {
     chunk_id: str,          # f"{doc_id}_chunk_{idx}"
     text: str,
     token_count: int,
     page_start: int,
     page_end: int,
     overlap_with_prev: str  # 64-token overlap text
   }

Step 4: Metadata Extraction
────────────────────────────
MetadataExtractor.extract(doc_text_sample, filename)
→ DocumentMetadata
   {
     coffee_varieties: List[str],   # ["Robusta", "TR4", "Arabica"]
     topics: List[str],             # ["fertilizer", "disease", "harvest"]
     region: List[str],             # ["Tây Nguyên", "Đắk Lắk"]
     growth_stages: List[str],      # ["flowering", "fruit_development"]
     document_type: str,            # "research_paper" | "guide" | "regulation"
     source_year: int | None,
     language: "vi" | "en" | "bilingual"
   }

Sử dụng OpenAI với prompt đặc biệt (xem Section 9.2)

Step 5: Generate Embeddings (parallel batch)
──────────────────────────────────────────────
For each chunk in chunks (batched, batch_size=20):
  dense_vector  = OpenAIEmbedder.embed(chunk.text)
                  # text-embedding-3-large → 3072 dims float32
  sparse_vector = SparseEmbedder.embed(chunk.text)
                  # BM42 → sparse indices + values

Step 6: Upsert vào Qdrant
───────────────────────────
QdrantIndexer.upsert_batch(collection, points)
Each point:
  {
    id: uuid5(doc_id + chunk_id),
    vector: {
      "dense":  dense_vector,      # 3072 floats
      "sparse": sparse_vector      # sparse indices
    },
    payload: {
      doc_id, chunk_id, text,
      page_start, page_end,
      token_count, overlap_with_prev,
      ...DocumentMetadata fields,
      minio_path, indexed_at
    }
  }

Step 7: Update Document Registry (MongoDB)
────────────────────────────────────────────
Save to bazan_knowledge.indexed_documents:
  {
    doc_id, minio_path, collection,
    total_chunks, total_tokens,
    metadata: DocumentMetadata,
    status: "indexed",
    indexed_at, updated_at
  }

Step 8: Publish Event
──────────────────────
RabbitMQ → bazan.knowledge.variety.indexed (nếu có variety mới)
RabbitMQ → bazan.knowledge.base.updated
```

### 3.3 Error Handling & Retry

```python
# Idempotent indexing — an toàn khi chạy lại
async def index_document(minio_path: str, collection: str) -> IndexResult:
    # Kiểm tra đã index chưa
    existing = await doc_registry.find_by_path(minio_path)
    if existing and existing.status == "indexed":
        logger.info("Document already indexed", doc_id=existing.doc_id)
        return IndexResult(skipped=True, doc_id=existing.doc_id)

    # Mark as in-progress để tránh race condition
    doc_id = await doc_registry.create(minio_path, status="indexing")

    try:
        # ... indexing steps ...
        await doc_registry.update(doc_id, status="indexed")
    except PDFExtractionError as e:
        await doc_registry.update(doc_id, status="failed", error=str(e))
        raise  # Sẽ được retry bởi RabbitMQ consumer (3 lần, exponential backoff)
    except QdrantUpsertError as e:
        # Rollback: xóa chunks đã upsert một phần
        await qdrant_indexer.delete_by_doc_id(doc_id, collection)
        await doc_registry.update(doc_id, status="failed", error=str(e))
        raise
```

---

## 4. Retrieval Pipeline — Tìm kiếm tri thức

### 4.1 Query Processing

```python
async def process_query(
    raw_query: str,
    farmer_context: FarmerContext,
    collection: str = "agronomy_knowledge"
) -> RetrievalResult:

    # Step 1: Query Rewriting
    # Mở rộng query ngắn, thêm context nông học
    expanded = await query_rewriter.rewrite(
        query=raw_query,
        context={
            "coffee_variety": farmer_context.coffee_variety,
            "soil_type":      farmer_context.soil_type,
            "region":         farmer_context.region,
            "growth_stage":   farmer_context.current_growth_stage
        }
    )
    # "vàng lá" → "vàng lá cà phê Robusta đất bazan Đắk Lắk triệu chứng nguyên nhân điều trị"

    # Step 2: Parallel Embedding Generation
    dense_vec, sparse_vec = await asyncio.gather(
        openai_embedder.embed(expanded.text),
        sparse_embedder.embed(expanded.text)
    )

    # Step 3: Build Payload Filter
    payload_filter = build_filter(farmer_context, collection)

    # Step 4: Hybrid Search
    results = await hybrid_search_service.search(
        collection=collection,
        dense_vector=dense_vec,
        sparse_vector=sparse_vec,
        payload_filter=payload_filter,
        limit=20  # lấy nhiều để re-rank
    )

    # Step 5: Re-ranking
    reranked = await reranking_service.rerank(
        query=raw_query,
        chunks=results,
        farmer_context=farmer_context,
        top_k=5
    )

    # Step 6: Context Compression
    compressed = await context_compressor.compress(
        chunks=reranked,
        query=raw_query,
        max_tokens=2000
    )

    return RetrievalResult(
        chunks=compressed,
        sources=extract_citations(reranked),
        retrieval_metadata={
            "expanded_query": expanded.text,
            "total_candidates": len(results),
            "collection": collection,
            "filter_applied": payload_filter is not None
        }
    )
```

### 4.2 Multi-collection Strategy

Tùy theo intent của câu hỏi, retrieval chạy trên collection khác nhau hoặc kết hợp:

```python
INTENT_TO_COLLECTIONS = {
    "disease_diagnosis":    ["pest_disease_library", "agronomy_knowledge"],
    "fertilizer_advice":    ["agronomy_knowledge", "wcr_documents"],
    "market_question":      ["market_reports"],
    "harvest_timing":       ["agronomy_knowledge", "wcr_documents"],
    "certification_query":  ["agronomy_knowledge"],
    "general_agronomy":     ["agronomy_knowledge", "wcr_documents"],
}

async def multi_collection_search(query, intent, farmer_context):
    collections = INTENT_TO_COLLECTIONS.get(intent, ["agronomy_knowledge"])

    tasks = [
        retrieve_from_collection(query, col, farmer_context)
        for col in collections
    ]
    results_per_collection = await asyncio.gather(*tasks)

    # Merge và re-rank across collections
    merged = merge_results(results_per_collection, strategy="rrf")
    return await reranking_service.rerank(query, merged, farmer_context, top_k=5)
```

---

## 5. Generation Pipeline — Sinh câu trả lời

### 5.1 Prompt Template

```python
SYSTEM_PROMPT = """
Bạn là Bazan AI — trợ lý nông nghiệp chuyên về cà phê Tây Nguyên, Việt Nam.

NGUYÊN TẮC TRẢ LỜI:
1. Chỉ trả lời dựa trên TÀI LIỆU THAM KHẢO được cung cấp bên dưới.
2. Nếu tài liệu không đủ thông tin, nói rõ: "Tôi chưa có đủ dữ liệu về vấn đề này."
3. LUÔN trích dẫn nguồn theo định dạng [Nguồn: <tên tài liệu>].
4. Dùng ngôn ngữ đơn giản, gần gũi với nông dân — tránh thuật ngữ khó hiểu.
5. Nếu có đề xuất hành động, liệt kê rõ ràng theo bước.
6. Lưu ý điều kiện cụ thể của vườn nông dân (đất, giống, thời tiết) khi đưa ra khuyến nghị.

THÔNG TIN VƯỜN NÔNG DÂN:
- Tên: {farmer_name}
- Giống cà phê: {coffee_variety}
- Loại đất: {soil_type}
- Địa bàn: {region}
- Diện tích: {area_ha} ha
- Giai đoạn sinh trưởng hiện tại: {growth_stage}
- Dữ liệu cảm biến gần nhất: Độ ẩm đất {soil_moisture}%, Nhiệt độ {temperature}°C

TÀI LIỆU THAM KHẢO:
{retrieved_context}
"""

USER_PROMPT = """
Câu hỏi của nông dân: {user_query}

Lịch sử hội thoại gần nhất:
{conversation_history}

Hãy trả lời câu hỏi trên dựa vào tài liệu tham khảo và thông tin vườn của nông dân.
"""
```

### 5.2 Context Assembly

```python
def build_retrieved_context(chunks: List[CompressedChunk]) -> str:
    """
    Định dạng retrieved chunks thành text block cho LLM.
    Mỗi chunk kèm source citation để LLM reference.
    """
    context_parts = []
    for i, chunk in enumerate(chunks, 1):
        context_parts.append(
            f"[Tài liệu {i}] "
            f"(Nguồn: {chunk.source_title}, Trang {chunk.page_start}–{chunk.page_end})\n"
            f"{chunk.text}\n"
            f"---"
        )
    return "\n".join(context_parts)
```

### 5.3 Streaming Response

```python
# Trong Semantic Kernel Agent (BazanAI.Orchestrator)
async def stream_rag_response(
    session_id: str,
    user_query: str,
    farmer_context: FarmerContext,
    signalr_hub: IHubContext
):
    # 1. Retrieve
    retrieval_result = await knowledge_rag_client.search_context(
        query=user_query,
        farmer_context=farmer_context
    )

    # 2. Build prompt
    prompt = build_prompt(user_query, retrieval_result, farmer_context)

    # 3. Stream generation
    full_response = []
    async for token in kernel.invoke_stream(prompt):
        full_response.append(token)
        # Gửi token về client qua SignalR
        await signalr_hub.Clients.Client(session_id).SendAsync(
            "ReceiveToken", token.Content
        )

    # 4. Lưu message vào Conversation Service
    await conversation_client.add_message(
        session_id=session_id,
        role="assistant",
        content="".join(full_response),
        sources=retrieval_result.sources,
        context_used=retrieval_result.retrieval_metadata
    )

    # 5. Signal kết thúc stream
    await signalr_hub.Clients.Client(session_id).SendAsync(
        "StreamComplete",
        {"sources": retrieval_result.sources}
    )
```

---

## 6. Hybrid Search — Thiết kế chi tiết

### 6.1 Tại sao Hybrid Search?

| Loại Search | Tốt cho | Không tốt cho |
|------------|---------|---------------|
| **Dense only** (semantic) | Câu hỏi ngữ nghĩa, paraphrase | Tên kỹ thuật cụ thể ("NPK 16-16-8", "TR4"), mã bệnh |
| **Sparse only** (keyword) | Tên cụ thể, mã số, term kỹ thuật | Câu hỏi diễn đạt khác nhau cùng ý nghĩa |
| **Hybrid (RRF)** | Cả hai loại câu hỏi | — |

Với nông dân Tây Nguyên, câu hỏi thường xen lẫn: "cây TR4 bị gỉ sắt" — vừa có tên kỹ thuật (TR4, gỉ sắt) vừa cần semantic matching (triệu chứng, điều trị).

### 6.2 Reciprocal Rank Fusion (RRF)

```python
def reciprocal_rank_fusion(
    dense_results: List[ScoredPoint],
    sparse_results: List[ScoredPoint],
    k: int = 60,
    dense_weight: float = 0.7,
    sparse_weight: float = 0.3
) -> List[ScoredPoint]:
    """
    RRF score = Σ weight_i / (k + rank_i)

    k=60 là hằng số chuẩn của RRF, giảm ảnh hưởng của outlier
    dense_weight cao hơn vì câu hỏi nông nghiệp thường semantic
    """
    scores = defaultdict(float)
    id_to_point = {}

    for rank, point in enumerate(dense_results):
        scores[point.id] += dense_weight / (k + rank + 1)
        id_to_point[point.id] = point

    for rank, point in enumerate(sparse_results):
        scores[point.id] += sparse_weight / (k + rank + 1)
        id_to_point[point.id] = point

    sorted_ids = sorted(scores.keys(), key=lambda x: scores[x], reverse=True)
    return [id_to_point[pid] for pid in sorted_ids]
```

### 6.3 Qdrant Hybrid Search API Call

```python
async def hybrid_search(
    collection: str,
    dense_vector: List[float],
    sparse_vector: SparseVector,
    payload_filter: Filter | None,
    limit: int = 20
) -> List[ScoredPoint]:

    return await qdrant_client.query_points(
        collection_name=collection,
        prefetch=[
            # Dense search — semantic similarity
            Prefetch(
                query=dense_vector,
                using="dense",
                limit=limit,
                filter=payload_filter
            ),
            # Sparse search — keyword matching (BM42)
            Prefetch(
                query=models.SparseVector(
                    indices=sparse_vector.indices,
                    values=sparse_vector.values
                ),
                using="sparse",
                limit=limit,
                filter=payload_filter
            ),
        ],
        # RRF Fusion của Qdrant — tương đương triển khai thủ công
        query=models.FusionQuery(fusion=models.Fusion.RRF),
        limit=limit,
        with_payload=True,
        with_vectors=False  # Không cần vector trong response
    )
```

---

## 7. Qdrant Collection Schema

### 7.1 Collection: `agronomy_knowledge`

```python
await qdrant_client.create_collection(
    collection_name="agronomy_knowledge",
    vectors_config={
        "dense": VectorParams(
            size=3072,
            distance=Distance.COSINE,
            on_disk=True,             # Tiết kiệm RAM
            hnsw_config=HnswConfigDiff(
                m=16,                  # Số kết nối mỗi node
                ef_construct=200,      # Chất lượng index (build time)
                full_scan_threshold=10000
            )
        )
    },
    sparse_vectors_config={
        "sparse": SparseVectorParams(
            index=SparseIndexParams(
                on_disk=True
            )
        )
    },
    optimizers_config=OptimizersConfigDiff(
        indexing_threshold=20000,      # Index sau khi có 20k vectors
        memmap_threshold=50000
    ),
    quantization_config=ScalarQuantization(
        scalar=ScalarQuantizationConfig(
            type=ScalarType.INT8,      # 4x compression, ~1% accuracy loss
            quantile=0.99,
            always_ram=True            # Quantized vectors luôn trong RAM
        )
    )
)
```

**Payload Index** (để filter nhanh):

```python
# Index các trường thường dùng để filter
for field, schema in [
    ("coffee_variety",  PayloadSchemaType.KEYWORD),
    ("topics",          PayloadSchemaType.KEYWORD),
    ("region",          PayloadSchemaType.KEYWORD),
    ("growth_stages",   PayloadSchemaType.KEYWORD),
    ("document_type",   PayloadSchemaType.KEYWORD),
    ("language",        PayloadSchemaType.KEYWORD),
    ("source_year",     PayloadSchemaType.INTEGER),
    ("indexed_at",      PayloadSchemaType.DATETIME),
]:
    await qdrant_client.create_payload_index(
        collection_name="agronomy_knowledge",
        field_name=field,
        field_schema=schema
    )
```

### 7.2 Payload Schema (mỗi điểm vector)

```json
{
  "doc_id":           "wcr-arabica-processing-2024",
  "chunk_id":         "wcr-arabica-processing-2024_chunk_003",
  "minio_path":       "knowledge-docs/wcr/2024/arabica-processing.pdf",
  "source_title":     "Arabica Processing Methods — WCR Technical Report 2024",
  "source_url":       "https://worldcoffeeresearch.org/...",
  "text":             "Phương pháp chế biến ướt giúp giữ lại độ chua tự nhiên...",
  "token_count":      487,
  "page_start":       12,
  "page_end":         14,
  "overlap_with_prev": "...fermentation temperature control is critical",
  "coffee_varieties": ["Arabica", "Bourbon", "Caturra"],
  "topics":           ["processing", "fermentation", "quality", "wet_process"],
  "region":           ["Central Highlands", "Lâm Đồng"],
  "growth_stages":    ["post_harvest"],
  "document_type":    "research_paper",
  "source_year":      2024,
  "language":         "en",
  "indexed_at":       "2026-03-19T06:00:00Z",
  "schema_version":   "1.0"
}
```

### 7.3 Tất cả Collections

| Collection | Mô tả | Vector dims | Dung lượng ước tính |
|-----------|-------|------------|---------------------|
| `wcr_documents` | Tài liệu nghiên cứu WCR | 3072 | ~2GB |
| `agronomy_knowledge` | Hướng dẫn canh tác tổng hợp | 3072 | ~1.5GB |
| `pest_disease_library` | Sâu bệnh, triệu chứng, phác đồ | 3072 | ~500MB |
| `market_reports` | Báo cáo thị trường, phân tích giá | 3072 | ~800MB |

---

## 8. Chunking Strategy

### 8.1 Recursive Character Text Splitter

```python
class RecursiveChunker:
    # Thứ tự ưu tiên tách: đoạn văn → câu → dòng → ký tự
    SEPARATORS = ["\n\n", "\n", ".", "?", "!", " ", ""]

    CHUNK_SIZE   = 512   # tokens (~400 từ tiếng Việt / ~380 từ tiếng Anh)
    CHUNK_OVERLAP = 64   # tokens (~50 từ) — giúp không mất context tại ranh giới

    def chunk(self, pages: List[PageContent]) -> List[Chunk]:
        full_text = "\n\n".join(p.text for p in pages)
        raw_chunks = self._split(full_text, self.CHUNK_SIZE, self.CHUNK_OVERLAP)

        return [
            Chunk(
                chunk_id=f"{doc_id}_chunk_{i:04d}",
                text=chunk,
                token_count=self._count_tokens(chunk),
                page_start=self._find_page(chunk, pages),
                page_end=self._find_page_end(chunk, pages),
            )
            for i, chunk in enumerate(raw_chunks)
        ]
```

### 8.2 Quyết định Chunk Size

Chunk size 512 token được chọn dựa trên trade-off:

| Chunk Size | Ưu điểm | Nhược điểm |
|-----------|---------|------------|
| 256 tokens | Retrieval chính xác hơn | Mất context, câu trả lời thiếu đầy đủ |
| **512 tokens** | **Cân bằng tốt nhất** | **—** |
| 1024 tokens | Context đầy đủ hơn | Noise cao, retrieval kém chính xác |
| 2048 tokens | Giữ nguyên đoạn văn | Khó match với câu hỏi ngắn |

Với tài liệu bảng (fertilizer tables, disease charts): tách theo bảng, không chia nhỏ hơn hàng.

### 8.3 Xử lý đặc biệt

```python
# Bảng → chuyển thành text có cấu trúc
def process_table(table: TableContent) -> str:
    rows = []
    headers = " | ".join(table.headers)
    rows.append(headers)
    rows.append("-" * len(headers))
    for row in table.rows:
        rows.append(" | ".join(str(cell) for cell in row))
    return "\n".join(rows)

# Tên kỹ thuật → giữ nguyên, không lemmatize
# "NPK 16-16-8", "TR4", "Carbendazim 50SC" → không thay đổi

# Số đo → giữ đơn vị
# "6.2 pH", "700m", "1.5 ha" → không tách
```

---

## 9. Metadata & Filtering

### 9.1 Farmer Context → Payload Filter

```python
def build_filter(farmer_context: FarmerContext) -> Filter | None:
    """
    Xây dựng Qdrant filter từ context nông dân.
    Filter được áp dụng TRƯỚC khi vector search — giảm search space,
    tăng tốc độ và độ liên quan.
    """
    conditions = []

    # Lọc theo giống cà phê (nếu câu hỏi liên quan đến giống cụ thể)
    if farmer_context.coffee_variety:
        conditions.append(
            FieldCondition(
                key="coffee_varieties",
                match=MatchAny(any=farmer_context.coffee_variety + ["general"])
            )
        )

    # Lọc theo ngôn ngữ — ưu tiên tiếng Việt
    conditions.append(
        FieldCondition(
            key="language",
            match=MatchAny(any=["vi", "bilingual"])
        )
    )

    # Lọc theo giai đoạn sinh trưởng (nếu có)
    if farmer_context.current_growth_stage:
        conditions.append(
            FieldCondition(
                key="growth_stages",
                match=MatchAny(any=[
                    farmer_context.current_growth_stage,
                    "general"
                ])
            )
        )

    if not conditions:
        return None

    return Filter(must=conditions)
```

### 9.2 Metadata Extraction Prompt

```python
METADATA_EXTRACTION_PROMPT = """
Phân tích đoạn văn bản từ tài liệu nông nghiệp cà phê và trích xuất metadata.
Trả về JSON thuần túy, không markdown.

Văn bản (300 từ đầu của tài liệu):
{text_sample}

Tên file: {filename}

Trả về JSON với cấu trúc sau:
{
  "coffee_varieties": [],     // Chỉ dùng: "Robusta", "Arabica", "TR4", "Catimor", "Bourbon", "general"
  "topics": [],               // Chỉ dùng: "disease", "fertilizer", "irrigation", "harvest",
                              //           "processing", "soil", "pest_control", "variety",
                              //           "market", "certification", "general"
  "region": [],               // Chỉ dùng: "Tây Nguyên", "Đắk Lắk", "Lâm Đồng", "Gia Lai",
                              //           "Đắk Nông", "Kon Tum", "Vietnam", "global"
  "growth_stages": [],        // Chỉ dùng: "seedling", "vegetative", "flowering",
                              //           "fruit_development", "ripening", "post_harvest", "general"
  "document_type": "",        // Chỉ dùng: "research_paper", "guide", "regulation", "report"
  "source_year": null,        // Năm xuất bản (số nguyên) hoặc null
  "language": ""              // Chỉ dùng: "vi", "en", "bilingual"
}
"""
```

---

## 10. Re-ranking & Context Compression

### 10.1 Re-ranking

Sau Hybrid Search, top-20 chunks được re-rank bằng cross-encoder để chọn top-5:

```python
class ReRankingService:
    """
    Cross-encoder re-ranking: đánh giá từng cặp (query, chunk) 
    thay vì chỉ dùng vector distance.
    Chính xác hơn nhưng chậm hơn → chỉ dùng sau khi đã filter top-20.
    """

    async def rerank(
        self,
        query: str,
        chunks: List[ScoredPoint],
        farmer_context: FarmerContext,
        top_k: int = 5
    ) -> List[RankedChunk]:

        # Cross-encoder score với OpenAI (1 call cho toàn bộ)
        pairs = [(query, chunk.payload["text"]) for chunk in chunks]

        rerank_prompt = f"""
Câu hỏi: {query}
Thông tin vườn: giống {farmer_context.coffee_variety}, đất {farmer_context.soil_type}

Đánh giá mức độ liên quan của mỗi đoạn văn với câu hỏi (0.0 đến 1.0).
Ưu tiên cao hơn nếu đoạn văn đề cập cụ thể đến:
- Giống cà phê của nông dân
- Điều kiện đất và khí hậu Tây Nguyên
- Có số liệu cụ thể, liều lượng, thời gian

Trả về JSON: [{{"index": 0, "score": 0.95}}, ...]

Các đoạn văn:
{format_chunks_for_reranking(pairs)}
"""

        scores = await llm_client.get_rerank_scores(rerank_prompt)
        ranked = sorted(
            zip(chunks, scores),
            key=lambda x: x[1]["score"],
            reverse=True
        )
        return [RankedChunk(chunk=c, relevance_score=s["score"])
                for c, s in ranked[:top_k]]
```

### 10.2 Context Compression

Sau re-ranking, nén chunks để tối ưu token budget:

```python
class ContextCompressor:
    MAX_CONTEXT_TOKENS = 2000  # Budget cho retrieved context

    async def compress(
        self,
        chunks: List[RankedChunk],
        query: str,
        max_tokens: int = MAX_CONTEXT_TOKENS
    ) -> List[CompressedChunk]:
        """
        Nếu tổng tokens của top-5 chunks > max_tokens:
        - Tóm tắt những chunks có score thấp hơn
        - Giữ nguyên chunks có score cao (thông tin quan trọng)
        """
        total_tokens = sum(c.chunk.payload["token_count"] for c in chunks)

        if total_tokens <= max_tokens:
            return [CompressedChunk.from_ranked(c) for c in chunks]

        # Giữ nguyên top-3 chunks quan trọng nhất
        keep_full = chunks[:3]
        to_compress = chunks[3:]

        compressed_parts = []
        for chunk in to_compress:
            summary = await self._summarize_for_query(
                chunk.chunk.payload["text"],
                query,
                max_summary_tokens=100
            )
            compressed_parts.append(CompressedChunk(
                text=summary,
                source_title=chunk.chunk.payload["source_title"],
                page_start=chunk.chunk.payload["page_start"],
                page_end=chunk.chunk.payload["page_end"],
                is_compressed=True
            ))

        return [CompressedChunk.from_ranked(c) for c in keep_full] + compressed_parts
```

---

## 11. Evaluation Framework

### 11.1 Offline Evaluation (Test Set)

**Test set:** 200 cặp (câu hỏi, tài liệu đúng) được tạo bởi chuyên gia nông nghiệp.

```python
EVALUATION_METRICS = {
    # Retrieval metrics
    "recall_at_5":     "Tỉ lệ câu hỏi mà tài liệu đúng nằm trong top-5 kết quả",
    "ndcg_at_5":       "Normalized Discounted Cumulative Gain — đánh giá thứ tự kết quả",
    "mrr":             "Mean Reciprocal Rank — thứ hạng trung bình của kết quả đúng đầu tiên",

    # Generation metrics
    "answer_relevance": "RAGAS score — câu trả lời có trả lời đúng câu hỏi không?",
    "faithfulness":     "RAGAS score — câu trả lời có trung thực với context không?",
    "context_precision": "Tỉ lệ retrieved chunks thực sự được dùng trong câu trả lời",
    "context_recall":   "Tỉ lệ thông tin cần thiết có trong retrieved chunks",
}

async def evaluate_pipeline(test_set: List[EvalCase]) -> EvalReport:
    results = []
    for case in test_set:
        # Chạy retrieval
        retrieved = await retrieval_pipeline.retrieve(
            query=case.question,
            farmer_context=case.farmer_context
        )
        # Kiểm tra recall
        relevant_found = any(
            chunk.doc_id == case.expected_doc_id
            for chunk in retrieved.chunks
        )
        results.append({
            "recall":        int(relevant_found),
            "reciprocal_rank": compute_mrr(retrieved.chunks, case.expected_doc_id),
            "ndcg":          compute_ndcg(retrieved.chunks, case.expected_doc_id),
        })
    return aggregate_metrics(results)
```

### 11.2 Online Evaluation (Production)

```python
# Thu thập từ feedback nông dân (RLHF signal)
# Mỗi câu trả lời có nút 👍 / 👎

# Log tự động cho mỗi RAG call
RAG_TRACE_LOG = {
    "session_id":        str,
    "query":             str,
    "expanded_query":    str,
    "collection_used":   str,
    "chunks_retrieved":  int,
    "chunks_after_rerank": int,
    "top_chunk_score":   float,
    "retrieval_ms":      int,
    "generation_ms":     int,
    "total_tokens_used": int,
    "farmer_feedback":   "positive" | "negative" | None,
    "timestamp":         datetime,
}

# Grafana alert nếu:
# - Tỉ lệ negative feedback > 20% trong 1 giờ
# - P95 retrieval latency > 500ms
# - top_chunk_score < 0.6 (retrieval kém)
```

### 11.3 Baselines so sánh

| Pipeline Variant | Recall@5 (target) | Ghi chú |
|-----------------|-----------------|---------|
| Dense only (no hybrid) | 0.72 | Baseline |
| Sparse only (BM42) | 0.68 | Baseline |
| **Hybrid RRF (current)** | **0.85** | **Target** |
| Hybrid + Re-ranking | 0.88 | Phase 2 |
| Hybrid + Fine-tuned Embeddings | 0.92 | Phase 4 |

---

## 12. Giới hạn & Rủi ro

### 12.1 Giới hạn hiện tại

**Ngôn ngữ:** Embedding model `text-embedding-3-large` được tối ưu cho tiếng Anh. Với tài liệu tiếng Việt, chất lượng embedding có thể thấp hơn 10–15% so với tiếng Anh. Giảm thiểu bằng cách ưu tiên index tài liệu tiếng Việt, và cân nhắc dùng `multilingual-e5-large` cho Phase 2.

**Tài liệu bảng & hình ảnh:** PyMuPDF extract text tốt nhưng mất thông tin từ hình ảnh (biểu đồ, sơ đồ bệnh). Phase 3 sẽ bổ sung multimodal indexing.

**Knowledge cutoff:** Kho dữ liệu phản ánh thời điểm index. Giá thị trường và dự báo thời tiết không đi qua RAG mà qua Market Service và Weather Service trực tiếp.

**Chunking cứng:** Việc chia theo token count đôi khi cắt giữa ý. Phiên bản sau sẽ thử semantic chunking dùng sentence embeddings.

### 12.2 Rủi ro vận hành

| Rủi ro | Mức độ | Giảm thiểu |
|--------|--------|------------|
| Qdrant OOM khi collection lớn | Cao | Bật `on_disk=True`, INT8 quantization |
| OpenAI API rate limit khi batch index | Trung bình | Exponential backoff, queue-based batching |
| Hallucination khi context không đủ | Cao | Prompt strict: "chỉ trả lời từ tài liệu" |
| Stale knowledge (tài liệu cũ) | Trung bình | Weekly re-index job, version tracking |
| PDF quality kém (scan, OCR lỗi) | Trung bình | Pre-processing OCR với Tesseract cho PDF scan |

---

*Tài liệu này là living document — cập nhật khi pipeline thay đổi hoặc có kết quả evaluation mới.*

**© 2026 Bazan AI Project — Confidential**

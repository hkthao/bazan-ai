# Knowledge Base Content Strategy
## Bazan AI — Kế hoạch xây dựng & Quản lý Knowledge Base

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | AI/ML Lead · Chuyên gia nông nghiệp |
| **Người review** | WASI Collaboration Team, Knowledge RAG Team |
| **Liên quan** | RAG Pipeline Design, Embedding Strategy, Agronomy Knowledge Taxonomy |

---

## 1. Tầm nhìn

Knowledge Base của Bazan AI là **nguồn tri thức nông nghiệp cà phê đáng tin cậy nhất bằng tiếng Việt**, được tổng hợp từ nghiên cứu quốc tế (WCR) và thực tiễn canh tác Tây Nguyên, có thể truy xuất real-time bởi AI Agent để trả lời câu hỏi nông dân.

---

## 2. Taxonomy nội dung

### 2.1 Phân cấp chủ đề

```
Cà phê Tây Nguyên
├── Kỹ thuật canh tác
│   ├── Đất & Phân bón
│   │   ├── Đặc điểm đất bazan Tây Nguyên
│   │   ├── pH và điều chỉnh đất
│   │   ├── Phân bón NPK theo giai đoạn
│   │   ├── Phân hữu cơ và vi sinh
│   │   └── Phân tích đất và giải thích kết quả
│   ├── Nước & Tưới tiêu
│   │   ├── Lịch tưới theo mùa Tây Nguyên
│   │   ├── Phương pháp tưới (nhỏ giọt, phun mưa)
│   │   ├── Chỉ số ẩm độ đất và ngưỡng tưới
│   │   └── Quản lý nước mưa và thoát nước
│   ├── Giống cà phê
│   │   ├── Robusta (đặc tính, năng suất, thích nghi)
│   │   ├── Arabica (Di Linh, Cầu Đất)
│   │   ├── TR4, TR5A, TR9, TR11, TR12 (giống kháng bệnh)
│   │   ├── Catimor và lai ghép
│   │   └── Chọn giống theo vùng và độ cao
│   ├── Quản lý cây
│   │   ├── Tạo hình, cắt tỉa
│   │   ├── Trồng mới và tái canh
│   │   ├── Cây che bóng và xen canh
│   │   └── Quản lý giai đoạn sinh trưởng
│   └── Thu hoạch & Chế biến
│       ├── Xác định độ chín
│       ├── Phương pháp thu hoạch
│       ├── Chế biến ướt (washed)
│       ├── Chế biến khô (natural)
│       └── Bảo quản sau thu hoạch
├── Sâu bệnh & Bảo vệ thực vật
│   ├── Bệnh nấm
│   │   ├── Gỉ sắt (Hemileia vastatrix)
│   │   ├── Khô cành (Phoma costaricensis)
│   │   ├── Thán thư (Colletotrichum)
│   │   └── Thối rễ (Phytophthora)
│   ├── Côn trùng gây hại
│   │   ├── Mọt đục quả (Hypothenemus hampei)
│   │   ├── Rệp sáp (Planococcus citri)
│   │   ├── Sâu đục thân (Xylotrechus)
│   │   └── Ve nhện đỏ
│   ├── Bệnh sinh lý (thiếu dinh dưỡng)
│   │   ├── Thiếu N, P, K
│   │   ├── Thiếu Mg, Ca, B
│   │   └── Vàng lá (nguyên nhân và phân biệt)
│   └── Thuốc BVTV
│       ├── Danh mục được phép sử dụng (Bộ NN&PTNT)
│       ├── Liều lượng và cách pha
│       └── Thời gian cách ly trước thu hoạch
├── Kinh tế & Thị trường
│   ├── Giá cà phê thế giới và trong nước
│   ├── Phân tích chi phí — lợi nhuận
│   ├── Chứng nhận chất lượng (VietGAP, 4C, UTZ, Organic)
│   └── Thị trường xuất khẩu
└── Ứng phó biến đổi khí hậu
    ├── Thích nghi với hạn hán
    ├── Ứng phó mưa bất thường
    └── Giống kháng biến đổi khí hậu
```

---

## 3. Nguồn tài liệu & Ưu tiên Index

### 3.1 Tier 1 — Priority (index ngay Phase 1)

| Nguồn | Số lượng | Trạng thái | URL/Contact |
|-------|---------|-----------|------------|
| WCR Arabica Varieties Catalog | 1 tài liệu (~200 trang) | Cần mua license | worldcoffeeresearch.org |
| WCR Leaf Rust Management | 15 technical reports | Open access | wcr.org/resources |
| WCR Agronomy Guides | 20 guides | Open access | wcr.org |
| WASI — Kỹ thuật canh tác Robusta | 8 tài liệu | Cần liên hệ WASI | 0262 3831 056 |
| WASI — Sâu bệnh cà phê | 12 tài liệu | Cần liên hệ WASI | |
| Bộ NN&PTNT — Quy trình kỹ thuật | 5 quy trình | Public domain | mard.gov.vn |
| Danh mục thuốc BVTV 2024 | 1 tài liệu (quan trọng nhất) | Public domain | |

### 3.2 Tier 2 — Important (index Phase 2)

| Nguồn | Số lượng | Ghi chú |
|-------|---------|---------|
| ICO Annual Coffee Reports | 5 năm × 1 = 5 | Market context |
| Nescafé Plan Vietnam Reports | 10 tài liệu | Sustainability practices |
| CBI Coffee Market Intelligence | 6 tài liệu | Export market |
| Cupping protocols (SCA) | 3 tài liệu | Quality grading |
| VietGAP Coffee Standard | 1 tài liệu | Certification |
| 4C Coffee Standard | 1 tài liệu | Certification |

### 3.3 Tier 3 — Supplementary (index Phase 3)

| Nguồn | Số lượng | Ghi chú |
|-------|---------|---------|
| Peer-reviewed research papers | ~50 | Từ Scopus/Google Scholar |
| WASI Internal Research Reports | ~30 | Cần MOU chính thức |
| Local farmer case studies | ~20 | Do Bazan AI team tổng hợp |
| Climate adaptation guides | ~10 | FAO, CGIAR |

---

## 4. Document Preparation Workflow

### 4.1 Quy trình chuẩn bị trước khi index

```
Bước 1: Tiếp nhận tài liệu
  ├─ PDF từ nguồn chính thức → scan quality check
  ├─ Scan PDF → OCR với Tesseract nếu cần
  └─ DOCX/HTML → convert sang PDF

Bước 2: Quality Check thủ công
  ├─ Xác nhận tác giả và tổ chức phát hành
  ├─ Kiểm tra năm xuất bản (ưu tiên < 5 năm)
  ├─ Đánh giá relevance: có đặc thù Tây Nguyên không?
  └─ Flag tài liệu có thể outdated (thuốc cũ, giống cũ)

Bước 3: Metadata annotation (thủ công)
  ├─ coffee_varieties: [Robusta, Arabica, TR4, ...]
  ├─ topics: [disease, fertilizer, irrigation, ...]
  ├─ region: [Tây Nguyên, Đắk Lắk, ...]
  ├─ growth_stages: [flowering, fruit_development, ...]
  ├─ document_type: research_paper | guide | regulation | report
  └─ source_year: int

Bước 4: Upload lên MinIO
  Bucket: knowledge-docs
  Key: {tier}/{category}/{year}/{filename}.pdf

Bước 5: Trigger indexing
  POST /api/v1/knowledge/index
  {
    "minio_path": "tier1/disease/2024/wcr-leaf-rust-2024.pdf",
    "collection": "pest_disease_library",
    "metadata": {...}  // từ Bước 3
  }

Bước 6: Quality validation
  ├─ Chạy 5 câu hỏi test → xem tài liệu có được retrieve không
  └─ Nếu recall < 0.6 → xem xét re-chunk hoặc improve metadata
```

### 4.2 Naming Convention

```
Tier 1 (Priority):
  tier1/{category}/{year}/{source}_{title_slug}.pdf

Ví dụ:
  tier1/disease/2024/wcr_leaf-rust-management-guide.pdf
  tier1/fertilizer/2023/wasi_ky-thuat-bon-phan-robusta.pdf
  tier1/regulations/2024/mard_danh-muc-thuoc-bvtv.pdf

Tier 2:
  tier2/{category}/{year}/{source}_{title_slug}.pdf

Tier 3:
  tier3/{category}/{year}/{source}_{title_slug}.pdf
```

---

## 5. Content Gap Analysis

### 5.1 Khoảng trống cần ưu tiên lấp

**Rất cấp bách:**
- Hướng dẫn xử lý bệnh **gỉ sắt** chi tiết theo từng giống (TR4 khác Robusta truyền thống)
- Lịch bón phân cụ thể cho **độ cao 600–900m** Tây Nguyên (hiện tài liệu chỉ có chung)
- Nhận biết và xử lý **rệp sáp** — phổ biến nhất vùng Đắk Lắk nhưng ít tài liệu tiếng Việt

**Cấp bách:**
- Kỹ thuật canh tác cho giống **TR9, TR11** (mới, nông dân chuyển đổi nhiều)
- Hướng dẫn **tái canh** cà phê già (>20 năm) — vấn đề cấp bách tại Đắk Lắk
- Quy trình **chứng nhận VietGAP** step-by-step bằng tiếng Việt đơn giản

**Trung hạn:**
- Tài liệu **biến đổi khí hậu** — thích nghi với hạn kéo dài, mưa trái mùa
- So sánh **hệ thống tưới** (nhỏ giọt vs phun mưa vs tưới gốc) cụ thể cho Tây Nguyên

### 5.2 Tài liệu cần tự sản xuất

Một số chủ đề không có tài liệu sẵn — Bazan AI team phối hợp WASI tạo mới:

| Nội dung | Format | Timeline | Người phụ trách |
|---------|--------|---------|----------------|
| Lịch canh tác cà phê Tây Nguyên 12 tháng (có hình ảnh) | PDF | Q2/2026 | WASI + AI team |
| Hướng dẫn chẩn đoán bệnh qua điện thoại (Q&A flow) | PDF | Q2/2026 | WASI |
| Giải thích kết quả phân tích đất cho nông dân | PDF | Q3/2026 | WASI |
| So sánh 10 loại phân bón phổ biến nhất Đắk Lắk | PDF | Q3/2026 | AI team |

---

## 6. License & Copyright Tracking

| Tài liệu | License | Có thể index | Ghi chú |
|---------|---------|------------|---------|
| WCR Open Access reports | Creative Commons BY | ✅ Có | Citation bắt buộc |
| WCR Paid reports | Proprietary | ⚠️ Cần license | Đang đàm phán |
| WASI reports | Nhà nước Việt Nam | ✅ Có (public interest) | Cần xác nhận với WASI |
| Bộ NN&PTNT | Nhà nước Việt Nam | ✅ Có | Public domain |
| ICO reports | Proprietary | ⚠️ Fair use only | Chỉ summary, không index full |
| Nescafé Plan | Proprietary | ❌ Không | Cần MOU |

---

## 7. Content Maintenance

### 7.1 Lịch review định kỳ

| Task | Tần suất | Người phụ trách |
|------|---------|----------------|
| Review tài liệu cũ hơn 3 năm | Hàng quý | Knowledge Manager |
| Cập nhật Danh mục thuốc BVTV | Hàng năm (tháng 1) | AI team |
| Thêm WCR reports mới | Khi phát hành | Knowledge Manager |
| Re-index sau khi đổi embedding model | Khi có model mới | AI/ML team |
| Gap analysis dựa trên negative feedback | Hàng tháng | AI/ML team |

### 7.2 Deprecation Tài liệu

Tài liệu cần đánh dấu deprecated (không xóa, nhưng giảm weight trong search):
- Thuốc BVTV đã bị cấm sau ngày index
- Giống cà phê không còn được khuyến cáo
- Kỹ thuật canh tác đã có hướng dẫn mới thay thế

```python
# Đánh dấu deprecated trong Qdrant payload
await qdrant_client.set_payload(
    collection_name="agronomy_knowledge",
    payload={"is_deprecated": True, "deprecated_reason": "Thuốc bị cấm 2026-01-01"},
    points_selector=Filter(must=[FieldCondition(key="doc_id", match=MatchValue(value="old-doc-id"))])
)

# Filter loại bỏ trong search
payload_filter = Filter(must=[
    FieldCondition(key="is_deprecated", match=MatchValue(value=False))
])
```

---

## 8. KPIs Knowledge Base

| Metric | Target | Đo bằng |
|--------|--------|---------|
| Tổng số tài liệu indexed | ≥ 200 (Phase 1) | Qdrant stats |
| Coverage by topic (≥10 chunks) | ≥ 80% topics | Content audit |
| Avg document freshness | ≤ 3 năm | Metadata analysis |
| Recall@5 trên test set | ≥ 0.85 | Weekly eval run |
| % câu hỏi có citation | ≥ 95% | Conversation log analysis |
| Deprecated docs ratio | ≤ 5% | Content audit |

---

**© 2026 Bazan AI Project — Confidential**

# AI Model Card — Bazan Agent
## Theo chuẩn Hugging Face Model Card & Google Model Cards

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | AI/ML Lead · Principal Architect |
| **Người review** | Chuyên gia nông nghiệp, Ethics Reviewer |
| **Liên quan** | Prompt Engineering Playbook, RAG Pipeline Design, RLHF Protocol |

---

## 1. Model Details

### 1.1 Thông tin cơ bản

| Thuộc tính | Giá trị |
|-----------|---------|
| **Tên model** | Bazan Agent v1.0 |
| **Loại** | Agentic AI System (RAG + LLM + Tool Calling) |
| **LLM Base** | GPT-4o (OpenAI) — Phase 1 |
| **Embedding Model** | text-embedding-3-large (OpenAI) |
| **Framework** | Microsoft Semantic Kernel 1.x |
| **Ngôn ngữ** | Tiếng Việt (chính), Tiếng Anh (tài liệu) |
| **Domain** | Nông nghiệp cà phê Tây Nguyên, Việt Nam |
| **Deployment** | API-based, không self-hosted model |

### 1.2 Mô tả hệ thống

Bazan Agent **không phải** là một model ML thuần túy mà là một **AI System** kết hợp:

```
User Query
    │
    ▼
Intent Classifier (GPT-4o-mini)
    │
    ▼
Tool Orchestrator (Semantic Kernel)
    ├── RAG Retrieval (Qdrant hybrid search)
    ├── Agronomy Engine (rule-based + CV)
    ├── Market Data (real-time API)
    └── Weather Service (forecast API)
    │
    ▼
Response Generator (GPT-4o + retrieved context)
    │
    ▼
Streamed Answer với Citations
```

### 1.3 Phiên bản lịch sử

| Version | Ngày | Thay đổi chính |
|---------|------|---------------|
| v1.0.0 | 2026-03-19 | Initial release — MVP Phase 1 |
| v1.1.0 | Kế hoạch Q3/2026 | RLHF fine-tune với 5,000 feedback pairs |
| v2.0.0 | Kế hoạch Q1/2027 | Đổi sang bge-m3 embedding + fine-tuned LLM |

---

## 2. Mục đích sử dụng

### 2.1 Intended Use

Bazan Agent được thiết kế **chuyên biệt** cho:

- Tư vấn kỹ thuật canh tác cà phê (Robusta, Arabica, TR4, Catimor)
- Phát hiện và tư vấn xử lý sâu bệnh từ mô tả triệu chứng hoặc ảnh
- Tính toán ROI, phân tích kinh tế mùa vụ
- Cung cấp thông tin giá cà phê và tư vấn thời điểm bán
- Lập lịch canh tác cá nhân hóa theo từng vườn

**Đối tượng sử dụng:**
- Nông dân trồng cà phê tại Tây Nguyên (Đắk Lắk, Lâm Đồng, Gia Lai, Đắk Nông, Kon Tum)
- Cán bộ kỹ thuật khuyến nông
- Nhân viên hợp tác xã cà phê

### 2.2 Out-of-scope Use

Bazan Agent **không được thiết kế** và **không nên dùng** cho:

- Cây trồng khác (lúa, tiêu, ca cao) — chưa có knowledge base
- Tư vấn y tế cho con người
- Tư vấn tài chính/đầu tư (chỉ thông tin tham khảo, không phải lời khuyên đầu tư)
- Ra quyết định pháp lý
- Ứng dụng ngoài lãnh thổ Việt Nam (chưa tối ưu cho điều kiện địa phương khác)
- Thay thế chuyên gia nông nghiệp trong các tình huống phức tạp

---

## 3. Dữ liệu Training & Knowledge Base

### 3.1 Knowledge Base (RAG)

| Nguồn | Số lượng | Ngôn ngữ | Verified by |
|-------|---------|---------|------------|
| WCR (World Coffee Research) Technical Reports | ~200 tài liệu | EN | WCR |
| WASI (Viện nghiên cứu Tây Nguyên) | ~80 tài liệu | VI | WASI |
| Bộ NN&PTNT — Kỹ thuật canh tác | ~40 tài liệu | VI | Bộ NN&PTNT |
| Danh mục thuốc BVTV được phép sử dụng | 1 danh mục (cập nhật hàng năm) | VI | Bộ NN&PTNT |
| Báo cáo thị trường ICO | ~60/năm | EN | ICO |

### 3.2 Dữ liệu thực tế (Real-time)

- Giá cà phê: ICE Futures London + đại lý địa phương (cập nhật 3h/lần)
- Thời tiết: OpenWeatherMap + VNMHA (cập nhật 3h/lần)
- Dữ liệu IoT: Sensor readings của từng vườn (real-time)

### 3.3 Không dùng dữ liệu cá nhân để train

Thông tin cá nhân nông dân (tên, số điện thoại, GPS vườn) **không được dùng** để fine-tune model. Chỉ dùng:
- Feedback tổng hợp ẩn danh (thumbs up/down, rating)
- Cặp (câu hỏi, câu trả lời tốt) đã được expert review và ẩn danh hóa

---

## 4. Đánh giá Hiệu năng

### 4.1 Retrieval Metrics (RAG)

| Metric | Phase 1 Target | Thực đo |
|--------|---------------|--------|
| Recall@5 | ≥ 0.85 | TBD (cập nhật sau deployment) |
| NDCG@5 | ≥ 0.80 | TBD |
| MRR | ≥ 0.74 | TBD |
| Retrieval P95 latency | < 500ms | TBD |

### 4.2 Generation Metrics (RAGAS Framework)

| Metric | Mô tả | Target |
|--------|-------|--------|
| Answer Relevance | Câu trả lời có đúng câu hỏi không | ≥ 0.80 |
| Faithfulness | Có trung thực với context không | ≥ 0.85 |
| Context Precision | Retrieved chunks được dùng hiệu quả | ≥ 0.75 |
| Hallucination Rate | Tỉ lệ thông tin bịa đặt | ≤ 5% |

### 4.3 User Satisfaction (Production)

| Metric | Target | Đo bằng |
|--------|--------|---------|
| Thumbs-up rate | ≥ 75% | In-app feedback |
| Rating trung bình | ≥ 4.0/5.0 | In-app rating |
| Re-engagement rate | ≥ 60% quay lại trong 7 ngày | Analytics |

### 4.4 Test Set

- 200 cặp (câu hỏi, tài liệu đúng) — do chuyên gia nông nghiệp WASI tạo
- Phân phối: 50 disease, 50 fertilizer, 50 cross-lingual, 50 technical term
- Được giữ private, không dùng để train

---

## 5. Giới hạn & Rủi ro

### 5.1 Giới hạn kỹ thuật

**Knowledge cutoff:** Knowledge base được cập nhật định kỳ (hàng tuần cho market, hàng quý cho agronomic knowledge). Tài liệu mới hơn cutoff sẽ không có trong câu trả lời.

**Ngôn ngữ:** Embedding model text-embedding-3-large tốt hơn cho tiếng Anh so với tiếng Việt (~15% accuracy gap). Cross-lingual retrieval hoạt động nhưng có thể miss một số tài liệu tiếng Việt chuyên biệt.

**Câu hỏi ngoài domain:** Model có thể trả lời câu hỏi nông nghiệp tổng quát (dựa trên GPT-4o training data) mà không có retrieval — câu trả lời vẫn có thể xuất hiện nhưng thiếu citation và ít tin cậy hơn.

**Ảnh bệnh:** Computer Vision model đạt accuracy ~87% trên test set nội bộ — không phải 100%. Luôn kèm confidence score và yêu cầu xác nhận.

### 5.2 Rủi ro tiềm tàng

| Rủi ro | Mức độ | Biện pháp giảm thiểu |
|--------|--------|---------------------|
| Hallucination về liều lượng thuốc | Cao | Chỉ trích dẫn từ tài liệu approved, không tự suy diễn |
| Tư vấn thuốc cấm | Cao | Danh sách thuốc cấm hardcode, không phụ thuộc LLM |
| Thông tin giá sai lệch | Trung bình | Luôn kèm timestamp, nhắc đây là tham khảo |
| Overconfidence trong chẩn đoán | Trung bình | Luôn hiển thị confidence score, đề xuất kiểm tra thực tế |
| Thiếu thông tin cảnh báo an toàn | Cao | Template có section "An toàn" bắt buộc cho tư vấn thuốc |

---

## 6. Ethical Considerations

### 6.1 Bias đã biết

- **Geographic bias:** Knowledge base thiên về điều kiện Đắk Lắk và Lâm Đồng — ít tài liệu về Kon Tum và Đắk Nông
- **Variety bias:** Robusta được cover tốt hơn Arabica và các giống mới (TR9, TR11)
- **Literacy bias:** Ngôn ngữ trong câu trả lời được điều chỉnh theo `literacyLevel` — nhưng nông dân chưa khai báo sẽ nhận câu trả lời mặc định "intermediate"

### 6.2 Fairness

- Không phân biệt đối xử dựa trên quy mô vườn (nông dân 0.5 ha và 10 ha nhận cùng chất lượng tư vấn)
- Khuyến nghị không ưu tiên thương hiệu cụ thể (chỉ tên hoạt chất)
- Giá đại lý được cập nhật cho tất cả địa bàn, không chỉ vùng trung tâm

### 6.3 Privacy

- GPS coordinates của vườn chỉ dùng nội bộ cho weather/alert routing, không share với bên thứ ba
- Lịch sử chat được mã hóa at-rest, không dùng cho quảng cáo
- Quyền xóa dữ liệu: nông dân có thể yêu cầu xóa toàn bộ dữ liệu (xem Data Privacy Policy)

---

## 7. Recommendations cho người dùng hệ thống

- **Luôn xác nhận trước khi phun hóa chất** — hỏi cán bộ khuyến nông nếu không chắc
- **Ảnh chụp phải rõ** — chụp gần, đủ ánh sáng, cả mặt trên và dưới lá để chẩn đoán chính xác
- **Thông tin giá là tham khảo** — thực tế có thể khác tùy đại lý địa phương
- **Bazan AI không thay thế chuyên gia** khi bệnh lan rộng hoặc tình huống bất thường

---

## 8. Thông tin liên hệ & Báo cáo lỗi

- **Báo cáo câu trả lời sai:** Nút 👎 trong ứng dụng + mô tả lỗi
- **Báo cáo hóa chất cấm hoặc liều lượng nguy hiểm:** ai-safety@bazanai.vn (ưu tiên 24h)
- **Yêu cầu thêm tài liệu vào knowledge base:** knowledge@bazanai.vn

---

**© 2026 Bazan AI Project — Confidential**

# RLHF Data Collection Protocol
## Bazan AI — Reinforcement Learning from Human Feedback

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | AI/ML Lead |
| **Người review** | Principal Architect, Data Ethics Officer |
| **Liên quan** | AI Model Card, Prompt Engineering Playbook, Data Labeling Guidelines |

---

## 1. Mục tiêu

RLHF Pipeline của Bazan AI thu thập phản hồi từ nông dân và chuyên gia để:
1. Đánh giá chất lượng câu trả lời hiện tại (baseline measurement)
2. Tạo dataset (prompt, chosen, rejected) cho DPO fine-tuning Phase 2
3. Phát hiện sớm các loại câu hỏi AI đang trả lời kém
4. Cải thiện RAG retrieval bằng implicit feedback signal

---

## 2. Các loại Feedback Signal

### 2.1 Explicit Feedback (từ nông dân)

**Thumbs up/down** — sau mỗi câu trả lời AI:
```
👍 (thumbs up) → rating = positive
👎 (thumbs down) → rating = negative + optional comment
```

**Star rating (1–5)** — sau khi kết thúc session:
```
5 ⭐ = Rất hữu ích, tôi sẽ áp dụng ngay
4 ⭐ = Hữu ích, cần xem thêm
3 ⭐ = Bình thường, chưa chắc
2 ⭐ = Không đủ thông tin
1 ⭐ = Sai hoặc có hại
```

**Comment text** — optional khi nhấn 👎:
- Max 500 ký tự
- Prompt gợi ý: "Câu trả lời sai ở điểm nào?" / "Bạn mong đợi thông tin gì?"

### 2.2 Implicit Feedback (hành vi người dùng)

| Signal | Meaning | Weight |
|--------|---------|--------|
| Session kết thúc ngay sau câu trả lời | Đủ thông tin | +0.2 |
| Hỏi lại câu tương tự | Câu trả lời không đủ | -0.3 |
| Chia sẻ câu trả lời qua Zalo | Hữu ích, muốn share | +0.5 |
| Copy text câu trả lời | Hữu ích | +0.3 |
| Nhấn vào citation link | Muốn xác nhận nguồn | Neutral |
| App close ngay sau nhận câu trả lời | Ambiguous | Neutral |

### 2.3 Expert Feedback (từ chuyên gia nông nghiệp)

Mỗi tuần, team WASI review 20–30 câu trả lời được chọn ngẫu nhiên:

```json
{
  "messageId": "msg-002",
  "expertId": "expert_wasi_01",
  "factualAccuracy": 5,         // 1-5
  "technicalCorrectness": 4,    // 1-5
  "safetyAssessment": "safe",   // safe | warning | dangerous
  "missingInfo": "Nên đề cập đến ngưỡng pH cụ thể cho Robusta",
  "incorrectInfo": null,
  "suggestedResponse": "...",   // Optional improvement
  "reviewedAt": "2026-03-21T10:00:00Z"
}
```

---

## 3. Data Collection Pipeline

### 3.1 Luồng thu thập

```
Farmer submits feedback (thumbs + comment)
          │
          ▼
Conversation Service saves to chatMessages.feedback
          │
          ▼
RabbitMQ: bazan.conversation.feedback.collected event
          │
          ▼
Scheduler Service: RLHF Export Job (chạy 2h sáng hàng ngày)
          │
    ┌─────┴──────────────────────────────┐
    │                                    │
    ▼                                    ▼
Filter high-quality                Filter negative
(rating >= 4, thumbs up)           (rating <= 2, thumbs down)
    │                                    │
    ▼                                    ▼
"Chosen" responses                 "Rejected" responses (raw)
    │                                    │
    └─────────────┬──────────────────────┘
                  │
                  ▼
         Expert Review Queue
         (20-30 samples/tuần)
                  │
                  ▼
         Annotated Dataset (DPO format)
                  │
                  ▼
         MinIO: bazan-backups/rlhf/{date}/
```

### 3.2 RLHF Export Job

```csharp
// Jobs/RlhfDataExportJob.cs
public class RlhfDataExportJob(
    IConversationRepository convRepo,
    IMinioStorageService storage,
    IAnonymizerService anonymizer
)
{
    [Cron("0 2 * * *")]  // 2AM hàng ngày
    public async Task Execute()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1).Date;

        // Lấy feedback từ ngày hôm qua
        var feedbackData = await convRepo.ExportFeedbackAsync(
            from: yesterday,
            to: yesterday.AddDays(1),
            minRating: 1,
            usedForTraining: false
        );

        // Tạo DPO dataset format
        var dpoRecords = BuildDpoDataset(feedbackData);

        // Ẩn danh hóa PII
        var anonymized = await anonymizer.AnonymizeAsync(dpoRecords);

        // Export JSON
        var json = JsonSerializer.Serialize(anonymized);
        var filename = $"rlhf_{yesterday:yyyyMMdd}.jsonl";

        await storage.UploadAsync(
            bucket: "bazan-backups",
            key: $"rlhf/{yesterday:yyyy/MM}/{filename}",
            content: json
        );

        // Đánh dấu đã export
        await convRepo.MarkUsedForTrainingAsync(
            feedbackData.Select(f => f.MessageId).ToList()
        );

        logger.LogInformation(
            "RLHF export: {Count} records → {Filename}",
            anonymized.Count, filename
        );
    }

    private List<DpoRecord> BuildDpoDataset(List<FeedbackExport> data)
    {
        var records = new List<DpoRecord>();

        // Group by session để lấy context
        var grouped = data.GroupBy(d => d.SessionId);

        foreach (var session in grouped)
        {
            var positive = session.Where(m => m.Feedback.Rating >= 4).ToList();
            var negative = session.Where(m => m.Feedback.Rating <= 2).ToList();

            // Tạo DPO pairs: cùng prompt, chosen vs rejected
            foreach (var pos in positive)
            {
                records.Add(new DpoRecord
                {
                    Prompt = BuildPromptWithContext(pos),
                    Chosen = pos.AssistantResponse,
                    Rejected = null,   // Will be paired later by expert
                    ChosenRating = pos.Feedback.Rating,
                    FarmerContext = AnonymizeFarmerContext(pos.FarmerContext),
                    ToolsUsed = pos.ContextUsed.ToolsInvoked,
                    CitationsUsed = pos.ContextUsed.RagDocuments.Count
                });
            }
        }

        return records;
    }
}
```

### 3.3 DPO Record Format

```jsonl
{
  "id": "dpo_20260319_001",
  "prompt": "Cây cà phê Robusta vườn bazan 650m bị vàng lá từ mép vào, mặt dưới có bột cam",
  "chosen": "Dựa vào mô tả của bạn, cây đang bị bệnh gỉ sắt (Hemileia vastatrix)...",
  "rejected": null,
  "chosen_rating": 5,
  "farmer_context": {
    "coffee_variety": "Robusta",
    "soil_type": "basalt",
    "altitude_m": 650,
    "growth_stage": "fruit_development",
    "province": "Đắk Lắk"
  },
  "metadata": {
    "tools_used": ["diagnose_farm_disease", "search_agronomy_knowledge"],
    "citations_count": 2,
    "response_tokens": 287,
    "retrieval_recall": 0.92,
    "expert_reviewed": false,
    "collected_at": "2026-03-19T07:32:00Z"
  }
}
```

---

## 4. Anonymization Protocol

### 4.1 PII được loại bỏ

| Field | Xử lý |
|-------|-------|
| `farmerId` | Replace bằng UUID ngẫu nhiên cho batch |
| `phoneNumber` | Xóa hoàn toàn |
| `fullName` | Replace bằng "Nông dân X" |
| `zaloId` | Xóa hoàn toàn |
| `GPS coordinates` | Round đến 0.01 độ (~1km) |
| `farmId` | Replace bằng UUID ngẫu nhiên |
| `sessionId` | Keep (internal ref only) |

### 4.2 Giữ lại để học

- Giống cà phê, loại đất, vùng (tỉnh)
- Giai đoạn sinh trưởng
- Thời điểm trong năm (tháng)
- Loại câu hỏi (intent)
- Đặc điểm ngôn ngữ (tiếng địa phương, viết tắt)

### 4.3 Consent

Trong Terms of Service của Bazan AI, nông dân đồng ý:
- Feedback ẩn danh có thể được dùng để cải thiện AI
- Không bao giờ chia sẻ dữ liệu cá nhân với bên thứ ba
- Có quyền yêu cầu xóa toàn bộ dữ liệu

---

## 5. Quality Gates cho Training Data

```python
# Bộ lọc chất lượng trước khi đưa vào training

QUALITY_GATES = [
    # Gate 1: Độ dài câu trả lời
    lambda r: 50 <= len(r['chosen'].split()) <= 500,

    # Gate 2: Có citation
    lambda r: r['metadata']['citations_count'] > 0,

    # Gate 3: Rating đủ cao
    lambda r: r['chosen_rating'] >= 4,

    # Gate 4: Không chứa thông tin thuốc cấm
    lambda r: not any(
        banned in r['chosen'].lower()
        for banned in ["paraquat", "endosulfan", "monocrotophos"]
    ),

    # Gate 5: Có farmer context
    lambda r: r['farmer_context']['coffee_variety'] is not None,

    # Gate 6: Không phải greeting/chitchat
    lambda r: r['metadata']['tools_used'] != [],
]

def passes_quality_gates(record: dict) -> bool:
    return all(gate(record) for gate in QUALITY_GATES)
```

---

## 6. Milestones thu thập dữ liệu

| Phase | Timeline | Target | Dùng cho |
|-------|---------|--------|---------|
| Baseline | Tháng 1–3 | 500 feedback pairs | Evaluation baseline |
| Phase 1 | Tháng 4–6 | 2,000 pairs (1,500 positive) | Prompt optimization |
| Phase 2 | Tháng 7–12 | 5,000 pairs (3,500 positive) | DPO fine-tuning |
| Phase 3 | Tháng 13–18 | 10,000 pairs + 500 expert-reviewed | Full RLHF training |

---

## 7. Monitoring RLHF Signals

**Grafana dashboard — RLHF metrics:**

```
Daily feedback rate      → Số feedback / DAU (target > 30%)
Positive feedback rate   → Thumbs up / total (target > 75%)
Expert accuracy rate     → Expert score avg / 5 (target > 4.0)
Coverage by intent       → Feedback distribution across intents
Coverage by variety      → Feedback distribution across coffee varieties
Missing coverage alert   → Intents với < 50 feedback pairs
```

**Alert nếu:**
- Positive feedback rate < 60% trong 3 ngày liên tiếp → Investigate và fix prompt
- Expert accuracy < 3.5 cho một intent cụ thể → Add more RAG documents
- Export job thất bại → PagerDuty alert

---

**© 2026 Bazan AI Project — Confidential**

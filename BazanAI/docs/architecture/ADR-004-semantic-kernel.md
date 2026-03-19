# ADR-004 — AI Orchestration: Microsoft Semantic Kernel

| Trường | Nội dung |
|--------|---------|
| **ID** | ADR-004 |
| **Tiêu đề** | Lựa chọn Microsoft Semantic Kernel làm AI Agent Orchestration Framework |
| **Trạng thái** | Accepted |
| **Ngày tạo** | 2026-03-19 |
| **Ngày cập nhật** | 2026-03-19 |
| **Tác giả** | Principal Software Architect · AI/ML Lead |
| **Người review** | Backend Lead, Product Lead |
| **Liên quan** | ADR-001 (MongoDB), ADR-002 (Qdrant), ADR-003 (RabbitMQ), ADR-005 (YARP), RAG Pipeline Design, Prompt Engineering Playbook |

---

## 1. Bối cảnh

Bazan AI không chỉ là một chatbot đơn giản gọi một LLM và trả về kết quả. Mỗi câu hỏi của nông dân có thể cần phối hợp nhiều nguồn thông tin và dịch vụ khác nhau:

### 1.1 Ví dụ câu hỏi phức hợp

> "Vườn TR4 của tôi đang ra hoa, hôm nay trời mưa nhiều, giá cà phê đang tăng — tôi có nên phun thuốc phòng bệnh không và nên bán bây giờ không?"

Để trả lời câu hỏi này, AI cần:

```
Bước 1: Phân loại intent
  → Disease prevention + Market decision (dual intent)

Bước 2: Lấy dữ liệu song song
  → AgronomyPlugin:  Rủi ro bệnh khi mưa nhiều + giai đoạn ra hoa TR4
  → WeatherPlugin:   Dự báo 7 ngày tới cho vị trí vườn
  → KnowledgePlugin: Phác đồ phòng bệnh giai đoạn ra hoa từ WCR docs
  → MarketPlugin:    Giá hiện tại, xu hướng 30 ngày, điểm hòa vốn

Bước 3: Tổng hợp kết quả thành câu trả lời nhất quán
  → Không phải nối chuỗi output của 4 service
  → Phải synthesize thành lời khuyên có logic thống nhất

Bước 4: Stream câu trả lời về client
  → Token-by-token qua SignalR
```

Không có orchestration framework, phải tự viết toàn bộ luồng này trong Orchestrator Service — phức tạp, khó maintain, khó test.

### 1.2 Yêu cầu orchestration

| Yêu cầu | Mức độ | Chi tiết |
|---------|--------|---------|
| **Multi-step reasoning** | Must-have | Lập kế hoạch và thực hiện nhiều bước tự động |
| **Tool/Plugin calling** | Must-have | Gọi service nội bộ như công cụ của AI |
| **Streaming response** | Must-have | Token-by-token về client |
| **LLM provider abstraction** | Must-have | Đổi OpenAI → Azure OpenAI không sửa code |
| **Token management** | Must-have | Kiểm soát context window budget |
| **Memory / Context** | Should-have | Nhớ thông tin trong phiên làm việc |
| **.NET native** | Should-have | Cùng stack với toàn bộ backend |
| **Prompt management** | Should-have | Version, template, test prompt |
| **Auto function calling** | Should-have | AI tự quyết định gọi tool nào |
| **Observability** | Should-have | Trace từng bước reasoning |

### 1.3 Ràng buộc

- **Tech stack:** Toàn bộ backend .NET 8 / C# — không muốn thêm Python runtime vào Orchestrator
- **LLM vendor:** Bắt đầu với OpenAI, có thể chuyển Azure OpenAI hoặc local model (Phase 4)
- **Team size:** Nhỏ (3–5 engineers) — không có chuyên gia MLOps riêng
- **Budget:** Kiểm soát chi phí OpenAI token — cần token budget management

---

## 2. Các phương án đã xem xét

### Phương án A — Microsoft Semantic Kernel

**Mô tả:** Open-source SDK của Microsoft cho AI orchestration, native .NET/C# và Python. Hỗ trợ plugins, planners, memory, multi-model.

**Ưu điểm:**
- **Native .NET 8:** Viết Kernel, Plugin, Agent bằng C# thuần — không cần interop, không subprocess
- **Plugin/Function Calling:** Đăng ký C# method làm AI tool bằng attribute `[KernelFunction]`
- **AutoFunctionCalling:** AI tự quyết định gọi function nào và theo thứ tự nào
- **LLM abstraction:** Cùng code, swap giữa OpenAI / Azure OpenAI / Mistral / Ollama bằng config
- **Streaming native:** `InvokeStreamingAsync()` trả về `IAsyncEnumerable<StreamingChatMessageContent>`
- **Memory connectors:** Redis, Qdrant, volatile memory cho short-term context
- **Microsoft backed:** Long-term support, không bị abandon
- **Prompt template engine:** Handlebars, Liquid — tách prompt khỏi code

**Nhược điểm:**
- API thay đổi nhanh (vẫn đang evolve mạnh đến v1.x)
- Documentation còn thiếu ở một số advanced scenario
- Community nhỏ hơn LangChain

---

### Phương án B — LangChain (Python) với .NET interop

**Mô tả:** Python framework phổ biến nhất cho LLM application, dùng Python runtime trong một service riêng và .NET gọi qua HTTP/gRPC.

**Ưu điểm:**
- Ecosystem lớn nhất (integrations, loaders, tools)
- Community documentation đồ sộ
- LangGraph cho complex agent workflow

**Nhược điểm:**
- **Python runtime thêm vào Orchestrator:** Team không có Python backend engineer, chỉ có Python cho RAG Service
- **Interop overhead:** .NET → HTTP → Python Orchestrator → LangChain — thêm latency, thêm điểm lỗi
- **Hai codebase khác nhau:** Bug tracking, deployment, monitoring phải cover cả .NET và Python
- **LangChain API instability:** Nhiều breaking changes giữa các version (0.1 → 0.2 → 0.3)
- **Type safety kém:** Python dynamic typing khó đảm bảo contract với strongly-typed .NET services

**Không chọn vì:** Thêm Python runtime vào Orchestrator khi có Semantic Kernel .NET native là không cần thiết. Đã có Python service (Knowledge RAG) — không muốn thêm nữa.

---

### Phương án C — LangChain.NET (Community port)

**Mô tả:** Community port của LangChain cho .NET.

**Ưu điểm:**
- .NET native như Semantic Kernel
- API familiar với Python LangChain

**Nhược điểm:**
- **Community maintained, không có backing lớn** — risk bị abandon
- **Tính năng lag sau Python version** — LangGraph, advanced agents chưa có đủ
- **Ít được dùng production** — ít case study, khó tìm giải pháp cho edge case
- **Documentation kém** so với cả LangChain Python và Semantic Kernel

**Không chọn vì:** Rủi ro long-term support không chấp nhận được cho core component của hệ thống.

---

### Phương án D — Tự implement Orchestration

**Mô tả:** Viết Orchestrator Service từ đầu: intent classifier → parallel service calls → response synthesizer.

**Ưu điểm:**
- Full control
- Không phụ thuộc external SDK
- Tối ưu cho Bazan AI use case cụ thể

**Nhược điểm:**
- **Chi phí build cao:** Cần tự implement: function calling loop, token counting, retry, streaming, memory
- **Khó maintain:** Mỗi LLM provider update API → phải update adapter thủ công
- **Re-inventing the wheel:** Semantic Kernel đã giải quyết tất cả vấn đề trên
- **Risk cao:** Orchestration logic phức tạp, nhiều edge case khó test

**Không chọn vì:** Không hợp lý khi Semantic Kernel cung cấp đúng những gì cần, native .NET, có Microsoft support.

---

### Phương án E — OpenAI Assistants API

**Mô tả:** Dùng OpenAI Assistants API với tool calling và thread management do OpenAI host.

**Ưu điểm:**
- Zero orchestration code — OpenAI xử lý tool calling loop
- Persistent thread / conversation native
- File retrieval built-in

**Nhược điểm:**
- **Vendor lock-in cực cao:** Toàn bộ reasoning logic nằm trên OpenAI server
- **Không kiểm soát được tool execution:** Bazan AI tools cần gọi internal services — không expose ra internet
- **Chi phí cao hơn:** Assistants API tốn thêm storage token
- **Không thể fine-tune orchestration:** Khi AI chọn sai tool, không can thiệp được
- **Latency:** Thêm round-trip đến OpenAI cho mỗi tool call decision

**Không chọn vì:** Vendor lock-in và không kiểm soát được tool execution trong network nội bộ.

---

## 3. Quyết định

**Chọn Phương án A — Microsoft Semantic Kernel v1.x** làm AI Orchestration Framework cho Bazan AI Orchestrator Service.

**Version:** `Microsoft.SemanticKernel` 1.x (latest stable)
**Language:** C# / .NET 8
**LLM:** OpenAI GPT-4o (Phase 1), có thể swap sang Azure OpenAI (Phase 2)

---

## 4. Lý do

### 4.1 Plugin System — C# method trở thành AI tool

```csharp
// Plugins/AgronomyPlugin.cs
public class AgronomyPlugin(IAgronomyServiceClient agronomyClient)
{
    [KernelFunction("diagnose_farm_disease")]
    [Description("Chẩn đoán bệnh hoặc sâu hại trên vườn cà phê dựa vào triệu chứng mô tả hoặc ảnh")]
    public async Task<string> DiagnoseFarmDisease(
        [Description("ID của vườn cà phê")]
        string farmId,
        [Description("Mô tả triệu chứng quan sát được: màu lá, vị trí tổn thương, tỉ lệ cây bị ảnh hưởng")]
        string symptomDescription,
        [Description("URL ảnh lá bị bệnh nếu có, để lại rỗng nếu không có")]
        string? imageUrl = null)
    {
        var result = await agronomyClient.DiagnoseAsync(
            farmId, symptomDescription, imageUrl);

        // Serialize thành text mô tả — LLM sẽ đọc và tổng hợp
        return $"""
            Kết quả chẩn đoán:
            - Bệnh có thể: {result.DiseaseName} (confidence: {result.Confidence:P0})
            - Mức độ: {result.Severity}
            - Phác đồ: {result.RecommendedTreatment}
            - Nguồn: {result.SourceDocument}
            """;
    }

    [KernelFunction("get_seasonal_schedule")]
    [Description("Lấy lịch canh tác cá nhân hóa cho vườn theo mùa hiện tại")]
    public async Task<string> GetSeasonalSchedule(
        [Description("ID vườn cà phê")]
        string farmId)
    {
        var schedule = await agronomyClient.GetScheduleAsync(farmId);
        return JsonSerializer.Serialize(schedule);
    }
}
```

AI biết tên function, description, và parameter description — tự quyết định gọi khi nào và với argument gì.

### 4.2 AutoFunctionCalling — AI tự lập kế hoạch

```csharp
// Agents/BazanAgent.cs
public class BazanAgent(
    Kernel kernel,
    IConversationClient conversationClient
)
{
    public async IAsyncEnumerable<string> StreamResponseAsync(
        string sessionId,
        string userMessage,
        FarmerContext farmerContext,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Build prompt với farmer context
        var chatHistory = await BuildChatHistoryAsync(sessionId, farmerContext);
        chatHistory.AddUserMessage(userMessage);

        // Semantic Kernel sẽ:
        // 1. Gọi GPT-4o với chat history + available functions
        // 2. GPT-4o quyết định gọi function nào
        // 3. SK execute function, thêm result vào context
        // 4. GPT-4o có thể gọi thêm function khác
        // 5. GPT-4o sinh câu trả lời cuối khi đủ thông tin
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            MaxTokens        = 1000,
            Temperature      = 0.3,    // Thấp hơn cho domain chuyên biệt
        };

        var fullResponse = new StringBuilder();

        await foreach (var chunk in kernel.InvokePromptStreamingAsync<string>(
            promptTemplate: chatHistory.ToString(),
            arguments: new KernelArguments(settings),
            cancellationToken: ct))
        {
            fullResponse.Append(chunk);
            yield return chunk;  // Stream token về caller
        }

        // Lưu vào Conversation Service
        await conversationClient.AddMessageAsync(sessionId, "assistant",
            fullResponse.ToString(), ct);
    }
}
```

### 4.3 LLM Provider Abstraction

```csharp
// Program.cs — Chỉ cần thay đổi đây khi đổi provider
var kernelBuilder = Kernel.CreateBuilder();

if (config["AI:Provider"] == "AzureOpenAI")
{
    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: config["AzureOpenAI:DeploymentName"]!,
        endpoint:       config["AzureOpenAI:Endpoint"]!,
        apiKey:         config["AzureOpenAI:ApiKey"]!
    );
}
else if (config["AI:Provider"] == "Ollama")
{
    // Phase 4: Local model option
    kernelBuilder.AddOpenAIChatCompletion(
        modelId:  "llama3.2",
        endpoint: new Uri("http://ollama:11434/v1"),
        apiKey:   "ollama"
    );
}
else // Default: OpenAI
{
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "gpt-4o",
        apiKey:  config["OpenAI:ApiKey"]!
    );
}

// Đăng ký tất cả plugins
kernelBuilder.Plugins.AddFromType<AgronomyPlugin>();
kernelBuilder.Plugins.AddFromType<MarketPlugin>();
kernelBuilder.Plugins.AddFromType<KnowledgePlugin>();
kernelBuilder.Plugins.AddFromType<WeatherPlugin>();
kernelBuilder.Plugins.AddFromType<FarmerContextPlugin>();

var kernel = kernelBuilder.Build();
```

Toàn bộ plugin code không thay đổi khi swap provider.

### 4.4 Token Budget Management

```csharp
// Services/TokenBudgetService.cs
public class TokenBudgetService
{
    private const int MAX_CONTEXT_TOKENS  = 10_000;
    private const int SYSTEM_PROMPT_BUDGET = 1_200;
    private const int FARMER_CONTEXT_BUDGET = 300;
    private const int RESPONSE_BUDGET      = 1_000;
    private const int HISTORY_BUDGET =
        MAX_CONTEXT_TOKENS - SYSTEM_PROMPT_BUDGET
                           - FARMER_CONTEXT_BUDGET
                           - RESPONSE_BUDGET;  // = 7,500

    public ChatHistory TrimHistory(ChatHistory history, int currentBudget)
    {
        // Giữ System message đầu
        // Trim từ giữa ra nếu vượt budget
        // Luôn giữ 5 tin gần nhất
        var trimmed = new ChatHistory();
        trimmed.Add(history[0]); // System prompt

        var tail = history.TakeLast(10).ToList();
        var tailTokens = EstimateTokens(tail);

        if (tailTokens <= HISTORY_BUDGET)
        {
            tail.ForEach(trimmed.Add);
        }
        else
        {
            // Thêm summary của phần bị trim
            var summary = $"[Tóm tắt {history.Count - 11} tin trước]";
            trimmed.AddSystemMessage(summary);
            history.TakeLast(5).ToList().ForEach(trimmed.Add);
        }

        return trimmed;
    }

    private int EstimateTokens(IEnumerable<ChatMessageContent> messages)
        => messages.Sum(m => m.Content?.Length / 3 ?? 0); // ~3 chars/token tiếng Việt
}
```

### 4.5 SignalR Streaming Integration

```csharp
// Hubs/ChatHub.cs
public class ChatHub(BazanAgent agent) : Hub
{
    public async Task SendMessage(
        string sessionId,
        string message,
        string farmerId)
    {
        var farmerContext = await farmerContextService.GetAsync(farmerId);

        await foreach (var token in agent.StreamResponseAsync(
            sessionId, message, farmerContext,
            Context.ConnectionAborted))
        {
            // Mỗi token được gửi ngay về client
            await Clients.Caller.SendAsync("ReceiveToken", token);
        }

        await Clients.Caller.SendAsync("StreamComplete");
    }
}
```

---

## 5. Thiết kế triển khai

### 5.1 Kiến trúc Orchestrator Service

```
BazanAI.Orchestrator/
├── Agents/
│   ├── BazanAgent.cs           # Core agent với AutoFunctionCalling
│   └── AgentPlanner.cs         # Intent → Tool selection pre-planning
├── Plugins/
│   ├── AgronomyPlugin.cs       # → HTTP đến agronomy-svc:5004
│   ├── MarketPlugin.cs         # → HTTP đến market-svc:5005
│   ├── KnowledgePlugin.cs      # → HTTP đến knowledge-rag:5003
│   ├── WeatherPlugin.cs        # → HTTP đến weather-svc:5008
│   └── FarmerContextPlugin.cs  # → HTTP đến identity-svc:5001
├── Hubs/
│   └── ChatHub.cs              # SignalR endpoint
├── Services/
│   ├── TokenBudgetService.cs
│   └── PromptBuilderService.cs # Load prompt từ PromptRegistry
└── Program.cs
```

### 5.2 Plugin HTTP Client Pattern

```csharp
// Plugins/KnowledgePlugin.cs
public class KnowledgePlugin(IKnowledgeRagClient ragClient)
{
    [KernelFunction("search_agronomy_knowledge")]
    [Description("Tìm kiếm tài liệu kỹ thuật nông nghiệp cà phê từ WCR và các nguồn uy tín")]
    public async Task<string> SearchAgronomyKnowledge(
        [Description("Câu hỏi cần tìm kiếm (tiếng Việt hoặc tiếng Anh)")]
        string query,
        [Description("Chủ đề cụ thể: disease, fertilizer, irrigation, harvest, variety, soil")]
        string topic = "general")
    {
        var results = await ragClient.SearchAsync(
            query:      query,
            collection: TopicToCollection(topic),
            limit:      5
        );

        if (!results.Any())
            return "Không tìm thấy tài liệu liên quan trong cơ sở tri thức.";

        return string.Join("\n\n", results.Select((r, i) =>
            $"[Tài liệu {i + 1}] {r.SourceTitle}\n{r.Text}\n(Nguồn: {r.SourceTitle})"));
    }

    private static string TopicToCollection(string topic) => topic switch
    {
        "disease" => "pest_disease_library",
        "market"  => "market_reports",
        _         => "agronomy_knowledge"
    };
}
```

### 5.3 Docker Compose

```yaml
ai-orchestrator:
  build:
    context: ./src/Services/BazanAI.Orchestrator
    dockerfile: Dockerfile
  container_name: bazan-orchestrator
  environment:
    ASPNETCORE_ENVIRONMENT: Production
    AI__Provider:           OpenAI
    OpenAI__ApiKey:         ${OPENAI_API_KEY}
    OpenAI__Model:          gpt-4o
    Services__AgronomyUrl:  http://agronomy-svc:5004
    Services__MarketUrl:    http://market-svc:5005
    Services__KnowledgeUrl: http://knowledge-rag:5003
    Services__WeatherUrl:   http://weather-svc:5008
    Services__IdentityUrl:  http://identity-svc:5001
    Redis__ConnectionString: redis:6379,password=${REDIS_PASSWORD}
    Seq__ServerUrl:         http://seq:80
  ports:
    - "5010:5010"
  depends_on:
    - agronomy-svc
    - market-svc
    - knowledge-rag
    - redis
  networks:
    - bazan-network
```

### 5.4 Observability — Trace từng bước AI

```csharp
// OpenTelemetry tracing cho Semantic Kernel
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Microsoft.SemanticKernel*")  // SK tự publish traces
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(opt =>
            opt.Endpoint = new Uri("http://otel-collector:4317")));

// Kết quả: Grafana Tempo hiển thị full trace:
// User Request
//   └─ BazanAgent.StreamResponseAsync (1,840ms)
//        ├─ GPT-4o: tool decision (320ms)
//        ├─ AgronomyPlugin.DiagnoseFarmDisease (450ms)
//        ├─ WeatherPlugin.GetWeatherForecast (180ms)
//        ├─ KnowledgePlugin.SearchAgronomyKnowledge (290ms)
//        └─ GPT-4o: synthesis + streaming (600ms)
```

---

## 6. Hậu quả & Trade-offs

### 6.1 Tích cực

- **Developer experience:** Plugin = C# method với attribute — dễ viết, dễ test
- **LLM agnostic:** Swap provider bằng 1 dòng config, không thay đổi business logic
- **Streaming native:** `IAsyncEnumerable` tích hợp tự nhiên với SignalR
- **Observability:** SK tự publish OpenTelemetry traces cho mỗi bước reasoning
- **Token management:** Built-in token counting, context window management

### 6.2 Tiêu cực & Cách giảm thiểu

| Trade-off | Mức độ | Cách giảm thiểu |
|-----------|--------|----------------|
| SK API thay đổi nhanh (v1.x đang evolve) | Trung bình | Pin version cụ thể trong .csproj. Viết integration test. Review changelog trước khi upgrade. |
| AutoFunctionCalling đôi khi gọi sai tool | Thấp | Function description rõ ràng. Fallback manual routing khi confidence thấp. |
| Latency tăng khi multi-step (3+ tool calls) | Thấp | Parallel tool calling cho tools không phụ thuộc nhau. Cache tool results trong Redis. |
| Chi phí token tăng khi tool results dài | Thấp | Tool chỉ trả về summary, không trả raw data. Token budget enforcement. |

### 6.3 Anti-patterns cần tránh

```csharp
// ❌ SAI: Tool trả về dữ liệu thô, tốn token
[KernelFunction("get_market_data")]
public async Task<string> GetMarketData(string farmId)
{
    var allPrices = await marketClient.GetAllPricesAsync(); // 10,000 records
    return JsonSerializer.Serialize(allPrices); // Tốn hàng nghìn token
}

// ✅ ĐÚNG: Tool trả về summary ngắn gọn
[KernelFunction("get_market_data")]
public async Task<string> GetMarketData(string farmId)
{
    var summary = await marketClient.GetMarketSummaryAsync(farmId);
    return $"Giá Robusta: {summary.CurrentPrice:N0} đ/kg | Xu hướng: {summary.Trend} ({summary.ChangePct:+0.##;-0.##}%) | Khuyến nghị: {summary.Recommendation}";
    // ~80 tokens thay vì 10,000 tokens
}
```

---

## 7. Tiêu chí đánh giá lại

- SK API breaking changes làm mất nhiều hơn 3 ngày để migrate → Đánh giá chi phí so với benefit, có thể fork version cũ
- AutoFunctionCalling sai > 15% queries → Implement intent-to-tool mapping cứng (rule-based routing) thay vì để AI tự quyết
- Latency P95 vượt 5 giây do multi-step reasoning → Xem xét caching aggressive, giảm số lượng tools, hoặc pre-compute common paths
- Cần local LLM (chi phí OpenAI quá cao ở Phase 3+) → Evaluate Ollama với Llama 3.x hoặc Mistral — SK hỗ trợ sẵn

---

## 8. Tham khảo

- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/overview/)
- [Semantic Kernel GitHub](https://github.com/microsoft/semantic-kernel)
- [Auto Function Calling in SK](https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/function-calling/)
- [SK Streaming with SignalR](https://learn.microsoft.com/en-us/semantic-kernel/concepts/streaming)
- [OpenTelemetry with Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/concepts/enterprise-readiness/observability/)
- ADR-002 — Qdrant Vector Database
- ADR-003 — RabbitMQ Messaging
- ADR-005 — YARP API Gateway
- RAG Pipeline Design Document
- Prompt Engineering Playbook

---

*Tài liệu theo template ADR của Michael Nygard (2011).*

**© 2026 Bazan AI Project — Confidential**

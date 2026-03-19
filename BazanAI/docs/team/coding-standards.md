# Coding Standards & Style Guide — Bazan AI
## C# (.NET 8) · Python 3.12 · General Conventions

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Engineering Lead |
| **Áp dụng** | Tất cả developers — Backend, AI/ML |
| **Liên quan** | Developer Onboarding, Code Review Checklist, Git Workflow |

---

## 1. Nguyên tắc Chung

```
1. READABLE FIRST — Code được đọc nhiều hơn viết.
   Ưu tiên rõ ràng hơn thông minh.

2. EXPLICIT OVER IMPLICIT — Đặt tên rõ nghĩa.
   GetFarmerByPhoneNumber() hơn là GetFarmer(x).

3. FAIL FAST — Validate sớm, throw exception sớm.
   Không để lỗi propagate xa điểm phát sinh.

4. NO MAGIC NUMBERS/STRINGS — Mọi constant phải có tên.
   const int MaxOtpAttempts = 5; KHÔNG phải if (attempts > 5)

5. ONE RESPONSIBILITY — Mỗi method/class làm đúng 1 việc.
   Nếu method > 30 dòng → cân nhắc tách.
```

---

## 2. C# / .NET 8 Standards

### 2.1 Naming Conventions

```csharp
// Classes, Interfaces, Enums: PascalCase
public class FarmerAggregate { }
public interface IFarmerRepository { }
public enum CoffeeVariety { Robusta, Arabica, TR4 }

// Methods, Properties: PascalCase
public async Task<Farmer> GetByPhoneAsync(string phoneNumber) { }
public string FullName { get; private set; }

// Local variables, parameters: camelCase
var farmerId = command.FarmerId;
async Task HandleAsync(RegisterFarmerCommand command)

// Private fields: _camelCase
private readonly IFarmerRepository _farmerRepository;
private readonly ILogger<RegisterFarmerCommandHandler> _logger;

// Constants: PascalCase
private const int MaxOtpAttempts = 5;
private const string DefaultLanguage = "vi";

// Async methods: Always suffix Async
public async Task<Farmer> FindByIdAsync(string id)
// KHÔNG: public async Task<Farmer> FindById(string id)
```

### 2.2 File Organization

```csharp
// ✅ ĐÚNG: 1 class chính per file, file name = class name
// FarmerRepository.cs
namespace BazanAI.Identity.Infrastructure.Persistence.Repositories;

public class FarmerRepository : IFarmerRepository
{
    private readonly IMongoCollection<Farmer> _collection;

    public FarmerRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Farmer>("farmers");
    }

    public async Task<Farmer?> FindByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        return await _collection
            .Find(f => f.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Farmer?> FindByPhoneAsync(
        string phoneNumber,
        CancellationToken ct = default)
    {
        return await _collection
            .Find(f => f.PhoneNumber == phoneNumber)
            .FirstOrDefaultAsync(ct);
    }
}
```

### 2.3 Clean Architecture Rules

```csharp
// Domain — không import bất kỳ thứ gì ngoài System.*
// Application — chỉ import Domain và MediatR
// Infrastructure — implement interfaces từ Domain/Application
// API — chỉ gọi Application commands/queries

// ✅ ĐÚNG: Application chỉ phụ thuộc Domain
using BazanAI.Identity.Domain.Entities;
using BazanAI.Identity.Domain.Repositories;

// ❌ SAI: Application không import Infrastructure
using BazanAI.Identity.Infrastructure.Persistence; // KHÔNG!

// ✅ ĐÚNG: Dependency Injection
public class RegisterFarmerCommandHandler(
    IFarmerRepository farmerRepo,  // Interface, không phải implementation
    IEventPublisher eventPublisher,
    ILogger<RegisterFarmerCommandHandler> logger)
    : IRequestHandler<RegisterFarmerCommand, Result<string>>
```

### 2.4 Error Handling

```csharp
// Dùng Result<T> pattern — không throw exception cho business errors
public async Task<Result<string>> Handle(
    RegisterFarmerCommand command,
    CancellationToken ct)
{
    // Validate
    var existing = await _farmerRepo.FindByPhoneAsync(command.PhoneNumber, ct);
    if (existing is not null)
        return Result<string>.Failure("phone_already_registered",
            "Số điện thoại này đã được đăng ký");

    // Business logic
    var farmer = Farmer.Register(command.PhoneNumber, command.FullName);
    await _farmerRepo.SaveAsync(farmer, ct);

    return Result<string>.Success(farmer.Id);
}

// Chỉ throw exception cho infrastructure errors (DB timeout, network)
// Middleware sẽ catch và return 500
```

### 2.5 Async/Await Rules

```csharp
// ✅ ĐÚNG: async all the way down
public async Task<IActionResult> Register(
    RegisterFarmerCommand command,
    CancellationToken ct)
{
    var result = await _mediator.Send(command, ct);
    return result.IsSuccess
        ? Created($"/api/v1/farmers/{result.Value}", result.Value)
        : BadRequest(result.Error);
}

// ❌ SAI: .Result hoặc .Wait() — có thể deadlock
var farmer = _farmerRepo.FindByIdAsync(id).Result; // KHÔNG!

// ✅ ĐÚNG: ConfigureAwait trong library code (không trong ASP.NET Core)
// ASP.NET Core không cần ConfigureAwait(false)

// ✅ ĐÚNG: Pass CancellationToken xuống mọi async call
await _collection.InsertOneAsync(farmer, null, ct);
```

### 2.6 Logging

```csharp
// ✅ ĐÚNG: Structured logging với named parameters
_logger.LogInformation(
    "Farmer {FarmerId} registered from {Province}",
    farmer.Id, command.Province);

// ✅ ĐÚNG: Log exception với context
_logger.LogError(ex,
    "Failed to process {CommandType} for farmer {FarmerId}",
    nameof(RegisterFarmerCommand), command.FarmerId);

// ❌ SAI: String interpolation — mất structured logging!
_logger.LogInformation($"Farmer {farmer.Id} registered"); // KHÔNG!

// ❌ SAI: Log PII
_logger.LogInformation("Farmer phone {Phone}", farmer.PhoneNumber); // KHÔNG!

// ✅ ĐÚNG: Mask PII nếu cần log
_logger.LogInformation("Farmer {PhoneMasked} registered",
    MaskPhone(farmer.PhoneNumber)); // +849****67
```

### 2.7 Testing Conventions

```csharp
// File: RegisterFarmerCommandHandlerTests.cs
// Pattern: Arrange_Act_Assert + MethodName_Scenario_ExpectedResult

[Fact]
public async Task Handle_WhenPhoneAlreadyRegistered_ReturnsFailure()
{
    // Arrange
    var existingFarmer = FarmerBuilder.Create().WithPhone("+84901234567").Build();
    _farmerRepoMock.FindByPhoneAsync("+84901234567", Arg.Any<CancellationToken>())
                   .Returns(existingFarmer);

    var command = new RegisterFarmerCommand
    {
        PhoneNumber = "+84901234567",
        FullName = "Test Farmer"
    };

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.ErrorCode.Should().Be("phone_already_registered");
}
```

---

## 3. Python 3.12 Standards

### 3.1 Naming Conventions

```python
# Modules, packages: snake_case
hybrid_search_service.py
document_loader/

# Classes: PascalCase
class HybridSearchService:
class DocumentIndexer:

# Functions, methods, variables: snake_case
async def search_agronomy_knowledge(query: str) -> SearchResult:
total_chunks = len(chunks)

# Constants: UPPER_SNAKE_CASE
MAX_CHUNK_SIZE = 512
DEFAULT_COLLECTION = "agronomy_knowledge"

# Private: _prefix
_qdrant_client: QdrantClient
async def _embed_text(text: str) -> list[float]:
```

### 3.2 Type Hints — Bắt buộc

```python
# ✅ ĐÚNG: Đầy đủ type hints
from __future__ import annotations

async def search(
    query: str,
    collection: str,
    limit: int = 5,
    filters: Filter | None = None,
) -> list[SearchResult]:
    ...

# ✅ ĐÚNG: Dùng Pydantic cho data models
from pydantic import BaseModel, Field

class SearchResult(BaseModel):
    chunk_id: str
    text: str
    score: float = Field(ge=0.0, le=1.0)
    source_title: str
    page_start: int
    page_end: int

# ❌ SAI: No type hints
def search(query, collection, limit=5):  # KHÔNG!
```

### 3.3 Async Best Practices

```python
# ✅ ĐÚNG: Parallel async operations
import asyncio

dense_vector, sparse_vector = await asyncio.gather(
    openai_embedder.embed(query),
    sparse_embedder.embed(query),
)

# ✅ ĐÚNG: Proper exception handling trong async
async def index_document(minio_path: str) -> IndexResult:
    try:
        content = await minio_client.download(minio_path)
        chunks = chunker.split(content)
        embedded = await batch_embedder.embed(chunks)
        await qdrant_indexer.upsert(embedded)
        return IndexResult(success=True, chunks_count=len(chunks))
    except MinioError as e:
        logger.error("Failed to download document",
                     path=minio_path, error=str(e))
        raise IndexingError(f"Download failed: {e}") from e
    except QdrantError as e:
        logger.error("Failed to upsert to Qdrant", error=str(e))
        raise IndexingError(f"Upsert failed: {e}") from e
```

### 3.4 Logging với structlog

```python
import structlog

logger = structlog.get_logger(__name__)

# ✅ ĐÚNG: Structured logging
logger.info("search_completed",
            query_length=len(query),
            collection=collection,
            results_count=len(results),
            latency_ms=elapsed_ms)

# ✅ ĐÚNG: Log exception context
logger.error("embedding_failed",
             text_length=len(text),
             model=model_name,
             error=str(e),
             exc_info=True)

# ❌ SAI: f-string logging
logger.info(f"Search completed: {len(results)} results")  # KHÔNG!
```

### 3.5 Code Organization

```python
# ✅ ĐÚNG: Service class với dependency injection
class HybridSearchService:
    def __init__(
        self,
        qdrant_client: QdrantClient,
        openai_embedder: OpenAIEmbedder,
        sparse_embedder: SparseEmbedder,
    ) -> None:
        self._qdrant = qdrant_client
        self._dense = openai_embedder
        self._sparse = sparse_embedder

    async def search(
        self,
        query: str,
        collection: str,
        limit: int = 5,
        filters: Filter | None = None,
    ) -> list[SearchResult]:
        # 1 method = 1 responsibility
        dense_vec, sparse_vec = await self._generate_embeddings(query)
        raw_results = await self._execute_search(
            collection, dense_vec, sparse_vec, filters, limit
        )
        return self._parse_results(raw_results)

    async def _generate_embeddings(
        self, query: str
    ) -> tuple[list[float], SparseVector]:
        return await asyncio.gather(
            self._dense.embed(query),
            self._sparse.embed(query),
        )
```

### 3.6 Tooling

```toml
# pyproject.toml
[tool.ruff]
line-length = 100
target-version = "py312"
select = ["E", "F", "I", "N", "UP", "ANN"]

[tool.black]
line-length = 100
target-version = ["py312"]

[tool.mypy]
python_version = "3.12"
strict = true
ignore_missing_imports = false

[tool.pytest.ini_options]
asyncio_mode = "auto"
```

```bash
# Run trước khi commit
ruff check src/
black --check src/
mypy src/
pytest tests/ -v
```

---

## 4. General Conventions

### 4.1 Comments & Documentation

```csharp
// ✅ ĐÚNG: Comment WHY, không phải WHAT
// OTP comparison dùng time-constant để tránh timing attack
if (!CryptographicOperations.FixedTimeEquals(
    Encoding.UTF8.GetBytes(inputOtp),
    Encoding.UTF8.GetBytes(storedOtp)))

// ❌ SAI: Comment không value
// Get farmer by ID
var farmer = await _repo.FindByIdAsync(id);

// ✅ ĐÚNG: XML doc cho public API methods
/// <summary>
/// Register a new farmer and send OTP for phone verification.
/// </summary>
/// <param name="command">Registration details</param>
/// <returns>Farmer ID on success, error code on failure</returns>
public async Task<Result<string>> Handle(RegisterFarmerCommand command)
```

### 4.2 No Magic Values

```csharp
// ❌ SAI
if (attempts > 5) { ... }
var token = GenerateToken(24 * 60); // 24 giờ?
await Task.Delay(1000); // 1 giây?

// ✅ ĐÚNG
private const int MaxOtpAttempts = 5;
private const int AccessTokenExpiryHours = 24;
private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

if (attempts > MaxOtpAttempts) { ... }
var token = GenerateToken(TimeSpan.FromHours(AccessTokenExpiryHours));
await Task.Delay(PollingInterval);
```

### 4.3 Security Reminders trong Code

```csharp
// ✅ ĐÚNG: Annotate security-sensitive code
// SECURITY: Using time-constant comparison to prevent timing attacks
if (!CryptographicOperations.FixedTimeEquals(...))

// ✅ ĐÚNG: Mark sensitive data không log
// DO NOT LOG: Contains PII (phone number)
[SensitiveData]
public string PhoneNumber { get; private set; }

// ✅ ĐÚNG: Validate input trước khi dùng
// Mọi input từ user phải qua FluentValidation trước khi Handle()
```

---

## 5. Code Review Standards

Xem `docs/team/code-review-checklist.md` để có checklist đầy đủ.

**TL;DR cho reviewer:**
```
□ Logic đúng không?
□ Tests cover happy path và edge cases?
□ Security: không có PII trong log, input validated?
□ Naming rõ ràng không?
□ Không có hardcoded credentials/values?
□ Error handling đầy đủ?
□ Không break Clean Architecture boundaries?
```

---

## 6. .editorconfig (áp dụng tự động)

```ini
# .editorconfig — Áp dụng tự động trong Rider/VS/VSCode
root = true

[*]
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_size = 4
charset = utf-8-bom

[*.py]
indent_size = 4
charset = utf-8

[*.{json,yaml,yml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

---

**© 2026 Bazan AI — Internal Document**

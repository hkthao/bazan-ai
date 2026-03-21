# GEMINI.md — Bazan AI Project Context

This file provides permanent project context for Gemini CLI. Read completely before starting any task. Sprint-specific requirements are in `docs/sprints/`.

---

## 1. Project Overview

**Bazan AI** is an Agentic Microservices platform for coffee farmers in Vietnam's Central Highlands. It delivers AI-driven cultivation advice, market intelligence, and agronomy support.

**North Star Metric:** Weekly Active Value Users (WAVU) — farmers with ≥ 1 quality AI session/week with thumbs-up feedback.

---

## 2. Services & Ports

| Service | Lang | Port | Responsibility |
|---------|------|------|---------------|
| `BazanAI.Gateway` | C# | 8080 | YARP API Gateway, JWT validation, rate limiting |
| `BazanAI.Identity` | C# | 5001 | Auth (OTP/OAuth/Email), Farmer profile, Farm CRUD |
| `BazanAI.Conversations` | C# | 5002 | Chat sessions, message history, RLHF feedback |
| `BazanAI.Agronomy` | C# | 5004 | Disease detection, irrigation, cultivation schedule |
| `BazanAI.Market` | C# | 5005 | Coffee prices, ROI calculator, buyer geo-search |
| `BazanAI.Notification` | C# | 5006 | Zalo OA, FCM, SMS dispatch |
| `BazanAI.IoT` | C# | 5007 | MQTT broker, sensor ingestion, DQA |
| `BazanAI.Weather` | C# | 5008 | Forecast polling, alerts |
| `BazanAI.Media` | C# | 5009 | MinIO file upload, image pre-processing |
| `BazanAI.Orchestrator` | C# | 5010 | Semantic Kernel AI agent, SignalR streaming |
| `BazanAI.Scheduler` | C# | 5011 | Hangfire jobs, RLHF export, backup |
| `bazan-knowledge-rag` | Python | 5003 | FastAPI RAG service, Qdrant hybrid search |

| Infrastructure | Port | Notes |
|---------------|------|-------|
| MongoDB Primary | 27017 | Replica Set `rs0` (3 nodes) |
| Qdrant HTTP/gRPC | 6333/6334 | Vector DB |
| Redis | 6379 | Cache, OTP, blacklist |
| RabbitMQ AMQP | 5672 | Messaging |
| RabbitMQ UI | 15672 | SSH tunnel only |
| MinIO API | 9000 | Object storage |
| Seq | 5341 | Structured logs — SSH tunnel only |
| Grafana | 3000 | Metrics — SSH tunnel only |

---

## 3. Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 8 / C# — Clean Architecture, CQRS (MediatR) |
| Mobile | **React Native** (TypeScript) — NOT Flutter |
| AI Service | Python 3.12 / FastAPI |
| Primary DB | MongoDB 7.0 Replica Set |
| Vector DB | Qdrant 1.9 (hybrid search: dense + BM42 sparse + RRF) |
| Cache | Redis 7 |
| Messaging | RabbitMQ 3.13 + MassTransit 8.x |
| AI Orchestration | Semantic Kernel 1.x + GPT-4o |
| Embeddings | OpenAI text-embedding-3-large (3072 dims) |
| Object Storage | MinIO |
| Observability | Serilog → Seq, Prometheus → Grafana, OpenTelemetry |
| Infrastructure | Docker Compose (Phase 1–3), Kubernetes (Phase 4) |

---

## 4. Project Structure

```
BazanAI/
├── .github/workflows/          # ci.yml, cd-staging.yml, cd-production.yml
├── docs/
│   ├── architecture/           # ADR-001..006, DFD, ERD, Sequence Diagrams
│   ├── api/openapi/            # OpenAPI specs per service
│   ├── ai/                     # RAG Pipeline, Prompt Playbook, Embedding Strategy
│   ├── team/                   # Coding Standards, Git Workflow, Testing Strategy
│   ├── sprints/                # Sprint task specs — READ before implementing
│   └── postman/                # Postman collections
├── infra/
│   ├── docker-compose.yml
│   ├── .env.example            # Template — NEVER commit .env
│   ├── mongo-init/             # RS init scripts, keyfile
│   ├── rabbitmq/               # definitions.json
│   └── qdrant/config.yaml
├── src/
│   ├── Python/bazan-knowledge-rag/
│   │   ├── app/api/            # FastAPI routers
│   │   ├── app/domain/         # Services, models
│   │   ├── app/infrastructure/ # Qdrant, OpenAI clients
│   │   └── tests/
│   ├── Services/
│   │   └── BazanAI.{Service}/
│   │       ├── BazanAI.{Service}.Domain/
│   │       ├── BazanAI.{Service}.Application/
│   │       ├── BazanAI.{Service}.Infrastructure/
│   │       └── BazanAI.{Service}.API/
│   └── SharedKernel/
│       ├── BazanAI.SharedKernel/           # Result<T>, domain primitives
│       └── BazanAI.Infrastructure.Common/  # MongoDB, Redis, RabbitMQ base
├── tests/
│   ├── Unit/
│   └── Integration/
├── BazanAI.sln
└── Makefile
```

---

## 5. Common Commands

```bash
# Start / stop full stack
make up && make health
make down

# First-time setup only
make gen-keyfile && make init-rs

# Hot reload a single service
docker compose stop {service-name}
cd src/Services/BazanAI.{Service}/BazanAI.{Service}.API
dotnet watch run --no-launch-profile

# Python RAG service
cd src/Python/bazan-knowledge-rag && uvicorn app.main:app --reload

# Run all tests
make test

# Run specific test suite
dotnet test tests/Unit/BazanAI.{Service}.Tests/ -v
dotnet test tests/Integration/ -v
cd src/Python/bazan-knowledge-rag && pytest tests/ -v

# Security check
trivy fs . --severity CRITICAL,HIGH
dotnet build BazanAI.sln -c Release

# MongoDB shell
docker exec bazan-mongo-1 mongosh \
  -u root -p ${MONGO_ROOT_PASSWORD} --authenticationDatabase admin

# Redis shell
docker exec bazan-redis redis-cli -a ${REDIS_PASSWORD}

# Generate secrets
openssl genrsa -out jwt_private.pem 2048
openssl rsa -in jwt_private.pem -pubout -out jwt_public.pem
openssl rand -hex 32   # PII encryption key or random secret
```

---

## 6. Development Conventions

### 6.1 C# Naming

```csharp
// Classes, Interfaces, Enums: PascalCase
public class FarmerAggregate { }
public interface IFarmerRepository { }

// Methods, Properties: PascalCase
// Async methods MUST end in Async — no exceptions
public async Task<Farmer> FindByIdAsync(string id, CancellationToken ct = default) { }

// Private fields: _camelCase
private readonly IFarmerRepository _farmerRepository;

// Local variables, parameters: camelCase
var farmerId = command.FarmerId;

// Constants: PascalCase
private const int MaxOtpAttempts = 5;
```

### 6.2 Clean Architecture — Dependency Rules

```
Domain ← Application ← Infrastructure / API

Domain:         No external deps. Entities, Value Objects, Domain Events.
Application:    CQRS via MediatR. Depends only on Domain. Result<T> for errors.
Infrastructure: Implements interfaces. MongoDB, Redis, RabbitMQ, HTTP clients.
API:            Controllers, Program.cs, DI registration.
```

Hard violations — never do:
- Application importing Infrastructure namespace
- Domain importing anything outside `System.*`
- Controller containing business logic

### 6.3 Result\<T\> Pattern — Business Errors

```csharp
// Business errors → Result<T>
// Infrastructure failures → throw exception
public async Task<Result<string>> Handle(MyCommand cmd, CancellationToken ct)
{
    var existing = await _repo.FindAsync(cmd.Id, ct);
    if (existing is not null)
        return Result.Failure("already_exists", "Tài nguyên đã tồn tại");

    return Result.Success(entity.Id);
}
```

### 6.4 Logging — Structured Only

```csharp
// CORRECT
logger.LogInformation("Farmer {FarmerId} from {Province} logged in", id, province);

// WRONG — string interpolation loses structured logging
logger.LogInformation($"Farmer {id} logged in");  // ❌

// WRONG — PII in logs
logger.LogInformation("Phone {Phone}", phoneNumber);  // ❌
```

### 6.5 Python — Type Hints & Logging

```python
# Type hints MANDATORY on all function signatures
async def search(query: str, collection: str, limit: int = 5) -> list[SearchResult]:
    ...

# Structured logging — never f-string
logger.info("search_completed", query_len=len(query), results=len(results))
# NOT: logger.info(f"Found {len(results)}")  ❌
```

---

## 7. Security Rules — Non-negotiable

```
NEVER commit to git:
  *.pem, *.key, .env, anything named *secret* or *credential*

NEVER log:
  Phone numbers, OTP values, JWT tokens,
  Passwords or hashes, Exact GPS coordinates

ALWAYS use:
  RS256 (asymmetric)      for JWT signing        — NEVER HS256
  AES-256-GCM             for PII encryption     — NEVER AES-CBC
  BCrypt cost ≥ 12        for passwords          — NEVER MD5/SHA256 bare
  time-constant comparison for secret comparison  — NEVER ==
  RandomNumberGenerator   for IVs and nonces     — NEVER sequential

PII fields that must be AES-256-GCM encrypted before MongoDB:
  ExternalIdentity.ProviderUserId (when provider = "phone_otp")
  Farmer.FullName
  Farmer.ZaloId

GPS coordinates: round to 0.01° (~1km) — do NOT encrypt, only round.
```

---

## 8. Key Domain Models

### 8.1 Farmer — Multi-Provider Identity

`phoneNumber` is **NOT** a root field. Phone lives inside `identities[]`.

```csharp
public class Farmer
{
    public string Id { get; private set; }
    public string FullName { get; private set; }       // encrypted
    public string? PrimaryEmail { get; private set; }
    public ConsentRecord PrivacyConsent { get; private set; }
    public IReadOnlyList<ExternalIdentity> Identities { get; private set; }

    public static Farmer RegisterWithPhone(
        string encryptedPhone, string fullName, ConsentRecord consent) { }

    public static Farmer RegisterWithOAuth(
        string provider, string providerUserId, string email, string fullName) { }

    public Result LinkIdentity(ExternalIdentity newIdentity) { }

    // Helper — use instead of querying phoneNumber directly
    public string? GetPhone()
        => Identities.FirstOrDefault(i => i.Provider == "phone_otp")?.ProviderUserId;
}

public record ExternalIdentity(
    string Provider,         // "phone_otp" | "google" | "email_password"
    string ProviderUserId,   // encrypted phone | google_uid | email
    string? Email,
    DateTime LinkedAt
);

public record ConsentRecord(
    bool Accepted, DateTime AcceptedAt,
    string PolicyVersion,    // e.g. "1.0.0"
    string AcceptedFromIp
);
```

### 8.2 MongoDB Indexes — Identity

```javascript
// CORRECT — identity-based constraint
db.farmers.createIndex(
  { "identities.provider": 1, "identities.providerUserId": 1 },
  { unique: true, sparse: true, name: "idx_identity_provider_unique" }
)

// WRONG — phoneNumber is not a root field
// db.farmers.createIndex({ "phoneNumber": 1 })  ❌
```

### 8.3 JWT Claims

```json
{
  "sub": "farmerId",  "role": "farmer",
  "farm_ids": ["..."], "province": "Dak Lak",
  "jti": "uuid-v4",   "iss": "bazan-ai", "aud": "bazan-ai-services"
}
// NEVER include: phoneNumber, fullName, coordinates
```

### 8.4 Redis Key Naming

```
otp:{phoneNumber}              TTL 300s   — hashed OTP value
otp_attempts:{phoneNumber}     TTL 900s   — integer attempt count
blacklist:{jti}                TTL = token remaining expiry
oauth_state:{nonce}            TTL 300s   — CSRF protection
cache:price:{coffeeType}       TTL 300s
cache:embedding:{sha256}       TTL 3600s
```

### 8.5 RabbitMQ Routing Key Format

```
Pattern: bazan.{domain}.{entity}.{action}

Examples:
  bazan.farmer.registered
  bazan.farmer.location.updated
  bazan.agronomy.disease.alert.generated
  bazan.market.price.updated
  bazan.iot.sensor.data.received
  bazan.weather.alert.triggered
  bazan.conversation.feedback.collected
```

---

## 9. Testing Conventions

| Layer | Coverage Target | Method |
|-------|----------------|--------|
| Domain | ≥ 90% | Unit tests |
| Application | ≥ 80% | Unit tests |
| Infrastructure | ≥ 60% | Integration tests |
| Overall | ≥ 70% | CI gate |

```csharp
// Unit test naming: MethodName_WhenScenario_ExpectedResult
// Mocking: NSubstitute  |  Assertions: FluentAssertions

[Fact]
public async Task Handle_WhenEntityNotFound_ReturnsFailure()
{
    // Arrange
    _repo.FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
         .Returns((Entity?)null);
    // Act
    var result = await _sut.Handle(command, CancellationToken.None);
    // Assert
    result.IsSuccess.Should().BeFalse();
}

// Integration tests — always verify:
// - BOLA: cross-tenant access returns 403
// - PII roundtrip: encrypt → store → retrieve → decrypt → equals plaintext
```

---

## 10. CI/CD Pipeline

| Branch | Deploy | Gate |
|--------|--------|------|
| `feature/*`, `bugfix/*` | None | CI must pass |
| `develop` | Staging (auto) | CI + smoke test |
| `main` | Production (manual approval) | CI + full regression |

CI gates in order:
1. `dotnet build` (Release)
2. Unit tests — coverage ≥ 70%
3. Integration tests — TestContainers
4. `pytest` — Python RAG
5. `trivy fs` — zero CRITICAL vulns
6. AI eval regression — only with `ai-change` label

---

## 11. Automated Developer Workflow

**MANDATORY:** Create a GitHub issue before starting. Read the sprint spec before writing code.

### Preparation

```bash
# Read sprint task specification
cat docs/sprints/{sprint-file}.md

# Check or create issue
gh issue view {id}

# Sync and branch
git checkout develop && git pull origin develop
git checkout -b feature/{id}-{short-desc}
# e.g.: feature/42-multi-provider-identity
# Rules: lowercase, kebab-case, include issue ID
```

### Before Writing Code

- [ ] Sprint spec AC read completely
- [ ] ADR consulted if touching architecture (`docs/architecture/ADR-*.md`)
- [ ] Critical path dependencies in sprint spec checked
- [ ] Security rules reviewed (Section 7)

### Verify Before Commit

```bash
dotnet build BazanAI.sln -c Release
dotnet test tests/Unit/ -v
trivy fs . --severity CRITICAL,HIGH --exit-code 1

# Verify no PII in logs — expected: zero results
grep -rn "Log.*Phone\|Log.*phoneNumber\|Log.*otp\|Log.*password" src/ --include="*.cs"
```

### Commit Convention

```bash
# Format: {type}({scope}): {description}
git commit -m "feat(identity): add multi-provider ExternalIdentity schema"
git commit -m "fix(agronomy): correct disease threshold for TR4 variety"
git commit -m "test(market): add integration test for geospatial buyer query"
git commit -m "chore(infra): replace idx_phone_unique with idx_identity_provider"

# Types:  feat | fix | refactor | test | docs | chore | perf | style | revert
# Scopes: identity | agronomy | market | conversations | orchestrator |
#         rag | notification | iot | weather | gateway | shared | infra | docs
```

### Pull Request

```bash
git push origin {branch-name}

gh pr create \
  --base develop \
  --head {branch-name} \
  --title "[TYPE-{ID}] Short description" \
  --body "## Changes
- List of changes

## Related
- Closes #{ID}

## Checklist
- [ ] Sprint spec AC verified
- [ ] Unit tests added/updated
- [ ] No PII in logs (grep verified)
- [ ] No secrets hardcoded
- [ ] Clean Architecture boundaries respected
- [ ] Security-critical code reviewed by Engineering Lead
- [ ] OpenAPI spec updated (if endpoint changed)"
```

### Merge

```bash
gh pr checks {pr-number}
gh pr merge {pr-number} --squash --delete-branch
git checkout develop && git pull origin develop
```

---

## 12. Environment Variables Reference

```bash
# MongoDB
MONGO_ROOT_USER=admin
MONGO_ROOT_PASSWORD=<strong>

# Redis
REDIS_PASSWORD=<strong>

# JWT — RS256 asymmetric
JWT_PRIVATE_KEY_PATH=/run/secrets/jwt_private.pem
JWT_PUBLIC_KEY_PATH=/run/secrets/jwt_public.pem
JWT_ISSUER=bazan-ai
JWT_AUDIENCE=bazan-ai-services

# PII Encryption
PII_ENCRYPTION_KEY=<32-byte hex>     # openssl rand -hex 32
OTP_PEPPER=<32-char random>

# Internal service-to-service
SERVICE_API_KEY_ORCHESTRATOR=<32-char>
SERVICE_API_KEY_AGRONOMY=<32-char>

# External APIs
OPENAI_API_KEY=sk-...
ZALO_OA_ACCESS_TOKEN=...
GOOGLE_CLIENT_ID=xxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-xxx
TWILIO_ACCOUNT_SID=...
TWILIO_AUTH_TOKEN=...
OPENWEATHER_API_KEY=...
ICE_FUTURES_API_KEY=...
MINIO_ACCESS_KEY=...
MINIO_SECRET_KEY=...
```

---

## 13. Architectural Decisions (Summary)

Read full ADRs in `docs/architecture/` when implementing related features.

| ADR | Decision | Key constraint |
|-----|---------|---------------|
| ADR-001 | MongoDB primary DB | Replica Set required for transactions + Change Streams |
| ADR-002 | Qdrant vector DB | Hybrid search: dense + BM42 sparse + RRF mandatory |
| ADR-003 | RabbitMQ + MassTransit | Outbox pattern for reliability; idempotent consumers required |
| ADR-004 | Semantic Kernel | AutoFunctionCalling; token budget enforced per conversation |
| ADR-005 | YARP API Gateway | JWT validated at gateway; `X-Farmer-Id` forwarded to services |
| ADR-006 | Offline-first mobile | 3-tier cache; client outbox for offline writes |

---

## 14. Permanent Schema Constraints

These apply across all sprints. Violating them creates migration debt on live data.

**Farmer schema — phone is NOT a root field:**
```
❌ db.farmers.phoneNumber = "+84..."
✅ db.farmers.identities[{ provider: "phone_otp" }].providerUserId = "<encrypted>"
```

**ConsentRecord is mandatory when creating Farmer:**
```
Legal requirement per Nghị định 13/2023.
Never create Farmer without a valid ConsentRecord.
```

**OAuth callback — never re-implement platform flow:**
```
ASP.NET Core AddGoogle() handles: redirect, code exchange, token verify, CSRF.
Our callback only does: read User.Claims → upsert Farmer → issue JWT.
Do not add code that re-implements what the middleware already does.
```

**Mobile stack is React Native — never Flutter:**
```
Secure storage:  react-native-keychain  (NOT AsyncStorage)
Google OAuth:    @react-native-google-signin/google-signin
Form handling:   react-hook-form + zod
```

---

*For task requirements and acceptance criteria → `docs/sprints/{sprint}.md`*
*For architectural rationale → `docs/architecture/ADR-*.md`*
*This file covers permanent conventions only.*

---

## 15. AI Agent Operating Mode

Gemini CLI operates as a **Senior Software Engineer** on this project. Default behavior is fully autonomous unless a decision is explicitly irreversible or high-risk.

### Autonomy Levels

| Situation | Behavior |
|-----------|---------|
| Implement sprint task | Fully autonomous — read spec, code, test, commit, PR |
| Refactor within a service | Autonomous — no approval needed |
| Add new file or class | Autonomous |
| Modify shared kernel | Ask before proceeding — affects all services |
| Schema migration on live data | Ask before proceeding — data loss risk |
| Delete files | Ask before proceeding |
| Change CI/CD pipeline | Ask before proceeding |
| Merge to `main` | Always ask for explicit approval |
| External API credentials | Never touch — ask human to provide |

### Default Assumptions When Spec Is Ambiguous

```
Language not specified → C# for services, TypeScript for mobile
Pattern not specified  → follow existing pattern in same service
Test framework         → NSubstitute + FluentAssertions (C#), pytest (Python)
Error handling         → Result<T> for business, throw for infrastructure
Logging level          → Information for business events, Warning for retries
```

### Autonomous Decision Log

After completing any task autonomously, append a summary to the PR description:

```
## Autonomous Decisions
- Used X pattern because Y (spec did not specify)
- Added Z index because query would COLLSCAN without it
- Chose BCrypt cost 12 per GEMINI.md security rules
```

---

## 16. GitHub Issue Execution Protocol

When told "work on issue #X" or "implement task Y", execute this exact sequence without skipping steps.

### Phase 1 — Understand (do not skip)

```bash
# 1. Read the issue
gh issue view {id}

# 2. Read the referenced sprint spec
cat docs/sprints/{sprint-file}.md

# 3. Read relevant ADR if architectural
cat docs/architecture/ADR-{N}-{name}.md

# 4. Read existing code in the service being modified
# Do NOT assume — read before writing
find src/Services/BazanAI.{Service} -name "*.cs" | head -20
```

### Phase 2 — Plan (state this out loud before coding)

```
Before writing code, output:
  "PLAN:
   - Files to create: [list]
   - Files to modify: [list]
   - Tests to write: [list]
   - Dependencies / blockers: [list]
   - Estimated risk: Low / Medium / High"
```

If risk is **High** (schema change, shared kernel, security) — pause and ask for confirmation.

### Phase 3 — Implement

```bash
git checkout develop && git pull origin develop
git checkout -b feature/{id}-{short-desc}

# Implement in this order:
# 1. Domain entities / value objects (no dependencies)
# 2. Application commands / queries / handlers
# 3. Infrastructure (repositories, clients)
# 4. API (controllers, registrations)
# 5. Tests (unit first, then integration)
```

### Phase 4 — Verify (all must pass)

```bash
dotnet build BazanAI.sln -c Release
dotnet test tests/Unit/ -v
dotnet test tests/Integration/ -v   # if DB/messaging touched
trivy fs . --severity CRITICAL,HIGH --exit-code 1
grep -rn "Log.*Phone\|Log.*phoneNumber\|Log.*otp" src/ --include="*.cs"
# Above grep must return zero results
```

### Phase 5 — Ship

```bash
git push origin {branch}
gh pr create --base develop --head {branch} --title "[FEAT-{id}] ..." --body "..."
gh pr checks {pr-number}   # wait for CI
# Then ask human: "PR #{number} is ready. Approve to merge?"
```

---

## 17. Multi-Role Simulation

Gemini CLI switches roles at defined points in the workflow. Each role has a distinct mindset.

### Role 1 — Developer 🛠

**Active during:** Phase 3 (Implement)

Mindset: "Make it work correctly, follow the spec exactly."

```
Rules:
  - Implement only what the AC specifies — no gold-plating
  - Follow Clean Architecture boundaries strictly
  - Write just enough code to pass all AC
  - Leave no TODO comments without a linked issue
```

### Role 2 — Reviewer 🔍

**Active during:** Phase 4 (Verify), before creating PR

Mindset: "Would I approve this PR if someone else wrote it?"

```
Reviewer checklist — read diff as if seeing it for the first time:

Security:
  □ No PII in logs
  □ No hardcoded secrets
  □ Cryptography uses correct algorithms (RS256, AES-256-GCM, BCrypt)
  □ Authorization check at resource level, not only role level

Architecture:
  □ No Clean Architecture boundary violations
  □ No business logic in controllers
  □ Result<T> for business errors, not exceptions

Quality:
  □ No dead code or commented-out blocks
  □ No magic numbers — all constants named
  □ Method length ≤ 30 lines (if longer, extract)
  □ All public methods have XML doc comments

Tests:
  □ Happy path covered
  □ Error cases covered
  □ Edge cases covered (null, empty, boundary values)
  □ No test that always passes (assert something meaningful)
```

If any box is unchecked → fix before creating PR, not after.

### Role 3 — QA 🧪

**Active during:** After PR is merged to staging

Mindset: "Does this actually work end-to-end from a farmer's perspective?"

```
QA verification steps:
  1. Hit the endpoint manually via Postman collection (docs/postman/)
  2. Verify Seq logs: no PII, correct structured fields
  3. Verify MongoDB: data persisted correctly (including encryption)
  4. Verify event published to RabbitMQ (if applicable)
  5. Verify downstream consumers received the event

If QA fails → create a bugfix issue immediately, do not close the feature issue.
```

---

## 18. Code Generation Rules

Rules that prevent breaking existing structure when generating new code.

### Never Break These

```
1. SharedKernel public interfaces
   — Any change to IFarmerRepository, Result<T>, base entities
   → affects ALL services. Ask before changing.

2. MongoDB document schemas with live data
   — Adding a required field without default value → all existing documents break
   → Always add new fields as optional (?) with a default value first

3. RabbitMQ event contracts
   — Removing or renaming fields in published events → consumer breaks
   → Events are append-only. Never remove fields. Deprecate with [Obsolete] first.

4. JWT claim names
   — "sub", "role", "farm_ids", "province", "jti"
   → Renaming a claim breaks Gateway + all services simultaneously

5. Public API response fields
   — Removing a field breaks mobile clients
   → Follow API versioning — add to new version, don't remove from current
```

### File Creation Rules

```
New service file placement:
  Domain entity          → BazanAI.{Service}.Domain/Entities/
  Value object           → BazanAI.{Service}.Domain/ValueObjects/
  Domain event           → BazanAI.{Service}.Domain/Events/
  Command / Query        → BazanAI.{Service}.Application/Commands/ or /Queries/
  Command Handler        → same folder as Command, named {Command}Handler.cs
  Repository interface   → BazanAI.{Service}.Domain/Repositories/
  Repository impl        → BazanAI.{Service}.Infrastructure/Persistence/
  Controller             → BazanAI.{Service}.API/Controllers/
  Unit test              → tests/Unit/BazanAI.{Service}.Tests/
  Integration test       → tests/Integration/BazanAI.{Service}.IntegrationTests/

One class per file. File name = class name. No exceptions.
```

### Code Modification Rules

```
Before modifying an existing file:
  1. Read the full file — understand all existing methods
  2. Check if a base class in SharedKernel already provides the behavior
  3. Prefer extending over modifying if the class is used by multiple services

When adding a method to an existing class:
  1. Place it in logical order (public before private, CRUD in CRUD order)
  2. Follow the naming pattern of existing methods in the same file
  3. Add XML doc comment if the class already has them

When adding a new dependency:
  1. Check if the same library is already in another service's .csproj
  2. Use the same version — never introduce a different version of the same package
```

---

## 19. Branch & Commit Enforcement

### Branch Rules

```
Allowed branches:
  feature/{issue-id}-{kebab-desc}    e.g. feature/42-multi-provider-identity
  bugfix/{issue-id}-{kebab-desc}     e.g. bugfix/55-otp-rate-limit-reset
  hotfix/{issue-id}-{kebab-desc}     e.g. hotfix/71-jwt-expiry-timezone
  chore/{description}                e.g. chore/update-nuget-packages
  docs/{description}                 e.g. docs/update-rag-pipeline-spec

Forbidden:
  Branching from main directly (use develop)
  Branch names without issue ID (except chore/ and docs/)
  Working on develop directly
  Reusing a branch after it has been merged
```

### Commit Rules

```bash
# Format: {type}({scope}): {imperative description}
# Description: present tense, no period, max 72 chars

# Types
feat     — new feature
fix      — bug fix
refactor — code change that neither fixes bug nor adds feature
test     — adding or updating tests
docs     — documentation only
chore    — build process, dependencies, CI
perf     — performance improvement
style    — formatting only (no logic change)
revert   — reverts a previous commit

# Scopes — match service name
identity | agronomy | market | conversations | orchestrator |
rag | notification | iot | weather | gateway | shared | infra | docs

# Good examples
feat(identity): add Google OAuth callback handler
fix(agronomy): correct TR4 disease threshold calculation
test(market): add BOLA test for buyer geo-search endpoint
chore(infra): replace phoneNumber index with identity provider index

# Bad examples — do not use
git commit -m "fix bug"                   ❌ no scope, no description
git commit -m "WIP"                       ❌ never commit WIP
git commit -m "feat: add stuff"           ❌ no scope
git commit -m "FEAT(Identity): Add Auth"  ❌ wrong case
```

### Commit Atomicity

```
Each commit must be independently deployable and meaningful.

Good:  1 commit = "feat(identity): add ExternalIdentity domain model"
       1 commit = "test(identity): add unit tests for RegisterWithPhone"

Bad:   1 commit = "add identity stuff, fix tests, update db" ❌
       Mix of feat + test + fix in one commit ❌
```

---

## 20. Self-Review Checklist

Run this checklist mentally before creating any PR. If any item fails, fix it first.

### Architecture

```
□ Clean Architecture boundaries not violated
  (Application does not import Infrastructure namespace)
□ No business logic in API controllers
□ Domain entities do not have public setters
□ All dependencies injected via constructor (no service locator)
□ New interfaces added to Domain or Application layer, not Infrastructure
```

### Security

```
□ No PII (phone, name, GPS, OTP, token) appears in any log call
□ No secret, key, or password hardcoded in any file
□ No .env, .pem, or .key file staged for commit
  (run: git diff --cached --name-only | grep -E "\.env|\.pem|\.key")
□ Cryptography uses project-standard algorithms (Section 7)
□ Authorization checked at resource level, not just endpoint level
□ Input validated before reaching business logic (FluentValidation)
```

### Data Integrity

```
□ New MongoDB fields are nullable or have default values
  (no required fields added without migration plan)
□ RabbitMQ events only have fields added, never removed
□ Public API responses only have fields added, never removed
□ Farmer.phoneNumber not referenced as root field anywhere in new code
□ ConsentRecord included when creating Farmer
```

### Tests

```
□ Every new public method in Application layer has at least 1 unit test
□ Every new endpoint has at least 1 integration test
□ Test coverage has not decreased (run coverage report)
□ No test uses Thread.Sleep — use mocked time or async waits
□ All test assertions are meaningful (not just Assert.True(true))
```

### Ops

```
□ New service dependencies added to docker-compose.yml if needed
□ New environment variables added to .env.example with comments
□ OpenAPI spec updated if any endpoint was added or changed
□ New Hangfire jobs registered in Program.cs scheduler
```

---

## 21. Failure Handling Rules

What to do when things go wrong during task execution.

### Build Failure

```
1. Read the full error — do not guess
2. Fix only the reported error — do not refactor unrelated code
3. Re-run build to confirm fix
4. If failure is in SharedKernel → stop and ask before continuing
   (SharedKernel changes affect all services)
```

### Test Failure

```
1. Read the failing assertion — understand what it expected vs got
2. Determine cause: wrong implementation OR wrong test?
   - If implementation is wrong → fix implementation
   - If test assumption was wrong → fix test AND document why
3. Never delete a failing test — fix it or create a tracking issue
4. Never add [Ignore] or [Skip] without a comment explaining why
```

### CI Failure on PR

```
1. Run gh pr checks {pr-number} to see which check failed
2. Pull the branch and reproduce locally
3. Fix and push — CI re-runs automatically
4. If Trivy finds a CRITICAL vulnerability in a dependency:
   a. Check if newer version is available
   b. Update the package
   c. Re-run tests to ensure compatibility
   d. If no fix available → document in PR and ask human to decide
```

### Merge Conflict

```
1. Always rebase onto develop — never merge develop into feature branch
   git fetch origin && git rebase origin/develop
2. Resolve conflict by understanding BOTH changes:
   - Read git log of the conflicting commit
   - Understand what the other change was doing
   - Merge semantically, not textually
3. After conflict resolution: run full test suite before pushing
4. If conflict is in SharedKernel or critical path → ask human to review resolution
```

### Runtime Error on Staging

```
1. Check Seq logs first (SSH tunnel: ssh -L 5341:localhost:5341 deploy@staging)
2. Find the CorrelationId from the error response
3. Search Seq: CorrelationId = "{id}"  — this traces the full request
4. If data corruption suspected → do NOT run more requests
   Stop and assess before proceeding
5. Create a bugfix issue immediately with:
   - Error message
   - CorrelationId
   - Steps to reproduce
   - Suspected root cause
```

### Blocked by Dependency

```
If a task cannot start because a dependency is not complete:
  1. Comment on the GitHub issue: "Blocked by #X — {dependency task}"
  2. Check if any subtask can be done in isolation
  3. Write tests for the blocked feature using mocks
     (tests can be written before implementation)
  4. Ask human to reprioritize if blocking critical path
```

---

## 22. Multi-Service Awareness

Before implementing any task, identify which services are involved.

### Service Interaction Map

```
Request enters: Gateway (8080)
    │
    ├── Auth requests → Identity (5001)
    ├── Chat requests → Orchestrator (5010) ──► Knowledge RAG (5003)
    │                                       ──► Agronomy (5004)
    │                                       ──► Market (5005)
    │                                       ──► Weather (5008)
    ├── Market data   → Market (5005)
    ├── Agronomy data → Agronomy (5004)
    ├── Sensor data   → IoT (5007) ──► RabbitMQ ──► Agronomy, Identity
    └── Files         → Media (5009) ──► MinIO
                                     ──► Agronomy (CV model)

Events flow via RabbitMQ:
  Identity  ──publishes──► FarmerRegistered, LocationUpdated
  Agronomy  ──publishes──► DiseaseAlertGenerated, IrrigationReminderCreated
  Market    ──publishes──► PriceUpdated
  IoT       ──publishes──► SensorDataReceived
  Weather   ──publishes──► WeatherAlertTriggered
```

### Cross-Service Change Impact Assessment

Before modifying any service, check:

```
Modifying Identity Service?
  → Gateway depends on JWT claims format
  → Orchestrator calls /farmers/{id}/context
  → All services validate JWT with Identity's public key
  → Test: does Gateway still route correctly after change?

Modifying SharedKernel?
  → ALL services are affected
  → Run full test suite: dotnet test BazanAI.sln
  → Do not merge without all services building and passing

Modifying RabbitMQ event contracts?
  → Identify all consumers in RabbitMQ Event Catalog (docs/api/)
  → Add new fields only — never remove or rename
  → Update all consumer handlers before publishing new field

Modifying Gateway YARP routing?
  → Test all routes after change
  → Verify JWT validation still blocks unauthorized requests
  → Verify rate limiting still applies
```

### Service Call Patterns

```csharp
// Service calling another service — always use typed HTTP client
// Registered in DI: services.AddHttpClient<IAgronomyServiceClient, AgronomyServiceClient>()

// Always pass CancellationToken — never fire-and-forget in request pipeline
var result = await _agronomyClient.DiagnoseAsync(farmId, symptoms, ct);

// Always apply Polly circuit breaker for external service calls
// Config in SharedKernel/Extensions/PollyExtensions.cs

// Internal service calls use X-Service-Api-Key header, NOT JWT
// Never forward a farmer's JWT to another internal service
```

---

## 23. AI/RAG Integration Rules

Rules for working on Orchestrator, Knowledge RAG, and any AI-related code.

### Semantic Kernel — Plugin Rules

```csharp
// Plugin methods must be:
// 1. Async
// 2. Return string (LLM reads text, not objects)
// 3. Have [KernelFunction] and [Description] attributes
// 4. Description must explain WHEN to call this, not just what it does

[KernelFunction("get_disease_diagnosis")]
[Description("Call this when farmer describes plant symptoms or uploads a disease photo. Returns diagnosis with treatment plan. Do NOT call for general farming questions.")]
public async Task<string> GetDiseaseDiagnosis(
    [Description("Farm ID of the affected farm")] string farmId,
    [Description("Symptom description in Vietnamese or English")] string symptoms)
{
    var result = await _agronomyClient.DiagnoseAsync(farmId, symptoms);
    // Return concise text — NOT full JSON object (wastes tokens)
    return $"Bệnh: {result.DiseaseName} (confidence: {result.Confidence:P0})\n" +
           $"Phác đồ: {result.Treatment}\nNguồn: {result.SourceDocument}";
}

// Plugin return values must be under 500 tokens
// Never return raw API responses or full document content
```

### Prompt Rules

```
Prompt files location: src/Services/BazanAI.Orchestrator/Prompts/v{N}/

Version prompts — never edit in place:
  v1/system.txt  → current production
  v2/system.txt  → next version (A/B test or staged rollout)

Never hardcode prompts in C# strings.
Never include PII in prompts — use pseudonyms or role descriptions.

Token budget per conversation (enforced in code):
  System prompt:    1,200 tokens max
  Farmer context:     300 tokens max
  Conversation history: 2,000 tokens max
  RAG retrieved:    2,000 tokens max
  User query:         200 tokens max
  Total:            5,700 tokens max
```

### RAG Pipeline Rules

```python
# Hybrid search is mandatory — never pure dense or pure sparse
results = await qdrant_client.query_points(
    collection_name=collection,
    prefetch=[
        Prefetch(query=dense_vector, using="dense", limit=20),
        Prefetch(query=sparse_vector, using="sparse", limit=20),
    ],
    query=FusionQuery(fusion=Fusion.RRF),
    with_payload=True,
    limit=5,
)

# Always apply payload filter before search — reduces search space
# Filter on: coffee_varieties, region, growth_stages, language, is_deprecated=false

# Never index without metadata annotation
# Required fields: coffee_varieties, topics, region, language, source_year, document_type
# Never index a document where is_deprecated=True would apply
```

### Safety Rules for AI Output

```
Before returning any AI response to the client:
  1. Check for banned chemicals (hardcoded list — never LLM-dependent)
  2. Verify response contains citation when making technical claims
  3. Confidence score included for any disease diagnosis
  4. If AI response would recommend a dosage > 150% of label → intercept and flag

Banned chemical check must run in code, not in prompt:
  // In OrchestratorService.ValidateAiResponse()
  foreach (var banned in BannedChemicals.List)
      if (response.Contains(banned, StringComparison.OrdinalIgnoreCase))
          return SafetyViolationResult(banned);
```

---

## 24. Definition of Done

A task is **Done** only when ALL of the following are true. Partial completion is not Done.

### Code Complete

```
□ All Acceptance Criteria from sprint spec pass
□ No TODO comments without linked GitHub issue
□ No commented-out code blocks
□ No unused using statements or imports
□ All new public methods have XML doc comments (C#) or docstrings (Python)
```

### Tests Pass

```
□ Unit tests: all pass, coverage not decreased
□ Integration tests: all pass (if DB/messaging touched)
□ No test marked [Ignore] or [Skip] without explanation
□ CI pipeline: all checks green
```

### Security Verified

```
□ Trivy scan: zero CRITICAL vulnerabilities
□ PII grep: zero results
   grep -rn "Log.*Phone\|Log.*phoneNumber\|Log.*otp" src/ --include="*.cs"
□ No secrets in git diff
   git diff --cached --name-only | grep -E "\.env|\.pem|\.key|secret|credential"
□ Security-critical tasks signed off by Engineering Lead (see sprint spec)
```

### Documentation Updated

```
□ OpenAPI spec updated (if endpoint added/changed/removed)
□ CHANGELOG.md updated (for user-facing changes)
□ .env.example updated (if new environment variable added)
□ README or architecture doc updated (if new service or major change)
```

### Deployed & Verified

```
□ Deployed to staging (auto on develop merge)
□ Smoke test passes (make health)
□ Seq logs: no unexpected errors in 15 minutes post-deploy
□ QA role verification completed (Section 17)
```

---

## 25. Forbidden Actions

These actions are absolutely prohibited. If a situation seems to require one of these, stop and ask the human.

### Code

```
❌ Delete or rename a field from a published RabbitMQ event
❌ Add a required non-nullable field to an existing MongoDB collection without migration
❌ Change JWT claim names (sub, role, farm_ids, province, jti)
❌ Remove or rename a public API endpoint without versioning
❌ Implement custom cryptography — use only .NET/Python standard libraries
❌ Disable or bypass the PII masking Serilog policy
❌ Return stack traces in API error responses
❌ Use Thread.Sleep in production code
❌ Call another service's database directly — only via HTTP API
❌ Store secrets in appsettings.json or any committed file
```

### Git

```
❌ Force push to develop or main
❌ Commit directly to develop or main
❌ Merge without CI passing
❌ Squash commits that have already been reviewed and approved
❌ Add .env, .pem, .key, or any secret file to any commit
❌ Rewrite git history on a shared branch
```

### AI / Prompts

```
❌ Include real farmer PII (phone, name, GPS) in any prompt sent to OpenAI
❌ Bypass the banned chemicals check in AI response validation
❌ Return AI-generated content without a citation when making technical claims
❌ Change prompt files in-place — always create a new versioned file
❌ Remove the token budget enforcement from Orchestrator
```

### Process

```
❌ Close a GitHub issue without all AC verified
❌ Merge a PR that fails any CI check
❌ Skip the Self-Review Checklist (Section 20)
❌ Create a PR to main without explicit human approval
❌ Deploy to production without passing staging smoke test
```

---

## 26. Execution Priority Rules

When multiple tasks compete for attention or a situation is ambiguous, apply these rules in order.

### Priority 1 — Safety (always first)

```
If any action could expose PII, introduce a security vulnerability,
or corrupt live data → STOP and ask the human before proceeding.

This overrides sprint deadlines, feature requests, and any other rule.
```

### Priority 2 — Correctness over Speed

```
A slow correct implementation > a fast broken one.

If the sprint spec AC and the implementation approach conflict:
  → Follow the AC, not the approach
  → Document the deviation in the PR

If unsure whether implementation is correct:
  → Write the test first, then implement until the test passes
```

### Priority 3 — Spec over Assumption

```
If sprint spec says X but this file says Y:
  → Sprint spec wins for that specific task
  → Note the conflict in PR description

If neither spec nor this file addresses a situation:
  → Follow the pattern established in the existing codebase
  → Document the decision in the PR
```

### Priority 4 — Existing Patterns over New Patterns

```
Before introducing a new pattern or library:
  1. Search the codebase for how similar problems are solved
  2. Use the existing solution unless there is a clear documented reason not to
  3. If introducing something new → document why in ADR or PR description
```

### Priority 5 — Incremental Delivery

```
Prefer shipping a smaller working increment over a large incomplete one.

If a task is too large to complete in one PR:
  1. Identify the minimum slice that delivers value
  2. Create sub-issues for remaining work
  3. Ship the first slice, reference sub-issues in PR
  4. Never merge code that leaves the system in a broken intermediate state
```

### Conflict Resolution Matrix

| Conflict | Resolution |
|---------|-----------|
| Sprint spec vs GEMINI.md | Sprint spec wins |
| This file vs existing code pattern | This file wins (code may be outdated) |
| Speed vs correctness | Correctness always |
| New library vs existing pattern | Existing pattern (ask to justify new) |
| Autonomy vs data safety | Always ask for data-affecting changes |
| CI failing vs deadline | Fix CI — never merge a failing build |

---

*Version: 3.0.0 | Updated: 2026-03-19*
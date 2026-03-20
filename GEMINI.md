# GEMINI.md - Bazan AI Project Context

This file provides instructional context for Gemini CLI when working on the **Bazan AI** project.

## Project Overview
**Bazan AI** is an Agentic Microservices platform designed for coffee farmers in Vietnam's Central Highlands. It provides AI-driven insights, market data, and agronomy support using a modern, distributed architecture.

- **Primary Goal:** Empower coffee farmers with real-time AI assistance and data-driven cultivation advice.
- **Architecture:** Clean Architecture (Domain-Driven Design), Microservices, Agentic AI.
- **Key Services:**
    - `Identity`: Farmer registration, authentication (JWT/OTP).
    - `Conversations`: Chat session management and message history.
    - `Agronomy`: Disease detection, irrigation planning, soil analysis.
    - `Market`: Coffee prices, ROI calculations, buyer location.
    - `Orchestrator`: AI Agent using Semantic Kernel, streaming via SignalR.
    - `Knowledge RAG`: Python-based Retrieval-Augmented Generation service.
    - `IoT Ingestion`: Sensor data processing (MQTT).
    - `Weather`: Meteorological data polling and alerting.
    - `Gateway`: API Gateway using YARP.

## Tech Stack
- **Backend:** .NET 8 (C#)
- **AI Service:** Python 3.12 (FastAPI)
- **Databases:** MongoDB (Replica Set), Qdrant (Vector DB), Redis (Cache)
- **Messaging:** RabbitMQ (MassTransit)
- **AI Orchestration:** Microsoft Semantic Kernel
- **Observability:** Serilog, Seq, Prometheus, Grafana, OpenTelemetry
- **Infrastructure:** Docker, Docker Compose

## Building and Running
The project uses a `Makefile` for common operations:

- **Start Infrastructure:** `make up` (Runs `docker compose -f infra/docker-compose.yml up -d`)
- **Stop Infrastructure:** `make down`
- **Build All Images:** `make build`
- **Initialize Databases:** `make migrate` (Sets up MongoDB replica set)
- **Run Tests:** `make test` (Runs `dotnet test` and `pytest`)
- **View Logs:** `make logs`

### Individual Service Development
- **.NET Services:** `dotnet run --project src/Services/BazanAI.Identity/BazanAI.Identity.API`
- **Python Service:** `cd src/Python/bazan-knowledge-rag && uvicorn app.main:app --reload`

## Development Conventions

### Naming & Style
- **C#:** PascalCase for Classes/Methods/Properties, camelCase for variables, `_camelCase` for private fields. Always suffix async methods with `Async`.
- **Python:** snake_case for functions/variables/modules, PascalCase for classes. Type hints are **mandatory**.
- **Indentation:** 4 spaces for C# and Python, 2 spaces for JSON/YAML/Markdown (enforced by `.editorconfig`).

### Clean Architecture (C#)
Strictly follow the dependency flow: `Domain` <- `Application` <- `Infrastructure` / `API`.
- **Domain:** No external dependencies. Entities, Aggregate Roots, Value Objects, Domain Events.
- **Application:** CQRS pattern using MediatR. Only depends on Domain.
- **Infrastructure:** Implementations of repositories and external clients.
- **API:** Controllers and service registration.

### AI & Data
- **RAG:** Uses Qdrant for vector storage. Hybrid search (Dense + Sparse/BM42).
- **Orchestrator:** Uses Semantic Kernel for function calling and intent classification.

### Error Handling
- Use the `Result<T>` pattern for business logic errors instead of throwing exceptions.
- Throw exceptions only for infrastructure/unexpected failures.

### Logging
- Use structured logging with named parameters (Serilog/structlog).
- Never log PII (Personally Identifiable Information) like phone numbers or exact coordinates without masking.

## Key Files
- `BazanAI.sln`: Main .NET solution.
- `infra/docker-compose.yml`: Full infrastructure stack definition.
- `src/SharedKernel/`: Common logic shared across all .NET microservices.
- `docs/architecture/`: Detailed design documents (ADRs, Diagrams).
- `docs/team/coding-standards.md`: Detailed coding standards.

## Project Structure
```
BazanAI/
├── .github/workflows/      # CI/CD Pipelines
├── docs/                   # Comprehensive documentation
├── infra/                  # Docker, Configs, DB scripts
├── src/
│   ├── Python/             # Python-based AI services
│   ├── Services/           # .NET Microservices
│   └── SharedKernel/       # Shared .NET libraries
├── tests/                  # Unit and Integration tests
├── Makefile                # Task runner
└── README.md               # Project overview
```

## Automated Developer Workflow
When instructed to "work on issue #X" or "implement feature Y", follow this strict workflow to simulate a Senior Developer's process:

### 1. Preparation & Branching
1.  **Read Context:** Retrieve issue details using `gh issue view {id}`. Read related `docs/` or existing code to understand the requirement.
2.  **Sync Main:** Ensure local main is up-to-date: `git checkout main && git pull origin main`.
3.  **Create Branch:**
    -   Feature: `git checkout -b feature/{id}-{short-desc}` (e.g., `feature/5-ci-pipeline`)
    -   Bugfix: `git checkout -b bugfix/{id}-{short-desc}` (e.g., `bugfix/12-login-error`)
    -   Naming: Lowercase, kebab-case, include ID.

### 2. Implementation & Verification
1.  **Coding Standards:** Adhere to Clean Architecture, naming conventions, and project style.
2.  **Testing:**
    -   **MUST** add or update Unit Tests (`tests/Unit/`) for all new logic.
    -   Run tests locally: `dotnet test {Project}` or `make test`.
    -   Ensure build passes: `dotnet build`.
3.  **Commit Strategy:**
    -   Use Conventional Commits: `feat(scope): description`, `fix(scope): description`.
    -   Keep commits granular and logical.

### 3. Pull Request & Review
1.  **Push:** `git push origin {branch-name}`.
2.  **Create PR:**
    -   Command: `gh pr create --base main --head {branch-name} --title "[{TYPE}-{ID}] {Description}" --body "{Template}"`
    -   Template:
        ```markdown
        ## Changes
        - [Concise list of changes]

        ## Related
        - Closes #{ID}

        ## Checklist
        - [x] Unit tests added/updated
        - [x] Manual testing verified
        - [x] No PII/Secrets exposed
        ```
3.  **Self-Review:**
    -   Verify diffs.
    -   Check for "dummy" code or "TODOs".
    -   Ensure CI checks pass (simulate or check `gh pr checks`).

### 4. Merge & Cleanup
1.  **Wait for Approval:** Ask user for confirmation to merge if critical.
2.  **Merge:**
    -   Command: `gh pr merge {pr-number} --squash --delete-branch`
    -   Strategy: **Squash Merge** to keep `main` clean.
3.  **Sync:** `git checkout main && git pull origin main`.

---
*This file is maintained by Gemini CLI to ensure consistent development patterns.*

# CI/CD Pipeline Documentation — Bazan AI
## Quy trình Build, Test & Deploy tự động

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | DevOps Lead |
| **Công cụ** | GitHub Actions, Docker, Docker Compose |
| **Liên quan** | Release Management Process, Performance Testing Plan |

---

## 1. Branch Strategy

```
main          ← Production (protected, chỉ merge qua PR)
develop       ← Staging (auto-deploy khi push)
feature/*     ← Feature branches (PR → develop)
hotfix/*      ← Hotfix branches (PR → main + develop)
release/*     ← Release preparation (PR → main)
```

**Branch protection rules (main):**
- Require PR review: 2 approvers (1 phải là senior)
- Require status checks: CI pipeline phải pass
- Require linear history: no merge commits
- Restrict force push: disabled

---

## 2. CI Pipeline (Pull Request)

**Trigger:** Mọi PR vào `develop` hoặc `main`
**File:** `.github/workflows/ci.yml`

```yaml
name: CI — Build & Test

on:
  pull_request:
    branches: [main, develop]

env:
  DOTNET_VERSION: '8.0.x'
  PYTHON_VERSION: '3.12'

jobs:
  # ─── JOB 1: .NET Build & Test ───────────────────────────────
  dotnet-ci:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore BazanAI.sln

      - name: Build (Release)
        run: dotnet build BazanAI.sln -c Release --no-restore

      - name: Run Unit Tests
        run: |
          dotnet test tests/Unit/ \
            --no-build \
            --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage

      - name: Run Integration Tests
        run: |
          dotnet test tests/Integration/ \
            --no-build \
            --configuration Release
        env:
          TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE: /var/run/docker.sock

      - name: Code Coverage Gate
        run: |
          COVERAGE=$(cat coverage/*/coverage.cobertura.xml | grep -oP 'line-rate="\K[^"]+' | head -1)
          echo "Coverage: ${COVERAGE}"
          if (( $(echo "$COVERAGE < 0.70" | bc -l) )); then
            echo "❌ Coverage ${COVERAGE} < 70% — failing"
            exit 1
          fi

      - name: Upload Coverage to Codecov
        uses: codecov/codecov-action@v4

  # ─── JOB 2: Python RAG Service ──────────────────────────────
  python-ci:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Python
        uses: actions/setup-python@v5
        with:
          python-version: ${{ env.PYTHON_VERSION }}

      - name: Install dependencies
        run: |
          cd src/Python/bazan-knowledge-rag
          pip install -r requirements.txt --break-system-packages
          pip install pytest pytest-asyncio ruff black mypy

      - name: Lint (ruff)
        run: ruff check src/Python/

      - name: Format check (black)
        run: black --check src/Python/

      - name: Type check (mypy)
        run: mypy src/Python/bazan-knowledge-rag/app/

      - name: Run tests (pytest)
        run: |
          cd src/Python/bazan-knowledge-rag
          pytest tests/ -v --asyncio-mode=auto

  # ─── JOB 3: Docker Build ────────────────────────────────────
  docker-build:
    runs-on: ubuntu-latest
    needs: [dotnet-ci, python-ci]
    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Build all images (no push — PR only)
        run: |
          SERVICES=(
            "gateway:src/Services/BazanAI.Gateway"
            "identity-svc:src/Services/BazanAI.Identity/BazanAI.Identity.API"
            "agronomy-svc:src/Services/BazanAI.Agronomy/BazanAI.Agronomy.API"
            "market-svc:src/Services/BazanAI.Market/BazanAI.Market.API"
            "knowledge-rag:src/Python/bazan-knowledge-rag"
          )
          for svc_path in "${SERVICES[@]}"; do
            SVC="${svc_path%%:*}"
            PATH="${svc_path##*:}"
            echo "Building $SVC..."
            docker build -t bazanai/$SVC:pr-${{ github.event.pull_request.number }} $PATH
          done

  # ─── JOB 4: Security Scan ───────────────────────────────────
  security:
    runs-on: ubuntu-latest
    needs: docker-build
    steps:
      - uses: actions/checkout@v4

      - name: Trivy vulnerability scan (.NET)
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: 'fs'
          scan-ref: '.'
          severity: 'CRITICAL,HIGH'
          exit-code: '1'

      - name: Snyk dependency check
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}

  # ─── JOB 5: AI Eval Regression ──────────────────────────────
  ai-eval-gate:
    runs-on: ubuntu-latest
    needs: docker-build
    if: contains(github.event.pull_request.labels.*.name, 'ai-change')
    steps:
      - uses: actions/checkout@v4

      - name: Run AI Evaluation Regression
        run: |
          python scripts/eval/run_regression.py \
            --test-set tests/ai_eval/golden_set_top50.json \
            --threshold-recall 0.80 \
            --threshold-safety 1.00
        env:
          OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY_CI }}
          QDRANT_URL: ${{ secrets.STAGING_QDRANT_URL }}
```

---

## 3. CD Pipeline — Staging (develop branch)

**Trigger:** Push vào `develop`
**File:** `.github/workflows/cd-staging.yml`

```yaml
name: CD — Deploy to Staging

on:
  push:
    branches: [develop]

jobs:
  deploy-staging:
    runs-on: ubuntu-latest
    environment: staging

    steps:
      - uses: actions/checkout@v4

      - name: Build & Push images to Registry
        run: |
          echo ${{ secrets.REGISTRY_PASSWORD }} | \
            docker login registry.bazanai.vn -u ${{ secrets.REGISTRY_USER }} --password-stdin

          COMMIT_SHA="${GITHUB_SHA::8}"
          SERVICES=(gateway identity-svc conversation-svc agronomy-svc market-svc \
                    ai-orchestrator knowledge-rag notification-svc weather-svc)

          for SVC in "${SERVICES[@]}"; do
            docker build -t registry.bazanai.vn/bazanai/$SVC:$COMMIT_SHA \
              -t registry.bazanai.vn/bazanai/$SVC:staging-latest \
              src/Services/BazanAI.${SVC^}
            docker push registry.bazanai.vn/bazanai/$SVC:$COMMIT_SHA
            docker push registry.bazanai.vn/bazanai/$SVC:staging-latest
          done

      - name: Deploy to Staging Server
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.STAGING_HOST }}
          username: deploy
          key: ${{ secrets.STAGING_SSH_KEY }}
          script: |
            cd /opt/bazanai
            git pull origin develop

            # Pull latest images
            docker compose -f docker-compose.yml \
              -f docker-compose.staging.yml pull

            # Rolling restart (infrastructure trước, services sau)
            docker compose restart redis rabbitmq
            sleep 5

            docker compose up -d \
              identity-svc conversation-svc agronomy-svc market-svc \
              ai-orchestrator knowledge-rag notification-svc \
              weather-svc gateway

            # Health check
            sleep 15
            curl -f http://localhost:8080/healthz || exit 1

      - name: Smoke Test Staging
        run: |
          python scripts/smoke_test.py \
            --url https://staging.bazanai.vn \
            --api-key ${{ secrets.STAGING_API_KEY }}

      - name: Notify Slack
        if: always()
        uses: rtCamp/action-slack-notify@v2
        env:
          SLACK_WEBHOOK: ${{ secrets.SLACK_WEBHOOK }}
          SLACK_MESSAGE: |
            ${{ job.status == 'success' && '✅' || '❌' }} Staging deploy
            Commit: ${{ github.sha }}
            By: ${{ github.actor }}
```

---

## 4. CD Pipeline — Production (main branch)

**Trigger:** Push vào `main` (sau PR merge) + **Manual approval gate**
**File:** `.github/workflows/cd-production.yml`

```yaml
name: CD — Deploy to Production

on:
  push:
    branches: [main]

jobs:
  deploy-prod:
    runs-on: ubuntu-latest
    environment:
      name: production
      url: https://api.bazanai.vn/healthz
    # GitHub Environment protection requires manual approval

    steps:
      - uses: actions/checkout@v4

      # Reuse images built in CI (same commit SHA)
      - name: Tag staging image as production
        run: |
          COMMIT_SHA="${GITHUB_SHA::8}"
          SERVICES=(gateway identity-svc agronomy-svc market-svc \
                    ai-orchestrator knowledge-rag notification-svc)

          for SVC in "${SERVICES[@]}"; do
            docker pull registry.bazanai.vn/bazanai/$SVC:$COMMIT_SHA
            docker tag registry.bazanai.vn/bazanai/$SVC:$COMMIT_SHA \
                       registry.bazanai.vn/bazanai/$SVC:prod-latest
            docker push registry.bazanai.vn/bazanai/$SVC:prod-latest
          done

      - name: Blue-Green Deploy to Production
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.PROD_HOST }}
          username: deploy
          key: ${{ secrets.PROD_SSH_KEY }}
          script: |
            cd /opt/bazanai

            # Snapshot MongoDB trước khi deploy
            ./scripts/backup-mongodb.sh pre-deploy-$(date +%Y%m%d%H%M)

            # Rolling update: services lần lượt, không down toàn bộ
            SERVICES=(identity-svc conversation-svc agronomy-svc market-svc \
                      ai-orchestrator notification-svc weather-svc knowledge-rag gateway)

            for SVC in "${SERVICES[@]}"; do
              echo "Updating $SVC..."
              docker compose pull $SVC
              docker compose up -d --no-deps $SVC
              sleep 10

              # Health check service vừa update
              HEALTH=$(docker inspect --format='{{.State.Health.Status}}' bazan-$SVC 2>/dev/null)
              if [ "$HEALTH" != "healthy" ]; then
                echo "❌ $SVC không healthy — rollback!"
                docker compose up -d --no-deps --scale $SVC=0 $SVC
                exit 1
              fi
              echo "✅ $SVC healthy"
            done

      - name: Production Smoke Test
        run: |
          python scripts/smoke_test.py \
            --url https://api.bazanai.vn \
            --api-key ${{ secrets.PROD_SMOKE_API_KEY }} \
            --critical  # Nghiêm ngặt hơn staging

      - name: Create GitHub Release
        uses: actions/create-release@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          tag_name: v${{ github.run_number }}
          release_name: "Release v${{ github.run_number }}"
          body: ${{ github.event.head_commit.message }}
```

---

## 5. Smoke Test Script

```python
# scripts/smoke_test.py
import httpx, sys, time

CRITICAL_ENDPOINTS = [
    ("GET",  "/healthz",                     None,                    200),
    ("POST", "/api/v1/auth/login",           {"phoneNumber": "+84909999999"}, 200),
    ("GET",  "/api/v1/market/prices/current", None,                   200),
    ("GET",  "/api/v1/agronomy/pest-alerts?province=Đắk Lắk", None,  200),
]

def run_smoke_tests(base_url: str, api_key: str) -> bool:
    client = httpx.Client(base_url=base_url, timeout=10)
    passed = failed = 0

    for method, path, body, expected_status in CRITICAL_ENDPOINTS:
        try:
            resp = client.request(
                method, path,
                json=body,
                headers={"X-Smoke-Test-Key": api_key}
            )
            if resp.status_code == expected_status:
                print(f"  ✅ {method} {path} → {resp.status_code}")
                passed += 1
            else:
                print(f"  ❌ {method} {path} → {resp.status_code} (expected {expected_status})")
                failed += 1
        except Exception as e:
            print(f"  ❌ {method} {path} → ERROR: {e}")
            failed += 1

    print(f"\nResults: {passed} passed, {failed} failed")
    return failed == 0

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", required=True)
    parser.add_argument("--api-key", required=True)
    args = parser.parse_args()

    success = run_smoke_tests(args.url, args.api_key)
    sys.exit(0 if success else 1)
```

---

## 6. Rollback Procedure

```bash
# Rollback toàn bộ production về commit trước

# 1. Xác định image tag muốn rollback về
docker images registry.bazanai.vn/bazanai/identity-svc

# 2. Update compose file
ROLLBACK_TAG="abc12345"  # commit SHA của version cũ

# 3. Rolling rollback (ngược với deploy)
SERVICES=(gateway notification-svc weather-svc knowledge-rag \
          ai-orchestrator market-svc agronomy-svc conversation-svc identity-svc)

for SVC in "${SERVICES[@]}"; do
  docker compose stop $SVC
  docker tag registry.bazanai.vn/bazanai/$SVC:$ROLLBACK_TAG \
             registry.bazanai.vn/bazanai/$SVC:prod-latest
  docker compose up -d --no-deps $SVC
  sleep 5
done

# 4. Verify
curl https://api.bazanai.vn/healthz
```

---

**© 2026 Bazan AI Project — Confidential**

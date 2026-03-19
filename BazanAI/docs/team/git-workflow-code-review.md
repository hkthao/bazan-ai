# Git Workflow & Branch Strategy — Bazan AI

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 |
| **Ngày tạo** | 2026-03-19 |
| **Tác giả** | Engineering Lead · DevOps Lead |
| **Liên quan** | CI/CD Pipeline, Release Management, Developer Onboarding |

---

## 1. Branch Model

```
main          ← Production — protected, chỉ merge qua PR
develop       ← Staging — auto-deploy khi push
feature/*     ← Feature development (từ develop)
bugfix/*      ← Bug fixes (từ develop)
hotfix/*      ← Critical production fixes (từ main)
release/*     ← Release preparation (từ develop)
chore/*       ← Infrastructure, dependencies, docs
```

### Branch Protection Rules

**`main`:**
```
✅ Require pull request: 2 approvers (1 phải là senior/lead)
✅ Require status checks: ci-pipeline phải pass
✅ Require linear history (no merge commits, use squash/rebase)
✅ Include administrators: không exempt
❌ Force push: disabled
❌ Delete: disabled
```

**`develop`:**
```
✅ Require pull request: 1 approver
✅ Require status checks: ci-pipeline phải pass
❌ Force push: disabled
```

---

## 2. Naming Conventions

```
Pattern: {type}/{ticket-id}-{short-description-kebab-case}

Examples:
  feature/US-042-disease-alert-zalo
  feature/US-055-offline-chat-cache
  bugfix/BUG-012-otp-rate-limit-not-working
  hotfix/HOTFIX-003-jwt-expiry-calculation
  chore/update-dotnet-nuget-packages
  release/v2.1.0
  docs/update-rag-pipeline-design

Rules:
  - Lowercase
  - Kebab-case (không dùng underscore)
  - Ticket ID bắt buộc (trừ chore/docs)
  - Short description ≤ 5 từ
```

---

## 3. Commit Message Convention

**Format:** Conventional Commits (https://conventionalcommits.org)

```
<type>(<scope>): <short description>

[optional body]

[optional footer]
```

**Types:**
| Type | Khi nào dùng |
|------|-------------|
| `feat` | Feature mới |
| `fix` | Bug fix |
| `refactor` | Refactor không thay đổi behavior |
| `test` | Thêm hoặc sửa tests |
| `docs` | Chỉ documentation |
| `chore` | Build system, CI, dependencies |
| `perf` | Performance improvement |
| `style` | Formatting, không thay đổi code |
| `revert` | Revert commit trước |

**Scopes (common):**
`identity` | `agronomy` | `market` | `conversation` | `orchestrator` | `rag` | `notification` | `gateway` | `iot` | `shared`

**Examples:**
```
feat(agronomy): add disease detection via Computer Vision API

fix(identity): OTP rate limit not resetting after lockout expiry

Closes #BUG-012

refactor(rag): extract ChunkingService to separate class for testability

test(market): add integration tests for geospatial buyer search

chore: update Microsoft.SemanticKernel to 1.12.0

BREAKING CHANGE: ChatSession schema v2 — add contextSnapshot field
```

**Rules:**
- Subject line ≤ 72 ký tự
- Subject không kết thúc bằng dấu chấm
- Body wrap ở 72 ký tự
- Breaking changes: thêm `BREAKING CHANGE:` trong footer

---

## 4. Pull Request Process

### 4.1 Tạo PR

```
PR Title: [US-{id}] Short description in English
          [BUG-{id}] Fix description
          [HOTFIX-{id}] Critical fix description

PR Description Template:
```

```markdown
## Changes

Brief description of what this PR does.

## Related

- Closes #US-042
- Related to #US-038

## Type of Change

- [ ] New feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Breaking change
- [ ] Documentation

## Testing Done

- [ ] Unit tests added/updated
- [ ] Integration tests pass
- [ ] Manual testing on local/staging

## Screenshots (if UI changes)

## Checklist

- [ ] Coding standards followed
- [ ] No hardcoded credentials/secrets
- [ ] No PII in logs
- [ ] OpenAPI spec updated (if API changes)
- [ ] CHANGELOG.md updated (for features)
```

### 4.2 Review Process

```
1. Developer tạo PR → CI tự chạy
2. CI pass → request review từ 2 người
3. Reviewers có 1 ngày làm việc để review
4. Address mọi comment
5. Khi có đủ approvals → Merge (squash commit vào develop)
6. Branch tự xóa sau merge
7. Staging deploy tự động
```

### 4.3 Merge Strategy

```
develop ← feature/*: SQUASH MERGE
  Lý do: 1 feature = 1 commit clean trong develop history
  Commit message: "[US-042] Add Zalo channel for disease alerts"

main ← develop: MERGE COMMIT (no fast-forward)
  Lý do: Preserve deployment history
  Commit message: "chore: release v2.1.0"

main ← hotfix/*: SQUASH MERGE
  Sau đó cherry-pick hoặc merge về develop
```

---

## 5. Hotfix Process

```bash
# Kịch bản: Production bug cần fix ngay

# 1. Tạo hotfix branch từ main (không phải develop)
git checkout main
git pull origin main
git checkout -b hotfix/HOTFIX-003-jwt-expiry-bug

# 2. Fix bug + commit
git add .
git commit -m "fix(identity): JWT expiry calculation uses wrong timezone"

# 3. PR vào main (bypass tuần deploy thông thường)
# Chỉ cần 1 senior review (không phải 2)
# CI phải pass

# 4. Sau khi merge vào main → tag version
git tag -a v2.0.1 -m "Hotfix: JWT expiry calculation"
git push origin main --tags

# 5. Merge hotfix về develop (để không bị mất)
git checkout develop
git merge hotfix/HOTFIX-003-jwt-expiry-bug
git push origin develop

# 6. Xóa hotfix branch
git branch -d hotfix/HOTFIX-003-jwt-expiry-bug
git push origin --delete hotfix/HOTFIX-003-jwt-expiry-bug
```

---

## 6. Release Process

```bash
# Bi-weekly release (thứ Năm)

# 1. Tạo release branch từ develop
git checkout develop
git pull origin develop
git checkout -b release/v2.1.0

# 2. Version bump
# Sửa VERSION file và CHANGELOG.md

# 3. Final testing trên release branch

# 4. Merge vào main
git checkout main
git merge --no-ff release/v2.1.0
git tag -a v2.1.0 -m "Release v2.1.0: Add IoT sensor support"
git push origin main --tags

# 5. Merge về develop (để sync)
git checkout develop
git merge --no-ff release/v2.1.0
git push origin develop

# 6. Xóa release branch
git branch -d release/v2.1.0
```

---

## 7. Useful Git Aliases

```bash
# Thêm vào ~/.gitconfig

[alias]
  # Xem graph đẹp
  lg = log --graph --abbrev-commit --decorate --format=format:'%C(bold blue)%h%C(reset) - %C(bold green)(%ar)%C(reset) %C(white)%s%C(reset) %C(dim white)- %an%C(reset)%C(bold yellow)%d%C(reset)'

  # Stash nhanh
  save = stash push -m

  # Undo last commit (giữ changes)
  undo = reset HEAD~1 --mixed

  # Clean up merged branches
  cleanup = "!git branch --merged | grep -v '\\*\\|main\\|develop' | xargs -n 1 git branch -d"

  # Pull rebase
  up = pull --rebase

  # Squash commits (interactive rebase)
  squash = "!f() { git rebase -i HEAD~$1; }; f"
```

---

**© 2026 Bazan AI — Internal Document**

---
---

# Code Review Checklist — Bazan AI
## Hướng dẫn Review Code hiệu quả

| Trường | Nội dung |
|--------|---------|
| **Phiên bản** | 1.0.0 | **Ngày tạo** | 2026-03-19 |
| **Liên quan** | Coding Standards, Git Workflow, Testing Strategy |

---

## Triết lý Review

> **Code review là về code, không phải người.**  
> Mục tiêu: cải thiện chất lượng code và chia sẻ kiến thức, không phải phán xét.

**Tinh thần:**
- Đặt câu hỏi thay vì ra lệnh: "Tại sao không dùng Result<T> ở đây?" thay vì "Phải dùng Result<T>"
- Giải thích lý do khi request change
- Distinguish between blocker và suggestion: `[BLOCKER]`, `[SUGGESTION]`, `[NITPICK]`
- Khen ngợi code tốt: "Nice use of ValueObject here 👍"

---

## Checklist cho Reviewer

### Layer 1: Correctness (Blocker nếu sai)

```
□ Logic đúng không? Code làm đúng những gì PR description nói?
□ Edge cases được handle? (null, empty list, 0, negative values)
□ Concurrent access có vấn đề không? (race conditions, shared mutable state)
□ MongoDB queries có index đúng không? (tránh full collection scan)
□ Transactions được dùng đúng chỗ không?
□ Breaking changes được document đầy đủ không?
```

### Layer 2: Security (Blocker nếu sai)

```
□ Không có PII trong logs (phone, name, GPS coordinates chính xác)
□ Input validation đầy đủ (FluentValidation trước khi Handle)
□ Không có hardcoded credentials hoặc API keys
□ SQL/NoSQL injection không thể xảy ra (strongly-typed queries)
□ Authorization check đúng chỗ (resource-level, không chỉ role-level)
□ Rate limiting được áp dụng cho endpoint nhạy cảm
□ Prompt injection được xử lý (nếu liên quan đến AI)
□ Secrets không xuất hiện trong logs hoặc error messages
```

### Layer 3: Tests

```
□ Happy path được test?
□ Error cases được test?
□ Edge cases được test?
□ Test names mô tả rõ scenario (MethodName_Scenario_Expected)?
□ Mocks được setup đúng?
□ Test không phụ thuộc thứ tự chạy?
□ Integration tests cho DB interactions?
□ Coverage không giảm so với trước (target ≥ 70%)?
```

### Layer 4: Code Quality

```
□ Naming conventions theo Coding Standards?
□ Method không quá 30 dòng?
□ Không có dead code (commented out code cần xóa)?
□ Không có magic numbers/strings?
□ Logging đầy đủ cho các operation quan trọng?
□ Error handling consistent với pattern của project?
□ Clean Architecture boundaries không bị vi phạm?
□ Dependency Injection đúng cách?
```

### Layer 5: API Changes (nếu có)

```
□ OpenAPI spec được cập nhật?
□ Backward compatible? (không xóa field, không thay đổi type)
□ Versioning đúng nếu breaking change?
□ Error responses consistent (dùng ErrorSchema chuẩn)?
□ HTTP status codes đúng (201 cho Create, 204 cho Delete, v.v.)?
```

### Layer 6: Performance (khi relevant)

```
□ N+1 query problem không xảy ra?
□ Expensive operations (CV API, OpenAI) có caching hoặc rate limit?
□ Batch operations được dùng thay vì individual calls?
□ Memory allocation excessive không? (large lists, unnecessary ToList())
```

---

## Checklist cho Author (trước khi tạo PR)

```
Self-review trước khi request:

□ Tôi đã tự review diff một lần?
□ PR description đầy đủ, rõ ràng?
□ Tests viết xong và pass?
□ CI pass trên branch?
□ Không có console.log/Debug.WriteLine/print debug code sót?
□ Không có TODO comment chưa giải quyết (hoặc đã tạo ticket)?
□ Branch up-to-date với develop?
□ PR size hợp lý (không quá 500 dòng diff)?
```

---

## PR Size Guidelines

| PR Size | Lines changed | Recommendation |
|---------|-------------|---------------|
| Small | < 200 | ✅ Perfect |
| Medium | 200–500 | ✅ Acceptable |
| Large | 500–1000 | ⚠️ Consider splitting |
| XL | > 1000 | ❌ Must split — reviewer sẽ miss issues |

**Cách split PR lớn:**
1. Tách refactor thành PR riêng
2. Tách infrastructure (DB schema) với business logic
3. Tách tests thành PR riêng nếu cần

---

## Review SLA

| Situation | Expectation |
|---------|------------|
| Normal PR | Review trong 1 ngày làm việc |
| Hotfix PR | Review trong 2 giờ (notify trực tiếp) |
| WIP/Draft PR | Không cần review cho đến khi ready |
| Large PR (>500 lines) | 2 ngày — reviewer cần thêm thời gian |

**Nếu reviewer không có thời gian:**
- Thông báo trong PR comment
- Assign reviewer khác

---

## Comment Prefixes

```
[BLOCKER]    — Phải fix trước khi merge
[SUGGESTION] — Nên fix, nhưng không block
[NITPICK]   — Nhỏ, author tự quyết định
[QUESTION]  — Hỏi để hiểu, không nhất thiết phải thay đổi
[PRAISE]    — Khen ngợi code tốt
[FYI]       — Chia sẻ thông tin, không cần action
```

---

**© 2026 Bazan AI — Internal Document**

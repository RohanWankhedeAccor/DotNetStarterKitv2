## Target Branch

> **Where does this PR merge into?**
> - `develop` — for all feature, fix, refactor, docs, chore, and test branches
> - `main` — **only** for release PRs (`develop → main`) or hotfixes (`hotfix/* → main`)
>
> If you're targeting `main` directly with feature work, stop and retarget to `develop`.

---

## Description

<!-- Describe what this PR does and why -->

### Issue #
<!-- Link to related issue: Closes #123 -->

### Type of Change
- [ ] 🐛 Bug fix
- [ ] ✨ New feature
- [ ] 📝 Documentation
- [ ] ♻️ Refactoring
- [ ] ⚡ Performance improvement
- [ ] 🔒 Security fix

---

## Backend Checklist

- [ ] Code follows C# naming conventions (PascalCase)
- [ ] `dotnet build` passes with zero warnings
- [ ] `dotnet test` passes (all tests green)
- [ ] All new handlers have validators
- [ ] All database reads use `AsNoTracking()`
- [ ] DTOs used at API boundaries (not entities)
- [ ] No `Task.Result` or `.Wait()` (async all the way)
- [ ] New migrations committed (if DB changes)
- [ ] Connection strings use config (not hardcoded)
- [ ] All public methods have XML docs (`/// <summary>`)
- [ ] No hardcoded secrets or API keys

## Frontend Checklist

- [ ] Code follows TypeScript naming conventions (camelCase)
- [ ] `npm run lint` passes
- [ ] `npm run type-check` passes (zero TS errors)
- [ ] `npm run test` passes (all tests green)
- [ ] No hardcoded API URLs (uses `VITE_API_BASE_URL`)
- [ ] Components wrapped in `<ErrorBoundary>`
- [ ] Loading states shown (skeleton/spinner)
- [ ] Error states handled (toast/error message)
- [ ] Responsive design verified (mobile/tablet/desktop)
- [ ] No console.log() left in code

## Testing

- [ ] Unit tests added for new logic
- [ ] Integration tests added (backend)
- [ ] Component tests updated (frontend)
- [ ] Edge cases tested
- [ ] Error scenarios tested

## Documentation

- [ ] README updated (if applicable)
- [ ] API endpoint documented
- [ ] `.env.example` updated (if env vars added)
- [ ] CLAUDE.md updated (if architecture changed)
- [ ] Comments added for non-obvious logic

## Review Checklist

**For Reviewers:**
- [ ] Code solves the stated problem
- [ ] No obvious bugs or edge cases missed
- [ ] Follows project patterns and conventions
- [ ] Tests are adequate and passing
- [ ] No security concerns
- [ ] Performance acceptable
- [ ] Documentation clear and complete

---

## Screenshots (if applicable)

<!-- Add screenshots for UI changes -->

---

## Additional Notes

<!-- Any additional context or gotchas -->

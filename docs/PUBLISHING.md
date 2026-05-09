# ElBruno.NetAgent – Publishing Guide

## 1. Goal

Publish ElBruno.NetAgent release artifacts using GitHub Actions and NuGet Trusted Publishing through OIDC.

The repository should not store long-lived NuGet API keys.

## 2. Package strategy

ElBruno.NetAgent is a WPF tray application.

Recommended release artifacts:

1. GitHub Release zip
2. NuGet package if packaging is useful for distribution
3. Future `ElBruno.NetAgent.CLI` NuGet global tool package

Important:

- Do not force the WPF app into a global tool if the experience is poor.
- A future CLI wrapper can be a separate project.

## 3. One-time NuGet setup

On NuGet.org:

1. Go to package management.
2. Add Trusted Publisher for this repository.
3. Use:
   - Repository owner: `elbruno`
   - Repository name: `ElBruno.NetAgent`
   - Workflow file: `publish.yml`
   - Environment: `release`

## 4. GitHub repository setup

Create GitHub environment:

```text
release
```

Add repository secret:

```text
NUGET_USER
```

Value:

```text
<NuGet.org username>
```

Do not add:

```text
NUGET_API_KEY
```

OIDC Trusted Publishing replaces long-lived API keys.

## 5. Release flow

Recommended flow:

1. Update package/app version.
2. Update `docs/CHANGELOG.md`.
3. Merge to `main`.
4. Create GitHub release with tag:

```text
vX.Y.Z
```

5. `publish.yml` builds, tests, packs, and publishes.

## 6. Version rules

Use semantic versioning:

```text
0.1.0
0.2.0
1.0.0
```

Tags must use:

```text
v0.1.0
v1.0.0
```

## 7. OIDC flow

Expected OIDC flow:

1. GitHub Actions receives `id-token: write` permission.
2. `NuGet/login@v1` requests a short-lived token.
3. NuGet validates repository, workflow, environment, and package policy.
4. Workflow uses temporary token to push package.
5. No long-lived API key is stored.

## 8. Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| 403 pushing to NuGet | Trusted publisher mismatch | Verify owner, repo, workflow file, environment |
| Missing OIDC token | permissions missing | Add `id-token: write` |
| Environment not found | GitHub environment missing | Create `release` environment |
| Version invalid | tag format mismatch | Use `vX.Y.Z` |
| Package not visible | NuGet indexing delay | Wait 5-15 minutes |

## 9. Safety Notice

**Publishing a NuGet package or creating a GitHub release does NOT imply production readiness.**

The application is currently **dry-run only**. Live network execution is intentionally blocked by default and is not yet implemented.

See `docs/SECURITY.md` (Phase 10) for the complete safety chain documentation.

## 10. References

- NuGet Login GitHub Action: `https://github.com/marketplace/actions/nuget-login`
- GitHub OIDC docs: `https://docs.github.com/actions/security-for-github-actions/security-hardening-your-deployments/about-security-hardening-with-openid-connect`
- Example ElBruno publishing doc: `https://github.com/elbruno/ElBruno.LocalLLMs/blob/main/docs/publishing.md`

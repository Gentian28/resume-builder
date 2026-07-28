# Things only you can do

> Everything automatable is done. This is the remaining list, in priority order, with the
> reason each one needs a human.

---

## 0. Cut the 1.1.0 release — DO THIS FIRST

Nineteen commits are sitting unreleased: the whole editor redesign, the first-run screen, and the
Anthropic provider. The newest thing anyone can install is 1.0.3, from before any of it.

Everything below is prepared. `CHANGELOG.md` already has the 1.1.0 entry written, and
`Directory.Build.props` is bumped.

**Optional but recommended first — verify the Anthropic provider against the real API.** Nothing
else in the suite makes an actual request, so this is the only check that proves the request shape
and response parsing are right. Costs a fraction of a cent:

```powershell
$env:ANTHROPIC_API_KEY = "sk-ant-..."
dotnet test ResumeBuilder.sln --filter "FullyQualifiedName~AnthropicLiveTests"
```

Without the key those three tests report **Skipped**, which is why CI stays offline and green.

**Then publish.** Push a tag — this is the path every release so far has used:

```powershell
git tag v1.1.0
git push public v1.1.0
```

(The Actions → Run workflow button also works now, but the tag push is the proven path.)

It builds win-x64, linux-x64, osx-arm64 and osx-x64 and packs Velopack installers, then creates
the GitHub release **as a draft**. Nothing is public and nothing auto-updates until you review the
draft and hit Publish. Until then `releases/latest/download/` still serves 1.0.3, so the portfolio
page is unaffected.

Once you publish: the auto-update feed updates and existing 1.0.3 installs pick up 1.1.0 on next
launch.

**Then regenerate the winget manifest** — after the release is published, because the script reads
the released `SHA256SUMS` so the hash always matches what people actually download:

```powershell
.\packaging\winget\new-version.ps1 -Version 1.1.0
ew-version.ps1 -Version 1.1.0
wingetcreate submit --token <github-PAT> packaging\winget\1.1.0
```

### Sequencing against the open 1.0.3 PR

**Let #408983 merge first, then submit 1.1.0 as a separate PR. Do not touch the open one.**

The instinct is to update the pending PR to 1.1.0 since 1.0.3 is now superseded. Don't:

- **Pushing to that branch restarts validation and resets its place in the review queue** — you
  would trade a nearly-approved PR for a fresh one at the back of the line.
- **The first submission is the expensive one.** It carries package-identity review — publisher,
  package ID, licence, installer type. Later version bumps are close to automatic. Merging 1.0.3
  buys that one-off scrutiny; 1.1.0 then rides through on the established identity.
- **The 1.0.3 manifest stays valid** after 1.1.0 ships. It points at the v1.0.3 release assets,
  which are not deleted, so the install works. And anyone who installs it is auto-updated to 1.1.0
  by Velopack on next launch — so nobody is stranded on the old version.

Closing #408983 and opening a fresh 1.1.0 PR also works, but throws away review time already
spent for no gain.

---

## 1. Submit the winget package — DONE, awaiting review

Submitted 2026-07-28 as **microsoft/winget-pkgs#408983** for v1.0.3. CLA signed, all checks
green, waiting on a moderator. Nothing to do but wait — and specifically **do not push to that
branch**, since new commits restart validation and reset the review.

Once merged: `winget install Gentian28.ResumeBuilder`.

Two things learned worth keeping for next time:

- The CLA is a one-off for your account. Reply on the PR with `@microsoft-github-policy-service
  agree` — no `company=`, since this is your own project and you hold the IP.
- Test-install before submitting. Doing so caught the off-screen-window bug that shipped in 1.0.1
  and 1.0.2; submitting either would have put a broken build in Microsoft's index.

### For future releases

```powershell
.\packaging\winget\new-version.ps1 -Version 1.0.4   # after the release is published
winget install --manifest packaging\winget\1.0.4    # prove it works
wingetcreate submit packaging\winget\1.0.4
```

The original submission instructions follow, for reference.

Manifests live at `packaging/winget/<version>/`.

### First, prove it installs

Do not skip this — winget's CI installs the package unattended, and a broken silent switch
fails the PR after a slow review cycle.

**Prerequisite, once per machine.** winget refuses local manifests by default
(`LocalManifestFiles` is off), so this needs enabling first. It requires an **administrator**
PowerShell — Start menu → type `PowerShell` → right-click *Windows PowerShell* → *Run as
administrator*:

```powershell
winget settings --enable LocalManifestFiles
```

Check it took with `winget settings export` — `"LocalManifestFiles":true`.

**Then, in a normal (non-admin) PowerShell.** Velopack installs per-user under `%LocalAppData%`,
so there is nothing to elevate for. Use the full path and the working directory stops mattering:

```powershell
winget install --manifest C:\Users\Pc\source\repos\resumebuilder\packaging\winget\1.0.1
```

winget reads the three YAML files, downloads the ~69 MB installer from the GitHub release,
verifies it against the `InstallerSha256` in the manifest, and runs it. A hash mismatch means the
manifest and the release have drifted apart — regenerate with `new-version.ps1`.

This performs a **real install**: Resume Builder appears in the Start menu. That is the point —
you are testing what a stranger will experience.

Confirm it launches, then remove it:

```powershell
winget uninstall Gentian28.ResumeBuilder
```

Optionally turn the setting back off afterwards (admin PowerShell again):

```powershell
winget settings --disable LocalManifestFiles
```

### Then submit

**Easy path** — `wingetcreate` opens the PR for you:

```powershell
winget install Microsoft.WingetCreate
wingetcreate submit --token <your-github-PAT> packaging\winget\1.0.1
```

The PAT needs `public_repo` scope. It forks, commits and opens the PR in one step.

**Manual path** — if you would rather see the diff:

1. Fork <https://github.com/microsoft/winget-pkgs>
2. Copy the three `.yaml` files to `manifests/g/Gentian28/ResumeBuilder/1.0.1/`
3. PR title exactly: `New version: Gentian28.ResumeBuilder version 1.0.1`

### After

Automated validation runs on the PR and comments if anything fails. A moderator reviews.
Expect days, not hours. Once merged, `winget install Gentian28.ResumeBuilder` works for everyone.

**For future releases:** `.\packaging\winget\new-version.ps1 -Version 1.0.2` regenerates and
validates the manifests by reading the published checksum. Run it after the release is out.

---

## 2. SignPath code-signing certificate — SUBMITTED, awaiting review

Applied 2026-07-28. Expect days to a few weeks; it is a human review.

The download page and README already credit SignPath, which the programme requires, marked
application-pending rather than claiming signed builds that do not exist yet.

**If it is declined, the likely reason is reputation, not eligibility.** The programme asks for
evidence the project is widely used or trusted, and at the time of applying the repo was hours
old with no stars, forks or downloads. That is a fixable objection: reapply once
microsoft/winget-pkgs#408983 merges, since "installable via `winget install`" is a concrete
third-party trust signal, and once there are some download statistics to point at.

The remaining eligibility question, if they raise one, is **QuestPDF**: it ships in the app and is
commercially dual-licensed (MIT for open-source projects and small businesses, paid above $1M
revenue), against a rule requiring "an OSI-approved Open Source license without commercial
dual-licensing for all components". QuestPDF grants MIT to open-source projects explicitly, so
this project's use is MIT — but it is their call, and it was disclosed rather than hidden.

It remains the single highest-impact item outstanding: it removes the Windows "unknown publisher"
warning, which is the biggest thing standing between a stranger and actually running the app.
On approval they provide an organisation, a signing policy and an API token.

1. Add the token as a repository secret, e.g. `SIGNPATH_API_TOKEN`.
2. In `.github/workflows/release.yml`, sign between publish and pack. `vpk pack` already accepts
   signing parameters and currently prints
   `No signing parameters provided, 260 file(s) will not be signed` on every Windows run — that
   warning is the marker for where this goes.
3. Re-tag to produce a signed release, and drop the unsigned-build warnings from the README and
   the download page.

---

## 3. Publish it on the portfolio

**Why you:** it is a different repo and a live deploy.

Full instructions: `docs/publishing-to-the-portfolio.md`.

Short version — the Lab card mechanism is one-link-per-card, so the cross-platform route needs
the download page ported to a React route. There is a no-code interim (publish an `ExternalLink`
tool pointing at the releases page) if you want something live today.

---

## 4. Optional: macOS notarisation

**Why you:** needs a paid Apple Developer account ($99/yr) in your name.

SignPath does not cover this. Until it is done, macOS users must allow the app under
**System Settings → Privacy & Security** after the first attempt. Both Mac builds work; Gatekeeper
just objects once.

Worth doing only if Mac downloads turn out to be significant — the warning is a papercut, not a
blocker, and the instructions are already on the download page.

---

## Not blocking anything

- **Flathub** (step 9) — more reach on Linux, but the AppImage already works with no
  package manager. Do it if Linux traffic justifies it.
- **The UI redesign** — specced across 12 cards in `docs/ui-redesign-proposal.md`, with only the
  theme foundation implemented. Independent of distribution; the app ships and works today. It is
  also why the download page uses a CSS mockup instead of a screenshot: the current UI is the one
  being replaced.

---

## 4. Turn on automatic winget submission — one-time setup

`.github/workflows/winget.yml` will generate the manifest and open the winget-pkgs PR for you on
every published release. It is **inert until you opt in**, so it does nothing today.

Do this only after #408983 has merged — the first submission carries package-identity review and
has to be done by hand.

1. **Create a fine-grained PAT** at github.com/settings/tokens with `public_repo` scope. It needs
   to push to your fork of microsoft/winget-pkgs.
2. **Add it as a secret:** repo → Settings → Secrets and variables → Actions → New repository
   secret → name `WINGET_TOKEN`.
3. **Flip the switch:** same page, Variables tab → New repository variable → name
   `WINGET_AUTO_SUBMIT`, value `true`.

From then on, publishing a release opens the winget PR by itself. To check it before trusting it,
run it by hand once: Actions → Submit to winget → Run workflow → version `1.1.0`.

**This has never run.** The pieces are the ones you already ran by hand for 1.0.3, but the workflow
around them is untested — watch the first one.

### What is deliberately left manual

- **The changelog.** Auto-generating it from commit subjects produces a log, not release notes.
  It is the one part users actually read. The release now *fails* if `CHANGELOG.md` has no section
  for the version being built, which catches forgetting rather than doing it for you.
- **Publishing the draft.** The release builds automatically but waits for you, because publishing
  is what pushes an auto-update to everyone already running the app. That is worth one human look.

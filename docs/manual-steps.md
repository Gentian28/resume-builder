# Things only you can do

> Everything automatable is automated. This is what needs a human, in the order it needs one.

**v1.1.1 is live.** Downloads serve it on all four platforms and existing installs auto-update.
Nothing here is blocking a user from installing the app — the rest is reach and polish.

| # | What | Status |
|---|---|---|
| 1 | Send screenshots to the portfolio session | **ready — your move** |
| 2 | winget PR #408983 for 1.0.3 | waiting on a moderator |
| 3 | Submit winget 1.1.1 | blocked on 2 |
| 4 | Turn on automatic winget submission | blocked on 2 |
| 5 | SignPath code-signing | waiting on review |
| 6 | macOS notarisation | optional, costs $99/yr |
| — | Verify the Anthropic provider against the real API | optional, 60 seconds |

### Still unverified: the Anthropic provider

It is the one surface nothing has exercised — it compiles and every offline test passes, but no
real request has ever been made. If you have a key it costs a fraction of a cent:

```powershell
$env:ANTHROPIC_API_KEY = "sk-ant-..."
dotnet test ResumeBuilder.sln --filter "FullyQualifiedName~AnthropicLiveTests"
```

Without the key those three tests report **Skipped**, which is how CI stays offline and green.

---

## 1. Send the screenshots to the portfolio session

The message itself is in `docs/portfolio-message-today.md` and is version-agnostic — it needs no
edit now that 1.1.1 is out, because the download links always resolve to the newest release.

What that session still needs is the image files: `docs/screenshots/editor.png`,
`template-gallery.png`, `first-run.png`. Have them committed into the portfolio repo rather than
hotlinked from GitHub.

---

## 2. winget PR #408983 — waiting on a moderator

Submitted 2026-07-28 for v1.0.3. CLA signed, checks green. Nothing to do but wait, and
specifically **do not push to that branch** — new commits restart validation and reset its place
in the queue.

Once merged: `winget install Gentian28.ResumeBuilder` works for everyone.

Two things worth keeping:

- The CLA is a one-off for your account. Reply `@microsoft-github-policy-service agree` — no
  `company=`, since this is your own project and you hold the IP.
- Test-install before submitting. Doing so caught the off-screen-window bug that shipped in 1.0.1
  and 1.0.2; submitting either would have put a broken build in Microsoft's index.

---

## 3. Submit winget 1.1.1 — after #408983 merges

**Let the 1.0.3 PR merge first, then submit 1.1.1 separately. Do not update the open one.**

The instinct is to bump the pending PR to 1.1.1 since 1.0.3 is two versions behind. Don't:

- **Pushing to that branch restarts validation and loses its queue position** — trading a
  nearly-approved PR for a fresh one at the back of the line.
- **The first submission is the expensive one.** It carries package-identity review — publisher,
  package ID, licence, installer type. Later version bumps are near-automatic. Merging 1.0.3 buys
  that scrutiny once; 1.1.1 then rides through on the established identity.
- **The 1.0.3 manifest stays valid.** It points at v1.0.3 assets, which are not deleted, and
  anyone who installs it is auto-updated to 1.1.1 on next launch. Nobody is stranded.

Then:

```powershell
.\packaging\winget\new-version.ps1 -Version 1.1.1
wingetcreate submit --token <github-PAT> packaging\winget\1.1.1
```

`new-version.ps1` must run **after** publishing — it reads the released `SHA256SUMS` so the hash
always matches what people actually download.

### Proving a manifest installs, if you ever need to

winget refuses local manifests by default. Enable it once, from an **administrator** PowerShell:

```powershell
winget settings --enable LocalManifestFiles
```

Then from a normal PowerShell (Velopack installs per-user, nothing to elevate):

```powershell
winget install --manifest C:\Users\Pc\source\repos\resumebuilder\packaging\winget\1.1.1
winget uninstall Gentian28.ResumeBuilder
```

A hash mismatch means the manifest and the release have drifted — regenerate with
`new-version.ps1`.

---

## 4. Turn on automatic winget submission — after #408983 merges

`.github/workflows/winget.yml` generates the manifest and opens the PR on every published release.
**Inert until you opt in**, so it does nothing today.

1. **PAT** at github.com/settings/tokens with `public_repo` scope.
2. **Secret:** Settings → Secrets and variables → Actions → New repository secret → `WINGET_TOKEN`.
3. **Switch:** same page, Variables tab → New repository variable → `WINGET_AUTO_SUBMIT` = `true`.

**This has never run.** The individual commands are the ones you ran by hand for 1.0.3, but the
workflow around them is untested. Try it manually first — Actions → Submit to winget → Run workflow
→ `1.1.1` — rather than finding out during a real release.

---

## 5. SignPath code-signing — awaiting review

Applied 2026-07-28. Days to a few weeks; it is a human review. The download page and README already
credit SignPath as the programme requires, marked application-pending rather than claiming signed
builds that do not exist.

**If declined, the likely reason is reputation, not eligibility.** The programme wants evidence the
project is used or trusted, and the repo was hours old when you applied. That is fixable: reapply
once #408983 merges, since "installable via `winget install`" is a concrete third-party trust
signal, and once there are download numbers to point at.

The other question they may raise is **QuestPDF** — it ships in the app and is commercially
dual-licensed, against a rule requiring no commercial dual-licensing. QuestPDF grants MIT to
open-source projects explicitly, so this project's use is MIT, but it is their call. It was
disclosed rather than hidden.

This is the highest-impact item outstanding: it removes the Windows "unknown publisher" warning,
the biggest thing between a stranger and running the app. On approval:

1. Add the token as a repository secret, e.g. `SIGNPATH_API_TOKEN`.
2. In `release.yml`, sign between publish and pack. `vpk pack` already takes signing parameters and
   currently prints `No signing parameters provided, 260 file(s) will not be signed` on every
   Windows run — that warning marks the spot.
3. Re-tag for a signed release, then delete the unsigned-build warnings from the README and the
   download page.

---

## 6. macOS notarisation — optional

Needs a paid Apple Developer account ($99/yr) in your name; SignPath does not cover it. Until then
Mac users allow the app once under **System Settings → Privacy & Security**. Both Mac builds work;
Gatekeeper just objects the first time.

Worth doing only if Mac downloads turn out to be significant — it is a papercut, not a blocker, and
the instructions are already on the download page.

---

## How a future release works

```powershell
# 1. add a "## 1.2.0" section to CHANGELOG.md   (the build fails without one)
# 2. bump <Version> in Directory.Build.props    (local dev builds only)
git tag v1.2.0
git push public v1.2.0
```

~4 minutes → review the draft → **Publish** → winget PR opens itself (once step 4 is on) and users
auto-update on next launch.

Two things stay manual on purpose:

- **The changelog.** Generated from commit subjects it would be a log, not release notes — and it
  is the part users actually read. The build fails when a section is missing, which catches
  forgetting without pretending to write it for you.
- **Publishing the draft.** That is the step that pushes an auto-update to everyone already running
  the app. Worth one human look.

---

## Not blocking anything

- **Flathub** — more reach on Linux, but the AppImage already works with no package manager. Do it
  if Linux traffic justifies it.
- **Remaining UI work** — the redesign spec in `docs/ui-redesign-proposal.md` is fully implemented.
  What is left is refinement: collapsing all but the entry being edited, and moving cover letters
  onto the three-zone surface.
- **Persisting API keys.** Deliberately memory-only today, and the privacy notice promises exactly
  that. With two providers, re-entering a key every launch is friction worth revisiting — but it needs a
  decision (OS credential store, opt-in) rather than a silent change.

# Distribution & UX plan

> Written 2026-07-27, carried over from a planning session in the `upwork-proposals`
> workspace. Goal: get this downloadable from **gentianshkembi.com** in the form that is
> simplest for a non-technical user, at **zero cost**, on as many OSes as possible.
> Companion docs: `web-app-plan.md` (future web version — not in scope here) and
> `../portfolio/docs/resumebuilder-distribution.md` (what the portfolio site must build to
> host these downloads — its `Tool` model needs extending from one Windows binary to
> multi-platform artifacts with checksums).

## Decisions made

- **Open source it.** Gentian confirmed he is fine with this. It is the only route to a
  **free code-signing certificate** (SignPath.io's Foundation programme for OSS), which is
  what stops the Windows SmartScreen "unknown publisher" warning that currently scares
  people off. It also unlocks unlimited GitHub Actions minutes and an easier Flathub path,
  and makes the repo usable as a portfolio artifact. Needs a LICENSE file (MIT or
  Apache-2.0) — the repo has none today.
- **Keep Avalonia. Do not switch to a webview.** The UI problems are a missing design
  system, not the framework (see below). A Photino/Blazor rewrite would need WebKitGTK on
  Linux, which breaks the clean self-contained AppImage — working directly against the
  "simplest for the user" goal. Core/Data/Templates/Export are UI-agnostic and survive any
  shell change anyway, so only `ResumeBuilder.App` is ever in question.
- **Host on Cloudflare R2**, which Gentian already runs (see the `infrastructure` repo:
  Hetzner + Coolify + Traefik + Cloudflare). Free tier, and crucially **zero egress fees**.
- **Velopack** (MIT) for installers + delta auto-updates. Works on Windows, macOS and
  Linux from one toolchain, and reads its update feed from any static HTTP host — so R2
  serves both the download and the update feed.

## Format per OS — never ship a bare .exe

The current `publish/win-x64` bare executable is the most alarming possible shape; that is
exactly the reaction Gentian noticed.

| OS | Ship | Notes |
| --- | --- | --- |
| Windows | Velopack `Setup.exe` installer + portable `.zip` | An *installer* exe reads completely differently from a loose binary. MSI via WiX is the alternative if a non-exe format is wanted, but loses built-in updates. |
| Linux | **AppImage** + Flathub | AppImage: single file, `chmod +x`, no root. Flathub adds trust and auto-updates, free. ⚠️ Must bundle **libfontconfig1** — see the finding under step 3 below. |
| macOS | `.dmg` containing a proper `.app` | ⚠️ Unsigned without the $99/yr Apple account — Gatekeeper says "damaged and can't be opened". Ship as an advanced download with instructions, or defer. Build-only; there is no Mac to test on. |

Publish **self-contained + single-file** so nobody installs a .NET runtime first.
`Directory.Build.props` currently sets only `net8.0` — RIDs, `SelfContained` and
`PublishSingleFile` still need adding.

## Secrets audit — done 2026-07-27, clean

Scanned the working tree **and every blob ever committed** (3 commits, 143 distinct paths,
so exhaustive not sampled):

- No API keys, tokens, private keys or connection strings — in HEAD or in history.
- No `.env`, `.pfx`, `.pem`, `.publishsettings` or credential files ever committed.
- `.gitignore` already covers `.vs/`, `publish/`, `*.user`, `.env*`, local appsettings.
- `samples/sample-resume.json` is correctly fictional (Jane Doe / example.com).

**One item to fix before going public:** `gentian_shkembi_resume.json` at the repo root
holds Gentian's real details including his **personal mobile number**. It is not needed —
`samples/sample-resume.json` is already the fixture. Remove it and purge it from history
(trivial at 3 commits). Do this **before** the repo goes public.

## UX diagnosis

The app is feature-rich but presents like a 2005 desktop app. Three concrete causes:

1. **No design system.** `App.axaml` is `<FluentTheme />` plus two ad-hoc styles (a dimmed
   opacity and a red error border). No tokens, no typography scale, no spacing system, no
   custom `ControlTheme`s. Inter *is* loaded via `.WithInterFont()` — that part is fine.
2. **`Views/MainWindow.axaml` is 1,722 lines in one file.** No UserControls. CLAUDE.md
   already describes it as "one window, overlays toggled by booleans, no navigation
   framework". There is no structure to iterate on.
3. **Menu-bar information architecture hides the selling points.** Local AI, Tailor to Job,
   the template gallery and PDF/LinkedIn import all live three levels deep under `_Tools`
   and `_File`. A first-time user sees a plain form and never discovers what the product
   actually does.

Point 3 is the expensive one: the features that differentiate this product are invisible.

## Agreed approach for the redesign

Design first in HTML as a **visual spec only** (the app stays Avalonia), get approval, then
implement. Same play as GL Motors' "bespoke Tailwind design system with a written spec
consumed by AI coding agents", which already worked.

Translation is mechanical: design tokens → `ResourceDictionary` entries, components →
`ControlTheme`s, and the 1,722-line monolith splits into UserControls per region.
Consider **Semi.Avalonia** as a modern base theme rather than styling stock Fluent.

## Next steps

1. ~~Purge `gentian_shkembi_resume.json` from the working tree **and** git history.~~
   **Done 2026-07-27** — see the progress log below. Local history only; not yet pushed.
2. ~~Add a LICENSE (MIT or Apache-2.0).~~ **Done** — MIT.
3. ~~Add RIDs + `SelfContained` + `PublishSingleFile`; verify `win-x64`, `linux-x64` and
   `osx-arm64` all produce runnable output.~~ **Done** — all three produce exactly one file.
4. ~~**Run the app and capture the current UI** before designing anything. Then produce the
   redesign proposal grounded in real screens.~~ **Done** — screens in `docs/ui-baseline/`,
   proposal in `docs/ui-redesign-proposal.md`.
5. ~~Add Velopack packaging.~~ **Done** — verified locally, see below.
6. ~~Release workflow: tag → matrix build → package → upload.~~ **Done** —
   `.github/workflows/release.yml`. Not yet exercised on a real runner.
7. Make the repo public + apply for the SignPath OSS certificate.
8. Download page on the portfolio: screenshots, the local-LLM privacy pitch, checksums,
   per-OS buttons.
9. Optional channels: winget manifest, then Flathub.

Steps 1–4 depend on nothing else and can start immediately.

## Progress log — steps 1–4, 2026-07-27

**1. Personal data purged.** `gentian_shkembi_resume.json` removed from the working tree and
from all three commits via `git filter-branch`. Verified exhaustively: 0 of 262 objects
reachable from the rewritten `main` contain the phone number, and no commit's tree carries
the path. A byte-identical copy (sha256 `e23be1e7…`) is preserved outside the repo at
`~/Documents/resumebuilder-personal-data/`, with a README explaining why. `.gitignore` now
blocks `/*_resume.json` at the root so it cannot return by accident.

> **Not yet pushed — this is the important caveat.** `origin/main` still holds the old
> history, so the file is still exposed on GitHub. Finish with:
> `git push --force-with-lease origin main`. The stale `origin/overhaul/bugfixes-features-templates`
> branch also still carries it; its content is already in `main` via the squash-merge of
> PR #1, so it can be deleted. A pre-rewrite backup bundle of all refs was taken first.

**2. MIT LICENSE added.** README's "Not currently licensed for redistribution" replaced.
Noted there that FluentAssertions 8.x (test-only, never shipped) is under the Xceed
Community License — free for open source, paid for commercial use. Does not affect
redistributing the app.

**3. Publish configured and verified on all three RIDs.** Properties live in
`ResumeBuilder.App.csproj`, not `Directory.Build.props`, so the libraries and the test
project stay RID-agnostic; they are conditioned on `'$(RuntimeIdentifier)' != ''` because
`SelfContained`/`PublishSingleFile` fail a plain solution build with NETSDK1031.
`DebugType=embedded` moved to `Directory.Build.props` so project-reference `.pdb`s stop
landing in the drop. Result — **exactly one file each**, no loose DLLs, no `LatoFont/`:

| RID | Output | Size | Verified |
| --- | --- | --- | --- |
| win-x64 | `ResumeBuilder.App.exe`, PE32+ GUI | 65.6 MB | **Launched** — real window, "Resume Builder" |
| linux-x64 | `ResumeBuilder.App`, ELF 64-bit PIE | 62.7 MB | Ran under WSL: runtime boots, bundle self-extracts all 5 native libs, managed `Main` runs, Avalonia initialises |
| osx-arm64 | `ResumeBuilder.App`, Mach-O arm64 | 63.8 MB | Correct format; no Mac to launch on |

> **Linux finding that matters for the AppImage.** A self-contained .NET app still needs
> some *system* libraries. On a minimal image the only unresolved dependency is
> **`libfontconfig.so.1`** (`libfontconfig1`) — SkiaSharp links against it, and without it
> the app aborts in `SKImageInfo`'s static constructor before drawing anything. Everything
> else (`libHarfBuzzSharp`, `libQuestPdfSkia`, `libqpdf`, `libe_sqlite3`) resolves cleanly.
> The AppImage must bundle or declare it. This also affects CI, which is why the Linux leg
> now installs it before running the tests.

Build stays at **0 warnings**; **323/323 tests pass**.

**Also done, prompted by "it should not be Windows only":** audited the source for
Windows-only assumptions and found none — all four storage paths use
`Environment.SpecialFolder.LocalApplicationData` (which .NET maps per-OS), and there is no
`System.Drawing`, `DllImport`, `Registry` or `Process.Start` anywhere. Every package is
cross-platform. To keep it that way, `ci.yml` is no longer Windows-only: `build & test` is
now a **windows/ubuntu/macos matrix**, plus a `publish` job that asserts each RID's drop is
exactly one file and smoke-tests the Linux binary under `xvfb` (staying up for 20s is the
pass condition). This overlaps step 6 deliberately — step 6 remains the *release* workflow.

*Unverified:* the new CI jobs have not run on a real GitHub runner yet.

**4. UI captured and proposal written.** Ran the published exe, imported
`samples/sample-resume.json`, captured nine surfaces into `docs/ui-baseline/`. Findings and
a sequenced plan are in `docs/ui-redesign-proposal.md`. Two headlines: the **PDF output is
already good** — every problem is in the chrome around it — and the **template gallery has
no thumbnails**, so users pick from 25 visual designs by reading text.

## Release pipeline — steps 5–6, done 2026-07-28

`git tag v1.0.0 && git push --tags` now builds every platform, packages installers and drafts
a GitHub Release. **This is the point at which the app is distributable.**

Verified end-to-end locally on win-x64 — `vpk pack` produced:

| Artifact | Size | What it is |
| --- | --- | --- |
| `ResumeBuilder-win-Setup.exe` | 70 MB | The installer (real PE32) — what most people download |
| `ResumeBuilder-win-Portable.zip` | 65 MB | No-install option |
| `ResumeBuilder-1.0.0-full.nupkg` | 65 MB | Update payload |
| `RELEASES`, `releases.win.json` | <1 KB | The update feed |

`vpk` also **verifies the `VelopackApp.Run()` call site** and fails packaging if it is missing or
misplaced — a useful guard, since getting it wrong breaks installs silently rather than loudly.

Two decisions the local run settled:

- **No separate portable step.** `vpk pack` already emits `Portable.zip`, so building our own
  single-file zip alongside it would ship two near-identically named portable downloads.
- **`PublishSingleFile` is off for the installer leg.** This resolves the tension flagged below:
  delta updates diff package contents, so a compressed single-file blob makes every update a full
  ~60 MB download. Self-contained still applies, so no .NET runtime is needed either way.

### First real run — v1.0.0, 2026-07-28

Tag pushed, both workflows exercised on real runners. **Everything green**, and the release
produced a complete set of installers:

| Platform | Installer | Portable | Feed |
| --- | --- | --- | --- |
| Windows | `ResumeBuilder-win-Setup.exe` (69 MB) | `win-Portable.zip` | `releases.win.json` |
| Linux | `ResumeBuilder.AppImage` (59 MB) | — | `releases.linux.json` |
| macOS | `ResumeBuilder-osx-Setup.pkg` (59 MB) | `osx-Portable.zip` | `releases.osx.json` |

Plus `SHA256SUMS-*.txt` per platform. The release is a **draft** — nothing is downloadable until
it is published by hand.

**The cross-platform CI paid for itself on its first run.** It failed on ubuntu with 322/323:
`PngExporter.StitchVertically` died in `SKData`'s static constructor because `libSkiaSharp.so` was
missing. SkiaSharp depends on the Win32 and macOS native packages but resolves the Linux one only
through the RID runtime graph, which needs a `RuntimeIdentifier` — so `dotnet publish -r linux-x64`
picked it up (which is why the packaged app ran) while the RID-less test build never did. The bug
was latent from the start; CI had only ever run on `windows-latest`. Fixed by referencing
`SkiaSharp.NativeAssets.Linux` from Export, which also matters for the Linux-hosted web version.

### Still required before the public can actually download it

1. ~~Force-push the purged history.~~ **Done** — `main` on GitHub is now the rewritten history.
2. ~~Auto-update is not wired.~~ **Done** — `UpdateService` reads the GitHub releases feed, so
   updates work without waiting for R2. Moving to R2 later is a URL change and nothing else.
3. ~~`release.yml` has never run on a real runner.~~ **Done** — see above.
4. **Builds are unsigned**, so Windows SmartScreen still warns. Fixed by the SignPath OSS
   certificate, which requires a public repo — and that is currently blocked, see below.

### Resolved: published from a fresh repo instead

**The public repo is [github.com/Gentian28/resume-builder](https://github.com/Gentian28/resume-builder)**,
created 2026-07-28 and pushed with the rewritten history only. Verified immediately after the
push that it exposes exactly two refs — `HEAD` and `refs/heads/main` — and no `refs/pull/*`, so
the problem described below cannot exist there.

The original `Gentian28/resumebuilder` **stays private and untouched**, keeping PR #1 and the
Actions history. Nothing was deleted and GitHub Support was not needed.

Consequences worth remembering:

- `UpdateService.DefaultFeedUrl` points at the **public** repo. A private feed returns 404 to
  unauthenticated clients, so installed copies would silently never find an update.
- The local clone has two remotes: `origin` (private, original) and `public`. `main` tracks
  `public`. Pushing work to `origin` is almost certainly a mistake now.
- Releases are cut from the public repo. v1.0.0 built green there and produced the same artifact
  set as the trial run.

### The problem this avoided: `refs/pull/1/head`

Rewriting `main` did not remove the personal résumé from GitHub. GitHub retains pull-request refs
permanently and independently of branches, and `refs/pull/1/head` still points at pre-rewrite
history. Verified 2026-07-28 by fetching that ref: `gentian_shkembi_resume.json` is present and
still contains the mobile number.

It stays harmless only because `Gentian28/resumebuilder` remains private, so only collaborators
can fetch that ref. Force-pushing cannot remove it — the ref is not reachable from any branch, and
GitHub keeps it regardless.

**So: never make `Gentian28/resumebuilder` public.** Publishing happens from `resume-builder`,
which never had a pull request and therefore never had the ref. Had we wanted to publish the
original instead, the options were deleting and recreating it, or asking GitHub Support to
garbage-collect unreachable objects — both worse than simply starting a clean repo.

The general lesson: rewriting history fixes branches, not pull-request refs. If a secret has ever
been in a PR on a repo that will go public, a fresh repo is the reliable fix.

### Tension resolved at step 5

`PublishSingleFile` + `EnableCompressionInSingleFile` produces one compressed 60 MB blob.
Velopack's **delta updates diff package contents** — a compressed single file defeats that,
so every update becomes a full 60 MB download. Single-file is right for the portable `.zip`
and the AppImage; for the Velopack-installed build it may be better to publish
multi-file. Both can be produced from the same csproj by passing `-p:PublishSingleFile=false`
for the installer leg.

## Selling point to lead with

**The local LLM already works** — `Core/SmartContent/LocalAiService.cs`, configurable base
URL, `UseLocalAiEndpoint()` pointing at `http://localhost:11434/v1` (Ollama). Combined with
local SQLite and no account, the pitch is: *your résumé never leaves your machine.* For a
document holding someone's entire employment history that is a real differentiator, and it
justifies installing a desktop app over using a web one.

Verified 2026-07-27: **323 tests, all passing**; 25 résumé + 3 cover-letter templates.

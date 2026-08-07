# Send this to the portfolio session today

Self-contained — assumes that session knows nothing about this project. Nothing in it depends on
work that isn't finished, and every download URL is verified to resolve.

Screenshots to copy across: `docs/screenshots/` in this repo (`editor.png`, `template-gallery.png`,
`first-run.png`). All three show an empty or synthetic résumé — no personal data.

**Do this first (1 minute, then it runs unattended):** GitHub → Actions → Release → Run workflow →
Version `1.1.0`. The links below always point at the newest release, so the page picks it up on its
own. If you skip it, the page still works — visitors just get 1.0.3.

---

> I want to add a download page for a desktop app I built, plus a card linking to it from my
> projects section.
>
> **Resume Builder** — a free, open-source résumé builder that runs entirely on your own computer.
> No account, no upload, no subscription. 25+ templates, PDF / DOCX / HTML / plain-text export,
> LinkedIn and PDF import, ATS keyword scoring, and optional AI assistance that can point at a local
> LLM so nothing leaves your machine. MIT licensed, Windows / macOS / Linux.
> Source: https://github.com/Gentian28/resume-builder
>
> **The main UX problem to solve:** it ships four builds and most visitors need exactly one. Detect
> the OS from `navigator.userAgentData` / `navigator.userAgent`, lead with a single primary button
> for that platform, and put the rest behind an "Other platforms" disclosure. Always render every
> option in the HTML — so it still works with JS disabled, and so someone on Windows can grab the
> Linux build. Treat detection as a guess, never a lock-in.
>
> **Download URLs.** Use the `releases/latest/download/` form so links never need updating when I
> release. Base: `https://github.com/Gentian28/resume-builder/releases/latest/download/`
>
> | Platform | Primary | Secondary |
> |---|---|---|
> | Windows | `ResumeBuilder-win-Setup.exe` | `ResumeBuilder-win-Portable.zip` |
> | macOS (Apple Silicon) | `ResumeBuilder-osx-Setup.pkg` | `ResumeBuilder-osx-Portable.zip` |
> | macOS (Intel) | `ResumeBuilder-osx-x64-Setup.pkg` | `ResumeBuilder-osx-x64-Portable.zip` |
> | Linux | `ResumeBuilder.AppImage` | — |
>
> Copy those exactly. Note the Apple Silicon build has **no `arm64`** in its filename — it is plain
> `osx`, while Intel is `osx-x64`. Easy to write backwards; an invented name 404s.
>
> **Two caveats that need stating plainly, because they cost installs when people hit them cold:**
>
> 1. **Windows SmartScreen.** The app isn't code-signed yet (certificate applied for). Windows shows
>    "Windows protected your PC". Say so up front and give the way through — *More info → Run
>    anyway* — instead of letting people meet it unwarned and assume malware. Frame it accurately:
>    unsigned, not unsafe. Link the checksum for anyone who wants to verify:
>    `SHA256SUMS-win-x64.txt` (also `-osx-arm64`, `-osx-x64`, `-linux-x64`).
> 2. **macOS Gatekeeper.** Same: right-click → Open the first time, or
>    `xattr -d com.apple.quarantine <file>`.
>
> **The privacy angle is the differentiator** — every competitor is a subscription web app that
> holds your employment history. Say it once, clearly, near the top: résumés are stored in a local
> database on your machine, there's no account, and the AI features can run against a model on your
> own computer. Once. Repeating it in every section reads as protesting too much.
>
> **Screenshots** are in the repo I'll share — an editor view, the template gallery, and the
> first-run screen. The gallery one is the most persuasive; it shows the range of designs at a
> glance. Lead with that.
>
> **Tone:** match the rest of my portfolio. This is a project page, not a SaaS landing page — no
> pricing table, no testimonials, no "trusted by" logos. Screenshot, what it does, download button,
> honest caveats.

---

## Deliberately left out

**winget.** ~~Deliberately left out~~ — no longer. microsoft/winget-pkgs#408983 merged and
published on 2026-08-07 and `winget install Gentian28.ResumeBuilder` now works (verified in the
index). Include this with the message above:

> The winget submission merged. On the Windows card, add a package-manager option under the
> download button:
>
> ```
> winget install Gentian28.ResumeBuilder
> ```
>
> Worth one line noting this route also avoids the SmartScreen prompt entirely, since winget
> installs through its own trusted client — that's the main reason a Windows user would pick it.

**Code-signing.** SignPath declined the application (2026-07-29 — new project, not enough public
adoption for their Foundation programme), so the SmartScreen section stays. If a future
reapplication succeeds, the whole section can be deleted rather than reworded.

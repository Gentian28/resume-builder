# Brief for the portfolio site

Paste the block below into the Claude session for your portfolio repo. It is written to be
self-contained — it does not assume that session knows anything about this project.

A working reference implementation of the page already lives at `docs/download-page/index.html`;
point that session at it if you want the markup rather than a description.

---

## The message to send

> I want to add a download page for a desktop app I built, and a card linking to it from my
> projects section.
>
> **The app:** Resume Builder — a free, open-source résumé builder that runs entirely on your own
> computer. No account, no upload, no subscription. 25+ templates, PDF/DOCX/HTML export, LinkedIn
> and PDF import, ATS keyword scoring, and optional AI assistance that can run against a local LLM
> so nothing leaves your machine. MIT licensed. Source:
> https://github.com/Gentian28/resume-builder
>
> **The core UX problem:** it ships for four platforms and most visitors need exactly one build.
> Detect the visitor's OS with `navigator.userAgent` / `navigator.userAgentData` and lead with a
> single primary button for that platform. Put the other platforms behind a "Other downloads"
> disclosure — present, not prominent. Always render every option in the HTML so it works with
> JavaScript disabled and so someone on Windows can still grab the Linux build.
>
> **Download URLs.** Use GitHub's `releases/latest/download/...` form so the links never need
> updating when I cut a release. These filenames are verified against the published release — copy
> them exactly, an invented one 404s:
>
> | Platform | Primary | Also |
> |---|---|---|
> | Windows | `ResumeBuilder-win-Setup.exe` | `ResumeBuilder-win-Portable.zip` |
> | macOS (Apple Silicon) | `ResumeBuilder-osx-Setup.pkg` | `ResumeBuilder-osx-Portable.zip` |
> | macOS (Intel) | `ResumeBuilder-osx-x64-Setup.pkg` | `ResumeBuilder-osx-x64-Portable.zip` |
> | Linux | `ResumeBuilder.AppImage` | — |
>
> Base: `https://github.com/Gentian28/resume-builder/releases/latest/download/`
>
> Note the Apple Silicon build has **no** `arm64` in its name — it is plain `osx`, while Intel is
> `osx-x64`. Easy to get backwards.
>
> Checksums are published per platform as `SHA256SUMS-win-x64.txt`, `SHA256SUMS-osx-arm64.txt`,
> `SHA256SUMS-osx-x64.txt`, `SHA256SUMS-linux-x64.txt` — link the relevant one next to the
> unsigned-build note so anyone who wants to verify can.
>
> **Two things that need saying plainly, because they cost me installs if I don't:**
>
> 1. **The Windows SmartScreen warning.** The app is not code-signed yet (a certificate is applied
>    for). Windows will show "Windows protected your PC". Tell people up front how to get past it —
>    *More info → Run anyway* — rather than letting them hit it cold and assume it's malware. Frame
>    it as what it is: unsigned, not unsafe, and here's the checksum if you want to verify.
> 2. **macOS Gatekeeper.** Same deal: right-click → Open the first time, or
>    `xattr -d com.apple.quarantine`.
>
> **The privacy claim is the differentiator** — every competitor is a subscription web app that
> holds your employment history. Say it once, clearly, near the top: your résumés are stored in a
> local database on your machine, there is no account, and the AI features can run against a model
> on your own computer. Don't repeat it in every section; repetition reads as protesting too much.
>
> **Tone:** match the rest of my portfolio. This is a project page, not a SaaS landing page — no
> pricing table, no testimonials, no "trusted by" logos. A screenshot, what it does, the download,
> and the honest caveats.

---

## winget — live as of 2026-08-07, send the follow-up

`winget install Gentian28.ResumeBuilder` **works now.** microsoft/winget-pkgs#408983 merged and
the publish pipeline completed on 2026-08-07; the package is verified present in the index
(currently serving 1.0.3, which auto-updates to the latest on first launch — a 1.2.0 manifest
bump is open as microsoft/winget-pkgs#413617).

Send this follow-up to the portfolio session:

> The winget submission went through. On the Windows card, add a package-manager option under the
> download button:
>
> ```
> winget install Gentian28.ResumeBuilder
> ```
>
> Worth a one-line note that installing this way also skips the SmartScreen prompt, since winget
> fetches through its own trusted client — that's the main reason a Windows user would prefer it.

---

## After you cut a release

Nothing on the page needs changing — the `releases/latest/download/` URLs resolve to whatever the
newest release is. Only revisit it if an asset is renamed, or if a SignPath reapplication ever
succeeds (the first application was declined 2026-07-29 for lack of public adoption) and the
SmartScreen section can be deleted.

**But cut 1.1.0 before you build the page.** The links work today and resolve to v1.0.3 — the
pre-redesign build, without the first-run screen, the three-zone layout, or Anthropic support. A
page whose screenshots don't match what downloads is a worse first impression than no page.

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
> updating when I cut a release:
>
> | Platform | File |
> |---|---|
> | Windows (installer) | `ResumeBuilder-win-Setup.exe` |
> | Windows (portable) | `ResumeBuilder-win-Portable.zip` |
> | macOS (Apple Silicon) | `ResumeBuilder-osx-arm64.zip` |
> | macOS (Intel) | `ResumeBuilder-osx-x64.zip` |
> | Linux | `ResumeBuilder.AppImage` |
>
> Base: `https://github.com/Gentian28/resume-builder/releases/latest/download/`
>
> Also show the winget line for Windows users who prefer it:
> `winget install Gentian28.ResumeBuilder`
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

## After you cut a release

Nothing on the page needs changing — the `releases/latest/download/` URLs resolve to whatever the
newest release is. Only revisit it if an asset is renamed, or when the SignPath certificate comes
through and the SmartScreen section can be deleted.

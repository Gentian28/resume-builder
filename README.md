# ResumeBuilder

A cross-platform desktop resume builder: edit a resume in a live editor, see a real-time PDF preview, pick from 25 templates, and export to PDF, DOCX, HTML, PNG, plain text, or JSON Resume.

**Your resume never leaves your machine.** No account, no upload, no cloud. Resumes are stored in a local SQLite database, and the AI features can run against a local model (Ollama, LM Studio) so even those work fully offline.

## Download

Grab the latest build from the [releases page](https://github.com/Gentian28/resume-builder/releases).

| Platform | Download | Notes |
| --- | --- | --- |
| Windows | `ResumeBuilder-win-Setup.exe` | Installer, updates itself. `win-Portable.zip` if you'd rather not install. |
| Linux | `ResumeBuilder.AppImage` | `chmod +x` and run. Needs `libfontconfig1` — present on most desktops. |
| macOS — Apple silicon | `ResumeBuilder-osx-Setup.pkg` | M1 and later. See the Gatekeeper note below. |
| macOS — Intel | `ResumeBuilder-osx-x64-Setup.pkg` | 2019 and earlier. An Apple silicon build will not run on Intel. |

Not sure which Mac you have? Apple menu → About This Mac.

No .NET runtime needed — every build is self-contained.

Builds are **not yet code-signed**, so Windows SmartScreen shows an "unknown publisher" warning
(More info → Run anyway) and macOS Gatekeeper will refuse the package outright until you allow it
under System Settings → Privacy & Security. `SHA256SUMS-*.txt` is published with every release if
you want to verify what you downloaded. Signing is planned via SignPath's free OSS certificate.

## Features

- **Live preview** — the editor renders the selected template to a paged PDF preview as you type.
- **25 resume templates** — Modern, Classic, Minimal, Creative, Executive, Technical, Academic, Two Column, Compact, Elegant, Professional, Starter, Simple, Timeline, Bold, Dark Sidebar, Infographic, ATS Plain, Chronology, Color Block, Developer, Europass, Federal, One Page, Photo Header — plus 3 cover-letter templates.
- **Section ordering and visibility** — reorder or hide any section, including custom sections.
- **Export** — PDF, DOCX, HTML, PNG, plain text (ATS-friendly), native JSON, and [JSON Resume](https://jsonresume.org/).
- **Import** — JSON Resume, native JSON, LinkedIn data export (.zip), and PDF text extraction.
- **Spell check** — Hunspell, with a personal dictionary.
- **ATS keyword analysis** — paste a job description to get a match score, matched/missing keywords, and warnings.
- **AI assistance** — optional; works against OpenAI or any OpenAI-compatible endpoint, including a local LLM (Ollama, LM Studio).
- **Undo/redo** — across text edits and list operations.
- **Sync** — two-way sync through a local folder (point it at Dropbox/OneDrive/Drive), with conflict detection.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later
- Windows, macOS, or Linux (the UI is [Avalonia](https://avaloniaui.net/))

## Getting started

```bash
git clone https://github.com/Gentian28/resume-builder.git
cd resume-builder

dotnet restore ResumeBuilder.sln
dotnet build ResumeBuilder.sln
dotnet run --project src/ResumeBuilder.App/ResumeBuilder.App
```

Run the tests:

```bash
dotnet test ResumeBuilder.sln
```

`samples/sample-resume.json` is a synthetic JSON Resume file you can import to see the app populated.

## Project layout

The solution is layered; each project depends only on the ones above it.

| Project | Responsibility |
| --- | --- |
| `ResumeBuilder.Core` | Domain models, validation, undo/redo, spell check, keyword analysis, AI, sync. No UI, no persistence. |
| `ResumeBuilder.Data` | EF Core + SQLite persistence. Owns the `Resume` schema and repository. |
| `ResumeBuilder.Templates` | QuestPDF template implementations and the template registry. |
| `ResumeBuilder.Export` | Exporters (PDF/DOCX/HTML/PNG/TXT/JSON) and importers (JSON Resume, LinkedIn, PDF). |
| `ResumeBuilder.App` | Avalonia desktop UI (MVVM, CommunityToolkit.Mvvm). |
| `ResumeBuilder.Tests` | xUnit tests across Core, Data, Export, and Templates. |

Dependency direction: `App → {Core, Data, Templates, Export}`, `Export → {Core, Templates}`, `Data → Core`, `Templates → Core`.

### Where things live

- **Data storage**: `%LocalAppData%/ResumeBuilder/resumes.db` (SQLite). Created on first run; schema upgrades are applied automatically by `DatabaseInitializer`.
- **Dictionaries**: `%LocalAppData%/ResumeBuilder/Dictionaries/` (downloaded on first spell check).
- **Sync state**: `%LocalAppData%/ResumeBuilder/sync-state.json`.

## Adding a template

1. Add a class under `ResumeBuilder.Templates/Templates/` deriving from `BaseTemplate`.
2. Fill in its `Info` (`Id`, `Name`, `Description`, `Category`, `Layout`, and its default accent color / font).
3. Drive the body from `GetOrderedSections()` and `ShouldRenderSection(...)` so the user's section order and visibility are honored, and use the shared `Compose*` helpers on `BaseTemplate` for standard blocks.
4. Register it in `TemplateRegistry`.

The template smoke test renders every registered template against a fully populated resume, so a new template is covered as soon as it is registered.

## Configuration

The AI features are off until configured. They target any OpenAI-compatible `/chat/completions` endpoint:

- **OpenAI**: supply an API key. Requests (including your resume text) go to `api.openai.com`.
- **Local LLM**: point the base URL at a loopback address (Ollama, LM Studio). No key is needed and nothing leaves your machine.

## License

[MIT](LICENSE) — © 2026 Gentian Shkembi.

One dependency to be aware of if you fork this for commercial work: **FluentAssertions 8.x**
(test project only, never shipped in the app) moved to the Xceed Community License, which is
free for open-source and non-commercial use but requires a paid licence for commercial use.
It does not affect using or redistributing the application itself.

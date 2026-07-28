# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build ResumeBuilder.sln            # must end with 0 warnings — analyzers are on and warnings are treated as regressions
dotnet test ResumeBuilder.sln             # full suite (~323 tests, a few seconds)
dotnet test src/ResumeBuilder.Tests/ResumeBuilder.Tests.csproj --filter "FullyQualifiedName~RepositoryTests"   # one class
dotnet test ResumeBuilder.sln --filter "FullyQualifiedName~Update_StaleCopy_ThrowsInsteadOfClobbering"         # one test
dotnet run --project src/ResumeBuilder.App/ResumeBuilder.App   # launch the desktop app
```

Package versions live **only** in `Directory.Packages.props` (Central Package Management); `TargetFramework` (net8.0), nullable, and analyzers live only in `Directory.Build.props`. Never add a `Version=` attribute or a `<TargetFramework>` to a csproj. CI (`.github/workflows/ci.yml`) fails on any NuGet package with a known advisory — if it flags a transitive package, pin a patched version in `Directory.Packages.props` (see the `SQLitePCLRaw` and `Tmds.DBus.Protocol` entries there for the pattern: check the advisory's `firstPatchedVersion` and take the smallest bump that clears it).

## Architecture

Layered class libraries under `src/`, each project only depending on the ones above it:

- **Core** — domain models, validation, undo/redo, spell check, keyword analysis/AI (`SmartContent/`), sync. No UI, no persistence. Server-ready.
- **Data** — EF Core + SQLite (`%LocalAppData%/ResumeBuilder/resumes.db`). Depends on Core.
- **Templates** — QuestPDF renderers (25 resume + 3 cover-letter templates) and `TemplateRegistry`. Depends on Core.
- **Export** — exporters (PDF/DOCX/HTML/PNG/TXT/JSON/JSON-Resume) and importers (JSON, LinkedIn zip, PDF). Depends on Core + Templates.
- **App** — Avalonia 11 desktop UI, MVVM via CommunityToolkit (`[ObservableProperty]`/`[RelayCommand]`). The composition root is `App.axaml.cs`; services are bundled in `Services/AppServices.cs`. Almost everything lives in `ViewModels/MainWindowViewModel.cs` (+ `.CoverLetters.cs` partial) and `Views/MainWindow.axaml` — one window, overlays toggled by booleans, no navigation framework.

Core cannot reference Data, so sync inverts the dependency: `ISyncResumeStore` (Core) is implemented by `IResumeRepository` (Data) and handed to `LocalFolderSyncService`.

`docs/web-app-plan.md` is the plan for a future web version — Core/Templates/Export are kept deliberately server-compatible; don't introduce desktop-only dependencies below the App layer.

## Persistence — the non-obvious parts

- **Short-lived DbContexts only.** `ResumeRepository` creates a context per operation via `IResumeDbContextFactory`. Never hold a long-lived context: the UI thread and the autosave timer save concurrently.
- **Collections are JSON columns.** `Resume.Experiences/Skills/...` persist via `HasConversion` in `ResumeDbContext` (`JsonList`/`JsonObject` helpers). The `ValueComparer` there is what makes in-place mutations (`resume.Skills.Add(...)`) detectable — don't add a converted column without one.
- **No EF migrations.** Schema upgrades are idempotent steps in `Data/DatabaseInitializer.cs`: new tables are created from the model's create-script; **new columns must be added to its `AddedColumns` table by hand** or existing installs silently lack them. Adding a property to `Resume`/`CoverLetter` is not done until `AddedColumns` knows about it.
- **Optimistic concurrency.** `UpdateAsync` matches on the loaded `RowVersion`, rotates it, and throws `ResumeConcurrencyException` on conflict. Callers must keep the entity returned by save (it carries the new RowVersion) or the next save spuriously conflicts.
- **`Id` vs `SyncId`.** `Id` is local to one database; `SyncId` is the stable cross-machine identity used by sync. `ResumeRepository.ResetIdentity` clears all ids **and mints a new SyncId** — correct for imports/duplicates (a copy must not impersonate its source), but a file pulled from the app's own sync folder must *keep* its SyncId (`AdoptImportedResume(..., preserveSyncId: true)` in the App).

## Templates — hard requirements

Every resume template derives from `BaseTemplate` and must:

- drive its body from `GetOrderedSections()` + `ShouldRenderSection(...)` — never hardcode section order; render custom sections via `ComposeCustomSectionItems`;
- never drop user data with `.Take(n)` — density comes from layout, not truncation;
- format dates through `ResumeDateFormat` / `DateRange` (invariant culture — output must not vary by machine locale);
- use `ComposeSkillBar`/`GetSkillPercent` for proportional bars (`RelativeItem(100 - pct)` throws at pct == 100);
- never mutate the passed-in `Resume` (settings are cloned in `CreateDocument`, where `ApplyTemplateDefaults` applies the template's default color/font unless `IsAccentColorCustomized`/`IsFontCustomized` is set).

Register in `TemplateRegistry`; `TemplateRenderTests` then automatically smoke-tests the new template against populated/empty/all-hidden/photo resumes, and `TemplateContentTests` extracts the PDF text with PdfPig to assert nothing was silently dropped. If those fail, fix the template, not the test.

Styling has two persisted sources kept in step: legacy `Resume.AccentColor`/`FontFamily` and `Resume.TemplateSettings`. The repository calls `SyncLegacyStyling()` on every save; exporters may read either.

## Other invariants worth knowing

- **Both `.json` formats (native and JSON Resume) share one extension**; `Importers/JsonImporter` sniffs content and dispatches. Register new importers in `ExportService`; cover-letter export is the parallel `CoverLetterExportService` (the `IExporter` interface is typed to `Resume`, so the two registries are separate).
- **Achievements ↔ editor text** conversion lives only in `Core/Models/AchievementLines`. Tailored edits (`JobTailoringService`) address achievements by index through it — never re-implement the line splitting/joining, or accepted AI rewrites land on the wrong bullet.
- **Undo:** `TextEditAction`s with the same `FieldKey` merge within a 2s window (`IMergeableAction`). Recording during undo/redo is suppressed via `UndoRedoManager.IsExecutingAction`.
- **AI features must degrade, not gate.** `KeywordAnalyzer`, tailoring, and cover-letter drafting all work with no API key configured (analysis-only / structured fallback). Preserve that: never disable a feature behind `IsConfigured`.
- **Two AI providers, one interface.** `AiProviderRouter` (Core) fronts `LocalAiService` (any OpenAI-compatible server, incl. local LLMs) and `AnthropicAiService` (official Anthropic SDK). Prompts live *only* in `PromptBasedAiService` — subclasses supply transport, so the two cannot drift and a provider switch changes where data goes, not which features work. The router's `IsConfigured` reports the **active** provider only; each provider keeps its own key and model so switching back doesn't lose settings.
- **HTML export is escape-sensitive:** colors must match the strict hex pattern, hrefs must go through `UrlRule.ToSafeAbsoluteUrl` (rejects `javascript:` etc.).

## Repo notes

- **Two remotes, and the difference matters.** `public` → `Gentian28/resume-builder` is the
  public repo everything is published from; `main` tracks it, and releases are cut there.
  `origin` → `Gentian28/resumebuilder` is the original, **private and superseded** — its
  `refs/pull/1/head` still holds pre-rewrite history containing real personal data, which is why
  it must never be made public. Push to `public`.

- **Never commit real résumé data.** `samples/sample-resume.json` (synthetic — Jane Doe / example.com) is the fixture for tests and examples. A root-level `gentian_shkembi_resume.json` holding the owner's real name/phone/address was purged from the working tree and from every commit on 2026-07-27, ahead of open-sourcing; `.gitignore` now blocks `/*_resume.json` at the root so it cannot come back by accident.
- Tests live in `src/ResumeBuilder.Tests` (xUnit + FluentAssertions). The App project is intentionally not referenced by the test project (Avalonia WinExe); logic that needs testing belongs in Core (e.g. `AchievementLines` was extracted for exactly that reason).

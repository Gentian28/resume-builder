# Plan: ResumeBuilder on the web

Status: **plan only — no implementation yet.** This describes how to deliver ResumeBuilder as a web app alongside the existing Avalonia desktop app, what can be reused as-is, what genuinely has to be rebuilt, and in what order.

---

## 1. The core question: how much of the existing code survives?

The layering already done pays off here. Four of the five projects are plain .NET class libraries with no UI dependency, and they run unchanged on a server:

| Project | Web verdict | Notes |
| --- | --- | --- |
| `ResumeBuilder.Core` | **Reuse as-is** | Models, validation, undo/redo, keyword analysis, AI, tailoring, cover letters. No UI or platform dependency. |
| `ResumeBuilder.Templates` | **Reuse as-is** | QuestPDF runs fine server-side. This is the single biggest asset — 18 templates you don't rewrite. |
| `ResumeBuilder.Export` | **Reuse as-is** | QuestPDF / OpenXml / SkiaSharp / PdfPig are all server-capable. |
| `ResumeBuilder.Data` | **Reuse the shape, swap the provider** | EF Core stays; SQLite → PostgreSQL, and every query gains a user scope. See §4. |
| `ResumeBuilder.App` | **Does not port** | Avalonia XAML + ViewModels are desktop-only. The web UI is a rewrite. |

So the honest split is: **the domain, the 18 templates, and all seven export formats come across for free. The UI is a rewrite, and persistence needs real multi-tenancy.** That is a much better starting position than it sounds — the UI is the part you'd want to redesign for the web anyway, and the hard, valuable part (pixel-perfect PDF rendering in 18 layouts) is already done and now covered by smoke tests.

---

## 2. Recommended architecture

```
┌─────────────────────────────────────────────────┐
│  Browser (React + TypeScript SPA)               │
│  editor · template gallery · preview · tailoring│
└───────────────┬─────────────────────────────────┘
                │ REST/JSON + SSE
┌───────────────▼─────────────────────────────────┐
│  ResumeBuilder.Api  (ASP.NET Core)              │
│  auth · resume CRUD · render · export · AI proxy│
└───┬──────────┬──────────┬───────────┬───────────┘
    │          │          │           │
┌───▼────┐ ┌───▼─────┐ ┌──▼───────┐ ┌─▼──────────┐
│ Core   │ │Templates│ │ Export   │ │ Data       │
│(reused)│ │(reused) │ │ (reused) │ │(EF→Postgres│
└────────┘ └─────────┘ └──────────┘ └────────────┘
                                          │
                              ┌───────────▼────────┐
                              │ PostgreSQL + S3    │
                              │ (rows)    (assets) │
                              └────────────────────┘
```

### Why not Blazor?

Blazor is the tempting answer because it's C# end-to-end, and it deserves a fair hearing:

- **Blazor Server** would let you reuse ViewModel-ish logic and get a live preview cheaply. But it holds a SignalR circuit per user, is latency-sensitive on every keystroke, and scales poorly for a keystroke-heavy editor with a live PDF preview. A dropped connection loses editor state. For a resume editor — long sessions, lots of typing — this is the wrong tradeoff.
- **Blazor WASM** ships a multi-megabyte runtime and *still* can't run QuestPDF/SkiaSharp rendering client-side in any pleasant way, so you'd call the server for previews regardless. You'd pay the download cost and get no rendering benefit.

**Recommendation: ASP.NET Core Web API + a React/TypeScript SPA.** The editor is a form-heavy, latency-sensitive UI where the mature React ecosystem (form state, drag-and-drop reordering, diff views for AI suggestions) is a real advantage, and the server keeps doing what .NET is good at: rendering documents. If the team is C#-only and hiring React skills is the binding constraint, Blazor WASM with a server-rendered preview is an acceptable second choice — but decide this deliberately, not by default.

---

## 3. The hard problem: live preview

The desktop app re-rasterizes the whole document to PNG on a 300 ms debounce. You cannot naively do that over a network — it would mean a full PDF render + image transfer on every keystroke.

Three options, in order of increasing effort:

1. **Server render, debounced + cancellable (start here).** POST the resume JSON, get back PNG/PDF pages. Debounce to ~500 ms, cancel superseded renders server-side, cache by a hash of `(resume content, templateId, settings)` so no-op edits are free. Return only the pages that changed. This reuses the template engine exactly and is correct by construction — the preview *is* the output.
2. **Add an HTML/CSS preview renderer.** Render an approximate preview in the browser from the same model, and only call the server for the true PDF on export. Instant preview, but you now maintain *two* renderers per template and they will drift — the exact class of bug that made the DOCX and HTML exporters diverge from the PDF templates in the desktop app. Only do this if measurements show option 1 is too slow.
3. **Ship the renderer to the browser (WASM).** Not realistic with QuestPDF today.

Start with (1). Measure. Most resumes are 1–2 pages and QuestPDF renders them in tens of milliseconds; the network round-trip will dominate, and a 500 ms debounce hides it.

---

## 4. Persistence and multi-tenancy

This is where the desktop assumptions break, and it's the part most likely to bite.

- **Provider:** SQLite → PostgreSQL. The EF model mostly survives. The JSON-column conversions (`Experiences`, `Skills`, …) should become **real `jsonb` columns** — Postgres can index and query inside them, which SQLite could not.
- **Every row gets an owner.** Add `UserId` to `Resume` and `CoverLetter` and enforce it with an EF **global query filter** so a missing `WHERE user_id = ...` is impossible by construction rather than by discipline. This is the single highest-risk change: a leak here shows one person's resume to another.
- **Concurrency:** the `RowVersion` token added for the desktop app becomes *more* important, not less — the same user in two browser tabs is now the common case. The existing `ResumeConcurrencyException` path already handles it.
- **Schema management:** the desktop uses a hand-rolled `DatabaseInitializer` (idempotent `ALTER TABLE`s) because it had no migration history. **The web app must use real EF Core migrations** from day one. Generate an initial migration from the current model and retire the initializer on the server side.
- **Sync becomes obsolete.** `LocalFolderSyncService` exists to move resumes between machines. On the web the server *is* the source of truth — drop it from the web deployment. (Keep it in the desktop app; long term, the desktop syncing to the web API is the better story — see §8.)
- **Assets:** photos currently live as a `byte[]` in the row. On the web, put them in S3/blob storage and keep a URL, so rows stay small and images can be served/cached directly.

---

## 5. Feature-by-feature port

| Feature | Effort | Notes |
| --- | --- | --- |
| Resume CRUD, sections, ordering | Medium | New UI; the domain and validation come free. |
| 18 templates + gallery | **Free** | Server renders thumbnails once and caches them. |
| Export (PDF/DOCX/HTML/PNG/TXT/JSON) | **Nearly free** | Wrap `ExportService` in an endpoint returning a file stream. |
| Import (JSON Resume, LinkedIn zip, PDF) | Small | File upload endpoint → existing importers. Needs size limits and virus scanning. |
| Keyword/ATS analysis | **Free** | Pure computation on the server. |
| Tailor-to-job + AI rewrites | Medium | See §6 — the API-key model must change. |
| Cover letters | **Free** (backend) | New UI only. |
| Variants | **Free** (backend) | New UI only. |
| Undo/redo | Medium | `UndoRedoManager` is in Core and reusable, but for a web editor it's usually cleaner to use a client-side undo stack in the SPA. Don't force the desktop abstraction onto the browser. |
| Spell check | **Drop it** | `HunspellService` downloads dictionaries to `%LocalAppData%`. Browsers already spell-check `<textarea>`s natively and better. Deleting this on the web is a feature, not a regression. |
| Folder sync | **Drop it** | Superseded by the server (§4). |

---

## 6. AI, keys, and cost — decide this early

The desktop app lets the user paste their own OpenAI key, or point at a local LLM. **Neither model survives the move to the web:**

- A user's key cannot live in the browser (it would be readable by anyone with devtools, and it would leak in any XSS).
- A local LLM on the user's machine is not reachable from your server.

So the web app must **proxy AI calls server-side with the operator's own key**, which means you are now paying per request. That forces three decisions that don't exist on the desktop:

1. **Rate limiting and abuse protection** per user (tailoring a resume is several LLM calls; `JobTailoringService` currently makes one call per achievement bullet on the three most recent roles — that's easily 10+ calls per tailor run).
2. **A cost model** — free tier limits, or paid plans. This is the point where the product needs a business model, and it's worth deciding *before* building the billing-shaped hole.
3. **A privacy statement.** Resume text is personal data and it will now transit your server on its way to a model provider. Say so plainly, and consider a "no AI" mode that keeps everything server-local (the keyword analyzer and the fallback cover-letter draft both already work with no AI configured — that path exists and is tested).

---

## 7. What the web adds that the desktop can't

Worth building *because* it's a web app, not just porting:

- **A public resume URL** (`/r/jane-doe`) — a hosted, always-current resume page. This is the classic reason people pick a web resume builder, and the HTML exporter is already most of the way there.
- **Share-a-link for feedback** — send a read-only or comment-enabled link to a mentor.
- **Application tracking** — variants already carry `TargetRole` + `JobDescription`. A simple board of "which variant went to which company, and what happened" is a small step from there and is the kind of thing that makes the product sticky.
- **Template thumbnails rendered server-side** — the desktop gallery has to render live; the web can cache PNGs on a CDN.

---

## 8. Suggested phasing

**Phase W1 — Backend, no UI (de-risks everything).**
`ResumeBuilder.Api` project. Postgres + EF migrations + `UserId` + global query filters. Auth (start with a hosted identity provider — Auth0/Entra/Clerk — do not hand-roll). Endpoints: resume CRUD, render-preview, export, import. Prove it with integration tests and curl. **No UI at all yet.** At the end of this phase you can render any of the 18 templates over HTTP.

**Phase W2 — The editor SPA.**
React + TypeScript. Section editing, template gallery, debounced server preview, export/import, client-side undo. This is the bulk of the work and the only genuinely new code.

**Phase W3 — Smart features.**
Server-side AI proxy + rate limits, tailor-to-job with the accept/reject diff UI, cover letters, variants.

**Phase W4 — Web-native features.**
Public resume URLs, share links, application tracking.

**Phase W5 — Converge the desktop.**
Point the Avalonia app at the web API as an optional backend (it keeps its local SQLite for offline). At that point `LocalFolderSyncService` retires in favor of real sync, and the two apps share one account.

---

## 9. Risks, honestly

- **The UI rewrite is the whole cost.** Don't let "we can reuse 80% of the code" hide the fact that the 20% you can't reuse — the editor — is where nearly all the user-visible work lives.
- **Two renderers will drift.** If you take the HTML-preview shortcut in §3, budget for keeping it honest, or you'll reproduce the exact DOCX/HTML-vs-PDF divergence that this codebase just spent a cleanup fixing. A golden-image test per template, run in CI, is the cheap insurance.
- **QuestPDF licensing.** The Community license is free only under a revenue threshold. A commercial hosted product may need a paid license — check this *before* building a business on it, not after.
- **AI cost is unbounded by default.** See §6. Rate-limit before launch, not after the first bill.
- **Multi-tenancy leaks are catastrophic and quiet.** Global query filters + a test that asserts user A cannot read user B's resume, written in Phase W1 on day one.

---

## 10. The one-paragraph version

Keep Core, Templates, and Export exactly as they are — they're server-ready and they're the hard part. Swap SQLite for Postgres, add a `UserId` and a real migration history, and wrap the lot in an ASP.NET Core API. Write a new React SPA for the editor; that's the real cost, and it's unavoidable because Avalonia XAML doesn't port. Render previews server-side (debounced and cached) rather than building a second renderer. Move AI behind a server-side proxy with your key and rate limits, which forces a pricing decision earlier than you'd like. Drop spell-check and folder-sync — the browser and the server respectively make them redundant. Then build the things only a web app can do: public resume URLs and application tracking.

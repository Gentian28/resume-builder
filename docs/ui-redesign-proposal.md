# UI redesign proposal

> Written 2026-07-27, step 4 of `distribution-and-ux-plan.md`. Grounded in real screens
> captured from the running app — see `docs/ui-baseline/`. The app stays Avalonia; nothing
> here requires a framework change.

## What the screens actually show

I ran the published `win-x64` build, imported `samples/sample-resume.json`, and captured
every surface. Two findings reframe the problem before any of the detail matters.

**The PDF output is good.** `02-populated.png` — the Modern template renders clean type,
a real hierarchy, sensible spacing. The renderer is not what makes this app look dated.
Every problem below is in the *chrome around* the document. That is a much cheaper thing
to fix than it looked from the outside, and it means the 25 templates are an asset to
show off, not something to rebuild.

**The product hides its own value.** Not as an abstraction — measured from the screens:

| What it does | Where a user has to find it | Screen |
| --- | --- | --- |
| Local-LLM AI (the privacy pitch) | `Tools ▸ AI Assistant ▸ Show AI Panel` | `05` |
| Tailor to Job | `Tools ▸ Tailor to Job ▸ …` | `04` |
| LinkedIn import | `File ▸ Import ▸ LinkedIn Export (.zip)` | `07` |
| 25 templates | `Templates ▸ Choose Template…` | `08` |

Everything that distinguishes this from a Word doc is three levels deep behind the two
least descriptive words in the menu bar: *File* and *Tools*.

## The five concrete problems

### 1. The template gallery has no thumbnails — `03-template-gallery.png`

25 templates presented as a wall of text cards: a name, a two-line description, a category
tag. For a product whose entire output is visual, the user picks a look **blind**. This is
the single worst moment in the app and the largest gap between what the product is and what
it appears to be.

It is also the cheapest to fix: `ResumeBuilder.Export/PngExporter.cs` already renders any
template to a PNG. Rendering each template against the sample resume and caching the
result gives a real thumbnail grid with no new rendering code.

The modal also runs about 60% empty black space below the cards.

### 2. Nothing uses the window — `02-populated.png`

At 3458 px wide: the editor rail is pinned to ~320 px, the preview page to ~600 px, and
roughly **530 px of dead grey** sits between them. Widening the window adds only grey. The
editor column is so narrow that "Customize Appearance" (`09`) — accent swatches, two font
pickers and four sliders, a genuinely capable panel — is crushed into a 320 px strip with
6 pt labels.

Meanwhile the top bar splits `−  100%  +  Reset | Undo Redo` at the far left from
`Save  Export PDF` at the far right, ~2800 px apart.

### 3. There is no design system — every screen

`App.axaml` is `<FluentTheme />` plus two ad-hoc styles. The consequences are visible:
panels are flat near-black rectangles with no border, elevation or grouping; "Template",
"Customize Appearance", "Section Order" and "Personal Information" are peer cards despite
being wildly different in weight; section headings sit at the same size and weight as field
labels, so nothing establishes a reading order.

### 4. Placeholders are doing the job of labels — `01` vs `02`

Empty, the fields read "First Name", "Last Name", "Job Title". Filled, they read "Jane",
"Doe", "Software Engineer" — with no labels at all. The user cannot tell which box is
which without clearing it. This is also a screen-reader problem, which matters more once
the repo is public.

### 5. First run shows an empty form — `01-empty-state.png`

A new user gets blank inputs beside a blank white page. No sample to explore, no "import
your LinkedIn export", no template preview. The app's answer to "what is this?" is "type
something and find out."

Related: in `05`, *Generate Summary* and *Suggest Skills* render greyed out when no AI
endpoint is configured. CLAUDE.md's rule is that AI features **degrade, not gate** — and
in code they do — but greyed-out menu items communicate "broken", not "works offline".
The visual treatment contradicts the architecture.

## Proposed direction

### Layout: three zones that grow

```
┌──────────┬────────────────────────┬──────────────────────────┐
│ sections │  editor (flex)         │  live preview (flex)     │
│ (fixed)  │                        │                          │
│ Personal │  ┌──────────────────┐  │   ┌──────────────────┐   │
│ Summary  │  │ Label            │  │   │                  │   │
│ Work   ● │  │ [ input        ] │  │   │   rendered page  │   │
│ Skills   │  └──────────────────┘  │   │   fits width     │   │
│ ...      │                        │   └──────────────────┘   │
└──────────┴────────────────────────┴──────────────────────────┘
  Template ▾   Tailor to Job   AI Panel   Import ▾      Export ▾
```

Section list becomes navigation (it already exists as "Section Order"). Editor and preview
both flex, so a wide window yields a wider editor and a larger page instead of more grey.

### Promote the four buried features to a command bar

Template picker (showing the current template's thumbnail), Tailor to Job, AI Panel toggle,
Import. The menu bar stays for completeness — it stops being the only route.

### First-run: three choices, not a blank form

"Start from a template" (thumbnail grid), "Import" (LinkedIn / PDF / JSON), "Start blank".

### Show the AI state honestly

Replace greyed-out items with an always-enabled panel that states its mode: *"Running
locally via Ollama — nothing leaves your machine"* / *"Not configured — set up a local
model or an API key"*. That is the headline selling point; it should be legible, not
inferred from a disabled menu item.

## Sequencing

| # | Work | Impact | Effort |
| --- | --- | --- | --- |
| 1 | Template thumbnails via existing `PngExporter` | very high | low |
| 2 | Design tokens → `ResourceDictionary`; type scale, spacing, elevation | high | low–med |
| 3 | Flexible three-zone layout | high | medium |
| 4 | Command bar promoting the four features | very high | medium |
| 5 | Real labels above inputs | medium | low |
| 6 | First-run screen | high | medium |
| 7 | Split `MainWindow.axaml` (1,722 lines) into UserControls | enables 2–6 | medium |

7 is not cosmetic — there is currently no seam to iterate on. It is worth doing early
enough that 2–6 land in files that make sense, but it should follow the token work so the
extraction has something to extract *to*.

## Decided: Semi.Avalonia as the base theme

**Decided 2026-07-27.** Adopt **Semi.Avalonia** (MIT) as the base theme and layer our tokens
on top, rather than hand-building a token set on stock Fluent. It already ships the
elevation, control themes and type scale that items 2 and 5 would otherwise be hand-rolled,
which is most of the "no design system" problem solved by dependency instead of by code.

Consequences for the spec:

- The HTML spec defines **our** layer — colour/spacing/type tokens, and the components Semi
  does not have (template thumbnail card, command bar, first-run chooser, AI status strip).
  It should not re-specify buttons, inputs or scrollbars; Semi owns those.
- Tokens translate to a `ResourceDictionary` that overrides Semi's, not a from-scratch one.
- Add `Semi.Avalonia` to `Directory.Packages.props` at implementation time, not before —
  swapping the theme and restyling in one commit makes the diff unreadable.

## Decided: light and dark are peers

**Decided 2026-07-27.** No default theme — the app keeps following the OS, and both themes get
equal design effort. Every component is specced twice. This costs roughly double on foundations
and review, and it rules out the shortcut of designing dark and deriving light from it.

Building both properly surfaced three things a single-theme pass would have hidden. All ratios
below were computed, not eyeballed:

- **`text-3` failed WCAG in both themes** — 3.57 dark and 2.98 light against `surface-2`. It
  carries captions, the autosave stamp and result counts, which is real text. Revised to
  `#868D9B` (dark) / `#656C7A` (light), now 4.89 / 4.71.
- **Borders had to split into two tokens.** A decorative card border can be low-contrast, but an
  input's border *is* the affordance signalling it is editable, and that needs 3:1. The single
  `border` token managed 1.31–1.40. Added `border-input`: `#697080` / `#818A99`.
- **A dark-theme filled button cannot carry a white label.** This is arithmetic, not taste:
  white-on-accent ≥ 4.5 needs accent luminance ≤ 0.183, while accent-on-dark-panel ≥ 4.5 needs
  ≥ 0.229. No colour satisfies both. Dark theme therefore uses a *light* accent with a near-black
  label (`on-accent: #0F1115`); light theme is the reverse. Semi.Avalonia already does this
  correctly — the constraint is that our overrides must not break it.

**The UI accent is indigo, not blue.** Teal and cyan fail as light-theme fills
(white-on-`#0D9488` is 3.74). Every blue accessible enough to serve as a light fill lands within
1.19 contrast of the document default `#2563EB` — visually the same colour, which defeats the
UI-accent-vs-document-accent rule. Indigo (`#8B93F8` dark / `#4F46E5` light) passes every check
and sits 1.88 away, the widest separation among accessible candidates.

## Positioning: benchmark resume.io, don't clone it

Worth taking from them: light-first sensibility, thumbnail-led template selection, preview-centric
editing, non-technical tone. Worth *not* taking: the step-by-step wizard funnel, which exists to
march users toward a paywall at download. We have no paywall, so copying it would import funnel
design with no funnel.

The wedge is free export, no account, offline, private — precisely what resume.io users resent
about it. The honest limit: we can match them on the editor and beat them on price and privacy,
but not on distribution, where they win on SEO and brand. That is what `web-app-plan.md` is for;
the desktop app is the privacy-first flagship rather than the volume play.

The app does **not** inherit the portfolio's visual language (warm near-black + `#F6851B` orange +
Montserrat/JetBrains Mono). That is a personal brand, and it signals "developer tool" to an
audience that is mostly not developers. The portfolio's language *does* belong on the step-8
download page, which is a page on gentianshkembi.com rather than part of the app.

## Spec status

Eight cards in the **ResumeBuilder Design System** project on claude.ai/design. Source lives in
`docs/design/` so it is version-controlled and readable by whoever implements it.

Twelve cards, every one specced in both themes.

| Group | Card | Covers |
| --- | --- | --- |
| Foundations | Color tokens | Light + dark ramps, WCAG-verified, indigo UI accent |
| Foundations | Type scale & spacing | 8 type tokens, 4px base, radius + elevation |
| Components | Template card | Default / hover / selected, thumbnail sourcing |
| Components | Command bar | Surfaces the four buried features |
| Components | AI status strip | Local / cloud / unconfigured |
| Components | Entry editor | Repeatable entries, per-bullet achievements |
| Components | Tailor to Job panel | Offline ATS match + optional rewrite diffs |
| Components | Appearance panel | Document accent, typeface, metrics |
| Screens | Template gallery | 6-col thumbnail grid |
| Screens | First run | Three routes in |
| Screens | Main editor | Three-zone flexible layout |
| Screens | Cover letters | Parallel surface, linked to its résumé |

### Constraints the spec encodes from the codebase

These are places where a reasonable-looking UI choice would silently break behaviour:

- **One control per achievement bullet.** `JobTailoringService` addresses achievements by index
  through `AchievementLines`. A single multi-line textarea makes the index depend on how the user
  wrapped their text, so accepted AI rewrites land on the wrong bullet. Every tailoring diff also
  names its target ("Northwind Systems · achievement 1") so an off-by-one is visible.
- **Status derives from the configured base URL, not `IsConfigured`.** Loopback → local, other host
  → cloud, unset → unconfigured. Using `IsConfigured` as a gate is what produced today's greyed-out
  menu items.
- **Cover-letter export is a separate registry.** `IExporter` is typed to `Resume`, so the
  identical-looking command bar on the cover-letter screen binds to `CoverLetterExportService`.
- **"Reset to template defaults", not "Reset to defaults"** — `ApplyTemplateDefaults` restores the
  *template's* accent and font, gated on `IsAccentColorCustomized` / `IsFontCustomized`.

### Open questions resolved

**Editor column count** — both, by field length. Short fields (title, company, location, dates)
keep the two-column grid; anything multi-line (description, achievements) spans full width. A
blanket single column wastes width on a flexible layout; a blanket two-column squeezes prose into
half-width boxes.

### Still open

**Gallery selection** — apply immediately with live preview behind the modal, or only on confirm?
Immediate suits the app's character, but re-rendering the PDF on every arrow-key move through 25
items may be too slow. Measure before deciding; it is the only question left that needs data
rather than an opinion.

## Implementation progress

**Step 1 done — theme foundation.** Build clean at 0 warnings, 323/323 tests, app verified running.

- `Semi.Avalonia` + `Semi.Avalonia.ColorPicker` added; `Avalonia.Themes.Fluent` removed.
- Avalonia bumped 11.3.10 → **11.3.14**. Not optional: Semi 11.3.14 requires it, and Semi's
  control themes target specific Avalonia control templates, so matching versions is safer than
  pinning Semi back to 11.3.7. Patch-level bump within the same minor.
- `Styles/Tokens.axaml` holds the token layer — light and dark theme dictionaries with the
  approved colour ramps, plus theme-independent type, spacing and radius scales. `Rb*` prefix so
  nothing can collide with a Semi or Avalonia key.

**The theme swap broke 52 resource lookups, which is worth recording.** `MainWindow.axaml` bound
21 backgrounds to `SystemControlBackgroundAltHighBrush`, 19 to `SystemControlBackgroundBaseLowBrush`
and 12 more to other Fluent `SystemControl*` keys. Semi does not define those, so every card panel
silently lost its background and the editor rail rendered flat. All 52 now map onto `Rb*` tokens;
the app no longer depends on Fluent at all.

### Overriding Semi's accent — the supported seam

Semi's palette (`SemiColorPrimary` in `Tokens/Palette/{Light,Dark}.axaml`) is built with
`StaticResource`, so it **cannot** be retargeted from outside — a merged dictionary redefining
`SemiColorPrimary` has no effect on the already-resolved aliases. `SemiTheme` also exposes no
`AccentColor` property.

The seam is the *alias* layer in `Themes/{Light,Dark}/Button.axaml`
(`ButtonSolidPrimaryBackground`, `ButtonDefaultPrimaryForeground`, …), which the control themes
consume via `DynamicResource`. Those are overridden in `Tokens.axaml`.

That layer also turns out to be the only place the light/dark accent inversion is expressible:
`ButtonDefaultPrimaryForeground` is the accent used as **text** and must be light on a dark panel
(`#8B93F8`, 6.45), while `ButtonSolid*` is the accent used as **fill** and flips to a near-black
label (`#0F1115` on `#8B93F8`, 6.86). A single accent token could not satisfy both — the same
arithmetic recorded above.

Verified visually: chrome now renders indigo while the preview page keeps the blue `#2563EB`
document accent, so the UI-accent-vs-document-accent separation holds in the running app.

### Implemented so far

| Item | State |
| --- | --- |
| Design tokens on Semi.Avalonia | Done |
| Template thumbnails | Done — lazy, disk-cached, 25 files / 1.5 MB |
| Three-zone flexible layout | Done — 196px nav, editor 1\*, preview 1.15\* |
| Command bar | Done — template/Tailor/AI/Import promoted out of the menus |
| Persistent field labels | Done — every content section |
| Split `MainWindow.axaml` | Done — 1,900 → 347 lines |
| Split `EditorView.axaml` | Done — 1,274 → 35 lines, 16 section controls |
| First-run screen | Done — three routes, shown only on an empty database |
| Per-bullet achievements | Done — see below |

### How the labels got unblocked

The obstacle was layout, not effort. In the repeatable entries a text box shared a grid row with
*Move up / Move down / Remove*, so stacking a label above it knocked the buttons out of alignment.
Splitting the editor per section made this solvable one section at a time: the buttons moved into
their own header row, which also gained the entry's title so a long list of roles stays readable
while scrolling.

Watermarks deliberately survive on the AI, Sync, Tailor and Appearance panels, where the text is
a hint rather than a name — `Base URL (e.g. https://api.openai.com/v1)` is worth keeping inside
the box. All of them carry `AutomationProperties.Name`, so nothing is unlabelled to a screen
reader.

### Per-bullet achievements — and a correction

An earlier draft of this doc claimed the single-textarea design was a *correctness* risk, on the
grounds that an accepted AI rewrite could land on the wrong bullet. **That was wrong.**
`AchievementLines.ReplaceAt` counts non-blank lines using exactly the rule `Parse` uses, which is
what the class was extracted to guarantee, and `ReplaceAt_TargetsTheSameEntryParseWouldProduce`
already pins it. The old design was consistent.

Per-bullet editing is a UX improvement: it makes the thing tailoring edits visible as a thing you
can see and reorder. The text stays the single source of truth and the bullets are a projection of
it, so `AchievementLines` remains the only authority on what an index means.

Driving the real UI found two bugs that reasoning about the code had not:

- A bullet added by *Add achievement* has no line in the text yet, so `ReplaceAt` had nothing to
  target and silently dropped the first thing typed into it. New bullets now commit by rebuilding
  the text; only existing ones take the `ReplaceAt` path.
- Repeated *Add* stacked blank rows that all vanished on reload, because `Parse` drops them.

### Next

Nothing in the original spec is outstanding. Candidates from here:

- **Collapse all but the entry being edited.** With several roles the Experience section is a long
  scroll; `docs/design/components/entry-editor.html` shows collapsed summary rows.
- **The AI status strip's three states** — the command-bar dot distinguishes local from everything
  else, but the panel itself does not yet spell out cloud vs unconfigured.
- **Cover letters** still use the old overlay rather than the three-zone surface.

### A note on verifying UI work

Screenshot-and-click automation gave a false negative during the split: clicks computed from
`GetWindowRect` land ~38px off, because the window rect includes the title bar and resize border
while the rendered content starts at the *client* origin. The app looked completely
unresponsive when it was fine.

Two things that actually work: send a bound keyboard shortcut (`Ctrl+T` toggles the theme and
shows in the status bar) to prove input reaches the app at all, and use `ClientToScreen` rather
than `GetWindowRect` for click coordinates. For structural refactors, diffing every `{Binding}`
before and after is far stronger evidence than any screenshot.

## Process: design in Claude Design before any code

Per `distribution-and-ux-plan.md`, the visual spec is built and approved **first**, as HTML,
and only then translated to Avalonia. The spec lives in a Claude Design project so it can be
reviewed in the browser rather than by reading markdown. See the "How to follow along"
section there.

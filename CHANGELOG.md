# Changelog

Every released version and what changed in it, written for the person reading it rather than as a
list of commits. This file *is* the GitHub release notes: the release workflow extracts the
section matching the tag and fails the build if there isn't one.

## 1.3.0

### Fixed

- Native JSON backups import through the correct format detector, and successful first-run imports reveal the editor immediately.
- Plain-text export works from the toolbar. Multi-page PNG export opens a file that exists and protects existing numbered page files.
- Preparing a preview, thumbnail or export no longer changes another document's settings or visible sections.
- Switching AI endpoints clears the old credential. AI results and accepted rewrites cannot overwrite another document or newer manual text.
- Job tailoring branches from a base without overwriting it, including when autosave runs before Save as variant.
- Deleting the open resume or letter no longer recreates it through autosave. Save, close, reload and update transitions preserve edits made during a write.
- Sync folders keep separate histories, variants resolve their parent on the receiving computer, and deleted archives stay out of the active remote list.
- Applications preserve dates, retain failed edits for retry, and capture the sent content for newly tracked or newly sent applications. Historical records without a snapshot keep their existing live link.
- Letter drafting uses the letter's own resume and posting. Save, export and undo target the active document, with separate letter undo history and macOS Command shortcuts.
- Creating or resetting a cover letter works in time zones with a nonzero UTC offset.
- Changing an application status keeps the active dropdown stable instead of crashing during selection.
- Word import retains names, complete job titles and October dates. Export review displays readable lines, and GitHub usernames no longer produce a false warning.
- A fresh Linux profile stores its workspace in the configured data folder even when that folder has not been created yet.
- Windows workspaces move safely outside the installer folder, preserving committed database writes and settings. Uninstalling and reinstalling no longer removes saved work.

### Added

- A bundled synthetic example and persistent import review beside the populated editor, plus bounded local Word (.docx) import with original body text retained for review.
- Export readiness with rendered page count, contact/date/small-text checks and extracted PDF text.
- Resume search with base/variant grouping, application search and active/archive/due filters, editable dates and contacts, CSV export and all-day follow-up calendar export.
- Linked letters and immutable generated PDFs captured when an application is sent, with actions to save the original captured PDFs later.
- Letter template, sender, source, target-role and posting controls, with explicit and undoable source replacement.
- A4/US Letter paper and English/Albanian generated document headings and dates, isolated per export.
- Local support diagnostics containing software details and fixed error codes only, plus a usability-session kit and distribution-link audit.
- Headless layout, theme and Escape/focus tests; native unsigned installer packaging is now rehearsed in CI.
- Release gates exercise native Windows, Linux, Apple silicon and Intel Mac installers and installed upgrades with synthetic data and verified safety backups.

- Complete workspace backup and restore through the JSON backup/import flow. Restore validates the archive, keeps a safety database copy and pauses sync.
- Persistent non-secret preferences for models, sync and the last open resume. API keys remain session-only.
- A separate app workflow test suite, contributor instructions, and privacy-conscious issue forms.

### Updated

- QuestPDF 2026.7.2 fixes concurrent Windows x64 exports corrupting embedded text mappings. A parallel export regression verifies extractable content.
- The app, tests and build workflows use .NET 10. Self-contained builds pin runtime 10.0.13, and SQLite persistence uses EF Core 10.0.13.

### Other fixes since 1.2.3

- **An edit typed while a save was writing is no longer marked as saved.** The save cleared the
  unsaved flag when it finished, even for changes made during the write, so the status line said
  "All changes saved on this computer" about text that existed only in memory. Manual save now waits for newer edits to be written too; autosave keeps newer edits pending for its next pass.
- **Closing the window and applying an update both check the cover letter too.** A cover letter
  with unsaved changes was lost when the window closed while the resume was clean, and an update
  restarted the app even when the pre-restart save had failed.
- **A database that cannot be opened now shows a window instead of nothing.** The app used to exit
  silently on a corrupt or truncated database. It now names the file and lists the backups next to
  it so you can go back to one.
- **A photo that is not an image no longer breaks every render.** A JSON import could carry bytes
  that were not a picture; the templates now fall back to initials, and the import says the photo
  was left out. DOCX export also strips the control characters a paste from a PDF can carry, which
  used to produce a file Word refused to open.
- **JSON Resume files validate and read back whole.** An ongoing role used to export as
  `"endDate": "Present"`, which the schema rejects; it now omits the end date the way the schema
  expects and keeps the ongoing flag under `meta.resumeBuilder`, alongside any custom section the
  schema has no field for, so a file this app wrote imports without losing them. Year-only and
  year-month dates from other tools now import instead of being dropped.
- **Folder sync can be trusted with a shared folder.** Files are written whole and then swapped
  in, so a cloud client never uploads a half-written résumé. A file exported before sync ids
  existed is adopted once instead of imported again on every sync. A "conflicted copy" the cloud
  client wrote no longer silently replaces the real file. And deleting a résumé on one machine
  stays deleted: the file is parked as `.deleted.json` rather than coming back, and a deletion on
  the other machine is respected here until you edit the résumé again.
- **The Compact template prints project highlights.** It was the one template that dropped a piece
  of content; every template is now held to the same content test.
- **A pre-upgrade backup that fails is reported.** The upgrade still goes ahead, and the app says
  so the moment its window opens, with the path to copy while it is still intact.

## 1.2.3

A polish pass over the editor and the printed page.

### Changed

- **Real icons.** Every reorder, remove, close and collapse control now uses a proper vector
  icon at one consistent size and spacing, instead of the mix of "^", "v", "×" and "..."
  characters that rendered differently row to row.
- **Section headers can't be orphaned in the PDF anymore.** A section title now brings the
  start of its content with it or moves to the next page whole, and an entry title always
  keeps a couple of lines attached, so "Projects" is never alone at the bottom of a page.
- **Appearance and Section order are open by default** instead of hiding behind a "..." button.
- **Navigating closes tool panels.** Opening a section from the left nav dismisses the AI
  Assistant / Tailor / Sync panel instead of stacking your section underneath it.

### Fixed

- **Importing shows its progress.** The loading overlay now reliably appears the moment an
  import starts and stays until the résumé is fully loaded into the editor. Previously a
  quick parse followed by the editor filling in could freeze the window with no feedback.
- The first-run screen's privacy note wraps inside its card instead of running off the edge.

## 1.2.2

Always knowing where your work is.

### Added

- **After every export, a toast offers Open and Show in folder.** Producing a PDF and finding
  it are now one flow, browser-download style. Works for cover letters too.
- **"Export as PDF..." sits directly in the File menu** with Ctrl+E, since producing a PDF is
  the point of the app.

### Changed

- **The save indicator says "All changes saved on this computer"** instead of a timestamp. It
  answers "did it save, and where?" at a glance.
- **"Save As..." is now "Duplicate...".** It makes a copy inside the app, and has never asked
  for a file location, so the Word phrase promised the wrong thing. Files come from Export.
- **The first-run screen states the storage model in one breath:** résumés save automatically
  inside the app, export a PDF when you need a file to send, turn on sync to have them on your
  other machines.

## 1.2.1

The rough edges a real import session showed up.

### Fixed

- **Importing and exporting now show progress.** The work used to run on the UI thread, so the
  loading overlay never appeared: a slow PDF import just froze the window with no sign anything
  was happening. The overlay now shows, animates, and says what it is doing ("Importing PDF...",
  "Exporting DOCX...").
- **The Skills editor no longer draws each row's name and category on top of each other.** A
  layout bug that made every imported skill list look corrupted.
- **Spell check knows industry vocabulary.** Kubernetes, SaaS, microservices, DevOps, "agentic",
  "architecting" and some 180 other terms a résumé routinely contains are no longer flagged, and
  no longer "corrected" to the nearest dictionary word (Kubernetes → "Rubbernecks").

### Changed

- **Buttons use real glyphs.** Move up/down is ↑/↓ instead of the letters "^" and "v"; every
  remove/close button is a proper × instead of the letter X.
- **One standard separator.** Every template's contact line uses the same "  |  " pipe; the
  Starter template's links line matches its contact line in size and colour instead of rendering
  its pipes smaller and fainter.
- Row controls line up with their text boxes, and the spelling-issues badge lines up with its
  Check button.

## 1.2.0

Keeping track of where your résumés actually went.

### Added

- **Applications.** Track which résumé you sent to which company, when, and what happened. The
  point is the moment weeks later when they call: one click opens the exact version they read,
  with the bullets you rewrote for them. Anything waiting on a reply for more than a fortnight is
  marked *worth chasing*, and the header tells you what needs attention: "14 tracked · 3 waiting ·
  2 silent 2+ weeks".
- **One-click cloud sync.** The sync panel now finds your Google Drive, OneDrive, Dropbox or
  iCloud folder and offers it as a button, instead of asking you to type a path. Your résumés live
  in the cloud folder you already have; no account, no upload to us.
- **The first-run screen says what the app does** (tailoring, ATS keyword checking, cover letters)
  rather than only how to start a document.

### Changed

- **Experience entries fold up.** Each collapses to its job title, company and dates, so a
  six-role history is a list you can scan rather than a page you scroll.
- **The cover letter editor uses the whole window** instead of a dimmed dialog, so the preview
  stays readable while you write.

### Fixed

- **Your résumés are backed up before any upgrade that changes the database.** The last three
  copies are kept. Nothing you have written should ever depend on an upgrade going perfectly.

## 1.1.1

### Fixed

- **The app has its own icon.** Every install so far showed the default Avalonia template logo in
  the Start menu, the taskbar, the title bar, and next to the uninstaller. It was never replaced
  when the project was created.

### Added

- The Windows installer now shows the app's name and mark while it works, instead of a bare
  progress bar.

## 1.1.0

The editor redesign, plus a second AI provider.

### Added

- **First-run screen.** A new install used to open on an empty form. It now offers the three ways
  in (start from one of the 25 templates, import a LinkedIn export / PDF / JSON Resume, or start
  blank) and says once, in the place where it matters, that everything stays on your computer.
- **Anthropic as an AI provider,** alongside OpenAI and any OpenAI-compatible server including a
  local LLM. Each provider keeps its own key and model, so switching between them doesn't make you
  re-enter settings, and one provider's key is never sent to another.
- **Real previews in the template gallery.** Templates were a list of names; each is now a rendered
  page of the design, so you can see what you're picking. Thumbnails render once and are cached.

### Changed

- **Three-zone editor layout**: navigation, editor, and live preview, replacing the fixed rail.
  A command bar carries the actions you reach for most.
- **Every field has a visible label,** not just placeholder text that vanishes once you type. All
  fields also carry accessible names for screen readers.
- **Achievements are edited one bullet at a time,** with reorder and remove per bullet, instead of
  one text box holding all of them.
- **The AI panel says which of three states it is in** (running locally, using a cloud provider,
  or not configured) rather than leaving you to infer it from a base URL. Only a local endpoint
  claims "nothing leaves your machine", because only there is it true.

### Fixed

- Typing into a newly added achievement no longer discards the first thing you type.
- Repeatedly clicking *Add achievement* no longer stacks blank rows that vanish on reload.

### Note

Keyword analysis and ATS scoring still run entirely on your machine and need no AI provider at all.

---

## 1.0.3

Window sizing fix: the app could open larger than the screen on scaled displays, putting the title
bar out of reach.

## 1.0.2

First winget release.

## 1.0.1

Packaging fixes.

## 1.0.0

Initial release.

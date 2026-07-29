# Changelog

Every released version and what changed in it, written for the person reading it rather than as a
list of commits. This file *is* the GitHub release notes — the release workflow extracts the
section matching the tag and fails the build if there isn't one.


## 1.1.1

### Fixed

- **The app has its own icon.** Every install so far showed the default Avalonia template logo in
  the Start menu, the taskbar, the title bar, and next to the uninstaller — it was never replaced
  when the project was created.

### Added

- The Windows installer now shows the app's name and mark while it works, instead of a bare
  progress bar.

## 1.1.0

The editor redesign, plus a second AI provider.

### Added

- **First-run screen.** A new install used to open on an empty form. It now offers the three ways
  in — start from one of the 25 templates, import a LinkedIn export / PDF / JSON Resume, or start
  blank — and says once, in the place where it matters, that everything stays on your computer.
- **Anthropic as an AI provider,** alongside OpenAI and any OpenAI-compatible server including a
  local LLM. Each provider keeps its own key and model, so switching between them doesn't make you
  re-enter settings, and one provider's key is never sent to another.
- **Real previews in the template gallery.** Templates were a list of names; each is now a rendered
  page of the design, so you can see what you're picking. Thumbnails render once and are cached.

### Changed

- **Three-zone editor layout** — navigation, editor, and live preview — replacing the fixed rail.
  A command bar carries the actions you reach for most.
- **Every field has a visible label,** not just placeholder text that vanishes once you type. All
  fields also carry accessible names for screen readers.
- **Achievements are edited one bullet at a time,** with reorder and remove per bullet, instead of
  one text box holding all of them.
- **The AI panel says which of three states it is in** — running locally, using a cloud provider,
  or not configured — rather than leaving you to infer it from a base URL. Only a local endpoint
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

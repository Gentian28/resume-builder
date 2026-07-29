# Roadmap

What this is: an opinionated order of work, with the reasoning. Not a backlog — things are listed
because they follow from what already exists, or because leaving them out would be a mistake.

Written 2026-07-29, against v1.1.1.

---

## Two things already exist that this roadmap does not need to add

**Cloud storage already works.** `LocalFolderSyncService` syncs résumés to a folder, and its own
summary says it: *"can be a cloud-synced folder like Dropbox, OneDrive, or Google Drive. This
provides immediate sync functionality without OAuth complexity."* Point the sync folder at your
Drive folder and your résumés are on Drive, on every machine, with conflict handling
(`NewestWins` preserves the loser as a `.conflict.json` rather than discarding it).

So the Drive question is not *can we* but *does anyone know*. That is a discoverability problem, and
it is item 1 below — not a storage-integration project.

**A native Google Drive API integration would be worse, not better**, for this product: it means
OAuth, a Google verification review for a sensitive scope, a client secret that can't live in a
desktop binary, and a token to store — all to reach a folder the OS already mounts. It also breaks
the claim the product is built on, since the app would then hold a credential to your Drive. The
folder approach gets the same outcome and keeps the app's only network dependency optional.

**The web version is already designed.** `docs/web-app-plan.md` is a real plan — architecture,
five phases, and an honest risk list. It does not need rewriting. It needs a *decision*, which is
§4 below.

---

## 1. Make what exists discoverable — next

The app has features nobody will find. Sync is behind a panel toggle; variants and tailoring are
in menus; the achievement-level AI rewrite is invisible until you own an API key. Every one of
these is built, tested, and shipping — and a first-run user meets none of them.

This is the cheapest possible work with the largest effect, because the alternative reading is
that the app is a form with a PDF button.

- **Say that folder sync means cloud sync.** The sync panel should name Drive, OneDrive and
  Dropbox explicitly, and offer a folder picker that starts in the user's Drive folder if one
  exists. One sentence and a default path turn an obscure feature into the answer to "where are
  my résumés?"
- **Surface variants.** Tailoring a résumé per application is the product's real value and it is
  currently a menu item.
- **A "what can this do" pass on the first-run screen** — the three routes in are good, but the
  screen never mentions tailoring, ATS scoring, or cover letters.

## 2. Close the application loop — the highest-value new feature

The data model already carries `TargetRole` and `JobDescription` on variants. That is most of an
application tracker without knowing it.

A simple board — which variant went to which company, when, and what happened — is a small step
from what exists, and it is the thing that turns a résumé builder from a tool you use twice into
one you open weekly. `docs/web-app-plan.md` §7 spotted this too, but filed it as web-only. It is
not: it needs no server, and shipping it on the desktop first is a way to learn whether people want
it before paying for infrastructure to host it.

Rough shape: a `JobApplication` entity (company, role, variant, applied date, status, notes,
optional link), a board view, and a status field with maybe five states. The export layer already
knows how to write JSON, so "export my applications" is nearly free.

## 3. Finish the polish already specced

Small, known, and specced in `docs/ui-redesign-proposal.md`:

- Collapse all but the entry being edited — Experience gets long with several roles.
- Move cover letters onto the three-zone surface; they still use the old overlay.
- Decide whether API keys persist. They are memory-only today and the privacy notice promises
  exactly that, so re-entering a key every launch is the honest cost of that promise. If it
  changes, it should be opt-in and use the OS credential store — never a config file.

## 4. The web version — a decision, not a plan

`docs/web-app-plan.md` covers the how. What it cannot decide is whether, and that turns on a
tension worth naming plainly:

**The desktop app's entire differentiator is that it is local, free, and account-free.** Every
competitor is a subscription web app that holds your employment history. A web version competes
with them on their terms and gives up the one claim that makes this product distinctive.

The plan itself flags what a web version forces:

- **AI cost becomes yours.** A user's own key cannot live in a browser, so calls proxy through your
  server on your key. Tailoring is 10+ calls per run. That forces a pricing decision before the
  product has users.
- **QuestPDF licensing.** Free under a revenue threshold; a commercial hosted product may need a
  paid licence. Check before building on it.
- **Multi-tenancy leaks are catastrophic and quiet.**

**Recommendation: don't build it yet.** Not never — the reusable 80% is genuinely reusable and the
plan is sound. But build §2 on the desktop first. If the application loop is what makes people
return, that is worth knowing before standing up Postgres, auth, and a bill. And if the web version
does get built, the reason should be a feature only the web can deliver — a public résumé URL, a
share-for-feedback link — not "because everyone else is a web app."

The one piece worth doing early regardless: **keep Core, Templates and Export free of desktop
dependencies.** That discipline is what keeps the option open, and it costs nothing today. It is
already a rule in `CLAUDE.md`.

## 5. Distribution and trust — in flight

Tracked in `docs/manual-steps.md`, not repeated here. The short version: SignPath code-signing is
the single highest-impact item outstanding, because the Windows "unknown publisher" warning is the
biggest thing between a stranger and a running app.

---

## Not planned, and why

- **A native Google Drive / Dropbox API integration.** See above — worse than the folder it would
  replace.
- **An account system on the desktop.** It would cost the product its main claim and buy nothing
  the folder sync doesn't already provide.
- **More templates.** 25 is past the point of diminishing returns; the gallery is already the
  hardest screen to scan. Better templates, or better defaults, beat more.
- **Mobile.** Résumé editing on a phone is a bad experience regardless of who builds it. A
  read-only public résumé URL — a web feature — covers the real mobile need.

---

## How this doc stays honest

Update it when something ships or a decision changes, in the same commit. A roadmap that is not
edited when reality moves is worse than none, because it is quoted with confidence.

Shipped work belongs in `CHANGELOG.md`, which is also the source of the GitHub release notes — the
release workflow extracts the version's section and fails the build if it is missing.

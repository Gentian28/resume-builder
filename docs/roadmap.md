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

**The problem, concretely.** The menu has 30+ items, and the most valuable thing the app does is
three levels down:

```
Tools → Tailor to Job → Save as Variant for This Job
Tools → Sync         → Show Sync Panel
```

*Correction: tailoring itself is not buried — the three-zone redesign put "Tailor to Job" in the
command bar. What is buried is saving the result as a variant, and sync.*

So the realistic first session is: install, pick a template, fill in the form, export a PDF, done.
That user may notice the tailor button, but never learns they can keep a tailored version per
application or put their résumés on Drive — and the first-run screen, the one place with their
full attention, says nothing about what the app does beyond starting a document.

None of this needs new features. It needs three existing ones surfaced at the moment they are
relevant rather than filed under Tools:

- **Exporting a PDF is the moment someone is applying to something.** That is where "tailor this
  to the job description?" belongs.
- **Saving a second résumé is the moment "where do these live?" becomes a real question.** That is
  where sync belongs — and it should name Drive, OneDrive and Dropbox outright, defaulting the
  folder picker to a Drive folder if one exists. One sentence turns an obscure panel into the
  answer.
- **The first-run screen never mentions tailoring, ATS scoring, or cover letters.** The three
  routes in are right; the pitch is missing.

## 2. Close the application loop — the highest-value new feature

**Where you end up today** after applying to ten jobs the intended way:

```
My Resume
  └─ Senior Backend Engineer     ← variant: stores TargetRole + JobDescription
  └─ Platform Engineer           ← variant
  └─ Senior Backend Engineer     ← ...which company was this one?
```

A flat list, some entries identically named, no way to tell them apart or see what happened. The
tailoring work is done and stored — it just isn't organised into anything actionable.

**What is missing is three fields:** company, date applied, status. With those:

> *"14 applications. 3 waiting on me. That one has been silent three weeks. And when Stripe calls
> on Thursday, this is the exact CV they read, with the bullets I rewrote for them."*

That last clause is the actual feature. **When the interview call comes, you need to know precisely
what you claimed to that company** — and the app already holds it.

`docs/web-app-plan.md` §7 spotted this too but filed it as web-only. It is not: it needs no server,
and shipping it on the desktop first is how you learn whether it is what makes people return,
before paying for infrastructure to host it.

Rough shape: a `JobApplication` entity (company, role, variant, applied date, status, notes,
optional link), a board view, five-ish statuses. The export layer already writes JSON, so "export
my applications" is nearly free.

## 3. Finish the polish already specced — done, except one decision

- ~~Collapse entries~~ — done. Each folds to a chevron, headline and dates.
- ~~Cover letters off the modal treatment~~ — done. Full surface; kept its own header, since a
  letter has one section and genuinely different actions.
- **API key persistence — still open, and it is yours to call.**

### The API key decision

Keys are held in memory for the session only, and the AI panel says exactly that. With two
providers, re-entering a key every launch is real friction — but the fix changes a promise the
product makes, so it should not be made quietly.

Three options:

1. **Leave it.** The promise stays absolute and needs no caveat. Cost: friction every launch,
   which is worst for exactly the users who use AI most.
2. **Opt-in, OS credential store.** A "remember this key" checkbox writing to Windows DPAPI /
   macOS Keychain / libsecret. Honest, and off by default — but that is three platform
   integrations, and a half-done version that silently falls back to a plain file on Linux would
   be worse than not offering it, because the notice would then be lying on that platform.
3. **Opt-in, Windows only.** Ship where the credential store is a one-liner, hide the option
   elsewhere. Pragmatic, at the cost of an inconsistent product.

**Recommendation: (1) until someone asks.** The friction is real but the promise is the product's
main differentiator, and no user has yet said the current behaviour bothers them. If it does come
up, (2) properly — not (3).

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

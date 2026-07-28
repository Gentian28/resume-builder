# Making it downloadable from gentianshkembi.com

> Written 2026-07-28, after v1.0.0 shipped. Verified against the portfolio repo as it stands
> today — `frontend/src/data/lab.ts`, `frontend/src/pages/LabPage.tsx`,
> `backend/src/Portfolio.Domain/Entities/Tool.cs` and `admin/src/pages/ToolsPage.tsx`.

**The portfolio already has everything needed. This takes one admin form and no deploy.**

The Lab page has a bench item for the Résumé Builder that is already wired to a tool slug:

```ts
// frontend/src/data/lab.ts
{ n: 'Résumé Builder', s: 'dev', stKey: 'lab.st.bench',
  meta: 'desktop · .NET + Avalonia', dKey: 'lab.d.resume',
  toolSlug: 'resume-builder-windows' },
```

`LabPage.tsx` resolves that slug against the published-tools endpoint and, if it finds a tool with
a working link, overrides the card's status:

```tsx
// Once a linked tool is published with a working link, the item is out in the world
// whatever the bench list says, so the status follows the dashboard, not the code.
state: link ? 'live' : m.s,
```

So publishing the tool flips the card from *bench* to *live* and gives it a **Download ↓** button
on its own. No code change, no deploy, no rebuild.

## Step by step — the whole thing

1. **Open the admin dashboard** and go to **Tools** (route `/tools`). The exact host is in
   `infrastructure/docs/current-setup.md`; it needs Tailscale to be up.

2. **Create a new tool** with exactly these values. The slug is the only one that must match
   character-for-character — it is what the bench item claims.

   | Field | Value |
   | --- | --- |
   | Name | `Resume Builder` |
   | Slug | `resume-builder-windows` |
   | Kind | `WindowsDownload` |
   | Download URL | `https://github.com/Gentian28/resume-builder/releases/latest/download/ResumeBuilder-win-Setup.exe` |
   | Version | `1.0.0` |
   | Platform | `Windows (x64)` |
   | URL | *(leave empty — only used by HostedApp and ExternalLink)* |
   | Description | *(see note below — it will not be shown)* |
   | Sort order | any |

3. **Tick Published** and save.

4. **Check the Lab page.** The Résumé Builder card should now read *live* and show
   **Download ↓**. If it says *Published but has no link*, the Kind is wrong — a
   `WindowsDownload` reads `downloadUrl`, everything else reads `url`.

That is the entire required path.

### Why that Download URL never needs changing

`/releases/latest/download/<asset>` redirects to that asset in whatever the current release is.
Tag `v1.1.0` tomorrow and the same URL serves the new installer. Verified returning 200 for all
five published assets. **Do not** paste the `/releases/tag/v1.0.0/...` form — that pins the site
to this version forever.

### Two gotchas found while reading the code

- **The admin form's placeholder is stale.** It shows
  `https://github.com/Gentian28/resumebuilder/releases/latest/download/...` — the *old private*
  repo, which is a dead URL. Worth correcting in `admin/src/pages/ToolsPage.tsx`, since following
  the hint produces a broken download.
- **The Description you type will not appear** on this card. `LabPage.tsx` keeps the hand-written
  bench copy for claimed items (`t(m.dKey)`) and only uses the DB description for standalone
  tools. Change the copy in the `lab.d.resume` i18n key instead.

## Optional: offer Linux and macOS too

Step 2 covers Windows only. Any published tool that no bench item claims renders as its own card,
so adding these creates two extra cards on the Lab page:

| Slug | Platform | Download URL |
| --- | --- | --- |
| `resume-builder-linux` | `Linux (x64)` | `.../releases/latest/download/ResumeBuilder.AppImage` |
| `resume-builder-macos` | `macOS (Apple silicon)` | `.../releases/latest/download/ResumeBuilder-osx-Setup.pkg` |

Both still use Kind `WindowsDownload` — the enum name is wrong for them, but it is the only kind
that renders a download button, and the `Platform` field carries the truth. Their Description
*does* show, since no bench item claims them.

If three cards for one app feels cluttered, the alternative is a single `ExternalLink` tool
pointing at a proper download page — see below.

## Optional: a real download page

`docs/download-page/index.html` in this repo is a complete, working page in the portfolio's own
design language (warm near-black, `#f6851b` orange, Montserrat / JetBrains Mono). It is a
**design reference, not a drop-in** — the portfolio frontend is React 19 + Vite + React Router +
Tailwind 4, so it needs porting rather than copying.

To ship it:

1. Add `frontend/src/pages/ResumeBuilderPage.tsx`, porting the markup to Tailwind classes.
2. Register the route in `frontend/src/App.tsx` alongside the existing ones:
   ```tsx
   <Route path="/resume-builder" element={<ResumeBuilderPage />} />
   ```
3. Point the Lab card at it by giving the bench item a curated `url` in `lab.ts` — a curated
   `url` takes precedence over the tool link (`href: m.url ?? link?.href`), so this cannot be
   broken by editing the tool later.
4. Push to `main`; Coolify auto-deploys.

Worth waiting for the UI redesign before doing this, since the page wants a real screenshot and
the current UI is the one being replaced. The CSS mockup in the file is a placeholder for exactly
that reason.

## Cosmetic, whenever

`lab.ts` has a `BUILD_LOG` whose newest résumé-builder line is stale:

```ts
['2026-06', 'resume-builder', '126 tests green, win-x64 publish'],
```

It is now 323 tests and a three-platform release, so a `['2026-07', 'resume-builder', 'v1.0.0 —
installers for win/linux/macos']` entry would be accurate. Needs a deploy, unlike everything in
the required path.

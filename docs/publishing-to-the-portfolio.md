# Making it downloadable from gentianshkembi.com

> Written 2026-07-28. Verified against the portfolio repo as it stands — `frontend/src/data/lab.ts`,
> `frontend/src/pages/LabPage.tsx`, `backend/src/Portfolio.Domain/Entities/Tool.cs` and
> `admin/src/pages/ToolsPage.tsx`.

## The constraint that decides the approach

The Lab page's tool mechanism is **one link per card**:

```tsx
// LabPage.tsx
const href = tool.kind === 'WindowsDownload' ? tool.downloadUrl : tool.url
```

A `Tool` has a single `DownloadUrl`, and the Résumé Builder bench item claims a single
`toolSlug: 'resume-builder-windows'`. So the admin-only route **can only ever offer one OS**.
That is fine for a single-platform tool and wrong for this one, which ships four builds:
Windows x64, Linux x64, macOS Apple silicon and macOS Intel.

Anything genuinely cross-platform therefore needs a page of its own.

## Recommended: a download page with OS detection

`docs/download-page/index.html` in this repo is that page, complete and working: hero button that
resolves to the visitor's platform, all four builds reachable, checksums and unsigned-build
warnings explained. It uses the portfolio's own design language (warm near-black, `#f6851b`,
Montserrat / JetBrains Mono).

It is a **design reference, not a drop-in** — the portfolio frontend is React 19 + Vite +
React Router + Tailwind 4, so the markup needs porting.

1. **Add the page.** Create `frontend/src/pages/ResumeBuilderPage.tsx`, porting the markup to
   Tailwind. The OS-detection script at the bottom of the HTML moves into a `useEffect`, or drop
   it in favour of `useState` + `navigator.userAgentData`.

2. **Register the route** in `frontend/src/App.tsx`, next to the existing ones:
   ```tsx
   <Route path="/resume-builder" element={<ResumeBuilderPage />} />
   ```

3. **Point the Lab card at it.** Give the bench item a curated `url` in `lab.ts`:
   ```ts
   { n: 'Résumé Builder', s: 'live', stKey: 'lab.st.live',
     meta: 'desktop · .NET + Avalonia', dKey: 'lab.d.resume',
     url: '/resume-builder', toolSlug: 'resume-builder-windows' },
   ```
   A curated `url` wins over the tool link (`href: m.url ?? link?.href`), so editing a tool in
   admin later cannot break a known-good link.

4. **Push to `main`.** Coolify auto-deploys.

Keep `toolSlug` even with a curated `url`: publishing the tool still flips the card's status to
*live* on its own (`state: link ? 'live' : m.s`), so the two work together.

### Download URLs never need updating

`/releases/latest/download/<asset>` redirects to that asset in whatever the current release is.
Proven: these URLs were published against v1.0.0 and now serve v1.0.1 with no change.

| Platform | Asset |
| --- | --- |
| Windows | `ResumeBuilder-win-Setup.exe` · `ResumeBuilder-win-Portable.zip` |
| Linux | `ResumeBuilder.AppImage` |
| macOS (Apple silicon) | `ResumeBuilder-osx-Setup.pkg` · `ResumeBuilder-osx-Portable.zip` |
| macOS (Intel) | `ResumeBuilder-osx-x64-Setup.pkg` · `ResumeBuilder-osx-x64-Portable.zip` |

Never use the `/releases/tag/v1.0.1/...` form — that pins the site to one version forever.

## Interim: something live today, no code

If you want it downloadable before the page exists, publish a tool in the admin dashboard
(**Tools**, route `/tools`; host is in `infrastructure/docs/current-setup.md`, needs Tailscale):

| Field | Value |
| --- | --- |
| Name | `Resume Builder` |
| Slug | `resume-builder-windows` |
| Kind | `ExternalLink` |
| URL | `https://github.com/Gentian28/resume-builder/releases/latest` |
| Version | `1.0.1` |
| Platform | `Windows · macOS · Linux` |

`ExternalLink` sends visitors to the releases page, where every platform is listed — so the card
is honest about being cross-platform. The trade is that they land on a list of ~20 files and have
to pick, which is exactly the problem the download page solves.

If you would rather give Windows users a one-click install and accept that the card only speaks
to them, use Kind `WindowsDownload` with Download URL
`.../releases/latest/download/ResumeBuilder-win-Setup.exe` instead. That is the version this doc
originally recommended, and it undersells the product.

Either way, tick **Published**. The card flips from *bench* to *live* by itself — `LabPage.tsx`
takes status from the dashboard, not the code.

## Two gotchas in the current code

- **The admin form's placeholder is stale.** It shows
  `https://github.com/Gentian28/resumebuilder/releases/latest/download/...` — the old *private*
  repo, a dead URL. Worth correcting in `admin/src/pages/ToolsPage.tsx`; following the hint
  produces a broken download.
- **The Description you type will not appear** on the Résumé Builder card. `LabPage.tsx` keeps the
  hand-written bench copy for claimed items (`t(m.dKey)`) and uses the DB description only for
  standalone tools. Edit the `lab.d.resume` i18n key instead.

## Cosmetic, whenever

`BUILD_LOG` in `lab.ts` still reads `['2026-06', 'resume-builder', '126 tests green, win-x64
publish']`. It is now 323 tests and a four-build release, so
`['2026-07', 'resume-builder', 'v1.0.1 — installers for win/linux/macos']` would be accurate.

# Things only you can do

> Everything automatable is done. This is the remaining list, in priority order, with the
> reason each one needs a human.

---

## 1. Submit the winget package

**Why you:** it ends in a pull request to `microsoft/winget-pkgs` from your GitHub account.

Manifests are written and validated at `packaging/winget/1.0.1/`.

### First, prove it installs

Do not skip this — winget's CI installs the package unattended, and a broken silent switch
fails the PR after a slow review cycle.

**Prerequisite, once per machine.** winget refuses local manifests by default
(`LocalManifestFiles` is off), so this needs enabling first. It requires an **administrator**
PowerShell — Start menu → type `PowerShell` → right-click *Windows PowerShell* → *Run as
administrator*:

```powershell
winget settings --enable LocalManifestFiles
```

Check it took with `winget settings export` — `"LocalManifestFiles":true`.

**Then, in a normal (non-admin) PowerShell.** Velopack installs per-user under `%LocalAppData%`,
so there is nothing to elevate for. Use the full path and the working directory stops mattering:

```powershell
winget install --manifest C:\Users\Pc\source\repos\resumebuilder\packaging\winget\1.0.1
```

winget reads the three YAML files, downloads the ~69 MB installer from the GitHub release,
verifies it against the `InstallerSha256` in the manifest, and runs it. A hash mismatch means the
manifest and the release have drifted apart — regenerate with `new-version.ps1`.

This performs a **real install**: Resume Builder appears in the Start menu. That is the point —
you are testing what a stranger will experience.

Confirm it launches, then remove it:

```powershell
winget uninstall Gentian28.ResumeBuilder
```

Optionally turn the setting back off afterwards (admin PowerShell again):

```powershell
winget settings --disable LocalManifestFiles
```

### Then submit

**Easy path** — `wingetcreate` opens the PR for you:

```powershell
winget install Microsoft.WingetCreate
wingetcreate submit --token <your-github-PAT> packaging\winget\1.0.1
```

The PAT needs `public_repo` scope. It forks, commits and opens the PR in one step.

**Manual path** — if you would rather see the diff:

1. Fork <https://github.com/microsoft/winget-pkgs>
2. Copy the three `.yaml` files to `manifests/g/Gentian28/ResumeBuilder/1.0.1/`
3. PR title exactly: `New version: Gentian28.ResumeBuilder version 1.0.1`

### After

Automated validation runs on the PR and comments if anything fails. A moderator reviews.
Expect days, not hours. Once merged, `winget install Gentian28.ResumeBuilder` works for everyone.

**For future releases:** `.\packaging\winget\new-version.ps1 -Version 1.0.2` regenerates and
validates the manifests by reading the published checksum. Run it after the release is out.

---

## 2. Apply for the SignPath code-signing certificate

**Why you:** a reviewed application tied to your identity, not an API call.

**This is the single highest-impact item left.** It removes the Windows "unknown publisher"
warning, which is the biggest thing standing between a stranger and actually running the app.

The prerequisites are already met: public repo, OSI licence (MIT), builds produced by public CI
from that source, no third-party binaries smuggled in.

1. Apply to the **SignPath Foundation** OSS programme at <https://signpath.org/>.
   Check their current criteria rather than trusting this list — they can change.
2. You will be asked for the repo URL, the licence, and a description of the project.
3. On approval they provide an organisation, a signing policy, and an API token.

### Wiring it in once approved

1. Add the token as a repository secret, e.g. `SIGNPATH_API_TOKEN`.
2. In `.github/workflows/release.yml`, sign between publish and pack. `vpk pack` already accepts
   signing parameters and currently prints
   `No signing parameters provided, 260 file(s) will not be signed` on every Windows run — that
   warning is the marker for where this goes.
3. Re-tag to produce a signed release, and drop the unsigned-build warnings from the README and
   the download page.

---

## 3. Publish it on the portfolio

**Why you:** it is a different repo and a live deploy.

Full instructions: `docs/publishing-to-the-portfolio.md`.

Short version — the Lab card mechanism is one-link-per-card, so the cross-platform route needs
the download page ported to a React route. There is a no-code interim (publish an `ExternalLink`
tool pointing at the releases page) if you want something live today.

---

## 4. Optional: macOS notarisation

**Why you:** needs a paid Apple Developer account ($99/yr) in your name.

SignPath does not cover this. Until it is done, macOS users must allow the app under
**System Settings → Privacy & Security** after the first attempt. Both Mac builds work; Gatekeeper
just objects once.

Worth doing only if Mac downloads turn out to be significant — the warning is a papercut, not a
blocker, and the instructions are already on the download page.

---

## Not blocking anything

- **Flathub** (step 9) — more reach on Linux, but the AppImage already works with no
  package manager. Do it if Linux traffic justifies it.
- **The UI redesign** — specced across 12 cards in `docs/ui-redesign-proposal.md`, with only the
  theme foundation implemented. Independent of distribution; the app ships and works today. It is
  also why the download page uses a CSS mockup instead of a screenshot: the current UI is the one
  being replaced.

# Release and external access

[Version 1.3.0 is public](https://github.com/Gentian28/resume-builder/releases/tag/v1.3.0), published 2026-10-15 from `cad57d1`. All 27 hosted assets match their SHA-256 manifests, all four update feeds name 1.3.0, and the public distribution audit passes. The application review and implementation are recorded in [the delivery report](reviews/2026-10-15-implementation.md).

All five [native acceptance cases passed](https://github.com/Gentian28/resume-builder/actions/runs/38784069423): Windows package update and installer-over-old-release, Linux, Apple silicon and Intel Mac. The [versioned evidence](reviews/2026-10-15-release-acceptance.json) records their scope and limits. The app's release commit passed CI and 571 tests; three optional live-provider tests were skipped.

## Release procedure

1. Update `Directory.Build.props` and the matching `CHANGELOG.md` section.
2. Push a version tag to the `public` remote, or dispatch `release.yml` with the version. Never publish from the superseded private `origin` repository.
3. The workflow builds and tests, packages all four native architectures, checks SHA-256 sums, installs v1.2.3 on disposable runners and applies the actual new Velopack package. It verifies resume, letter and application preservation, the pre-upgrade database backup, native startup, restart and the new installer. Windows uses the default install path and verifies workspace relocation, uninstall preservation and reinstall. Intel macOS runs on an Intel runner.
4. Inspect the `acceptance-*` evidence artifacts and the draft's files and checksums. Promote the draft only after every acceptance job passes. Publication makes the update available to existing installed copies.
5. Check the automatic winget submission and its resulting Microsoft validation/merge. Submission is enabled: repository variable `WINGET_AUTO_SUBMIT=true` and secret `WINGET_TOKEN` are configured. A submitted PR is not yet a published winget version.

For 1.3.0, automatic submission generated and committed the manifest but reported that `WINGET_TOKEN` was invalid. The release was submitted through the existing authenticated maintainer session as [Microsoft PR 453487](https://github.com/microsoft/winget-pkgs/pull/453487). Local validation passes with schema 1.12.0; the privacy description includes optional cloud AI. Microsoft validation/merge remains external. Future automatic submissions need the dedicated credential replaced; the existing Accession item `260902-rotate-the-winget-pat` is canonical. No general-purpose session token was copied into CI. The generator now validates and keeps an existing matching manifest on retry, and reads/writes UTF-8 consistently.

If a harness correction is needed after packaging, `verify-packages.yml` can test those exact artifacts using the original release run ID. `stage-verified-release.yml` then verifies the five proof files, their source commit and every asset checksum before uploading to an existing draft. Create that draft for the exact source commit using the authenticated maintainer CLI. GitHub's integration token cannot create or edit a historical release when its workflow files differ from main, as described in the [release API permissions](https://docs.github.com/en/rest/releases/releases#create-a-release). Do not rebuild and substitute different bytes after acceptance.

The acceptance harness refuses local and self-hosted runners because the previous app does not support a separate data-directory override. It uses synthetic data on disposable machines. macOS CI tests package installation and the updater with administrative access; it does not prove a human Gatekeeper/admin prompt. Linux launches the extracted AppImage under Xvfb, exercising the actual payload and update replacement without requiring FUSE. Physical display scaling and human task observations remain distinct checks.

On a Linux profile whose data base directory did not exist, v1.2.3 could create a relative `ResumeBuilder` folder in its launch directory. Version 1.3.0 resolves the intended data path before the folder exists. If an older workspace is in that relative folder, quit both versions and preserve the entire folder before recovery. Move a copy to `$XDG_DATA_HOME/ResumeBuilder` (or `~/.local/share/ResumeBuilder` when unset) only when that destination has no workspace. Never overwrite an existing destination database; retain both copies for recovery. The release upgrade fixture uses the normal existing desktop data base, and the new-build fresh-profile check covers a missing base directory separately.

Windows 1.3.0 uses `%LocalAppData%/ResumeBuilderData`, outside the `%LocalAppData%/ResumeBuilder` installer tree. On first launch it takes a SQLite online snapshot of the legacy workspace, validates it, copies theme/preferences/sync history and database backups, then publishes the database without replacing an existing destination. The legacy source stays intact. Migration errors open the existing recovery window rather than silently starting an empty workspace. Dictionary and thumbnail caches can be regenerated. Downgrading to an older app opens its older data location; it does not synchronize edits back from the new location.

Installing the new Setup.exe directly over v1.2.3 has its own gate, in addition to the in-app update route. Velopack renames the old install during repair and deletes that rollback when setup succeeds. The new install hook preserves a matching legacy tree containing user data under `%LocalAppData%/ResumeBuilderLegacyRecovery` before that cleanup. Ordinary launch then migrates from the preserved source. This recovery tree deliberately retains the old files; it is not an automatically deleted cache. Do not discard it before verifying the migrated workspace and an independent backup.

Submission retries now compare all three manifests against the indexed version or an open PR's exact fork commit. Matching submissions are skipped, and differing or mixed-package submissions stop for review. API failures are not treated as an absent submission. Runs serialize, reject unpublished/prerelease tags and check the dedicated credential before installing wingetcreate. Eight Node regression checks cover the retry decisions. The token is supplied through `WINGET_CREATE_GITHUB_TOKEN`, following [Microsoft's credential guidance](https://github.com/microsoft/winget-create/blob/main/doc/token.md), rather than the potentially logged `--token` command argument.

To restore future automatic submission, the owner creates a dedicated **classic** GitHub PAT with only `public_repo` and an appropriate expiry through [GitHub's token settings](https://github.com/settings/tokens). Fine-grained tokens are currently unsupported by wingetcreate. Do not select `repo`, `workflow` or `delete_repo`, and do not reuse the interactive gh credential. Run `gh secret set WINGET_TOKEN --repo Gentian28/resume-builder` and enter the dedicated token at its private prompt, never in chat or a command argument. GitHub credential creation/entry needs the owner; repository trust and computer-use permission do not issue a credential. After replacement, dispatch `winget.yml` for a future released version. Replaying 1.3.0 checks the existing PR and intentionally skips submission, so it does not prove the new token can submit.

The final native Windows pass on 2026-10-15 resized the isolated app to 893 by 931 and 893 by 594 pixels. The toolbar wrapped with Save and Export accessible, editor/navigation panes scrolled, and Applications and export review retained visible controls. A separate real display-scale pass changed the selected 3440 by 1440 display from its original 125% to 100%, 150% and 200%. Export review adapted to the reduced work area, editing at 200% updated the preview, and autosave was confirmed after restoring and visually verifying 125%. Both QA and Settings windows were closed. The real workspace was not opened. These observations cover this Windows display, not mixed-monitor transitions, human participants or human Mac/Linux workflows; [the versioned evidence](reviews/2026-10-15-release-acceptance.json) records their scope.

## Windows signing

The current builds are unsigned. SignPath declined the initial application on 2026-07-29 and invited reapplication with evidence of adoption. The refreshed [reapplication draft](signing-readiness.md) contains clearly labelled baseline download counts and current release/verification links. Its technical project fields were prepared in the live application form, with Productivity, Individual maintainer(s) and GitHub Actions selected. It has not been submitted. Owner contact details, actual discovery source and required legal/data-processing consent remain to be completed; optional marketing consent stays unchecked. Do not claim the project already uses SignPath before access is granted.

There is no signing secret configured in the public release repository and no usable code-signing certificate with a private key in this workstation's certificate stores. A certificate issuer or signing service must grant access before trusted signatures can be produced. Do not create a self-signed certificate and present it as public trust. Do not remove the unsigned-build notice before verifying the released signatures.

winget distribution does not guarantee suppression of SmartScreen or other Windows security prompts. The older claim that winget bypasses SmartScreen was incorrect.

The existing signing application is tracked in Accession as `260921-signpath-reapplication-now-has`. Keep that as the canonical follow-up rather than filing a second application task.

SignPath eligibility also needs clarification: its current conditions require OSI-approved licenses without commercial dual licensing for all components, while QuestPDF 2026.7.2 uses a source-available Community License. The package and upstream terms were checked, the README's incorrect MIT/all-OSI claim was corrected, and the live application draft now discloses this conflict. See [the dependency assessment and fallback plan](signing-readiness.md#dependency-eligibility-needs-clarification). Free-use eligibility under QuestPDF does not establish eligibility for SignPath's separate program. Do not assert compliance or accept a signing policy before resolving it.

## macOS signing and notarisation

No Apple signing/notarisation credentials are configured in the release repository. Trusted distribution needs owner-controlled Developer ID signing identities and notarisation access. Once available, configure Velopack signing for both app and installer, notarise both architectures, and verify with `codesign`, `spctl`, `pkgutil --check-signature` and the stapler validation commands before publication.

Passing native startup on an unsigned CI runner does not prove that a downloaded build passes Gatekeeper. Preserve the installation notice until signed, notarised downloads have been checked.

## Usability observations

[The session kit](usability-session-kit.md) explains the exercise in ordinary language, includes an unsent invitation and supplies the observation sheet. It needs real people willing to try the app. Automated testing and an agent's own inspection cannot supply those observations. No volunteers have been recruited, contacted or recorded by this work.

## Optional live AI check

The three Anthropic live tests remain skipped without an explicitly supplied API key. With an authorized test key in `ANTHROPIC_API_KEY`, run:

```powershell
dotnet test ResumeBuilder.sln --filter "FullyQualifiedName~AnthropicLiveTests"
```

Do not place a key in source, issue reports, screenshots or session materials. Offline drafting and analysis remain available without a key.

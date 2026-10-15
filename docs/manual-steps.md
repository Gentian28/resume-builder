# Release and external access

As of 2026-10-15, v1.2.3 is the latest public release. Version 1.3.0 is undergoing native installed-upgrade acceptance before publication. The application review and implementation are recorded in [the delivery report](reviews/2026-10-15-implementation.md).

## Release procedure

1. Update `Directory.Build.props` and the matching `CHANGELOG.md` section.
2. Push a version tag to the `public` remote, or dispatch `release.yml` with the version. Never publish from the superseded private `origin` repository.
3. The workflow builds and tests, packages all four native architectures, checks SHA-256 sums, installs v1.2.3 on disposable runners and applies the actual new Velopack package. It verifies resume, letter and application preservation, the pre-upgrade database backup, native startup, restart and the new installer. Windows uses the default install path and verifies workspace relocation, uninstall preservation and reinstall. Intel macOS runs on an Intel runner.
4. Inspect the `acceptance-*` evidence artifacts and the draft's files and checksums. Promote the draft only after every acceptance job passes. Publication makes the update available to existing installed copies.
5. Check the automatic winget submission and its resulting Microsoft validation/merge. Submission is enabled: repository variable `WINGET_AUTO_SUBMIT=true` and secret `WINGET_TOKEN` are configured. A submitted PR is not yet a published winget version.

The acceptance harness refuses local and self-hosted runners because the previous app does not support a separate data-directory override. It uses synthetic data on disposable machines. macOS CI tests package installation and the updater with administrative access; it does not prove a human Gatekeeper/admin prompt. Linux launches the extracted AppImage under Xvfb, exercising the actual payload and update replacement without requiring FUSE. Physical display scaling and human task observations remain distinct checks.

On a Linux profile whose data base directory did not exist, v1.2.3 could create a relative `ResumeBuilder` folder in its launch directory. Version 1.3.0 resolves the intended data path before the folder exists. If an older workspace is in that relative folder, quit both versions and preserve the entire folder before recovery. Move a copy to `$XDG_DATA_HOME/ResumeBuilder` (or `~/.local/share/ResumeBuilder` when unset) only when that destination has no workspace. Never overwrite an existing destination database; retain both copies for recovery. The release upgrade fixture uses the normal existing desktop data base, and the new-build fresh-profile check covers a missing base directory separately.

Windows 1.3.0 uses `%LocalAppData%/ResumeBuilderData`, outside the `%LocalAppData%/ResumeBuilder` installer tree. On first launch it takes a SQLite online snapshot of the legacy workspace, validates it, copies theme/preferences/sync history and database backups, then publishes the database without replacing an existing destination. The legacy source stays intact. Migration errors open the existing recovery window rather than silently starting an empty workspace. Dictionary and thumbnail caches can be regenerated. Downgrading to an older app opens its older data location; it does not synchronize edits back from the new location.

Installing the new Setup.exe directly over v1.2.3 has its own gate, in addition to the in-app update route. Velopack renames the old install during repair and deletes that rollback when setup succeeds. The new install hook preserves a matching legacy tree containing user data under `%LocalAppData%/ResumeBuilderLegacyRecovery` before that cleanup. Ordinary launch then migrates from the preserved source. This recovery tree deliberately retains the old files; it is not an automatically deleted cache. Do not discard it before verifying the migrated workspace and an independent backup.

## Windows signing

The current builds are unsigned. SignPath declined the initial application on 2026-07-29 and invited reapplication with evidence of adoption. The refreshed [reapplication draft](signing-readiness.md) contains current, clearly labelled download counts and project links. It has not been sent.

There is no signing secret configured in the public release repository and no usable code-signing certificate with a private key in this workstation's certificate stores. A certificate issuer or signing service must grant access before trusted signatures can be produced. Do not create a self-signed certificate and present it as public trust. Do not remove the unsigned-build notice before verifying the released signatures.

winget distribution does not guarantee suppression of SmartScreen or other Windows security prompts. The older claim that winget bypasses SmartScreen was incorrect.

The existing signing application is tracked in Accession as `260921-signpath-reapplication-now-has`. Keep that as the canonical follow-up rather than filing a second application task.

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

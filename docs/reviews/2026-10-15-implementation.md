# Review implementation, October 15, 2026

The [review](2026-10-15.md) assessed commit `524fbee`. This implementation repairs its 20 numbered defects and adds complete workspace recovery, immutable sent content, non-secret preferences, and contributor support for growing adoption of the free app. The review's minimum feature scopes are now implemented as well; the delivery table below separates software changes from external research and signing.

## Defect coverage

| Review finding | Implemented behaviour | Regression evidence |
| --- | --- | --- |
| B01, B20: native import and onboarding | The visible JSON action uses content detection for standard, native and workspace files. Resume adoption dismisses onboarding. | JSON importer tests; App import adoption test |
| B02, B09: text and PNG export | The visible text action resolves to TEXT. PNG returns actual page paths and protects existing numbered files. Export writes use temporary files before replacement. | Actual XAML format identifiers, PNG path/collision tests, cancelled atomic-write test |
| B03: endpoint credentials | Endpoint configuration is immutable per request. Switching endpoints clears the previous credential; remote endpoints require HTTPS and redirects are disabled. | Capturing HTTP transport and invalid-endpoint tests |
| B04, B05: sent version and dates | Newly sent applications capture immutable resume JSON. Source edits/deletion cannot change it. Historical records keep their existing live links. Creation and application dates survive row edits. | Repository snapshot/date tests; App sent-copy tests |
| B06, B13: stale AI work | Edits and document switches cancel old requests and clear suggestions. Rewrites check document identity, source text and stable experience keys before application. | Stale rewrite tests and delayed App AI response test |
| B07, B08, B14: sync state and identity | History belongs to a specific destination. Parent relationships travel by SyncId and resolve to receiving database IDs. Archived deleted/conflict files are excluded. | Two-folder history test; different-database parent test; remote-list test |
| B10: variant preservation | Adding a posting to a saved base branches before tailoring or autosave can overwrite it. The new variant becomes active. Further edits save that variant; its button does not create a duplicate after autosave. | App variant and autosave workflow tests |
| B11, B12: save and deletion | Saves serialize snapshots and retain newer dirty generations. Manual save finishes newer edits too. Navigation/reload/close checks protect edits made while awaiting I/O. Deleting an active document clears its editor. | Delayed-save test; resume/letter deletion tests; existing dirty/concurrency tests |
| B15: shared render state | Each lazy resume or letter document receives an isolated renderer with its own settings and section state. | All 25 resume templates and all three letter templates tested for lazy-document isolation |
| B16: hidden keywords | Keyword extraction follows section visibility and includes visible custom content. | Hidden-section analysis test |
| B17: letter drafting source | A letter retains its source resume snapshot, target role and posting. Draft replacement asks first and is undoable. Results from superseded requests are ignored. | App draft test after opening an unrelated resume |
| B18: application writes | Row saves coalesce and run in order, retain failed edits, and expose retry status. Save, Done, shutdown, backup and update await pending work. A failed write on close offers the existing Save/Discard/Cancel choice. | Delayed/failing queue tests; App retry and explicit-discard tests |
| B19: active-document commands | Save, export, duplicate, new/open and undo/redo route to the visible letter where applicable. Letters have separate undo history. macOS Command bindings are included. | App active-letter save/undo and duplicate tests |

## Recovery and runtime

Workspace backup uses a versioned JSON archive of all resumes, letters, applications and captured sent content. Import validates the archive before a transactional replacement. It makes a safety database backup, resets the old editors and pauses folder sync. It refuses restore while sync is running. A failed replacement rolls back. Old native resume JSON still imports as a single resume. API keys and preferences are excluded from workspace archives.

Preferences persist provider, endpoint, model, sync path/enabled state, conflict choice and last resume. Keys remain session-only. A missing or paused sync directory remains visible for reconfiguration. `RESUMEBUILDER_DATA_DIRECTORY` provides an isolated profile for testing.

The solution and CI target .NET 10; self-contained packages pin runtime 10.0.13 and SQLite persistence uses EF Core 10.0.13. Both macOS architectures are included in the publish matrix. Schema-upgrade tests cover existing data with the new columns absent, without fabricating a sent revision for historical applications.

## Validation

- Release solution build with warnings treated as errors: zero warnings and errors.
- **564 passed, three live-provider tests skipped**: 522 library, 24 App workflow and 18 headless UI tests after native acceptance corrections. Coverage was collected for all three test assemblies in the initial pass.
- NuGet direct and transitive advisory scan: no known vulnerable packages reported.
- Windows x64, Linux x64, macOS ARM64 and macOS x64 single-file publish checks are retained in CI, with native unsigned installer packaging added. Local Windows installer and portable packaging succeeded with the pinned Velopack CLI.
- Windows clean-profile process launch: stayed running for 20 seconds and created its temporary database, with no stderr output.
- Public v1.2.3 Windows installer/portable, Linux AppImage and both macOS package links returned HTTP 200, with four checksum files present. The repository homepage now points to the canonical latest public release.
- Temporary test databases and synthetic fixtures only. The normal personal profile was not opened or upgraded.

Cross-publishing does not prove native Linux/macOS operation. The headless UI suite now checks light/dark screens at 800 by 500 and 1400 by 900, real bindings, focus and Escape. File-dialog clickthrough, real display scaling, signed installers, installed updates and actual previous-release installation upgrades remain native release checks. No GitHub release or tag is created by this implementation. CI now rehearses unsigned native installer packaging as well as single-file publishing. Local design sources and synthetic rendered fixtures describe the changed controls and states; no design-canvas upload was performed.

The original `probe/` and captured results are historical evidence of defects at `524fbee`, preserved in review commit `3dbfe3d`. They intentionally report success when a bug is present. Run the maintained solution tests for regressions on the repaired app.

## Adoption work and next feature decisions

Implemented: accurate local/cloud privacy wording, sample import-to-export walkthrough, workspace recovery instructions, contributor checks, security reporting guidance and privacy-conscious issue forms. No telemetry, accounts, subscription gates or automatic cloud AI were added.

The business objective is adoption of the free open-source app. The next useful evidence is five to ten consenting users completing clean installation, import and first export, followed by a second-application check. Record failures and task completion time without collecting resume contents. Compare release asset downloads and support load separately; they are not an activation funnel.

## Completed feature scopes

| Capability | Delivered behaviour |
| --- | --- |
| First session and import review | Embedded synthetic example, separate imported identity, persistent warnings and section counts, dismiss action, local DOCX import preserving original body text for review. |
| Export readiness | Current rendered page count, contact/date/font warnings and extracted PDF text in a focused, scrollable review surface. Warnings do not block exports or claim hiring outcomes. |
| Search and filters | Name/person/role search keeps base/variant grouping. Applications search company/role/contact and filter active, archived and due rows. |
| Application follow-ups | Editable applied and next-action dates, contact and posting URL; in-app due indicators; CSV with formula neutralization; stable, escaped, UTF-8-folded all-day calendar events. |
| Application context | Draft state, letter selection, immutable resume/letter content and generated PDFs at sending. Save captured PDF actions retain original bytes after source changes/deletion. Historical records stay honest about missing captures. |
| Letter controls | Three templates, sender editor, explicit source label and replacement, editable role/posting, separate undo history including source replacement. |
| Paper and language | A4/US Letter across resume/letter templates and Word output; matching HTML print geometry. English/Albanian generated headings and dates, including HTML and plain text, scoped per operation. User-authored text is preserved. |
| Diagnostics and support | Explicit preview/copy of software versions and fixed error codes. No content, identity, paths, endpoints, keys or raw error messages in diagnostics. Contributor/security guides and issue forms remain in place. |
| Input and rendering boundaries | Bounded files and archive expansion, PDF page cap, safe image dimensions, normalized nested input/layout values, cancellable import adoption and coalesced preview rendering. |
| Adoption preparation | Bundled walkthrough, accurate privacy and installation copy, real screenshot reference, canonical public release homepage, verified current platform download links, reusable distribution audit and privacy-preserving usability-session kit. |

Workspace archives include the new application context and captured artifacts. Upgrade and roundtrip tests preserve them. Linked sent letters and PDFs cannot be replaced by ordinary row edits. Keys remain session-only.

Parallel Windows rendering also exposed corrupted extractable PDF text in the previous QuestPDF dependency. The app now uses 2026.7.2, whose fix is documented in [QuestPDF issue 1447](https://github.com/QuestPDF/QuestPDF/issues/1447). A parallel 24-document extraction regression verifies the app's exports without hiding the failure through serialized tests.

The [session kit](../usability-session-kit.md) is ready for consenting volunteers. Recruitment, observed sessions, signing/notarisation and owner-controlled credentials are external activities, not claims made by this implementation. No outreach, payment, telemetry or public release was performed. Native computer-use access was initially rejected; the user subsequently approved the app and the additional Windows pass below became possible. The explicitly deferred web rewrite, account system, cloud OAuth, paid features, UI localization and right-to-left expansion were outside the review's recommended implementation scope.

## Windows native acceptance follow-up

The approved computer-use pass used a new temporary profile and synthetic data. It verified first launch, the embedded example, field editing/autosave, undo, saved content after restarting, export review and Escape, native PDF and Word save dialogs, Word reimport, offline cover-letter drafting, active-letter save/export shortcuts, application draft creation and editing, linked letters, Saved-to-Applied capture, and native export of the captured letter PDF. The exported artifact exactly matches the stored PDF bytes. The normal profile was not opened or upgraded.

This pass found and repaired additional defects:

| Finding | Correction and evidence |
| --- | --- |
| Application status selection crashed the process | Row-list refresh was synchronously destroying the active dropdown during Avalonia's selection event. Membership-preserving edits retain containers; membership changes notify after the event finishes. A headless test protects container identity and filtered-out selection. The formerly crashing native transition now succeeds and captures both PDFs. |
| GitHub usernames produced a false readiness error | Validation accepts usernames as well as HTTP/HTTPS URLs, consistent with the editor and exports. Unsafe URL schemes still fail regression tests. |
| Word import was missing from the toolbar and duplicated in File | Both menus now contain one working Word action. Headless menu binding coverage and a native Word import verify it. |
| Export review wasted vertical space and joined unrelated text | Its read-only field stretches below the heading, aligns text to the top, and uses content-order extraction with detected word/line spacing. Layout and extracted-text regressions pass; the native review was rechecked. |
| Word import omitted the name and damaged experience fields | Word paragraph font size/bold hints now reach the parser. Date and title separators match complete delimiters rather than letters within words. Open-ended ranges retain their roles without inventing current employment. Tests cover names, complete titles, October dates and missing end dates; the native roundtrip now retains all three sample roles. Deleted revision text is excluded from paragraph content and hints. |

Narrow layouts remain covered by headless tests at 800 by 500; the native session used the current desktop size/scaling. Installer installation/upgrades, other physical scaling values, native macOS/Linux acceptance, signing and notarisation were not performed in this pass. A build attempted while the development executable was open encountered a Windows DLL lock; the final build ran successfully after isolating the executable copy.

## Release acceptance follow-up

The release harness installs the actual previous release on disposable GitHub-hosted Windows, Linux, Apple silicon and Intel Mac runners. The previous app creates its schema before synthetic resume, letter and application rows are inserted. The real updater applies the new package; checks cover preserved data, historical missing captures, safety backup contents, restart and the new installer. Windows additionally exercises its default install location, migration outside the install tree, uninstall preservation and reinstall. Linux runs the AppImage payload under Xvfb and tests a fresh missing XDG data directory. These are native process/package checks, not human Mac/Linux workflow observations or physical DPI verification.

Release preparation found two additional data-path defects. A missing Linux data base directory caused .NET's default folder verification to return an empty path; resolution now requests the intended path before creation. Windows kept its workspace inside the directory deleted by Velopack uninstall. The new `ResumeBuilderData` sibling receives a validated SQLite online snapshot, known user settings/sync files and backups. It never replaces an existing destination database, retains the source and reports copy failures through recovery. Tests cover committed WAL data, source preservation, existing destinations/settings, corruption and a missing source. Regenerable caches and installer files are excluded.

The initial Mac harness also mistook the manifest's symlink for a second independent manifest. It now resolves symlinks before checking uniqueness. This was an acceptance-script correction, not an application defect.

Windows also has a separate installer-over-v1.2.3 acceptance job. The install hook moves the old installer rollback tree containing the workspace outside Velopack's cleanup path before ordinary startup performs the validated database migration. The original package and user files are retained under `ResumeBuilderLegacyRecovery`. Tests reject unrelated manifest identities and verify that the preserved source can be migrated. This covers the winget/direct-installer upgrade route as well as the in-app package updater.

## Published result

[Version 1.3.0](https://github.com/Gentian28/resume-builder/releases/tag/v1.3.0) was published on 2026-10-15 at 18:24:49 UTC from `cad57d1`. Local validation passed 571 tests with three live-provider skips, a zero-warning Release build and clean formatting. [CI 38782836975](https://github.com/Gentian28/resume-builder/actions/runs/38782836975) passed all three OS suites, dependency checks and all four publishes/installer builds.

The exact packages from release build 38782837907 passed all five cases in [verification 38784069423](https://github.com/Gentian28/resume-builder/actions/runs/38784069423). The earlier Windows failures were Python harness connections retaining database locks, not app failures; explicit connection closure fixed the harness. [Staging 38785149181](https://github.com/Gentian28/resume-builder/actions/runs/38785149181) checked the source provenance, five proof files and every package checksum before uploading the tested bytes. Hosted GitHub asset digests then matched all 27 assets and all four update feeds named 1.3.0. The public HTTP distribution audit passed for Windows installer/portable, Linux AppImage and both Mac installers. [Versioned native evidence](2026-10-15-release-acceptance.json) retains the acceptance scope beyond artifact retention.

Signing and notarisation remain unavailable without issuer/Apple credentials. The refreshed signing draft is prepared and unsent. Real volunteer sessions have not happened; their invitation and observation materials are ready. Physical display scaling and human Mac/Linux workflow observations remain unverified. The additional Windows size-check attempt encountered a busy computer-use helper and was stopped; only its temporary synthetic process was closed. These limits are separate from the completed native installer/update checks.

The winget workflow generated the 1.3.0 manifests but its dedicated token was invalid. [PR 453487](https://github.com/microsoft/winget-pkgs/pull/453487) was submitted using the existing authenticated maintainer session without persisting that session credential in CI. It contains only the three files for this package/version; local `winget validate` passes and the CLA check passed. The schema is updated to 1.12.0 and the description now accurately explains optional cloud AI. The manifest generator preserves reviewed files on a matching retry and uses explicit UTF-8 to avoid corrupting text. Microsoft review/indexing and replacement of the dedicated CI token remain external follow-up.

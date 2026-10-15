# Review implementation, October 15, 2026

The [review](2026-10-15.md) assessed commit `524fbee`. This implementation repairs its 20 numbered defects and adds complete workspace recovery, immutable sent content, non-secret preferences, and contributor support for growing adoption of the free app. The larger product roadmap remains a staged plan.

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
- Full library and App workflow suites, with coverage collection: **498 passed, three live-provider tests skipped** (479 library tests and 19 App workflow tests passed).
- NuGet direct and transitive advisory scan: no known vulnerable packages reported.
- Windows x64, Linux x64, macOS ARM64 and macOS x64 self-contained publishes: one executable per drop.
- Windows clean-profile process launch: stayed running for 20 seconds and created its temporary database, with no stderr output.
- Temporary test databases and synthetic fixtures only. The normal personal profile was not opened or upgraded.

Cross-publishing does not prove native Linux/macOS operation. File-dialog clickthrough, small-window/DPI/accessibility checks, signed installers, installed updates and actual previous-release installation upgrades remain release checks. No GitHub release or tag is created by this implementation. Local design sources describe the changed controls and states; no design-canvas upload was performed.

The original `probe/` and captured results are historical evidence of defects at `524fbee`, preserved in review commit `3dbfe3d`. They intentionally report success when a bug is present. Run the maintained solution tests for regressions on the repaired app.

## Adoption work and next feature decisions

Implemented: accurate local/cloud privacy wording, sample import-to-export walkthrough, workspace recovery instructions, contributor checks, security reporting guidance and privacy-conscious issue forms. No telemetry, accounts, subscription gates or automatic cloud AI were added.

The business objective is adoption of the free open-source app. The next useful evidence is five to ten consenting users completing clean installation, import and first export, followed by a second-application check. Record failures and task completion time without collecting resume contents. Compare release asset downloads and support load separately; they are not an activation funnel.

The [review roadmap](2026-10-15.md#missing-capabilities-ranked-by-user-value) retains scoped proposals and acceptance gates for import review/bundled example, export readiness, search/filtering, application CSV/follow-ups, linked letters, DOCX import, letter controls, paper sizes and document language. Sent content is implemented; the broader application-context feature is partial. New screen designs and observed user demand determine the next implementation batch. Signing/notarisation and tester recruitment remain owner/external work in Accession.

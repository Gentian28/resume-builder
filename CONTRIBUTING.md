# Contributing

Use the .NET 10 SDK. Restore and run the checks before opening a pull request:

```powershell
dotnet restore ResumeBuilder.sln
dotnet build ResumeBuilder.sln -c Release -warnaserror
dotnet test ResumeBuilder.sln -c Release --no-build
```

The library tests cover persistence, sync, imports, exports, and every registered template. The separate App.Tests project checks editor commands and dialog decisions without opening native windows. Live provider tests are skipped unless deliberately configured. Never put an API key or a real resume into tests or issues.

Fixes should include a regression demonstrating the user-visible failure. Keep domain logic in Core, persistence in Data, renderers in Templates, and desktop concerns in App. Package versions are centralized in Directory.Packages.props. Schema additions must be included in DatabaseInitializer.AddedColumns and pass the schema guard.

Keep UI changes aligned with the repo design source under docs/design. Explain the problem and resulting behaviour in the pull request, with relevant validation. Use synthetic screenshots and fixtures from samples.

For a clean-profile desktop check, set RESUMEBUILDER_DATA_DIRECTORY to a new empty temporary directory before starting the app. All app data, including preferences and dictionary caches, uses that directory. Remove the variable for normal use. Never test database upgrades or restore operations against your only copy of personal work.

Before a release, check an import-to-export flow, restoring a workspace backup into a fresh profile, upgrading an older database, active-document keyboard commands, and the installed update flow. Repeat packaging on Windows x64, Linux x64, macOS Apple silicon, and macOS Intel. Cross-compiling alone does not establish that a package runs on another operating system.

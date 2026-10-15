# Historical baseline diagnostic

This diagnostic and the adjacent captured results document defects at app commit `524fbee`. Review commit `3dbfe3d` preserves that baseline and its runnable project. CONFIRMED means a baseline defect was reproduced, not that the app passed a test.

Do not use this diagnostic to validate the repaired app. Its delayed repository stubs intentionally model the old save behaviour and its reflection targets can change. Use `dotnet test ResumeBuilder.sln` for maintained library and editor regressions. See [implementation status](../2026-10-15-implementation.md).

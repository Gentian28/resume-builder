# Free-app adoption session kit

Use five to ten consenting volunteers. This is a ready-to-run research script, not evidence that sessions have happened. Participation is optional and stopping is always allowed. Use the bundled synthetic example. Do not request personal resumes, screen recordings, account credentials or API keys. Record observations under participant codes rather than names.

## First session

1. Explain: we are evaluating the app, not the participant. Ask permission to observe and take anonymous task notes.
2. Ask the participant to find the official download, identify their operating system/architecture and install or open the portable build. Record the route, version, security messages and whether help was required.
3. Ask them to open the bundled example, change its role and a bullet, select a template and export a PDF. Do not coach until they are blocked. Record elapsed time, completion, needed assistance, missing content and whether they could find the exported file.
4. Ask them to review the exported text, make a job-specific variant, create a letter and add an application with a next action. Ask which document they think was saved and sent. Compare that understanding with the application record.
5. Ask them to create a workspace backup and restore it into a separate test profile. Never replace a personal workspace in a study.
6. Ask what first brought them to the app, what was confusing, whether the export feels usable and what would make them return for another application. Let them answer before describing possible features.

## Follow-up

After the next application opportunity, ask whether they returned, which task they completed and which obstacle stopped them. Daily activity is not an appropriate retention target for an episodic job-search tool. No automatic messages or tracking are configured by this kit.

## Observation sheet

| Participant code | OS/architecture | Version | Install completed | First usable PDF completed | Minutes | Help needed | Obstacle | Second-application use |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| P01 | | | | | | | | |

Keep free-text notes about behaviour and software defects, not resume content. Delete accidental personal information. Participants can request deletion of their notes. Summarize aggregate findings before choosing the next feature. Prioritize repeated blocked tasks over feature votes. Use separate populations for installer downloads and observed participants; do not divide one by the other to invent a conversion rate.

## Release gates

Run the solution checks, all four publish jobs, archive roundtrip/rollback and old-schema upgrade tests before promoting a build. Use an isolated profile for manual file-picker, keyboard, DPI, install and update checks. Windows signing and macOS notarisation need owner-controlled credentials and are separate from functional verification. A passing headless render is not an installed-update test.

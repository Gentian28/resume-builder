# Privacy Policy

**Resume Builder does not collect any personal data.**

There is no account, no sign-up, no telemetry, no analytics, no crash reporting and no
advertising. Nothing you type is transmitted to the author of this software, and there is no
server operated by this project that your copy talks to.

Last updated: 2026-07-28.

## What is stored, and where

Everything stays on your own computer, in your user profile:

| What | Where |
| --- | --- |
| Résumés and cover letters | A local SQLite database under your user data directory |
| Spell-check dictionaries | Downloaded once on first use, then cached locally |
| Sync state | A local file, only if you enable folder sync |

The exact location follows the platform convention — `%LocalAppData%\ResumeBuilder` on Windows,
`~/.local/share/ResumeBuilder` on Linux, `~/Library/Application Support/ResumeBuilder` on macOS.

Uninstalling does not delete this data; remove that folder if you want it gone.

## When the app uses the network

The app works fully offline. There are exactly three cases where it makes a network request, all
of them either optional or clearly signposted:

**1. Update checks.** On start-up the app asks GitHub whether a newer release exists. GitHub
receives the request as it would any download. No information about you or your résumé is
included. This only happens in builds installed from the installer; portable builds never check.

**2. Spell-check dictionaries.** The first time spell check runs for a language, the dictionary
file is downloaded and then cached. Your text is not sent — only the dictionary comes down.

**3. AI features, only if you configure them.** These are off until you provide an endpoint:

- **Local model** (Ollama, LM Studio, anything on a loopback address): requests never leave your
  machine.
- **A cloud provider** (OpenAI or any compatible API): the résumé text relevant to the request is
  sent to the endpoint *you* configured, and that provider's own privacy policy then applies. The
  app states plainly which mode it is in before you use it.

Keyword analysis and ATS scoring are local computation and never use the network, with or without
a model configured.

## Optional folder sync

If you turn on sync, résumés are written to a folder you choose. If that folder is inside
Dropbox, OneDrive, Google Drive or similar, that provider's terms apply to the files. The app
itself uploads nothing.

## Your data

Because the data never leaves your machine, there is nothing for the author to hand over, correct
or delete. You have direct access to all of it: the database file is yours, and every résumé can
be exported to PDF, DOCX, HTML, PNG, plain text or JSON at any time.

## Questions

Open an issue at <https://github.com/Gentian28/resume-builder/issues>.

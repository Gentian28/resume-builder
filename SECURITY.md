# Reporting security problems

Do not publish API keys, personal resumes, database files, or workspace backups in an issue.

If GitHub offers Report a vulnerability on the repository Security tab, use that private channel for credential disclosure, unsafe file handling, and other security problems. If that option is unavailable, open a minimal issue requesting a private reporting channel without including exploit details or personal data.

Include the affected version, operating system, installation method, and a synthetic reproduction. Keep any real credentials revoked and out of the report. Ordinary bugs can use the bug report form.

The app uses local storage and optional user-configured AI endpoints. Cloud AI requests follow the configured provider's policy. API keys are held in memory for the current session. Workspace archives contain personal documents and should be stored and shared accordingly.

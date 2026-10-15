# Signing access and reapplication draft

Prepared 2026-10-15. Technical project fields are filled in the live [application form](https://signpath.org/apply), with Productivity, Individual maintainer(s) and GitHub Actions selected. Contact information, actual discovery source and required consent remain for the owner. No application or message was submitted. The form requires agreement to the [Foundation terms](https://signpath.org/terms) and personal-data processing; general permission to work does not replace action-time consent to those agreements. Marketing consent remains unchecked. If the live draft is lost, this document preserves the project text.

## Evidence

The public [repository](https://github.com/Gentian28/resume-builder) was created on 2026-07-28. GitHub's public API reported 4 stars, 1 fork and 10 published releases when checked on 2026-10-15. Those releases had 1,832 asset downloads in total. Only 555 downloads were installer, portable ZIP or AppImage assets; the remaining asset count includes update feeds, packages and checksum files. None of these figures establishes a number of distinct people, active installations or retained users. Some downloads include development and verification activity.

The app is distributed as `Gentian28.ResumeBuilder` through [winget](https://github.com/microsoft/winget-pkgs/tree/master/manifests/g/Gentian28/ResumeBuilder). Published releases trigger manifest generation and submission. The public project includes build/test workflows, a changelog, contributor instructions, a security reporting policy and synthetic fixtures. [Version 1.3.0 is public](https://github.com/Gentian28/resume-builder/releases/tag/v1.3.0), and all five exact-package native installer/upgrade acceptance cases passed for Windows, Linux and both Mac architectures in [verification 38784069423](https://github.com/Gentian28/resume-builder/actions/runs/38784069423). Its [winget update PR](https://github.com/microsoft/winget-pkgs/pull/453487) is submitted and awaiting Microsoft's remaining validation. Baseline counters above precede this release and its verification downloads.

## Suggested application text

I maintain Resume Builder, a free open-source desktop resume and cover-letter editor. Its core workflow works locally without an account, subscription or cloud AI key. The project is MIT licensed, and its source and build workflows are public at https://github.com/Gentian28/resume-builder.

My initial application was declined in July 2026 because the project was new and had little adoption evidence. Since then the project has published ten releases and is available through Windows Package Manager as Gentian28.ResumeBuilder. As of 15 October 2026, GitHub reports four stars, one fork and 555 installer/portable/AppImage asset downloads across those releases. These are download events rather than a claim of unique users. I can provide the public release and distribution links for verification.

I would like to reapply for open-source Windows signing so people downloading the installer can verify its publisher. Release builds are automated in GitHub Actions, with tests and native installed-upgrade checks before promotion. I can configure signing to follow your approved build provenance and release process and add the required acknowledgement after access is granted.

Please let me know whether the project now meets your eligibility requirements or which additional evidence is needed.

## Access needed after acceptance

Use the provider's supported integration and least-privilege repository secrets. The particular SignPath organization/project/policy identifiers and token must come from the granted account. Do not invent them or publish secret values. Integrate signing into packaging only after the approved signing policy is known; verify the resulting Windows installer and executable signatures on a clean machine before changing the download notice.

For macOS, the separate prerequisite is owner-controlled Apple Developer ID application and installer identities plus notarisation access. SignPath Windows eligibility does not provide Apple credentials. Signing/notarisation must cover both architectures and the actual final downloadable assets.

Technical references: [Velopack signing](https://docs.velopack.io/packaging/signing), [CI signing configuration](https://docs.velopack.io/packaging/signing-ci-cd), and [Apple notarisation](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution). No paid enrollment or certificate purchase has been made.

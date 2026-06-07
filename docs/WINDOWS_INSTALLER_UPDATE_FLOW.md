# Windows tester installer/update flow

This document defines the safe Velopack foundation for controlled Windows tester installer and update packaging. It is a parallel track beside the current tester zip handoff, not a replacement.

## Current status

- The existing tester zip flow remains the canonical and accepted fallback tester flow.
- The canonical zip script remains `scripts/package-tester-release.ps1`.
- The canonical zip output remains `artifacts/packages/EnglishVoiceTutor.Desktop-win-x64-self-contained.zip`.
- The Velopack flow is a new parallel tester installer/update foundation until installer and update smoke testing is accepted.
- This does **not** mean public release is ready.
- This does **not** mean Microsoft Store distribution is ready.
- This does **not** enable production billing.
- This does **not** add production RBAC/Admin readiness.
- This does **not** deploy release files to a server.

## Why Velopack before MSIX/Microsoft Store

Velopack is being used first because controlled tester distribution needs a simple installer and update artifact set before the stricter Microsoft Store/MSIX readiness work. This lets the team smoke test install, launch, uninstall, and later update behavior with a small tester channel while keeping the accepted zip package available as the fallback handoff. MSIX, Store identity, Store submission, signing policy, public release pages, and production release operations remain deferred.

## Channel and versioning

Use this tester channel name:

```text
win-x64-tester
```

Use SemVer 2 compatible versions, for example:

```text
0.1.0-tester.1
0.1.1-tester.1
```

Do not use four-part versions such as `0.1.0.0` for Velopack. Velopack package versions must be SemVer 2 compatible.

## Prerequisites

Install the .NET SDK required by the desktop project. Install the Velopack CLI as a .NET global tool using the same version as the desktop `Velopack` package reference:

```powershell
dotnet tool install -g vpk --version 1.2.0
```

If `vpk` is already installed, update it intentionally and keep it aligned with the project package version:

```powershell
dotnet tool update -g vpk --version 1.2.0
```

## Build a tester installer locally

Run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-velopack-tester-release.ps1 -Version 0.1.0-tester.1
```

The script publishes the desktop app in Release mode for `win-x64` as self-contained output at:

```text
artifacts/publish/win-x64-velopack-tester
```

It creates Velopack tester release files at:

```text
artifacts/releases/windows/tester
```

The script scans the publish output before packaging and fails if local settings/session/history, token/secret-looking files, API key-looking files, or OpenAI API key-looking files are present. User settings and auth/session data must stay outside the install directory and must never be stored in packaged release files.

## Expected local release files

After a successful package, `artifacts/releases/windows/tester` is expected to contain:

- `Setup.exe` — tester installer handoff executable.
- `releases.win-x64-tester.json` — Velopack release index for the tester channel.
- at least one `*-full.nupkg` package — the full release package.

Velopack may also create additional files such as a package-id-specific setup executable, portable zip, delta packages when previous versions are present, or generated reports/checksums if added later. Generated files under `artifacts/` must not be committed.

## Future static server upload layout

When server upload is approved later, upload the Velopack release files for this tester channel to a static folder such as:

```text
/releases/windows/tester/win-x64/
  Setup.exe
  releases.win-x64-tester.json
  EnglishVoiceTutor.Desktop-0.1.0-tester.1-full.nupkg
```

The release index and all package files referenced by the release index must stay together in the same static update folder. The zip fallback can remain separate, for example:

```text
/releases/windows/tester/zip/EnglishVoiceTutor.Desktop-win-x64-self-contained.zip
```

Do not deploy anything as part of the local packaging script. Upload policy, hosting, retention, authentication, and public download pages are separate future decisions.

## Code signing and Windows warnings

Code signing is deferred. Unsigned installers may trigger Windows Defender SmartScreen or other trust warnings. That is expected for early controlled tester builds and must be called out to testers until signing is implemented.

## Deferred update behavior

This foundation only adds Velopack startup support and local package creation. The following remain deferred:

- update-check UI;
- automatic updates;
- silent updates;
- update prompts;
- active-lesson-safe update confirmation;
- public download website;
- Microsoft Store/MSIX packaging.

The next implementation step should add explicit update-check UX only after deciding when updates are safe to offer, especially during active lessons.

## Smoke requirements

The first installer smoke must verify:

- install;
- launch;
- login/session restore;
- backend connection;
- core lesson flow;
- uninstall.

Update smoke requires two versions later, for example updating from `0.1.0-tester.1` to `0.1.1-tester.1`. Keep the existing zip package available as the accepted fallback until this installer/update smoke is accepted.

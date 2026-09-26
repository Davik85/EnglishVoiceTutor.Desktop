# Language Voice Tutor / Orralen

Language Voice Tutor is a guided speaking-practice product for six study languages. Learners use lesson chat, voice practice, hints, translation, feedback, and progress tracking. Orralen is the public brand; the Windows desktop client and Android app share a backend-owned account and learning model.

## Repository

- The root WPF project contains the Windows client, lesson content, and desktop tests.
- `backend/EnglishVoiceTutor.Api/` contains the ASP.NET Core API and the Admin and Website CMS surfaces.
- `site/public/` contains independently managed static website files alongside repository snapshots of CMS-managed output. Website CMS Publish owns its documented generated pages and resources.
- `scripts/` and `tools/` contain packaging, deployment, audit, and policy commands. Backend deployment, database migrations, website publication, and Windows release upload are separate operations.

The Windows client calls backend APIs for provider-backed features and does not contain provider credentials or make direct OpenAI calls. Installed release builds use the fixed production backend `https://api.languagevoicetutor.com`; local backend URLs are for Debug development only.

## Local development

From the repository root, restore and build the Windows client with `dotnet restore` and `dotnet build`. To run a local API, go to `backend/EnglishVoiceTutor.Api/`, restore and build with the same commands, then use `dotnet run`. The Debug desktop build defaults to `http://localhost:5000`. Real AI, transcription, and speech tests require a locally configured backend provider key; keep credentials outside Git and the client.

## Current state and operations

Repository documentation is a recorded checkpoint. Verify the public Windows release from the live `latest.json` manifest and the production backend from the server `current` symlink before reporting live versions. Local generated artifacts do not prove public availability.

- [Current product and production state](docs/CURRENT_STATE.md)
- [Current priorities](docs/NEXT_STEPS.md)
- [Operator command playbook](docs/COMMAND_PLAYBOOK.md)
- [Windows client functionality](docs/WINDOWS_CLIENT_FUNCTIONALITY_OVERVIEW.md)
- [Windows installer release flow](docs/WINDOWS_INSTALLER_RELEASE_FLOW.md), [update flow](docs/WINDOWS_INSTALLER_UPDATE_FLOW.md), and [server upload](docs/WINDOWS_RELEASE_SERVER_UPLOAD.md)
- [Backend deployment and rollback](docs/BACKEND_SERVER_DEPLOYMENT.md)
- [Website CMS operator guide](docs/CMS_PROMPT_MANAGEMENT_ADMIN_GUIDE.md)
- [Subscription and billing architecture](docs/subscription-billing-foundation.md)

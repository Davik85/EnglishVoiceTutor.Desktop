# Codex and GPT-5.6 Development Workflow

## Purpose

This document records how Codex and GPT-5.6 were used in the development workflow for Language Voice Tutor Desktop. It exists for transparency: repository changes may be planned, implemented, checked, or documented with AI assistance, but the workflow remains human-directed and subject to review.

## How Codex Was Used

Codex was used as an engineering agent for bounded repository tasks under human direction. Representative activities included:

- inspecting the repository structure, existing documentation, recent Git history, and affected source areas before making changes;
- implementing scoped code and documentation changes without intentionally expanding the approved task boundary;
- creating automated tests and maintaining regression tests when behavior changes required coverage;
- running relevant build, restore, static verification, and repository policy commands;
- keeping documentation synchronized with behavior, release gates, backend boundaries, and known follow-up work;
- identifying affected files, deployment boundaries, generated artifacts, and areas that should not be changed for a given task;
- returning summaries, changed-file lists, and verification results so humans could review the work before deciding whether to keep, commit, deploy, or release it.

Codex did not implement every part of the project and was not treated as an autonomous product owner. Its work was constrained by human-written instructions, repository state, project documentation, and follow-up review.

## How GPT-5.6 Was Used

GPT-5.6 was used for planning, review, and continuity across the product. Representative activities included:

- converting product requirements into bounded engineering tasks suitable for implementation;
- architecture and implementation planning for desktop, backend, website, CMS, billing, release, and mobile-readiness work;
- analyzing reported bugs, screenshots, behavior descriptions, and verification output;
- reviewing technical summaries, diffs, test results, release notes, and deployment notes prepared during AI-assisted work;
- identifying risks, missing checks, regression areas, and documentation that needed to be updated;
- preparing verification, build, release, backend deployment, rollback, and handoff instructions;
- maintaining continuity across the Windows client, backend, website, CMS, subscription model, and mobile planning.

GPT-5.6 was used to help reason about the work and prepare precise implementation instructions. It was not the final authority for product scope, architecture, billing, legal, release, or deployment decisions.

## Human Oversight

Human developers and maintainers remained responsible for the project. Humans:

- defined product requirements and priorities;
- selected and approved changes before they were treated as part of the product direction;
- reviewed AI output, including code, documentation, test results, and release notes;
- performed visual and product testing, including Windows application smoke testing where required;
- controlled Git commits and releases;
- handled production credentials and deployment access;
- made final architecture, billing, legal, and release decisions.

AI tools did not autonomously approve payments, publish releases, manage production secrets, or make final product decisions.

## Verification and Safety Boundaries

AI-assisted changes were expected to pass the relevant checks for their scope, such as:

- Git diff validation before handoff;
- `.NET` restore and builds when application or backend code was affected;
- automated tests and repository policy checks;
- desktop release gates where applicable;
- manual Windows application smoke testing for user-visible desktop behavior;
- backend health checks when backend changes were deployed;
- database migration review only when schema changes were intentionally made.

Safety and repository hygiene boundaries apply to AI-assisted work:

- no secrets should be placed in prompts, source code, documentation, logs, commits, or generated artifacts;
- generated installers, backend packages, SQL artifacts, and secret configuration files must not be committed;
- desktop, backend, website, database, and release operations are treated as separate deployment boundaries.

## Application Runtime AI Boundary

Codex and GPT-5.6 assisted the development process. They are separate from the learner-facing product runtime.

The Language Voice Tutor product uses backend-owned AI integrations for learner-facing functionality such as lesson replies, transcription, speech playback, hints, feedback, translation, or summaries when those features are enabled. The Windows client does not contain an OpenAI API key and does not call OpenAI directly. Backend configuration remains the source of truth for runtime model selection and provider credentials.

This document does not claim that GPT-5.6 is the learner-facing runtime model. Runtime model selection is controlled by backend configuration and operational deployment state.

## Representative Workflow

1. A human defines a narrowly scoped change.
2. GPT-5.6 helps analyze the requirement and prepare a bounded Codex task.
3. Codex inspects the repository and implements the approved scope.
4. Codex runs relevant automated verification and reports changed files.
5. A human reviews the diff and test output.
6. A human performs manual testing where required.
7. A human decides whether to commit, deploy, or release the change.

## Limitations

AI-generated suggestions, code, tests, and documentation can be incomplete or incorrect. They require review and verification. Use of Codex or GPT-5.6 does not guarantee correctness, security, accessibility, legal compliance, production readiness, or suitability for release.

## Related Documentation

- [Current State](CURRENT_STATE.md)
- [Next Steps](NEXT_STEPS.md)
- [Architecture Review](ARCHITECTURE_REVIEW.md)
- [Lesson Flow Review](LESSON_FLOW_REVIEW.md)
- [Voice and Realtime Review](VOICE_AND_REALTIME_REVIEW.md)
- [Manual Test Checklist](MANUAL_TEST_CHECKLIST.md)
- [Security Release Review](SECURITY_RELEASE_REVIEW.md)
- [Release Readiness Review](RELEASE_READINESS_REVIEW.md)
- [Windows Installer Release Flow](WINDOWS_INSTALLER_RELEASE_FLOW.md)
- [Backend Server Deployment](BACKEND_SERVER_DEPLOYMENT.md)
- [Pre-Mobile Readiness](PRE_MOBILE_READINESS.md)

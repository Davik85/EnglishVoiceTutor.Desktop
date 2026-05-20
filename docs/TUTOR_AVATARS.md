# Tutor Avatars

## Supported avatars

- `elena` (default)
- `nelli`

Tutor identity is loaded from `Content/Tutors/*.json` and remains separate from lesson scenario JSON in `Content/Lessons/**`.

## Avatar profiles

### Elena (`elena`)
- Display name: Elena
- Age: 22
- Role: fashion design student
- Interests: padel, art
- Personality: friendly, warm, supportive, natural
- Speaking style: short, clear, encouraging, conversational

### Nelli (`nelli`)
- Display name: Nelli
- Age: 18
- Role: future graphic design student
- Interests: drawing, computer games
- Personality: kind, cheerful, likes jokes
- Speaking style: friendly, playful, light, supportive

## Assets

Lesson avatar state animations are shared assets in:

- `Assets/Avatars/avatar-idle.gif`
- `Assets/Avatars/avatar-listening.gif`
- `Assets/Avatars/avatar-transcribing.gif`
- `Assets/Avatars/avatar-thinking.gif`
- `Assets/Avatars/avatar-speaking.gif`

Project settings include GIFs as WPF `Resource` in `EnglishVoiceTutor.Desktop.csproj`.

### Nelli GIF path

If product-specific tutor GIFs are introduced, place Nelli's GIF at:

- `Assets/Avatars/nelli.gif`

Current app behavior uses shared state animations and does not yet switch per tutor. If a tutor-specific GIF is missing in future wiring, keep fallback behavior to the existing shared avatar resources (and `Assets/Avatars/avatar-fallback.png` for image fallback).

## How to add a future avatar

1. Add a tutor option in `Models/TutorAvatarOptions.cs`.
2. Add localized card/profile text in `Models/TutorAvatarProfileLocalization.cs`.
3. Add a tutor profile JSON file in `Content/Tutors/<avatar-id>.json`.
4. Verify settings selection persists via existing `UserSettings.SelectedTutorAvatarId` pipeline.
5. Verify lesson chat display name and prompt personality come from selected tutor profile.
6. Keep lesson scenario JSON avatar-neutral (do not inject tutor identity into lesson JSON).

## Typical files to update

- `Models/TutorAvatarOptions.cs`
- `Models/TutorAvatarProfileLocalization.cs`
- `Content/Tutors/*.json`
- (Optional docs) `docs/TUTOR_AVATARS.md`

## Current limitations

- No CMS/admin avatar management yet (deferred).
- No dedicated per-tutor animation pipeline yet; lesson avatar animations are shared by state.

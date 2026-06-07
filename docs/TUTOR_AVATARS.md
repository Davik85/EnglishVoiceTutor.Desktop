# Tutor Avatars

## Supported avatars

- `elena` (default)
- `nelli`
- `david`

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
- City: Milan
- Role: future graphic design student
- Interests: drawing, computer games
- Personality: kind, cheerful, likes jokes
- Speaking style: friendly, playful, light, supportive

### David (`david`)
- Display name: David
- Age: 40
- City: Los Angeles
- Role: entrepreneur in the IT industry
- Interests: technology, startups
- Personality: friendly, cheerful, supportive
- Speaking style: clear, upbeat, encouraging, practical
- Preferred voice: Onyx (deep style hint; do not present voice labels as guaranteed speaker gender)

## Avatar state files

Each avatar can provide these files:

- `avatar-idle.gif`
- `avatar-listening.gif`
- `avatar-speaking.gif`
- `avatar-thinking.gif`
- `avatar-transcribing.gif`

## Folder structure

Preferred per-avatar structure:

- `Assets/Avatars/elena/`
- `Assets/Avatars/nelli/`
- `Assets/Avatars/david/`

Full expected David paths:

- `Assets/Avatars/david/avatar-idle.gif`
- `Assets/Avatars/david/avatar-listening.gif`
- `Assets/Avatars/david/avatar-speaking.gif`
- `Assets/Avatars/david/avatar-thinking.gif`
- `Assets/Avatars/david/avatar-transcribing.gif`

Full expected Nelli paths:

- `Assets/Avatars/nelli/avatar-idle.gif`
- `Assets/Avatars/nelli/avatar-listening.gif`
- `Assets/Avatars/nelli/avatar-speaking.gif`
- `Assets/Avatars/nelli/avatar-thinking.gif`
- `Assets/Avatars/nelli/avatar-transcribing.gif`

## Fallback order

For each avatar state, desktop resolves animation in this order:

1. selected avatar nested path (`Assets/Avatars/{avatarId}/avatar-{state}.gif`)
2. Elena nested path (`Assets/Avatars/elena/avatar-{state}.gif`)
3. legacy flat path (`Assets/Avatars/avatar-{state}.gif`)
4. safe app fallback (no crash; animated source can be empty when assets are unavailable)

Legacy flat GIF files in `Assets/Avatars/` are intentionally kept for compatibility.

## How to add a future avatar

1. Add a tutor option in `Models/TutorAvatarOptions.cs`.
2. Add localized card/profile text in `Models/TutorAvatarProfileLocalization.cs`.
3. Add a tutor profile JSON file in `Content/Tutors/<avatar-id>.json`.
4. Put per-state GIFs in `Assets/Avatars/<avatar-id>/`.
5. Verify settings selection persists via `UserSettings.SelectedTutorAvatarId` pipeline.
6. Verify lesson chat display name and prompt personality come from selected tutor profile.
7. Keep lesson scenario JSON avatar-neutral (do not hardcode tutor identity in lesson JSON).

## Voice-label caution

Voice labels are learner-facing style hints only and must not overclaim guaranteed speaker gender. David prefers Onyx by default, but the selected voice is still user-configurable in Settings and is passed through normal Lesson Chat TTS and Conversation Mode TTS.

## Deferred work

- CMS/admin avatar management is deferred.

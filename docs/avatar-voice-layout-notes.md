# Avatar, voice, and Conversation Mode layout notes

## Tutor avatars

The desktop tutor catalog includes Elena, Nelli, and David. David remains selectable and prefers the Onyx voice by default.

David's expected GIF folder is `Assets/Avatars/david/` with these per-state filenames:

- `avatar-idle.gif`
- `avatar-listening.gif`
- `avatar-speaking.gif`
- `avatar-thinking.gif`
- `avatar-transcribing.gif`

The WPF project includes `Assets/Avatars/**/*.gif` as resources, so correctly named David GIF files in that folder are included automatically. If David GIF files are missing, the existing avatar resolver falls back to Elena's available resources.

## Voice choices

Settings offers this shared safe built-in OpenAI voice catalog for normal Lesson Chat TTS and Conversation Mode TTS:

- Alloy — neutral style
- Ash — calm style
- Coral — warm style
- Echo — clear style
- Fable — expressive style
- Onyx — deep style
- Nova — bright style
- Sage — calm style
- Shimmer — soft style

These labels are style hints for learners and must not be presented as guaranteed speaker gender. The selected voice is persisted and passed into normal Lesson Chat TTS and Conversation Mode TTS; David prefers Onyx by default.

## Conversation Mode framing

Conversation Mode uses a smaller, start-screen-sized app window instead of the larger normal Lesson Chat window. The Conversation Mode avatar frame is smaller than the previous full-overlay frame and uses `UniformToFill` GIF rendering so Elena, Nelli, and David fill the visible frame without gray side bars. Conversation Mode also removes the global dimming overlay from the avatar frame so the GIF stays closer to its source brightness; readability is handled by localized semi-transparent message bubbles instead of darkening the whole image.

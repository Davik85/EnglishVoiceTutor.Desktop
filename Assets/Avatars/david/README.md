# David avatar assets

David is a selectable tutor avatar and prefers the Onyx voice by default. Drop David's per-state GIF files into this folder using the existing avatar naming convention:

- `avatar-idle.gif`
- `avatar-listening.gif`
- `avatar-speaking.gif`
- `avatar-thinking.gif`
- `avatar-transcribing.gif`

The desktop project includes `Assets/Avatars/**/*.gif` as WPF resources, so correctly named David GIF files in this folder are included in the app output automatically. Until these files are added, the desktop app falls back to Elena's available GIF resources.

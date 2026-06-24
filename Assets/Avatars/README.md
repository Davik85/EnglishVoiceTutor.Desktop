# Avatar assets

This folder contains animated Lesson Chat avatar GIF resources.

## Legacy shared files (still supported)

- `avatar-idle.gif`
- `avatar-listening.gif`
- `avatar-transcribing.gif`
- `avatar-thinking.gif`
- `avatar-speaking.gif`

## Preferred per-avatar layout

- `Assets/Avatars/lana/avatar-idle.gif`
- `Assets/Avatars/lana/avatar-listening.gif`
- `Assets/Avatars/lana/avatar-speaking.gif`
- `Assets/Avatars/lana/avatar-thinking.gif`
- `Assets/Avatars/lana/avatar-transcribing.gif`

- `Assets/Avatars/nelli/avatar-idle.gif`
- `Assets/Avatars/nelli/avatar-listening.gif`
- `Assets/Avatars/nelli/avatar-speaking.gif`
- `Assets/Avatars/nelli/avatar-thinking.gif`
- `Assets/Avatars/nelli/avatar-transcribing.gif`

- `Assets/Avatars/david/avatar-idle.gif`
- `Assets/Avatars/david/avatar-listening.gif`
- `Assets/Avatars/david/avatar-speaking.gif`
- `Assets/Avatars/david/avatar-thinking.gif`
- `Assets/Avatars/david/avatar-transcribing.gif`

Desktop resolution fallback order is:

1. selected avatar nested path
2. Lana nested path
3. legacy shared path in `Assets/Avatars/`
4. safe no-crash fallback when assets are unavailable

The desktop project includes `Assets/Avatars/**/*.gif` as WPF `Resource` items.

## Conversation Mode framing

Conversation Mode intentionally uses a smaller, start-screen-sized window and a smaller avatar frame than normal Lesson Chat. Avatar GIFs are displayed with preserved aspect ratio in Conversation Mode so Lana, Nelli, and David are not stretched aggressively while the overlay controls remain reachable.

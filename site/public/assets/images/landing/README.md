# Landing page images

Temporary SVG placeholders live in this folder for the public homepage split-screen panels.

Replace these files with final production images when ready, keeping the same filenames to avoid HTML changes:

- `windows-desktop-placeholder.svg` — laptop or desktop screenshot image for the Windows app panel.
- `mobile-placeholder.svg` — phone screenshot image for the future mobile app panel.

Recommended source images:

- Use wide images around 16:9 or 4:3 for the Windows panel.
- Use portrait phone-focused artwork around 9:16 for the mobile panel, with safe background space around the phone.
- Export optimized SVG, WebP, PNG, or JPG assets. If changing extensions, update `site/public/index.html`.
- The page uses `object-fit: cover`, so keep important content near the center of each image.

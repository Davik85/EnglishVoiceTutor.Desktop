# Landing page images

The public homepage split-screen panels load these final image filenames:

- `windows-desktop.webp` — replace this with the final laptop or desktop image for the Windows app panel.
- `mobile.webp` — replace this with the final mobile phone image for the future mobile app panel.

Keep these exact filenames when adding the final production images. Keeping the filenames unchanged means `site/public/index.html` does not need to be edited again.

Recommended images:

- Prefer optimized WebP files.
- Use images around `1600x1200` or `1920x1200` pixels.
- Keep the most important content near the center of the image.
- The homepage fits these images with `object-fit: cover`, so edges may be cropped depending on the visitor's screen size.

Replace the files in place when updating production artwork; do not change these filenames unless `site/public/index.html` is updated at the same time.

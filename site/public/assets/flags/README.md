# Flag assets

The accepted public static website flag assets are WebP files for the supported study-language display: English, French, German, Spanish, Italian, Portuguese.

Local repo paths:

- `site/public/assets/flags/gb.webp`
- `site/public/assets/flags/fr.webp`
- `site/public/assets/flags/de.webp`
- `site/public/assets/flags/es.webp`
- `site/public/assets/flags/it.webp`
- `site/public/assets/flags/pt.webp`

Production server folder: `/var/www/languagevoicetutor/site/assets/flags/`

Public URLs:

- `/assets/flags/gb.webp`
- `/assets/flags/fr.webp`
- `/assets/flags/de.webp`
- `/assets/flags/es.webp`
- `/assets/flags/it.webp`
- `/assets/flags/pt.webp`

Upload the static site with the repository script rather than a separate ad hoc copy flow:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-static-site.ps1 `
  -ServerHost "lvt-server" `
  -ServerUser "deploy" `
  -RemotePath "/var/www/languagevoicetutor/site"
```

Do not commit binary flag changes unless explicitly approved.

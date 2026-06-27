# Brand assets

The accepted public static website logo path is:

- Local repo path: `site/public/assets/brand/lvt-logo.png`
- Production server path: `/var/www/languagevoicetutor/site/assets/brand/lvt-logo.png`
- Public URL: `/assets/brand/lvt-logo.png`

Upload the static site with the repository script rather than a separate ad hoc copy flow:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-static-site.ps1 `
  -ServerHost "lvt-server" `
  -ServerUser "deploy" `
  -RemotePath "/var/www/languagevoicetutor/site"
```

Do not commit binary logo changes unless explicitly approved.

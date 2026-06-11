# Application icon placeholder

Binary icon files are intentionally not committed in this branch. Before generating or packaging a Windows release, add the real user-provided icon source image here:

```text
Assets/Branding/app-icon-source.png
```

Then generate the Windows icon file with:

```powershell
scripts/generate-app-icon.ps1
```

The generated output must be:

```text
Assets/Branding/app-icon.ico
```

The `.ico` file must include these Windows icon sizes: 16x16, 24x24, 32x32, 48x48, 64x64, 128x128, and 256x256.

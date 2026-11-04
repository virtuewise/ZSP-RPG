# Tools

## `strip_unneeded.ps1`

Creates `../StrippedSource/` containing only the reusable Unity asset bundle
(`assets/bin/Data/data.unity3d`) and a `REMOVED_ITEMS.txt` report. It never
modifies the original APK folder.

```powershell
powershell -ExecutionPolicy Bypass -File .\strip_unneeded.ps1 `
  -Source "C:\path\to\ZIMBABWE THE SHADOW PROTOCOL"
```

## Extraction scripts (repo root)

The one-off exporters used to build `Assets/Extracted/` live at the repository
root:

| Script | Output |
|---|---|
| `extract_assets.py` | Meshes, textures, materials, animations, controllers, shaders, text |
| `extract_audio.py` | 226 `.fsb` audio banks (slice from the `.resource` streams) |
| `extract_scenes.py` | 8 scene hierarchy manifests |

They contain absolute paths for this machine; edit the `ROOT` / `BUNDLE`
constants at the top before re-running on another machine.

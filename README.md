# TLDAccessibility v1.0

TLDAccessibility provides screen reader and SAPI5 narration for The Long Dark (Unity 6) via MelonLoader.

## v1.0 Scope

- Narration for menus, HUD, inventory, dialog, notifications, and common UI interactions.
- Speech backends:
  - **Screen reader output** via Tolk (NVDA, JAWS, Narrator, etc.).
  - **SAPI5** fallback for direct speech output.
- Configurable settings with profiles (JSON or optional ModSettings integration).
- Hotkeys for repeat, stop speech, read screen, and status summary.

## Dependencies

- **MelonLoader 0.7.2 nightly** (required for The Long Dark Unity 6 build).
- **.NET 6 runtime** (required).
- **ModSettings** (optional, recommended): enables in-game settings UI. JSON settings file is always supported.

## Install

1. Install MelonLoader 0.7.2 nightly for The Long Dark (Unity 6).
2. Install the .NET 6 runtime.
3. (Optional) Install ModSettings.
4. Copy the contents of `dist/` into your game folder so that the files land in the **root** `Mods` directory (MelonLoader only scans `Mods/` and does **not** detect subfolders like `Mods/TLDAccessibility/`). Place `TLDAccessibility.dll` directly in `<TLD>\Mods\`.

```
Mods/
  TLDAccessibility.dll
  Tolk.dll (optional, only if using screen readers)
UserData/
  TLDAccessibility/
    settings.json
```

## Usage

- Launch the game. TLDAccessibility will speak UI narration when it detects supported elements.
- Edit `settings.json` to customize profiles, hotkeys, verbosity, and speech backends.
- If ModSettings is installed, use the in-game ModSettings UI and the JSON file will stay in sync.

### Recommended configuration

- **Screen reader users**: set `speech.backendMode` to `screenReader` and ensure `Tolk.dll` is present.
- **SAPI5 users**: set `speech.backendMode` to `sapi5` and select a `sapi5VoiceName` if needed.

## Troubleshooting

### Load verification checklist

- Check `<TLD>\MelonLoader\Latest.log`.
- Search for `TLDAccessibility` and `Loading Melons`.
- If those entries are not present at all, the mod was not discovered (likely missing attributes or installed in the wrong folder).
- If the entries are present but show errors, paste the exception section into an issue.

- **No speech output**:
  - Verify MelonLoader 0.7.2 nightly is installed and the mod loads without errors.
  - Ensure the .NET 6 runtime is installed.
  - For screen reader output, confirm `Tolk.dll` is present and your screen reader is running.
  - For SAPI5, confirm Windows voices are installed and `speech.backendMode` is set to `sapi5`.
- **Settings not applying**:
  - Confirm `UserData/TLDAccessibility/settings.json` is valid JSON.
  - If ModSettings is installed, verify it is updated and enabled.
- **Some UI elements are silent**:
  - Narration coverage is limited to supported UI adapters; some screens and popups are not yet wired.

## Known Limitations (v1.0)

- Windows-only (relies on .NET 6, SAPI5, and/or Tolk).
- Narration coverage is incomplete; some screens may not announce all elements.
- Screen reader output depends on Tolk support and the screen reader being detected.

# TLDAccessibility Settings Schema (v1.0)

This document describes the JSON structure used by `Mods/TLDAccessibility/settings.json` and the meaning of each setting. The settings file is created automatically with defaults if it does not exist.

## Root

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `profiles` | array | yes | Named profiles containing all settings. |
| `activeProfileName` | string | yes | Profile name to apply at runtime. |

## Profile

Each entry in `profiles` has the following structure:

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `name` | string | yes | Unique profile name. |
| `speech` | object | yes | Speech backend and SAPI5 configuration. |
| `verbosityLevel` | int | yes | Range `1..5` (lower = less verbose). |
| `categories` | object | yes | Category enable flags + debounce times. |
| `interruptPolicy` | object | yes | Interrupt behavior settings. |
| `hotkeys` | object | yes | Hotkey bindings and enable toggles. |

### Speech

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `backendMode` | string | yes | `auto`, `screenReader`, or `sapi5`. |
| `sapi5VoiceName` | string | yes | Voice name to select (empty uses default). |
| `sapi5Rate` | int | yes | Speech rate (SAPI5 standard range, usually `-10..10`). |
| `sapi5Volume` | int | yes | Volume `0..100`. |

### Categories

Each category has the same shape:

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `enabled` | bool | yes | Enable/disable narration for the category. |
| `debounceMilliseconds` | int | yes | Minimum time between messages for the category. |

Categories:
`hud`, `inventory`, `ui`, `world`, `combat`, `dialog`, `notifications`.

### Interrupt Policy

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `allowInterruptByHigherPriority` | bool | yes | Allow higher priority messages to interrupt. |

### Hotkeys

Each hotkey has the same shape:

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `enabled` | bool | yes | Enable/disable the hotkey. |
| `keybind` | string | yes | Keybind string. |

Hotkeys:
`repeatLast`, `stopSpeech`, `readScreen`, `readStatusSummary`, `dumpDiagnostics`.

## Example

```json
{
  "profiles": [
    {
      "name": "Default",
      "speech": {
        "backendMode": "auto",
        "sapi5VoiceName": "",
        "sapi5Rate": 0,
        "sapi5Volume": 100
      },
      "verbosityLevel": 3,
      "categories": {
        "hud": { "enabled": true, "debounceMilliseconds": 250 },
        "inventory": { "enabled": true, "debounceMilliseconds": 250 },
        "ui": { "enabled": true, "debounceMilliseconds": 250 },
        "world": { "enabled": true, "debounceMilliseconds": 250 },
        "combat": { "enabled": true, "debounceMilliseconds": 250 },
        "dialog": { "enabled": true, "debounceMilliseconds": 250 },
        "notifications": { "enabled": true, "debounceMilliseconds": 250 }
      },
      "interruptPolicy": {
        "allowInterruptByHigherPriority": true
      },
      "hotkeys": {
        "repeatLast": { "enabled": true, "keybind": "F9" },
        "stopSpeech": { "enabled": true, "keybind": "F10" },
        "readScreen": { "enabled": true, "keybind": "F6" },
        "readStatusSummary": { "enabled": true, "keybind": "F7" },
        "dumpDiagnostics": { "enabled": false, "keybind": "" }
      }
    }
  ],
  "activeProfileName": "Default"
}
```

# 🌐 Browser Controller: Deep Tab & Media Automation

This specification documents the browser orchestration subsystem of **`win-chauffeur`**.

---

## 🏛️ 1. Modern Chromium Browser Architecture

On Windows 11, Microsoft Edge and Google Chrome operate on a **Single-Instance Multi-Process Architecture**:

```text
┌────────────────────────────────────────────────────────┐
│ Browser Master Process (PID: 1876 on winsta0\default)  │
│ ├── Network Service (mojom.NetworkService)             │
│ ├── Storage Service (mojom.StorageService)             │
│ ├── Audio Service (mojom.AudioService)                 │
│ └── GPU Acceleration Process                           │
└────────────────────────────────────────────────────────┘
          ▲                                 ▲
          │ Chromium Mojo IPC               │ Win32 Window Focus
┌──────────────────┐               ┌──────────────────┐
│ Tab 1: GitHub    │               │ Tab 2: YouTube   │
│ (Active Renderer)│               │ (Media Audio On) │
└──────────────────┘               └──────────────────┘
```

When a command attempts to launch a URL via `msedge.exe https://...`, Windows hands the request via Chromium IPC pipes to the existing master browser process on `winsta0\default`.

---

## 🗂️ 2. Tab Discovery & Awareness

Win-Chauffeur determines open browser state through a dual-channel inspection:

### Channel A: Desktop Window Title Telemetry
Windows dynamically appends background tab counts to the title bar of the main browser window:
* Example: `"Malang Sajna - YouTube and 1 more page - Personal - Microsoft Edge"`
* Parsing this title string gives instant awareness that **2 tabs** are active, with `Malang Sajna` currently selected.

### Channel B: Visual Strip Perception
By slicing the top 80 pixels (`assets/references/edge_tabs_strip.png`), the visual perception engine reads every tab label, sound badge (e.g. 🔊 playing audio), and close button `x` coordinate with sub-pixel precision.

---

## ⌨️ 3. High-Velocity Tab Navigation & Hotkey Macros

Rather than injecting heavy browser extensions or invasive drivers, Win-Chauffeur controls the active browser using native Win32 keyboard dispatch to the focused HWND:

| Action | Win32 Shortcut Sequence | Purpose |
| :--- | :--- | :--- |
| **Switch to Tab N** | `Ctrl + <1..8>` | Selects tab 1 through 8 instantly. |
| **Switch to Last Tab** | `Ctrl + 9` | Jumps straight to the rightmost tab. |
| **Next / Prev Tab** | `Ctrl + Tab` / `Ctrl + Shift + Tab` | Cycles through adjacent tabs. |
| **Close Active Tab** | `Ctrl + W` | Closes current tab with zero prompt. |
| **Reopen Closed Tab** | `Ctrl + Shift + T` | Recovers accidentally closed tabs. |
| **New Tab** | `Ctrl + T` | Opens blank tab or search bar. |
| **Duplicate Tab** | `Alt + D` $\rightarrow$ `Alt + Enter` | Duplicates active page. |

---

## 🎵 4. Media Playback Control (YouTube / Spotify / Web Audio)

Win-Chauffeur controls playback through two deterministic layers:

1. **Global Hardware Media Key (`VK_MEDIA_PLAY_PAUSE = 0xB3`)**:
   * Fires a global Windows media key event that directly toggles playback across any active audio session without needing the browser to be focused.
2. **In-Page YouTube Shortcuts**:
   * When Edge/Chrome is focused, dispatching `'k'` toggles Play/Pause.
   * Dispatching `'j'` rewinds 10 seconds; `'l'` fast-forwards 10 seconds.
   * Dispatching `'m'` toggles mute.

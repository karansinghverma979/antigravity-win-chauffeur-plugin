# 🎬 Win-Chauffeur: Real-World Scenarios & End-to-End Mental Model (Vol. 01)

> **Practical mental model, operational lifecycle, and step-by-step execution scenarios demonstrating how Win-Chauffeur acts as the eyes and hands on Windows 11.**

---

## 🏛️ 1. The Core Mental Model: The 3-Part Organism

Antigravity natively operates in a headless background sandbox (`exebox`) without a display. **Win-Chauffeur** provides the physical body connecting the AI brain to Karan's physical Motobook display (`winsta0\default`):

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. THE BRAIN (Antigravity LLM Core)                                         │
│    Understands user intent, reasons about UI layouts, parses goals.         │
│    Outputs structured tool calls: chauffeur_tile, chauffeur_look, etc.     │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ High-Level JSON-RPC Call (<5ms)
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. THE CHAUFFEUR ENGINE (Local Win32 Service on winsta0\default)            │
│    Permanently attached to the physical interactive desktop session.        │
│                                                                             │
│    👀 THE EYES (Sub-50ms Perception Engine)                                 │
│       • Pulls raw frames directly from GPU display buffer (GDI / DXGI).     │
│       • Feeds instant visual grounding back into the multimodal model.      │
│                                                                             │
│    🖐️ THE HANDS (Win32 Action Engine)                                       │
│       • Moves the cursor, injects hardware key events, tiles windows.       │
│       • Calls Win32 SetWindowPos, MoveWindow, and Explorer COM interfaces.  │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ Direct OS Win32 API Manipulation
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 3. THE PHYSICAL DESKTOP (Windows 11 Motobook Display)                       │
│    Active applications, browser tabs, and windows update in real time       │
│    on your 2.8K 120Hz OLED display with zero latency or screen flicker.     │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 2. Concrete End-to-End Scenarios

### 📐 Scenario 1: Programmatic Window Snapping & Tiling
* **User Request**: *"Tile my terminal to the left, and browser to the right."*
* **What Happens Under the Hood**:
  1. **The Brain**: Calls `chauffeur_tile({ left: "terminal", right: "edge" })`.
  2. **The Hands**:
     * Queries `EnumDesktopWindows` on `winsta0\default` to locate active HWNDs for `WindowsTerminal.exe` and `msedge.exe`.
     * Calculates the active work area dimensions via `SystemParametersInfo(SPI_GETWORKAREA)`: `Width = 2880px` (or `1440px`), `Height = 1800px` (or `900px`), automatically subtracting the taskbar height.
     * Calls Win32 `MoveWindow(hTerminal, 0, 0, Width/2, Height, true)` to snap the terminal across the left half.
     * Calls Win32 `MoveWindow(hEdge, Width/2, 0, Width/2, Height, true)` to snap the browser across the right half.
  3. **Visual Result on Your Screen**: In under **15 milliseconds**, your windows snap into a clean side-by-side developer workspace without you ever touching your mouse.

---

### 👁️ Scenario 2: Visual Perception & Autonomous UI Clicking
* **User Request**: *"Look at my screen, click the 'Skip Ad' button on YouTube."*
* **What Happens Under the Hood**:
  1. **The Eyes**:
     * Calls `chauffeur_capture()`. In under 40 milliseconds, the GDI/DXGI engine pulls the current display buffer.
     * The multimodal model analyzes the screenshot and identifies the "Skip Ad" button at pixel coordinates `(X: 1140, Y: 720)`.
  2. **The Hands**:
     * Calls `chauffeur_click({ x: 1140, y: 720 })`.
     * Win32 `SetCursorPos(1140, 720)` moves the mouse pointer instantly.
     * `mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP)` clicks the button.
  3. **Verification Loop**:
     * The engine takes a sub-second follow-up snapshot confirming the ad banner is gone and the main video is playing.

---

### 🌌 Scenario 3: Virtual Desktop Teleportation
* **User Request**: *"Move this heavy compiler / browser to Virtual Desktop 2 so it stops distracting me."*
* **What Happens Under the Hood**:
  1. **The Brain**: Calls `chauffeur_desktop_move({ window: "edge", target_desktop: 2 })`.
  2. **The Hands**:
     * Resolves the HWND of the target application.
     * Interops with Windows 11's internal COM service (`IVirtualDesktopManager::MoveWindowToDesktop`).
     * Teleports the window to Virtual Desktop 2's GUID.
  3. **Visual Result on Your Screen**: Edge instantly vanishes from your primary workspace without terminating the process or losing your open tabs. Your primary display returns to 100% focused flow.

---

### 🚀 Scenario 4: Sovereign Media & Asset Launching
* **User Request**: *"Open the photo of Archana / play Malang Sajna."*
* **What Happens Under the Hood**:
  1. **Pre-Launch Sweep**: Win-Chauffeur checks for any suspended background `Photos.exe` ghost instances and purges them.
  2. **Out-of-Process Dispatch**: Routes execution via `CLSID_ShellWindows` (`{9BA05972-F6A8-11CF-A442-00A0C90A8F39}`) directly into `explorer.exe` (PID 9376).
  3. **Visual Result on Your Screen**: The photo opens in Windows Photos with an active, visible window handle (`HWND`) on your physical monitor—not trapped in background sandbox memory.

---

## ⚡ 3. The Evolutionary Leap: Ugly vs Ultimate Goal

| Dimension | The "Ugly" Method (Before Win-Chauffeur) | The Ultimate Goal (With Win-Chauffeur) |
| :--- | :--- | :--- |
| **Execution Path** | Compiles 50-line C# scripts inside ad-hoc PowerShell strings on every turn. | Pre-compiled, persistent native Win32/COM engine (<10ms). |
| **Desktop Station** | Runs in isolated `exebox` sandbox; crashes with kernel error `0xC0000142`. | Permanently hooked to `winsta0\default` (your interactive physical screen). |
| **Ghost Population** | Leaves behind suspended UWP ghost instances (`Photos.exe`, `rundll32`). | Enforces deterministic lifecycle management; zero orphan ghost population. |
| **Visual Awareness** | Agent is blind; cannot verify if windows actually popped up or failed. | Real-time eyes (<50ms screen captures) provide direct visual grounding. |
| **Window Layout** | Zero ability to arrange, tile, resize, or snap applications. | Autonomous window manager handling grids, multi-monitors, and virtual desktops. |

---

## 💬 4. Open Topics for Next Deep-Dive Discussion

1. **Input Priority & Focus Guarding**: How strictly should Win-Chauffeur prevent mouse/cursor jumps while you are actively typing in Neovim/Terminal?
2. **Virtual Desktop Management Strategy**: Do you prefer dedicated named desktops (e.g. `Desktop 1: Code`, `Desktop 2: Browser/Media`, `Desktop 3: Background Tasks`)?
3. **Trigger Interfaces**: How do you want to invoke Win-Chauffeur during high-velocity coding? (e.g. natural chat commands, CLI slash commands like `/chauffeur tile`, or keyboard macro shortcuts).

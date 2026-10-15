# 🖥️ Display & Virtual Desktops Architecture

This specification documents the multi-display and virtual desktop governance engine of **`win-chauffeur`**.

---

## 🏛️ 1. The Multi-Display Topology

Windows treats multiple monitors as a unified virtual coordinate space spanned by a primary origin `(0, 0)`:

```text
┌──────────────────────────────────────┐  ┌──────────────────────────────────────┐
│ Monitor 1: Primary (OLED 2.8K)       │  │ Monitor 2: External Display          │
│ Origin: (0, 0)                       │  │ Origin: (2880, 0)                    │
│ Res: 2880 x 1800 @ 120Hz (200% DPI)  │  │ Res: 1920 x 1080 @ 60Hz (100% DPI)   │
└──────────────────────────────────────┘  └──────────────────────────────────────┘
└──────────────────────── Virtual Desktop Canvas ────────────────────────────────┘
```

### Win32 Query Primitives:
* **`EnumDisplayMonitors`**: Iterates all active physical and virtual display adapters.
* **`GetMonitorInfo`**: Retrieves physical bounds (`rcMonitor`) and work area excluding taskbar (`rcWork`).
* **`GetSystemMetrics`**:
  * `SM_CXVIRTUALSCREEN` / `SM_CYVIRTUALSCREEN`: Total width/height of the multi-monitor bounding box.
  * `SM_XVIRTUALSCREEN` / `SM_YVIRTUALSCREEN`: Coordinates of the top-left corner of the virtual canvas.
* **Per-Monitor DPI (`GetDpiForMonitor` / `GetDpiForWindow`)**:
  * Critical on Motobook: High-DPI 2.8K OLED requires DPI awareness to avoid blurry coordinate calculations when moving windows between scaled displays.

---

## 🪟 2. Virtual Desktops (`IVirtualDesktopManager`)

Windows 11 isolates user workspaces into Virtual Desktops. Applications live on a specific virtual desktop GUID.

### The COM Interface Architecture:
```csharp
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("a5e369c4-e6b5-4caa-925f-643632b86646")]
public interface IVirtualDesktopManager {
    bool IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow);
    Guid GetWindowDesktopId(IntPtr topLevelWindow);
    void MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}
```

### Teleporting Windows Between Virtual Desktops:
1. Identify the target window handle (`HWND`).
2. Query target virtual desktop GUID via `IVirtualDesktopManagerInternal` or Windows shell settings.
3. Call `MoveWindowToDesktop(hWnd, ref targetGuid)`:
   * Instantly slides the application to the target virtual desktop with zero UI flicker.
   * Teleports heavy builds, background browser tabs, or reference materials away from Karan's primary focus workspace.

---

## 🛡️ 3. The Window Station & Sandbox Isolation Boundary

Windows separates graphical subsystems into **Window Stations** and **Desktops**:

| Window Station / Desktop | Purpose | Accessibility |
| :--- | :--- | :--- |
| **`winsta0\default`** | **The Interactive Physical Desktop** mapped to display output, GPU, mouse, and keyboard. | Owned by `explorer.exe` (Session 1). |
| **`exebox-<hash>`** | **The Antigravity Background Sandbox** created to isolate automated CLI tasks. | Subprocess child token; blocked from GUI output (`0xC0000142`). |

### The Chauffeur Transition Protocol:
To interact with windows across the boundary without kernel termination:
1. Open handle to `winsta0` via `OpenWindowStation("winsta0", true, GENERIC_ALL)`.
2. Bind calling thread via `SetProcessWindowStation(hWinsta)`.
3. Open desktop handle to `default` via `OpenDesktop("default", 0, true, GENERIC_ALL)`.
4. Switch thread desktop via `SetThreadDesktop(hDesk)` in an uninitialized STA worker thread.
5. All downstream Win32 and UI Automation operations execute with native desktop rights.

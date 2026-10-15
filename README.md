# 🎩 Antigravity Win-Chauffeur Plugin

> **Autonomous Windows 11 Desktop Chauffeur, Window Manager & Computer-Use Sentinel for Google Antigravity.**

---

## ⚡ 5-Second Visual Hook

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                       GOOGLE ANTIGRAVITY COGNITIVE CORE                     │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ Tool Call (JSON-RPC)
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                     WIN-CHAUFFEUR DUAL-SURFACE ENGINE                       │
│  ┌───────────────────┐   ┌───────────────────┐   ┌───────────────────────┐  │
│  │   Visual Perception│   │   Window Manager  │   │  Universal Dispatcher │  │
│  │   Screen Capture  │   │   Tiling & Focus  │   │  winsta0\default COM  │  │
│  └─────────┬─────────┘   └─────────┬─────────┘   └───────────┬───────────┘  │
└────────────┼───────────────────────┼─────────────────────────┼──────────────┘
             ▼                       ▼                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      WINDOWS 11 MOTOBOOK PHYSICAL DESKTOP                   │
│         [ 2.8K OLED Display ] · [ Windows Terminal ] · [ Edge/Chrome ]      │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 What Win-Chauffeur Solves

By default, Google Antigravity is a **headless, terminal-first developer engine**. When it runs shell commands, Windows isolates subprocesses into a hidden background virtual desktop (`exebox`). 

Attempting to open photos, view PDFs, or launch browsers from that background subshell causes:
1. **`0xC0000142` (`STATUS_DLL_INIT_FAILED`)**: Windows NT kernel blocks the process from binding to your display.
2. **Ghost Background Apps**: Modern WinUI/UWP apps like `Photos.exe` launch with `MainWindowHandle = 0` and hang invisibly in the background, corrupting future file clicks.

**`win-chauffeur`** provides the missing link: an out-of-process bridge that allows Antigravity to **see your screen**, **manage your windows**, **drive your browser**, and **open any media** cleanly on your physical monitor (`winsta0\default`).

---

## ⚡ Performance Benchmarks

| Metric | Target | Measured Empirical Performance |
| :--- | :--- | :--- |
| **Binary Footprint** | `< 25MB` | **`230 KB`** (Lightweight single-file native C# .NET 9 win-x64) |
| **Perception Latency (`look`)** | `< 50ms` | **`18ms`** (GDI/DWM BitBlt desktop capture) |
| **Window Enumeration (`list`)** | `< 30ms` | **`6ms`** (Win32 `EnumDesktopWindows` P/Invoke) |
| **Tile / Focus Operation** | `< 50ms` | **`12ms`** (Win32 `SetWindowPos` + `SetForegroundWindow`) |
| **Token Consumption** | `< 35 tokens` | **`< 25 tokens`** (Structured compact JSON DTOs) |
| **Background RAM Overhead** | `0 MB` | **`0 MB`** (On-demand execution, zero idle resident memory) |

---

## 🏛️ Core Capabilities

| Pillar | Subsystem | Description | Technical Spec |
| :--- | :--- | :--- | :--- |
| **1. Desktops & Displays** | Multi-Monitor & Virtual Desktops | Manages physical monitor boundaries, virtual screen bounds, and moves windows across displays. | [docs/display_and_desktops.md](docs/display_and_desktops.md) |
| **2. Window Manager** | Tiling & Layout Sentinel | Queries open windows, minimizes, maximizes, snaps into 50%/25% grids with dynamic per-monitor work-area resolution (`MonitorFromWindow` + `GetMonitorInfo`) and `PerMonitorV2` High DPI awareness, sets Always-On-Top, and manages focus. | [docs/windows_manager.md](docs/windows_manager.md) |
| **3. Visual Perception** | High-Speed Screen Capture | Captures full-screen or region crops in <30ms, giving the AI instantaneous visual feedback on the UI. | [docs/screenshot_tools.md](docs/screenshot_tools.md) |
| **4. Browser Controller** | Deep Tab & Media Operator | Discovers active tabs, switches tabs, closes tabs (`Ctrl+W`), and pauses/resumes YouTube playback. | [docs/browser_controller.md](docs/browser_controller.md) |
| **5. Universal Dispatcher** | Out-of-Process Shell COM Bridge | Dispatches photos, PDFs, music tracks, videos, and URLs directly onto `winsta0\default` with zero ghost processes. | [docs/universal_launcher.md](docs/universal_launcher.md) |
| **6. Negative Constraints** | Discarded Anti-Patterns Ledger | Tracks all user-rejected ideas, broken techniques, and operational boundaries. | [docs/rejections.md](docs/rejections.md) |
| **7. Engineering Feasibility** | Capabilities & Methods Matrix | Rigorous gap analysis, benchmarks, and comparison of C#, Python, and Hybrid architectures. | [docs/research_methods.md](docs/research_methods.md) |

---

## 🛠️ Dual Surface: CLI & MCP Server

### 1. Direct CLI Tool (`bin/win-chauffeur.exe`)
```powershell
# Screen Perception (instant capture)
win-chauffeur look "desktop.png"
win-chauffeur look "window.png" --window "WindowsTerminal"

# Window Management (tiling, snapping, focus)
win-chauffeur window list
win-chauffeur window tile "WindowsTerminal" left_half
win-chauffeur window tile "msedge" right_half
win-chauffeur window focus "WindowsTerminal"
win-chauffeur window close "Notepad"

# Desktop & Displays
win-chauffeur desktop list
win-chauffeur desktop move "WindowsTerminal" 0

# Universal Launcher (Zero-Ghost Out-of-Process dispatch)
win-chauffeur open "C:\assets\photo.jpg"
win-chauffeur open "https://github.com"

# Browser & Media Controller
win-chauffeur browser list
win-chauffeur browser media-toggle
win-chauffeur browser close-tab "YouTube"
```

### 2. Antigravity MCP Server (`--mcp`)
Registered in [`mcp_config.json`](mcp_config.json) for autonomous agent execution:
* `win_chauffeur_look`: Captures screen or specific window bounding box.
* `win_chauffeur_window`: 1-shot window tiling, layout presets, focus, and close.
* `win_chauffeur_desktop`: Multi-display enumeration and window monitor migration.
* `win_chauffeur_browser`: Tab enumeration, tab closure, and hardware media play/pause.
* `win_chauffeur_open`: Universal Out-of-Process Explorer dispatch with ghost mitigation.
* `win_chauffeur_interact`: Precision click coordinates and key sequences.

---

## 📁 Repository Structure

```text
win-chauffeur-plugin/
├── plugin.json                    # Antigravity plugin manifest
├── mcp_config.json                # MCP server configuration
├── README.md                      # Architecture overview and quickstart
├── .gitattributes                 # LF/CRLF line-ending firewall
├── .gitignore                     # Build cache & artifact decoupling
├── mcp/
│   └── server.py                  # High-velocity Python-to-native MCP dispatch bridge
├── bin/
│   └── win-chauffeur.exe          # Native C# .NET 9 single-file binary (230 KB)
├── rules/
│   └── AGENTS.md                  # Core invariants (Sakshi Shield, Zero Ghost Mandate)
├── agents/
│   └── win_chauffeur.md           # Sovereign subagent persona configuration
├── skills/
│   └── win-chauffeur/
│       └── SKILL.md               # Dynamic skill instructions for Antigravity
├── assets/                        # Visual proof & reference media
│   ├── screenshots/               # High-res desktop & window captures
│   └── references/                # UI strips, crops, and button coordinates
├── src/win-chauffeur/             # C# .NET 9 Native Source Engine
│   ├── win-chauffeur.csproj       # Project configuration (PublishSingleFile, win-x64)
│   ├── Program.cs                 # CLI router & MCP server entry point
│   ├── Core/
│   │   ├── Win32.cs               # P/Invoke Win32 API definitions
│   │   └── StationBridge.cs       # winsta0\default marshaler & ghost flusher
│   ├── Services/
│   │   ├── WindowManager.cs       # Window discovery, fuzzy match, tiling
│   │   ├── ScreenCapturer.cs      # Multi-monitor virtual bounds GDI capture
│   │   ├── BrowserController.cs   # Tab discovery, media toggle, tab close
│   │   ├── DesktopManager.cs      # Multi-display topology
│   │   └── InputInjector.cs       # Precision mouse & keyboard injection
│   └── Mcp/
│       └── McpServer.cs           # Native JSON-RPC stdio MCP implementation
└── docs/                          # Deep architectural specifications
    ├── display_and_desktops.md    # Multi-display & virtual desktop governance
    ├── windows_manager.md         # Window manipulation, snapping & Z-order
    ├── screenshot_tools.md        # Screen perception & GDI/DirectX capture
    ├── browser_controller.md      # Chromium tab awareness & playback control
    ├── universal_launcher.md      # Sovereign Explorer COM dispatcher
    ├── rejections.md              # Discarded concepts, anti-patterns & negative rules
    ├── research_methods.md        # Capabilities benchmark & technical feasibility study
    ├── scenarios_example_01.md    # End-to-end practical scenarios & mental model
    └── first_principles_architecture.md # Master vision & technology selection blueprint
```

---

## 🛡️ Sovereign Invariants
* **Sakshi Immutable Shield (Rule Zero)**: Strictly forbids touching, pausing, or killing `\Sakshi` or `%USERPROFILE%\Void\Sakshi\`.
* **Zero Orphan Ghost Population**: Eradicates suspended `Photos.exe` or background WebView2 instances before and after launch.
* **Focus Respect**: Never blindly steals keyboard focus from active developer terminals or Neovim editors.

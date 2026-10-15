---
name: win-chauffeur
description: "Autonomous Windows 11 Desktop Chauffeur, Window Manager & Computer-Use Sentinel. Use whenever controlling Windows GUI windows, querying displays or virtual desktops, taking screenshots, driving the browser, launching visual media onto the active monitor, or managing desktop window layouts."
---

# 🎩 Win-Chauffeur: Windows Desktop Orchestrator Skill

Win-Chauffeur provides Antigravity with full visual perception and physical control over the Windows 11 desktop GUI (`winsta0\default`).

---

## 🚀 1. When to Activate This Skill

Activate **`win-chauffeur`** whenever the user asks to:
1. **Take Screenshots or "See" the Screen**: Capture full desktop, active window, or specific screen coordinates.
2. **Manage Windows**: Minimize, maximize, tile, snap, resize, hide, bring to front, or move windows across monitors.
3. **Orchestrate Desktops & Displays**: Switch virtual desktops, query multi-monitor bounds/DPI, or teleport windows between virtual desktops.
4. **Control the Browser**: Check open tabs, close specific tabs, pause/play web media (YouTube), or navigate URLs.
5. **Open Files / Visual Media**: Open photos, PDFs, markdown previews, music tracks, or videos on the user's monitor via the Explorer COM bridge.
6. **Eradicate Ghost Processes**: Hunt down and kill suspended background GUI processes (`Photos.exe`, `rundll32`, orphaned WebView2 instances).

---

## 📚 2. Core Architecture Documentation Sitemap

For technical implementation specifics and Win32 / COM interfaces, consult the corresponding guides in `docs/`:

* **Displays & Virtual Desktops**: [docs/display_and_desktops.md](../../docs/display_and_desktops.md)
* **Master Window Manager**: [docs/windows_manager.md](../../docs/windows_manager.md)
* **Visual Perception & Screenshot Tools**: [docs/screenshot_tools.md](../../docs/screenshot_tools.md)
* **Browser Controller & Tab Operator**: [docs/browser_controller.md](../../docs/browser_controller.md)
* **Universal Desktop Dispatcher**: [docs/universal_launcher.md](../../docs/universal_launcher.md)
* **Negative Constraints & Rejections**: [docs/rejections.md](../../docs/rejections.md)
* **Engineering Research & Feasibility Matrix**: [docs/research_methods.md](../../docs/research_methods.md)
* **Practical Scenarios & Mental Model (Vol. 01)**: [docs/scenarios_example_01.md](../../docs/scenarios_example_01.md)
* **First-Principles Architecture & Master Vision**: [docs/first_principles_architecture.md](../../docs/first_principles_architecture.md)




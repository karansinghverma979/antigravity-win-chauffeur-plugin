# 🚫 Rejections & Negative Constraints Ledger

*This document is the authoritative ledger of rejected approaches, banned anti-patterns, discarded concepts, and operational "DO NOT DO" boundaries for **`win-chauffeur`**. Updated continuously during development to prevent regression.*

---

## 🏛️ 1. Conceptual & Naming Anti-Patterns

### ❌ Rejection 1: Abstract Sci-Fi & Mythological Buzzwords
* **Rejected Names**: `win-drishti`, `sutradhar`, `oculus`, `wraith`, `phantom`, `synapse`, `pratyaksha`, `vayu`, `chakshu`, `cortex`, `reflex`, `actuator`.
* **Why Rejected**: Over-engineered, pretentious, and disconnected from Karan's utilitarian workstation aesthetic (`Void`, `Apex`, `Blaze`, `moto-mate`, `win-janitor`).
* **Accepted Direction**: **`win-chauffeur`** — gritty, functional, blue-collar utility. A chauffeur drives the Windows UI with precision, zero ego, and total obedience.

---

## ⚠️ 2. Architectural & Execution Anti-Patterns

### ❌ Rejection 2: In-Process `Shell.Application` Launch Trap
* **The Failure**: Calling `(New-Object -ComObject Shell.Application).ShellExecute(...)` directly from PowerShell.
* **Why Rejected**: `Shell.Application` resolves to `InProcServer32` (`shell32.dll`) inside the calling process. It inherits the hidden `exebox` virtual desktop, trapping child processes in background purgatory.
* **Invariant**: Never use direct in-process `Shell.Application` for visible GUI operations. Always marshal out-of-process via `CLSID_ShellWindows` (`{9BA05972-F6A8-11CF-A442-00A0C90A8F39}`).

### ❌ Rejection 3: Spawning Untracked GUI Processes into `exebox`
* **The Failure**: Launching WinUI / Modern UWP apps (e.g. `Photos.exe`, `mspaint.exe`, `rundll32.exe`) directly from background subshells.
* **Why Rejected**: Modern Windows 11 apps detect that the caller is non-interactive, assign `MainWindowHandle = 0`, and enter a suspended background state. These ghost instances hijack future file associations and prevent Karan from opening images normally.
* **Invariant**: Every GUI process must be dispatched to `winsta0\default` with verified non-zero HWNDs, or immediately swept if suspended.

### ❌ Rejection 4: Direct `lpDesktop = "winsta0\default"` via Child Tokens
* **The Failure**: Compiling `CreateProcess` with `si.lpDesktop = @"winsta0\default"` directly in the subshell.
* **Why Rejected**: Fails with NTSTATUS `0xC0000142` (`STATUS_DLL_INIT_FAILED`). The child process security token lacks DACL access to the active console window station.
* **Invariant**: Cross-desktop interaction must use an initialized STA thread with explicit `OpenWindowStation` / `SetProcessWindowStation` handles, or delegate to `explorer.exe`.

### ❌ Rejection 5: Launching Standalone Images Inside Web Browsers
* **The Failure**: Opening a local image (`.jpg`, `.png`) by launching `msedge.exe <path>` or `chrome.exe <path>`.
* **Why Rejected**: Browsers are heavy, multi-tabbed applications. Opening a photo in Edge clutters browser tabs, introduces browser toolbars, and fails user intent when a dedicated photo viewer was requested.
* **Invariant**: Route images to dedicated viewers (Windows Photos, Paint, Classic PhotoViewer, or dedicated lightweight viewers), never into the primary web browser unless explicitly requested.

---

## 🛡️ 3. User Experience & Focus Invariants

### ❌ Rejection 6: Focus-Stealing Keystrokes & Blind Clicks
* **The Failure**: Injecting arbitrary clicks or calling `SetForegroundWindow` on background tools while Karan is typing in Windows Terminal or Neovim.
* **Why Rejected**: Disrupts typing flow, drops characters, and can accidentally trigger unintended actions in active editors.
* **Invariant**: Use read-only background GDI/DirectX desktop frame copies and UI Automation trees for inspection. Only manipulate focus when explicitly ordered to manage a window.

### ❌ Rejection 7: Enterprise Bureaucracy on Solo Projects
* **The Failure**: Introducing CodeQL pipelines, Dependabot spam, 40-character SHA pinning, or cosmetic formatting CI failures.
* **Why Rejected**: Enterprise bloat slows down solo developer velocity with zero practical gain.
* **Invariant**: Lean, high-velocity, solo-developer pragmatism. Clean scripts, direct tests, zero fake enterprise overhead.

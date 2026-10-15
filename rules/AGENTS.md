# 🎩 Win-Chauffeur: Core Invariants

1. **Sakshi Immutable Shield (Rule Zero)**: Never touch, pause, kill, or alter `\Sakshi` scheduled task, `%USERPROFILE%\Void\Sakshi\`, or any asset matching `*Sakshi*`. Abort any operation touching Sakshi immediately.
2. **Sovereign Desktop Boundary (`winsta0\default` vs `exebox`)**: 
   - All interactive GUI operations, browser launches, image viewings, and media playback must marshal out-of-process to the interactive Windows Explorer desktop shell (PID of `explorer.exe` on `winsta0\default`) via `CLSID_ShellWindows` (`{9BA05972-F6A8-11CF-A442-00A0C90A8F39}`).
   - Never spawn GUI processes directly from the background sandbox (`exebox`) without display marshaling, as they convert into suspended ghost processes (`Photos.exe`, `rundll32`) with `MainWindowHandle = 0`.
3. **Zero Orphan Ghost Population ("Never Leave Residue")**:
   - Every spawned process, test window, or viewer must be deterministically tracked and cleanly terminable.
   - Run automated sweeps to eradicate lingering `Photos.exe`, `mspaint.exe`, or headless browser instances before concluding tasks.
4. **Non-Disruptive Focus Preservation**:
   - Never steal foreground window focus from Karan's active terminal or Neovim editor during automated background inspections.
   - Use background GDI/DirectX screen grabs and UI Automation trees for perception without spamming arbitrary clicks that disrupt typing flow.
5. **CRLF/LF Firewall & Decoupled State**:
   - Runtime cache images (`*.png`, `*.jpg`, `*.bmp`) and test screenshots live in `assets/` or git-ignored temporary directories, never polluting clean source trees.

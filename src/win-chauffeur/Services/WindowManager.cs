using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinChauffeur.Core;

namespace WinChauffeur.Services;

public record WindowInfo(IntPtr Hwnd, string Title, string ProcessName, int Pid, Win32.RECT Bounds);

public static class WindowManager
{
    public static List<WindowInfo> ListWindows(bool visibleOnly = true)
    {
        return StationBridge.ExecuteOnDesktop(() =>
        {
            List<WindowInfo> list = new List<WindowInfo>();
            IntPtr hDesk = Win32.OpenDesktop("default", 0, true, Win32.GENERIC_ALL);

            Win32.EnumDesktopWindows(hDesk, (hWnd, lParam) =>
            {
                if (visibleOnly && !Win32.IsWindowVisible(hWnd)) return true;

                int len = Win32.GetWindowTextLength(hWnd);
                if (len == 0 && visibleOnly) return true;

                StringBuilder sb = new StringBuilder(len + 1);
                Win32.GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString().Trim();

                if (string.IsNullOrWhiteSpace(title) && visibleOnly) return true;

                Win32.GetWindowThreadProcessId(hWnd, out uint pid);
                string procName = "Unknown";
                try
                {
                    Process p = Process.GetProcessById((int)pid);
                    procName = p.ProcessName;
                }
                catch { }

                Win32.RECT rect;
                if (Win32.DwmGetWindowAttribute(hWnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf<Win32.RECT>()) != 0)
                {
                    Win32.GetWindowRect(hWnd, out rect);
                }
                list.Add(new WindowInfo(hWnd, title, procName, (int)pid, rect));
                return true;
            }, IntPtr.Zero);

            return list;
        });
    }

    public static WindowInfo? FindWindow(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        query = query.Trim();

        List<WindowInfo> windows = ListWindows(visibleOnly: true);

        // 1. Try exact HWND parse
        if (query.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
            long.TryParse(query.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out long hexVal))
        {
            return windows.Find(w => w.Hwnd == new IntPtr(hexVal));
        }
        if (long.TryParse(query, out long intVal))
        {
            var matchHwnd = windows.Find(w => w.Hwnd == new IntPtr(intVal));
            if (matchHwnd != null) return matchHwnd;
        }

        // 2. Try Process Name exact match
        var procMatch = windows.Find(w => string.Equals(w.ProcessName, query, StringComparison.OrdinalIgnoreCase));
        if (procMatch != null) return procMatch;

        // 3. Try Process Name partial match
        var procPartial = windows.Find(w => w.ProcessName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        if (procPartial != null) return procPartial;

        // 4. Try Title exact/contains match
        var titleMatch = windows.Find(w => w.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        return titleMatch;
    }

    public static bool TileWindow(string query, string layout)
    {
        WindowInfo? win = FindWindow(query);
        if (win == null) return false;

        return StationBridge.ExecuteOnDesktop(() =>
        {
            IntPtr hWnd = win.Hwnd;
            Win32.RECT workArea = new Win32.RECT();

            IntPtr hMon = Win32.MonitorFromWindow(hWnd, Win32.MONITOR_DEFAULTTONEAREST);
            if (hMon != IntPtr.Zero)
            {
                Win32.MONITORINFO mi = new Win32.MONITORINFO();
                mi.cbSize = Marshal.SizeOf<Win32.MONITORINFO>();
                if (Win32.GetMonitorInfo(hMon, ref mi))
                {
                    workArea = mi.rcWork;
                }
            }

            if (workArea.Width <= 0 || workArea.Height <= 0)
            {
                Win32.SystemParametersInfo(Win32.SPI_GETWORKAREA, 0, ref workArea, 0);
            }

            int waX = workArea.Left;
            int waY = workArea.Top;
            int waWidth = workArea.Width;
            int waHeight = workArea.Height;

            switch (layout.ToLowerInvariant().Replace("-", "_"))
            {
                case "maximize":
                case "max":
                    Win32.ShowWindow(hWnd, Win32.SW_MAXIMIZE);
                    return true;

                case "fullscreen":
                    Win32.ShowWindow(hWnd, Win32.SW_MAXIMIZE);
                    Win32.SetForegroundWindow(hWnd);
                    Thread.Sleep(50);
                    Win32.keybd_event(Win32.VK_F11, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(30);
                    Win32.keybd_event(Win32.VK_F11, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    return true;

                case "minimize":
                case "min":
                    Win32.ShowWindow(hWnd, Win32.SW_MINIMIZE);
                    return true;

                case "restore":
                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
                    return true;

                case "topmost":
                    Win32.SetWindowPos(hWnd, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_SHOWWINDOW);
                    return true;

                case "untopmost":
                    Win32.SetWindowPos(hWnd, Win32.HWND_NOTOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_SHOWWINDOW);
                    return true;

                case "left":
                case "left_half":
                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
                    return Win32.MoveWindow(hWnd, waX, waY, waWidth / 2, waHeight, true);

                case "right":
                case "right_half":
                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
                    return Win32.MoveWindow(hWnd, waX + (waWidth / 2), waY, waWidth / 2, waHeight, true);

                case "top_left":
                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
                    return Win32.MoveWindow(hWnd, waX, waY, waWidth / 2, waHeight / 2, true);

                case "top_right":
                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
                    return Win32.MoveWindow(hWnd, waX + (waWidth / 2), waY, waWidth / 2, waHeight / 2, true);

                case "bottom_left":
                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
                    return Win32.MoveWindow(hWnd, waX, waY + (waHeight / 2), waWidth / 2, waHeight / 2, true);

                case "bottom_right":
                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
                    return Win32.MoveWindow(hWnd, waX + (waWidth / 2), waY + (waHeight / 2), waWidth / 2, waHeight / 2, true);

                case "center":
                    Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
                    int cW = (int)(waWidth * 0.70);
                    int cH = (int)(waHeight * 0.75);
                    int cX = waX + (waWidth - cW) / 2;
                    int cY = waY + (waHeight - cH) / 2;
                    return Win32.MoveWindow(hWnd, cX, cY, cW, cH, true);

                default:
                    return false;
            }
        });
    }

    public static bool SetFocus(string query)
    {
        WindowInfo? win = FindWindow(query);
        if (win == null) return false;

        return StationBridge.ExecuteOnDesktop(() =>
        {
            Win32.ShowWindow(win.Hwnd, Win32.SW_RESTORE);
            Win32.BringWindowToTop(win.Hwnd);
            return Win32.SetForegroundWindow(win.Hwnd);
        });
    }

    public static bool CloseWindow(string query, bool force = false)
    {
        WindowInfo? win = FindWindow(query);
        if (win == null) return false;

        StationBridge.EnsureSakshiShield(win.ProcessName);
        StationBridge.EnsureSakshiShield(win.Title);

        if (force)
        {
            try
            {
                Process p = Process.GetProcessById(win.Pid);
                p.Kill();
                return true;
            }
            catch { return false; }
        }

        return StationBridge.ExecuteOnDesktop(() =>
        {
            return Win32.PostMessage(win.Hwnd, Win32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        });
    }

    public static bool ApplyWorkspaceProfile(string profile)
    {
        string p = profile.ToLowerInvariant().Trim();
        switch (p)
        {
            case "coding":
            {
                var term = FindWindow("terminal") ?? FindWindow("cmd") ?? FindWindow("powershell");
                var browser = FindWindow("msedge") ?? FindWindow("chrome") ?? FindWindow("brave");
                var editor = FindWindow("obsidian") ?? FindWindow("code") ?? FindWindow("notepad");

                if (term != null) TileWindow(term.Title, "left_half");
                if (browser != null) TileWindow(browser.Title, "top_right");
                if (editor != null) TileWindow(editor.Title, "bottom_right");
                return true;
            }

            case "research":
            {
                var browser = FindWindow("msedge") ?? FindWindow("chrome") ?? FindWindow("brave");
                var notes = FindWindow("obsidian") ?? FindWindow("notepad") ?? FindWindow("terminal");

                if (browser != null) TileWindow(browser.Title, "left_half");
                if (notes != null) TileWindow(notes.Title, "right_half");
                return true;
            }

            case "focus":
            {
                IntPtr fg = Win32.GetForegroundWindow();
                if (fg != IntPtr.Zero)
                {
                    var win = FindWindow($"0x{fg.ToInt64():X}");
                    if (win != null)
                    {
                        TileWindow(win.Title, "center");
                        return true;
                    }
                }
                var topWin = ListWindows(true).Find(w => w.Bounds.Width > 200 && w.Bounds.Height > 200);
                if (topWin != null)
                {
                    TileWindow(topWin.Title, "center");
                    return true;
                }
                return false;
            }

            case "split":
            case "dual":
            {
                var wins = ListWindows(true).FindAll(w => w.Bounds.Width > 200 && w.Bounds.Height > 200);
                if (wins.Count >= 2)
                {
                    TileWindow(wins[0].Title, "left_half");
                    TileWindow(wins[1].Title, "right_half");
                    return true;
                }
                else if (wins.Count == 1)
                {
                    TileWindow(wins[0].Title, "maximize");
                    return true;
                }
                return false;
            }

            default:
                return false;
        }
    }
}

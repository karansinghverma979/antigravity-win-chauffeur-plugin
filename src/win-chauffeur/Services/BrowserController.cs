using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using WinChauffeur.Core;

namespace WinChauffeur.Services;

public record BrowserTabInfo(string Browser, string Title, IntPtr Hwnd);

public static class BrowserController
{
    public static List<BrowserTabInfo> ListTabs()
    {
        List<BrowserTabInfo> tabs = new List<BrowserTabInfo>();
        List<WindowInfo> windows = WindowManager.ListWindows(visibleOnly: true);

        foreach (var w in windows)
        {
            if (string.Equals(w.ProcessName, "msedge", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(w.ProcessName, "chrome", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(w.ProcessName, "brave", StringComparison.OrdinalIgnoreCase))
            {
                tabs.Add(new BrowserTabInfo(w.ProcessName, w.Title, w.Hwnd));
            }
        }

        return tabs;
    }

    public static bool ToggleMedia()
    {
        return StationBridge.ExecuteOnDesktop(() =>
        {
            // First try global hardware media key
            Win32.keybd_event(Win32.VK_MEDIA_PLAY_PAUSE, 0, 0, UIntPtr.Zero);
            Win32.keybd_event(Win32.VK_MEDIA_PLAY_PAUSE, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Also check if YouTube or Edge is active and send 'k' to be 100% sure
            var edgeWin = WindowManager.FindWindow("msedge");
            if (edgeWin != null && edgeWin.Title.IndexOf("YouTube", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Win32.SetForegroundWindow(edgeWin.Hwnd);
                Thread.Sleep(50);
                Win32.keybd_event(Win32.VK_K, 0, 0, UIntPtr.Zero);
                Thread.Sleep(30);
                Win32.keybd_event(Win32.VK_K, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }

            return true;
        });
    }

    public static bool CloseTab(string match)
    {
        var tabs = ListTabs();
        var target = tabs.Find(t => t.Title.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0);
        if (target == null)
        {
            // Fallback: search main window matching Edge or Chrome
            var win = WindowManager.FindWindow("msedge") ?? WindowManager.FindWindow("chrome");
            if (win == null) return false;
            target = new BrowserTabInfo(win.ProcessName, win.Title, win.Hwnd);
        }

        return StationBridge.ExecuteOnDesktop(() =>
        {
            Win32.ShowWindow(target.Hwnd, Win32.SW_RESTORE);
            Win32.SetForegroundWindow(target.Hwnd);
            Thread.Sleep(100);

            // Send Ctrl + 1 then Ctrl + W if targeting the first tab, or just Ctrl + W
            Win32.keybd_event(Win32.VK_CONTROL, 0, 0, UIntPtr.Zero);
            Win32.keybd_event(Win32.VK_W, 0, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            Win32.keybd_event(Win32.VK_W, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
            Win32.keybd_event(Win32.VK_CONTROL, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);

            return true;
        });
    }

    public static bool OpenUrl(string url)
    {
        return StationBridge.LaunchViaShell(url);
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WinChauffeur.Core;

namespace WinChauffeur.Services;

public record DisplayInfo(int Index, int X, int Y, int Width, int Height, bool IsPrimary);

public static class DesktopManager
{
    public static List<DisplayInfo> GetDisplays()
    {
        return StationBridge.ExecuteOnDesktop(() =>
        {
            List<DisplayInfo> list = new List<DisplayInfo>();
            int idx = 0;
            foreach (Screen s in Screen.AllScreens)
            {
                list.Add(new DisplayInfo(
                    idx++,
                    s.Bounds.X,
                    s.Bounds.Y,
                    s.Bounds.Width,
                    s.Bounds.Height,
                    s.Primary
                ));
            }
            return list;
        });
    }

    public static bool MoveToMonitor(string query, int monitorIndex)
    {
        var displays = GetDisplays();
        if (monitorIndex < 0 || monitorIndex >= displays.Count) return false;

        var targetDisplay = displays[monitorIndex];
        var win = WindowManager.FindWindow(query);
        if (win == null) return false;

        return StationBridge.ExecuteOnDesktop(() =>
        {
            Win32.ShowWindow(win.Hwnd, Win32.SW_RESTORE);
            return Win32.MoveWindow(
                win.Hwnd,
                targetDisplay.X + 50,
                targetDisplay.Y + 50,
                Math.Min(win.Bounds.Width, targetDisplay.Width - 100),
                Math.Min(win.Bounds.Height, targetDisplay.Height - 100),
                true
            );
        });
    }

    public static bool CreateVirtualDesktop()
    {
        return InputInjector.SendHotkey("win", "ctrl", "d");
    }

    public static bool SwitchVirtualDesktop(string direction = "right")
    {
        string dir = direction.ToLowerInvariant().Trim() == "left" ? "left" : "right";
        return InputInjector.SendHotkey("win", "ctrl", dir);
    }
}

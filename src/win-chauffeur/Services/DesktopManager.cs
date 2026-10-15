using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using WinChauffeur.Core;

namespace WinChauffeur.Services;

public record DisplayInfo(int Index, int X, int Y, int Width, int Height, bool IsPrimary);
public record VirtualDesktopInfo(int Index, string Name, Guid Id, bool IsCurrent);

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

    public static List<VirtualDesktopInfo> ListVirtualDesktops()
    {
        var list = new List<VirtualDesktopInfo>();
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops");
            if (key == null) return list;

            byte[]? ids = key.GetValue("VirtualDesktopIDs") as byte[];
            byte[]? curBytes = key.GetValue("CurrentVirtualDesktop") as byte[];
            Guid curGuid = curBytes != null && curBytes.Length >= 16 ? new Guid(curBytes) : Guid.Empty;

            if (ids != null && ids.Length >= 16)
            {
                using var desktopsKey = key.OpenSubKey("Desktops");
                int count = ids.Length / 16;
                for (int i = 0; i < count; i++)
                {
                    byte[] slice = new byte[16];
                    Array.Copy(ids, i * 16, slice, 0, 16);
                    Guid g = new Guid(slice);
                    string name = $"Desktop {i + 1}";

                    if (desktopsKey != null)
                    {
                        using var deskSub = desktopsKey.OpenSubKey(g.ToString("B").ToUpper());
                        string? customName = deskSub?.GetValue("Name") as string;
                        if (!string.IsNullOrWhiteSpace(customName))
                        {
                            name = customName;
                        }
                    }

                    list.Add(new VirtualDesktopInfo(i, name, g, g == curGuid));
                }
            }
        }
        catch { }

        return list;
    }

    public static bool GotoVirtualDesktop(string query)
    {
        var list = ListVirtualDesktops();
        if (list.Count == 0) return false;

        var cur = list.Find(d => d.IsCurrent) ?? list[0];
        VirtualDesktopInfo? target = null;

        if (int.TryParse(query, out int targetIdx))
        {
            target = list.Find(d => d.Index == targetIdx);
        }

        if (target == null)
        {
            target = list.Find(d => string.Equals(d.Name, query, StringComparison.OrdinalIgnoreCase));
        }

        if (target == null)
        {
            target = list.Find(d => d.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (target == null) return false;

        int delta = target.Index - cur.Index;
        if (delta == 0) return true;

        string dir = delta > 0 ? "right" : "left";
        int steps = Math.Abs(delta);

        for (int i = 0; i < steps; i++)
        {
            SwitchVirtualDesktop(dir);
            if (i < steps - 1) Thread.Sleep(120);
        }

        return true;
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

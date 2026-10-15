using System;
using System.Collections.Generic;
using System.Diagnostics;
using WinChauffeur.Core;

namespace WinChauffeur.Services;

public record MediaInfo(string App, string Title, bool IsActive);

public static class MediaController
{
    private static readonly string[] MediaProcessNames = new[] { "spotify", "msedge", "chrome", "brave", "vlc", "wmplayer" };

    public static List<MediaInfo> GetStatus()
    {
        var list = new List<MediaInfo>();
        var windows = WindowManager.ListWindows(visibleOnly: true);

        foreach (var w in windows)
        {
            string proc = w.ProcessName.ToLowerInvariant();
            if (Array.IndexOf(MediaProcessNames, proc) >= 0)
            {
                bool isMedia = w.Title.IndexOf("youtube", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               w.Title.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               w.Title.IndexOf("soundcloud", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               w.Title.IndexOf("music", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               proc == "spotify" || proc == "vlc";

                if (isMedia)
                {
                    list.Add(new MediaInfo(w.ProcessName, w.Title, true));
                }
            }
        }

        return list;
    }

    public static bool Control(string action)
    {
        return StationBridge.ExecuteOnDesktop(() =>
        {
            switch (action.ToLowerInvariant().Trim())
            {
                case "toggle":
                case "play_pause":
                case "play":
                case "pause":
                    Win32.keybd_event(Win32.VK_MEDIA_PLAY_PAUSE, 0, 0, UIntPtr.Zero);
                    Win32.keybd_event(Win32.VK_MEDIA_PLAY_PAUSE, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    return true;

                case "next":
                    Win32.keybd_event(Win32.VK_MEDIA_NEXT_TRACK, 0, 0, UIntPtr.Zero);
                    Win32.keybd_event(Win32.VK_MEDIA_NEXT_TRACK, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    return true;

                case "prev":
                case "previous":
                    Win32.keybd_event(Win32.VK_MEDIA_PREV_TRACK, 0, 0, UIntPtr.Zero);
                    Win32.keybd_event(Win32.VK_MEDIA_PREV_TRACK, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    return true;

                case "stop":
                    Win32.keybd_event(Win32.VK_MEDIA_STOP, 0, 0, UIntPtr.Zero);
                    Win32.keybd_event(Win32.VK_MEDIA_STOP, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    return true;

                case "mute":
                    Win32.keybd_event(Win32.VK_VOLUME_MUTE, 0, 0, UIntPtr.Zero);
                    Win32.keybd_event(Win32.VK_VOLUME_MUTE, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    return true;

                case "volup":
                case "volume_up":
                    for (int i = 0; i < 3; i++)
                    {
                        Win32.keybd_event(Win32.VK_VOLUME_UP, 0, 0, UIntPtr.Zero);
                        Win32.keybd_event(Win32.VK_VOLUME_UP, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }
                    return true;

                case "voldown":
                case "volume_down":
                    for (int i = 0; i < 3; i++)
                    {
                        Win32.keybd_event(Win32.VK_VOLUME_DOWN, 0, 0, UIntPtr.Zero);
                        Win32.keybd_event(Win32.VK_VOLUME_DOWN, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }
                    return true;

                default:
                    return false;
            }
        });
    }
}

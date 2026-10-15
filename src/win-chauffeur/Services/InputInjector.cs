using System;
using System.Threading;
using WinChauffeur.Core;

namespace WinChauffeur.Services;

public static class InputInjector
{
    public static bool Click(int x, int y, string button = "left", int count = 1)
    {
        return StationBridge.ExecuteOnDesktop(() =>
        {
            Win32.SetCursorPos(x, y);
            Thread.Sleep(30);

            uint down = Win32.MOUSEEVENTF_LEFTDOWN;
            uint up = Win32.MOUSEEVENTF_LEFTUP;

            if (string.Equals(button, "right", StringComparison.OrdinalIgnoreCase))
            {
                down = Win32.MOUSEEVENTF_RIGHTDOWN;
                up = Win32.MOUSEEVENTF_RIGHTUP;
            }

            for (int i = 0; i < count; i++)
            {
                Win32.mouse_event(down, 0, 0, 0, 0);
                Thread.Sleep(30);
                Win32.mouse_event(up, 0, 0, 0, 0);
                if (i < count - 1) Thread.Sleep(80);
            }

            return true;
        });
    }

    public static bool SendHotkey(params string[] keys)
    {
        if (keys == null || keys.Length == 0) return false;

        return StationBridge.ExecuteOnDesktop(() =>
        {
            List<byte> pressed = new List<byte>();

            foreach (var k in keys)
            {
                byte vk = ParseKey(k);
                if (vk != 0)
                {
                    Win32.keybd_event(vk, 0, 0, UIntPtr.Zero);
                    pressed.Add(vk);
                    Thread.Sleep(20);
                }
            }

            pressed.Reverse();
            foreach (var vk in pressed)
            {
                Win32.keybd_event(vk, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                Thread.Sleep(20);
            }

            return true;
        });
    }

    private static byte ParseKey(string key)
    {
        switch (key.ToLowerInvariant().Trim())
        {
            case "ctrl":
            case "control": return Win32.VK_CONTROL;
            case "alt":
            case "menu": return Win32.VK_MENU;
            case "shift": return Win32.VK_SHIFT;
            case "enter":
            case "return": return Win32.VK_RETURN;
            case "space": return Win32.VK_SPACE;
            case "left": return Win32.VK_LEFT;
            case "right": return Win32.VK_RIGHT;
            case "up": return Win32.VK_UP;
            case "down": return Win32.VK_DOWN;
            case "w": return Win32.VK_W;
            case "t": return Win32.VK_T;
            case "k": return Win32.VK_K;
            case "j": return Win32.VK_J;
            case "l": return Win32.VK_L;
            case "win":
            case "lwin":
            case "windows": return Win32.VK_LWIN;
            case "f11": return Win32.VK_F11;
            default:
                if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
                {
                    return (byte)char.ToUpperInvariant(key[0]);
                }
                return 0;
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace WinChauffeur.Core;

public static class StationBridge
{
    private static readonly Guid CLSID_ShellWindows = new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
    private const int SWC_DESKTOP = 8;
    private const int SWFO_NEEDDISPATCH = 1;

    public static void EnsureSakshiShield(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return;
        if (target.IndexOf("Sakshi", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidOperationException("CRITICAL: Operation blocked by Sakshi Immutable Shield (Rule Zero).");
        }
    }

    public static T ExecuteOnDesktop<T>(Func<T> action)
    {
        T result = default!;
        Exception? error = null;

        Thread t = new Thread(() =>
        {
            IntPtr hWinsta = IntPtr.Zero;
            IntPtr hDesk = IntPtr.Zero;
            try
            {
                hWinsta = Win32.OpenWindowStation("winsta0", true, Win32.GENERIC_ALL);
                if (hWinsta != IntPtr.Zero)
                {
                    Win32.SetProcessWindowStation(hWinsta);
                }

                hDesk = Win32.OpenDesktop("default", 0, false, Win32.GENERIC_ALL);
                if (hDesk != IntPtr.Zero)
                {
                    Win32.SetThreadDesktop(hDesk);
                }

                result = action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                if (hDesk != IntPtr.Zero) Win32.CloseDesktop(hDesk);
                if (hWinsta != IntPtr.Zero) Win32.CloseWindowStation(hWinsta);
            }
        });

        // Keep MTA apartment state so OLE/COM does not create an initial window on exebox desktop
        t.Start();
        t.Join();

        if (error != null) throw error;
        return result;
    }

    public static void ExecuteOnDesktop(Action action)
    {
        ExecuteOnDesktop(() =>
        {
            action();
            return true;
        });
    }

    public static bool LaunchViaShell(string target, string arguments = "")
    {
        EnsureSakshiShield(target);
        EnsureSakshiShield(arguments);

        // First purge any suspended ghosts for common viewers before launch
        if (target.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            target.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            target.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            target.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
        {
            PurgeGhosts("Photos");
        }

        try
        {
            Type? shellWindowsType = Type.GetTypeFromCLSID(CLSID_ShellWindows);
            if (shellWindowsType == null) return FallbackLaunch(target, arguments);

            dynamic? shellWindows = Activator.CreateInstance(shellWindowsType);
            if (shellWindows == null) return FallbackLaunch(target, arguments);

            object loc = 0;
            object locRoot = 0;
            int hwnd = 0;
            dynamic desktop = shellWindows.FindWindowSW(ref loc, ref locRoot, SWC_DESKTOP, ref hwnd, SWFO_NEEDDISPATCH);

            if (desktop != null && desktop.Document != null && desktop.Document.Application != null)
            {
                dynamic app = desktop.Document.Application;
                app.ShellExecute(target, arguments, "", "open", Win32.SW_SHOWNORMAL);
                return true;
            }
        }
        catch (Exception)
        {
            // Fall back to direct process start if Shell COM dispatch encounters an exception
        }

        return FallbackLaunch(target, arguments);
    }

    private static bool FallbackLaunch(string target, string arguments)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = target,
                Arguments = arguments,
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[StationBridge] Fallback launch failed: {ex.Message}");
            return false;
        }
    }

    public static int PurgeGhosts(string processName = "Photos")
    {
        EnsureSakshiShield(processName);
        int purged = 0;
        try
        {
            Process[] procs = Process.GetProcessesByName(processName);
            foreach (Process p in procs)
            {
                try
                {
                    // If it has no main window handle or has been suspended, kill cleanly
                    if (p.MainWindowHandle == IntPtr.Zero)
                    {
                        p.Kill();
                        purged++;
                    }
                }
                catch { }
            }
        }
        catch { }

        return purged;
    }
}

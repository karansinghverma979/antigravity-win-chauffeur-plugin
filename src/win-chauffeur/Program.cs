using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WinChauffeur.Core;
using WinChauffeur.Mcp;
using WinChauffeur.Services;

namespace WinChauffeur;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h" || args[0] == "help")
        {
            PrintUsage();
            return 0;
        }

        string cmd = args[0].ToLowerInvariant();

        if (cmd == "--mcp" || cmd == "-mcp" || cmd == "mcp")
        {
            await McpServer.RunAsync();
            return 0;
        }

        try
        {
            switch (cmd)
            {
                case "look":
                case "capture":
                case "screenshot":
                {
                    string? outPath = args.Length > 1 ? args[1] : null;
                    string saved = ScreenCapturer.CaptureScreen(outPath);
                    Console.WriteLine($"Screenshot saved: {saved}");
                    return 0;
                }

                case "window":
                {
                    if (args.Length < 2) { PrintUsage(); return 1; }
                    string sub = args[1].ToLowerInvariant();

                    if (sub == "list")
                    {
                        var list = WindowManager.ListWindows(visibleOnly: true);
                        Console.WriteLine($"Found {list.Count} visible windows:");
                        foreach (var w in list)
                        {
                            Console.WriteLine($"  [HWND: 0x{w.Hwnd.ToInt64():X}] [PID: {w.Pid,5}] [{w.ProcessName,-15}] {w.Title}");
                        }
                        return 0;
                    }
                    if (sub == "tile")
                    {
                        if (args.Length < 4)
                        {
                            Console.Error.WriteLine("Usage: win-chauffeur window tile <target> <layout>");
                            return 1;
                        }
                        string target = args[2];
                        string layout = args[3];
                        bool ok = WindowManager.TileWindow(target, layout);
                        Console.WriteLine(ok ? $"Tiled '{target}' to {layout}" : $"Failed to tile '{target}'");
                        return ok ? 0 : 1;
                    }
                    if (sub == "focus")
                    {
                        if (args.Length < 3) { Console.Error.WriteLine("Usage: win-chauffeur window focus <target>"); return 1; }
                        bool ok = WindowManager.SetFocus(args[2]);
                        Console.WriteLine(ok ? $"Focused '{args[2]}'" : $"Failed to focus '{args[2]}'");
                        return ok ? 0 : 1;
                    }
                    if (sub == "close")
                    {
                        if (args.Length < 3) { Console.Error.WriteLine("Usage: win-chauffeur window close <target> [--force]"); return 1; }
                        bool force = args.Length > 3 && args[3] == "--force";
                        bool ok = WindowManager.CloseWindow(args[2], force);
                        Console.WriteLine(ok ? $"Closed '{args[2]}'" : $"Failed to close '{args[2]}'");
                        return ok ? 0 : 1;
                    }
                    break;
                }

                case "open":
                case "launch":
                {
                    if (args.Length < 2) { Console.Error.WriteLine("Usage: win-chauffeur open <path-or-url> [args]"); return 1; }
                    string target = args[1];
                    string extraArgs = args.Length > 2 ? string.Join(" ", args[2..]) : "";
                    bool ok = StationBridge.LaunchViaShell(target, extraArgs);
                    Console.WriteLine(ok ? $"Dispatched to desktop: {target}" : $"Failed to dispatch: {target}");
                    return ok ? 0 : 1;
                }

                case "browser":
                {
                    if (args.Length < 2) { PrintUsage(); return 1; }
                    string sub = args[1].ToLowerInvariant();

                    if (sub == "tabs")
                    {
                        var tabs = BrowserController.ListTabs();
                        Console.WriteLine($"Found {tabs.Count} browser windows/tabs:");
                        foreach (var t in tabs)
                        {
                            Console.WriteLine($"  [{t.Browser}] {t.Title}");
                        }
                        return 0;
                    }
                    if (sub == "close-tab")
                    {
                        if (args.Length < 3) { Console.Error.WriteLine("Usage: win-chauffeur browser close-tab <match>"); return 1; }
                        bool ok = BrowserController.CloseTab(args[2]);
                        Console.WriteLine(ok ? $"Closed tab matching '{args[2]}'" : $"Failed to close tab");
                        return ok ? 0 : 1;
                    }
                    if (sub == "media-toggle")
                    {
                        bool ok = BrowserController.ToggleMedia();
                        Console.WriteLine(ok ? "Media playback toggled" : "Failed to toggle media");
                        return ok ? 0 : 1;
                    }
                    break;
                }

                case "desktop":
                {
                    if (args.Length < 2 || args[1] == "list")
                    {
                        var list = DesktopManager.GetDisplays();
                        Console.WriteLine($"Found {list.Count} active displays:");
                        foreach (var d in list)
                        {
                            Console.WriteLine($"  Display {d.Index}: {d.Width}x{d.Height} at ({d.X}, {d.Y}) {(d.IsPrimary ? "[PRIMARY]" : "")}");
                        }
                        return 0;
                    }
                    if (args[1] == "new" || args[1] == "create")
                    {
                        bool ok = DesktopManager.CreateVirtualDesktop();
                        Console.WriteLine(ok ? "Created new virtual desktop" : "Failed to create virtual desktop");
                        return ok ? 0 : 1;
                    }
                    if (args[1] == "switch")
                    {
                        string dir = args.Length > 2 ? args[2] : "right";
                        bool ok = DesktopManager.SwitchVirtualDesktop(dir);
                        Console.WriteLine(ok ? $"Switched virtual desktop {dir}" : "Failed to switch virtual desktop");
                        return ok ? 0 : 1;
                    }
                    break;
                }

                case "click":
                {
                    if (args.Length < 3) { Console.Error.WriteLine("Usage: win-chauffeur click <x> <y> [left|right]"); return 1; }
                    int x = int.Parse(args[1]);
                    int y = int.Parse(args[2]);
                    string btn = args.Length > 3 ? args[3] : "left";
                    bool ok = InputInjector.Click(x, y, btn);
                    Console.WriteLine(ok ? $"Clicked at ({x}, {y})" : "Click failed");
                    return ok ? 0 : 1;
                }

                default:
                    Console.Error.WriteLine($"Unknown command '{cmd}'. Use --help to list commands.");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"
Win-Chauffeur v1.0.0 — Autonomous Windows 11 Desktop Operator

USAGE:
  win-chauffeur --mcp                        Start in MCP stdio daemon mode
  win-chauffeur look [out.png]               Capture physical screen to image
  win-chauffeur window list                  List all visible desktop windows
  win-chauffeur window tile <target> <layout> Snap window (left_half, right_half, center, max...)
  win-chauffeur window focus <target>        Bring window to foreground
  win-chauffeur window close <target>        Close window gracefully
  win-chauffeur open <path-or-url>           Launch file/media via Explorer COM bridge
  win-chauffeur browser tabs                 List open browser tabs
  win-chauffeur browser close-tab <match>    Close specific tab in Edge/Chrome
  win-chauffeur browser media-toggle         Toggle Play/Pause on media/YouTube
  win-chauffeur desktop list                 List active physical monitors
  win-chauffeur click <x> <y> [left|right]   Click coordinates on display
");
    }
}

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Linq;
using WinChauffeur.Core;
using WinChauffeur.Services;

namespace WinChauffeur.Mcp;

public static class McpServer
{
    public static async Task RunAsync()
    {
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        using var writer = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8) { AutoFlush = true };

        while (!reader.EndOfStream)
        {
            string? line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var request = JsonNode.Parse(line)?.AsObject();
                if (request == null) continue;

                var id = request["id"];
                string? method = request["method"]?.GetValue<string>();

                if (method == "initialize")
                {
                    var initResult = new JsonObject
                    {
                        ["protocolVersion"] = "2024-11-05",
                        ["capabilities"] = new JsonObject
                        {
                            ["tools"] = new JsonObject()
                        },
                        ["serverInfo"] = new JsonObject
                        {
                            ["name"] = "win-chauffeur",
                            ["version"] = "1.0.0"
                        }
                    };
                    SendResponse(writer, id, initResult);
                }
                else if (method == "notifications/initialized")
                {
                    // No response required for notification
                }
                else if (method == "tools/list")
                {
                    var toolsList = GetToolsList();
                    SendResponse(writer, id, new JsonObject { ["tools"] = toolsList });
                }
                else if (method == "tools/call")
                {
                    var paramsObj = request["params"]?.AsObject();
                    string toolName = paramsObj?["name"]?.GetValue<string>() ?? "";
                    var args = paramsObj?["arguments"]?.AsObject() ?? new JsonObject();

                    string resultText = ExecuteTool(toolName, args);
                    var contentArray = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = resultText
                        }
                    };
                    SendResponse(writer, id, new JsonObject { ["content"] = contentArray });
                }
                else
                {
                    SendError(writer, id, -32601, $"Method '{method}' not found.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[McpServer] Error processing message: {ex.Message}");
            }
        }
    }

    private static void SendResponse(StreamWriter writer, JsonNode? id, JsonObject result)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["result"] = result
        };
        writer.WriteLine(response.ToJsonString());
    }

    private static void SendError(StreamWriter writer, JsonNode? id, int code, string message)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
        writer.WriteLine(response.ToJsonString());
    }

    private static JsonArray GetToolsList()
    {
        return new JsonArray
        {
            new JsonObject
            {
                ["name"] = "win_chauffeur_look",
                ["description"] = "Takes an instant screen capture of the entire display, active window, or specific bounding box in <30ms.",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["output_path"] = new JsonObject { ["type"] = "string", ["description"] = "Optional target file path for PNG." },
                        ["window_target"] = new JsonObject { ["type"] = "string", ["description"] = "Optional window title or process to capture exclusively." },
                        ["x"] = new JsonObject { ["type"] = "integer", ["description"] = "Optional crop start X." },
                        ["y"] = new JsonObject { ["type"] = "integer", ["description"] = "Optional crop start Y." },
                        ["width"] = new JsonObject { ["type"] = "integer", ["description"] = "Optional crop width." },
                        ["height"] = new JsonObject { ["type"] = "integer", ["description"] = "Optional crop height." }
                    }
                }
            },
            new JsonObject
            {
                ["name"] = "win_chauffeur_window",
                ["description"] = "One-shot Window Manager: list windows, tile into grids (left_half, right_half, center, max), focus, or close.",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["action"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "list", "tile", "focus", "close", "profile" }, ["description"] = "Action to execute." },
                        ["target"] = new JsonObject { ["type"] = "string", ["description"] = "Window title or process name to match." },
                        ["layout"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "left_half", "right_half", "top_left", "top_right", "bottom_left", "bottom_right", "center", "maximize", "minimize", "restore", "topmost", "untopmost" }, ["description"] = "Layout preset." },
                        ["profile"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "coding", "research", "focus", "split" }, ["description"] = "Workspace profile to apply." },
                        ["force"] = new JsonObject { ["type"] = "boolean", ["description"] = "Force terminate process on close." }
                    },
                    ["required"] = new JsonArray { "action" }
                }
            },
            new JsonObject
            {
                ["name"] = "win_chauffeur_desktop",
                ["description"] = "Multi-display discovery, virtual desktop enumeration, and named virtual desktop teleportation.",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["action"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "list_displays", "list_desktops", "goto", "move_to_monitor", "switch" } },
                        ["target"] = new JsonObject { ["type"] = "string", ["description"] = "Target virtual desktop name/index, or window to move." },
                        ["direction"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "left", "right" }, ["description"] = "Switch direction." },
                        ["monitor_index"] = new JsonObject { ["type"] = "integer", ["description"] = "Index of target monitor (0, 1...)." }
                    },
                    ["required"] = new JsonArray { "action" }
                }
            },
            new JsonObject
            {
                ["name"] = "win_chauffeur_browser",
                ["description"] = "Discovers open browser tabs across Edge and Chrome, closes specific tabs, or toggles media playback.",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["action"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "list_tabs", "close_tab", "media_toggle", "open_url" } },
                        ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Tab title match or URL to open." }
                    },
                    ["required"] = new JsonArray { "action" }
                }
            },
            new JsonObject
            {
                ["name"] = "win_chauffeur_open",
                ["description"] = "Universal Out-of-Process launcher: opens photos, PDFs, music, or URLs on winsta0 with zero ghost accumulation.",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["target"] = new JsonObject { ["type"] = "string", ["description"] = "File path or URL to launch." },
                        ["arguments"] = new JsonObject { ["type"] = "string", ["description"] = "Optional arguments." }
                    },
                    ["required"] = new JsonArray { "target" }
                }
            },
            new JsonObject
            {
                ["name"] = "win_chauffeur_interact",
                ["description"] = "Human-like input dispatcher: clicks coordinates, sends hotkeys, or types text.",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["action"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "click", "hotkey" } },
                        ["x"] = new JsonObject { ["type"] = "integer", ["description"] = "Click X coordinate." },
                        ["y"] = new JsonObject { ["type"] = "integer", ["description"] = "Click Y coordinate." },
                        ["button"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "left", "right" } },
                        ["keys"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Key sequence to press (e.g. ['ctrl', 'w'])." }
                    },
                    ["required"] = new JsonArray { "action" }
                }
            },
            new JsonObject
            {
                ["name"] = "win_chauffeur_media",
                ["description"] = "Controls media playback, track navigation, and volume levels on Motobook workstation.",
                ["inputSchema"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["action"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "status", "play", "pause", "toggle", "next", "prev", "stop", "mute", "volume_up", "volume_down" }, ["description"] = "Media or volume action to execute." }
                    },
                    ["required"] = new JsonArray { "action" }
                }
            }
        };
    }

    private static string ExecuteTool(string name, JsonObject args)
    {
        try
        {
            switch (name)
            {
                case "win_chauffeur_look":
                case "chauffeur_look":
                {
                    string? outPath = args["output_path"]?.GetValue<string>();
                    string? winTarget = args["window_target"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(winTarget))
                    {
                        string saved = ScreenCapturer.CaptureWindow(winTarget, outPath);
                        return JsonSerializer.Serialize(new { status = "ok", file = saved, window = winTarget });
                    }

                    int x = args["x"]?.GetValue<int>() ?? 0;
                    int y = args["y"]?.GetValue<int>() ?? 0;
                    int w = args["width"]?.GetValue<int>() ?? 0;
                    int h = args["height"]?.GetValue<int>() ?? 0;

                    string path = ScreenCapturer.CaptureScreen(outPath, x, y, w, h);
                    return JsonSerializer.Serialize(new { status = "ok", file = path, x, y, width = w, height = h });
                }

                case "win_chauffeur_window":
                case "chauffeur_window":
                {
                    string action = args["action"]?.GetValue<string>() ?? "list";
                    string target = args["target"]?.GetValue<string>() ?? "";
                    string layout = args["layout"]?.GetValue<string>() ?? "restore";
                    bool force = args["force"]?.GetValue<bool>() ?? false;

                    if (action == "list")
                    {
                        var windows = WindowManager.ListWindows(visibleOnly: true);
                        var windowDtos = windows.Select(w => new
                        {
                            hwnd = $"0x{w.Hwnd.ToInt64():X}",
                            title = w.Title,
                            process = w.ProcessName,
                            pid = w.Pid,
                            bounds = new { x = w.Bounds.Left, y = w.Bounds.Top, width = w.Bounds.Width, height = w.Bounds.Height }
                        });
                        return JsonSerializer.Serialize(new { status = "ok", count = windows.Count, windows = windowDtos });
                    }
                    if (action == "tile")
                    {
                        bool tiled = WindowManager.TileWindow(target, layout);
                        return JsonSerializer.Serialize(new { status = tiled ? "ok" : "failed", target, layout });
                    }
                    if (action == "focus")
                    {
                        bool focused = WindowManager.SetFocus(target);
                        return JsonSerializer.Serialize(new { status = focused ? "ok" : "failed", target });
                    }
                    if (action == "close")
                    {
                        bool closed = WindowManager.CloseWindow(target, force);
                        return JsonSerializer.Serialize(new { status = closed ? "ok" : "failed", target });
                    }
                    if (action == "profile" || action == "workspace")
                    {
                        string prof = args["profile"]?.GetValue<string>() ?? args["layout"]?.GetValue<string>() ?? "coding";
                        bool ok = WindowManager.ApplyWorkspaceProfile(prof);
                        return JsonSerializer.Serialize(new { status = ok ? "ok" : "failed", profile = prof });
                    }
                    return JsonSerializer.Serialize(new { status = "error", message = $"Unknown action '{action}'" });
                }

                case "win_chauffeur_desktop":
                case "chauffeur_desktop":
                {
                    string action = args["action"]?.GetValue<string>() ?? "list_displays";
                    if (action == "list_displays")
                    {
                        var displays = DesktopManager.GetDisplays();
                        return JsonSerializer.Serialize(new { status = "ok", count = displays.Count, displays });
                    }
                    if (action == "list_desktops")
                    {
                        var desktops = DesktopManager.ListVirtualDesktops();
                        return JsonSerializer.Serialize(new { status = "ok", count = desktops.Count, desktops });
                    }
                    if (action == "goto" || action == "to")
                    {
                        string target = args["target"]?.GetValue<string>() ?? "";
                        bool ok = DesktopManager.GotoVirtualDesktop(target);
                        return JsonSerializer.Serialize(new { status = ok ? "ok" : "failed", target });
                    }
                    if (action == "move_to_monitor")
                    {
                        string target = args["target"]?.GetValue<string>() ?? "";
                        int idx = args["monitor_index"]?.GetValue<int>() ?? 0;
                        bool moved = DesktopManager.MoveToMonitor(target, idx);
                        return JsonSerializer.Serialize(new { status = moved ? "ok" : "failed", target, monitor = idx });
                    }
                    if (action == "create_virtual_desktop" || action == "new_desktop")
                    {
                        bool ok = DesktopManager.CreateVirtualDesktop();
                        return JsonSerializer.Serialize(new { status = ok ? "ok" : "failed", action = "create_virtual_desktop" });
                    }
                    if (action == "switch_virtual_desktop" || action == "switch_desktop")
                    {
                        string direction = args["direction"]?.GetValue<string>() ?? "right";
                        bool ok = DesktopManager.SwitchVirtualDesktop(direction);
                        return JsonSerializer.Serialize(new { status = ok ? "ok" : "failed", action = "switch_virtual_desktop", direction });
                    }
                    return JsonSerializer.Serialize(new { status = "error", message = $"Unknown action '{action}'" });
                }

                case "win_chauffeur_browser":
                case "chauffeur_browser":
                {
                    string action = args["action"]?.GetValue<string>() ?? "list_tabs";
                    string query = args["query"]?.GetValue<string>() ?? "";

                    if (action == "list_tabs")
                    {
                        var tabs = BrowserController.ListTabs();
                        var tabDtos = tabs.Select(t => new
                        {
                            browser = t.Browser,
                            title = t.Title,
                            hwnd = $"0x{t.Hwnd.ToInt64():X}"
                        });
                        return JsonSerializer.Serialize(new { status = "ok", count = tabs.Count, tabs = tabDtos });
                    }
                    if (action == "close_tab")
                    {
                        bool closed = BrowserController.CloseTab(query);
                        return JsonSerializer.Serialize(new { status = closed ? "ok" : "failed", match = query });
                    }
                    if (action == "media_toggle")
                    {
                        bool toggled = BrowserController.ToggleMedia();
                        return JsonSerializer.Serialize(new { status = toggled ? "ok" : "failed", action = "media_toggle" });
                    }
                    if (action == "open_url")
                    {
                        bool opened = BrowserController.OpenUrl(query);
                        return JsonSerializer.Serialize(new { status = opened ? "ok" : "failed", url = query });
                    }
                    return JsonSerializer.Serialize(new { status = "error", message = $"Unknown action '{action}'" });
                }

                case "win_chauffeur_open":
                case "chauffeur_open":
                {
                    string target = args["target"]?.GetValue<string>() ?? "";
                    string arguments = args["arguments"]?.GetValue<string>() ?? "";
                    bool launched = StationBridge.LaunchViaShell(target, arguments);
                    return JsonSerializer.Serialize(new { status = launched ? "ok" : "failed", target });
                }

                case "win_chauffeur_interact":
                case "chauffeur_interact":
                {
                    string action = args["action"]?.GetValue<string>() ?? "click";
                    if (action == "click")
                    {
                        int x = args["x"]?.GetValue<int>() ?? 0;
                        int y = args["y"]?.GetValue<int>() ?? 0;
                        string button = args["button"]?.GetValue<string>() ?? "left";
                        bool clicked = InputInjector.Click(x, y, button);
                        return JsonSerializer.Serialize(new { status = clicked ? "ok" : "failed", x, y, button });
                    }
                    if (action == "hotkey")
                    {
                        var keysArr = args["keys"]?.AsArray();
                        List<string> keys = new List<string>();
                        if (keysArr != null)
                        {
                            foreach (var k in keysArr) if (k != null) keys.Add(k.GetValue<string>());
                        }
                        bool sent = InputInjector.SendHotkey(keys.ToArray());
                        return JsonSerializer.Serialize(new { status = sent ? "ok" : "failed", keys });
                    }
                    return JsonSerializer.Serialize(new { status = "error", message = $"Unknown action '{action}'" });
                }

                case "win_chauffeur_media":
                case "chauffeur_media":
                {
                    string action = args["action"]?.GetValue<string>() ?? "status";
                    if (action == "status")
                    {
                        var media = MediaController.GetStatus();
                        return JsonSerializer.Serialize(new { status = "ok", count = media.Count, media });
                    }
                    bool ok = MediaController.Control(action);
                    return JsonSerializer.Serialize(new { status = ok ? "ok" : "failed", action });
                }

                default:
                    return JsonSerializer.Serialize(new { status = "error", message = $"Tool '{name}' not recognized" });
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { status = "error", exception = ex.Message });
        }
    }
}

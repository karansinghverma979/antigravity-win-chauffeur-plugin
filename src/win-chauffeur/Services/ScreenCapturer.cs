using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WinChauffeur.Core;

namespace WinChauffeur.Services;

public static class ScreenCapturer
{
    public static string CaptureScreen(string? outputPath = null, int x = 0, int y = 0, int width = 0, int height = 0)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "win-chauffeur");
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            outputPath = Path.Combine(tempDir, $"capture_{DateTime.Now:yyyyMMdd_HHmmssfff}.png");
        }

        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return StationBridge.ExecuteOnDesktop(() =>
        {
            int screenW = Win32.GetSystemMetrics(0); // SM_CXSCREEN
            int screenH = Win32.GetSystemMetrics(1); // SM_CYSCREEN

            int vW = Win32.GetSystemMetrics(78); // SM_CXVIRTUALSCREEN
            int vH = Win32.GetSystemMetrics(79); // SM_CYVIRTUALSCREEN
            int vX = Win32.GetSystemMetrics(76); // SM_XVIRTUALSCREEN
            int vY = Win32.GetSystemMetrics(77); // SM_YVIRTUALSCREEN

            int totalW = vW > 0 ? vW : screenW;
            int totalH = vH > 0 ? vH : screenH;

            int targetX = (width == 0 && height == 0) ? (vW > 0 ? vX : 0) : x;
            int targetY = (width == 0 && height == 0) ? (vH > 0 ? vY : 0) : y;
            int targetW = width > 0 ? width : totalW;
            int targetH = height > 0 ? height : totalH;

            using (Bitmap bmp = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(targetX, targetY, 0, 0, new Size(targetW, targetH), CopyPixelOperation.SourceCopy);
                }
                bmp.Save(outputPath, ImageFormat.Png);
            }

            return outputPath;
        });
    }

    public static string CaptureWindow(string query, string? outputPath = null)
    {
        WindowInfo? win = WindowManager.FindWindow(query);
        if (win == null) throw new ArgumentException($"Window matching '{query}' not found.");

        int x = win.Bounds.Left;
        int y = win.Bounds.Top;
        int w = win.Bounds.Width;
        int h = win.Bounds.Height;

        return CaptureScreen(outputPath, x, y, w, h);
    }
}

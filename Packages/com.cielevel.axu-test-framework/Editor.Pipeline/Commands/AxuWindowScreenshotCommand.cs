#if !UNITY_6000_7_OR_NEWER
// ============================================================================
// axu_window_screenshot —— Windows 专属的后台无感窗口截图命令
// ============================================================================
// 【适用范围 / 重要】
//   1. 仅 Windows。macOS 无需对应实现（2026-08-31 决策）：下方平台守卫先于任何 Win32 调用返回
//      说明文本，不崩溃无副作用；Mac 截图由跨平台路径覆盖（AxuCapture + pipeline 的
//      capture_game_view/capture_scene_view/screenshot）。若 <6000.7 的 Mac 确需任意窗口后台截图，
//      优先升级 6000.7+ 改用 capture_editor_element，勿另写 CGWindowListCreateImage 变体；
//   2. 仅适用于没有 CaptureToRenderTexture 能力的 Unity 版本（< 6000.7）。
//      6000.7+ 存在引擎级离屏渲染 API（VisualElementCaptureExtensions）与
//      pipeline 自带的 capture_editor_element 命令（跨平台、无需窗口可见），
//      因此本文件在该版本起整体不参与编译（见文件顶部条件编译）。
//
// 【为什么存在】
//   pipeline 自带的 screenshot / capture_* 命令只覆盖 Game/Scene 视图；
//   capture_editor_element 在 < 6000.7 不存在。本命令补齐"后台截取任意
//   EditorWindow"的能力：窗口可以 被遮挡 / 失焦 / 不在前台，全程不抢焦点、
//   不置顶、不干扰用户当前操作（已在本工程 Windows 实测）。
//
// 【原理与限制】
//   PrintWindow(hwnd, memDC, PW_RENDERFULLCONTENT) 让窗口把自身内容离屏
//   渲染进内存位图（不要求窗口在屏幕上可见），再经 GetDIBits（自上而下）
//   或 GetBitmapBits（自下而上，需翻转行序）读回像素编码 PNG。
//   限制：最小化或已关闭的窗口无法截取（后者可经 menu 参数自动重开）；
//   停靠（docked）状态的窗口没有独立 OS 窗口，也无法截取。
// ============================================================================
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    public static class AxuWindowScreenshotCommand
    {
        private const uint PW_RENDERFULLCONTENT = 0x2;
        private const uint DIB_RGB_COLORS = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;   // 负值 = 自上而下
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern int PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint lines, int[] bits, ref BITMAPINFOHEADER bmi, uint usage);

        [DllImport("gdi32.dll")]
        private static extern int GetBitmapBits(IntPtr hbm, int cb, int[] bits);

        [CliCommand("axu_window_screenshot",
            "Windows-only: capture an EditorWindow (by exact title) to PNG via PrintWindow while it stays occluded/unfocused. For Unity versions without CaptureToRenderTexture (< 6000.7); on 6000.7+ prefer capture_editor_element.",
            MainThreadRequired = true,
            Tags = new[] { "capture", "axu" })]
        public static string CaptureWindow(
            [CliArg("window", "Exact EditorWindow title (titleContent.text), e.g. AxuTestRunner")] string window = "",
            [CliArg("menu", "Optional menu path used to (re)open the window when it is not found, e.g. AxuFrameKit/AxuTestRunner")] string menu = "",
            [CliArg("output", "Optional PNG path (absolute or relative to project root); defaults to a timestamped file under Temp/pipeline-screenshots/")] string output = "")
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                return "Error: axu_window_screenshot is Windows-only. macOS needs no equivalent: screenshots are covered cross-platform (AxuCapture, capture_game_view/capture_scene_view/screenshot); for arbitrary EditorWindows use capture_editor_element on Unity 6000.7+.";
            if (string.IsNullOrWhiteSpace(window))
                return "Error: 'window' (exact EditorWindow title) is required.";

            var target = ResolveWindow(window);
            if (target == null && !string.IsNullOrWhiteSpace(menu))
            {
                EditorApplication.ExecuteMenuItem(menu); // 自动开窗（GetWindow 会聚焦既有实例）
                target = ResolveWindow(window);
            }
            if (target == null)
                return "Error: no open EditorWindow matched title '" + window + "'.";

            // Unity 浮动窗口的 Win32 标题可能为空或不等于 EditorWindow 标题，
            // 因此不按标题找 HWND，而是按 进程PID + 可见 + 客户区尺寸最优匹配 定位
            var hwnd = FindWindowHandleOf(target, out var candidatesDump);
            if (hwnd == IntPtr.Zero)
                return "Error: native top-level window for '" + window + "' not found (docked windows have no own OS window - undock it first). Visible top-level windows of this process: " + candidatesDump + ".";

            try { target.Focus(); } catch { /* 无关紧要：截图不依赖焦点 */ }
            target.Repaint();

            GetWindowRect(hwnd, out RECT r);
            int w = r.Right - r.Left;
            int h = r.Bottom - r.Top;
            if (w <= 0 || h <= 0)
                return "Error: invalid window rect " + w + "x" + h + ".";

            // PrintWindow 离屏渲染：窗口可以处于被遮挡/失焦状态
            var log = new StringBuilder();
            var hdcWindow = GetDC(hwnd);
            var hdcMem = CreateCompatibleDC(hdcWindow);
            var hBmp = CreateCompatibleBitmap(hdcWindow, w, h);
            var oldBmp = SelectObject(hdcMem, hBmp);
            var pw = PrintWindow(hwnd, hdcMem, PW_RENDERFULLCONTENT);
            log.Append("pw=").Append(pw);

            var px = new int[w * h];
            bool gotPixels = false;
            bool flipRows = false;
            if (pw != 0)
            {
                // 路线 A：GetDIBits（biHeight 负值 = 自上而下，无需翻转）。
                // 注意：GetDIBits 要求位图此刻未选入任何 DC
                SelectObject(hdcMem, oldBmp);
                var bmi = new BITMAPINFOHEADER
                {
                    biSize = 40,
                    biWidth = w,
                    biHeight = -h,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0
                };
                var gd = GetDIBits(hdcWindow, hBmp, 0, (uint)h, px, ref bmi, DIB_RGB_COLORS);
                log.Append(" getDIBits=").Append(gd);
                if (gd != 0)
                {
                    gotPixels = true;
                    // 实测即使请求自上而下(负 biHeight)，部分驱动/组合下数据仍为自下而上，统一翻转
                    flipRows = true;
                }

                // 路线 B：GetBitmapBits（自下而上，需翻转行序）。
                // 实测部分 Mono/系统组合下 GetDIBits 静默失败，此路线兜底
                if (!gotPixels)
                {
                    var gbb = GetBitmapBits(hBmp, px.Length * 4, px);
                    log.Append(" getBitmapBits=").Append(gbb);
                    if (gbb == px.Length * 4)
                    {
                        gotPixels = true;
                        flipRows = true;
                    }
                }
            }

            SelectObject(hdcMem, oldBmp);
            DeleteObject(hBmp);
            DeleteDC(hdcMem);
            ReleaseDC(hwnd, hdcWindow);

            log.Append(" W=").Append(w).Append(" H=").Append(h);
            if (pw == 0 || !gotPixels)
                return log.Append(" => CAPTURE-FAILED").ToString();

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var c32 = new Color32[px.Length];
            for (int y = 0; y < h; y++)
            {
                int srcRow = flipRows ? h - 1 - y : y;
                for (int x = 0; x < w; x++)
                {
                    uint v = (uint)px[srcRow * w + x];
                    byte b = (byte)v;
                    byte g = (byte)(v >> 8);
                    byte rr = (byte)(v >> 16);
                    byte a = (byte)(v >> 24);
                    c32[y * w + x] = new Color32(rr, g, b, a == 0 ? (byte)255 : a);
                }
            }
            tex.SetPixels32(c32);
            tex.Apply(false, false);
            var png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            var path = ResolveOutputPath(output, "axu_win_" + SanitizeTitle(window));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, png);
            log.Append(" => OK bytes=").Append(png.Length).Append(" path=").Append(path);
            return log.ToString();
        }

        /// <summary>
        /// 枚举本进程可见顶层窗口，按 客户区尺寸最优匹配 定位目标窗口对应的原生 HWND。
        /// Unity 浮动窗口的 Win32 标题可能为空，不能按标题匹配；OS 矩形含边框/标题栏，
        /// 与 EditorWindow.position(客户区) 差一个帧值，因此给足容差、取差值最小者。
        /// </summary>
        private static IntPtr FindWindowHandleOf(EditorWindow target, out string candidatesDump)
        {
            const uint GW_HWNDNEXT = 2;
            uint myPid = (uint)Process.GetCurrentProcess().Id;
            var titles = new StringBuilder();
            var pos = target.position;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            int clientW = (int)(pos.width * ppp);
            int clientH = (int)(pos.height * ppp);
            var found = IntPtr.Zero;
            int bestScore = int.MaxValue;
            var cur = GetTopWindow(IntPtr.Zero);
            int scanned = 0;
            var rc = new RECT();
            while (cur != IntPtr.Zero && scanned < 1024)
            {
                scanned++;
                if (IsWindowVisible(cur) && GetWindowThreadProcessId(cur, out uint pid) != 0u && pid == myPid)
                {
                    GetWindowRect(cur, out rc);
                    int osW = rc.Right - rc.Left;
                    int osH = rc.Bottom - rc.Top;
                    bool isMainWindow = osW > 2400 && osH > 1300; // 最大化主编辑器窗口
                    if (!isMainWindow && osW > 400 && osH > 300)
                    {
                        titles.Append('[').Append(osW).Append('x').Append(osH).Append("] ");
                        int score = Math.Abs(osW - clientW) + Math.Abs(osH - clientH);
                        if (score < bestScore) { bestScore = score; found = cur; }
                    }
                }
                cur = GetWindow(cur, GW_HWNDNEXT);
            }
            candidatesDump = "client=" + clientW + "x" + clientH + " candidates: " + titles;
            return found;
        }

        private static EditorWindow ResolveWindow(string title)
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w.titleContent != null && w.titleContent.text == title
                    && w.rootVisualElement != null && w.rootVisualElement.panel != null)
                    return w;
            }
            return null;
        }

        private static string SanitizeTitle(string title)
        {
            var arr = title.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (!char.IsLetterOrDigit(arr[i])) arr[i] = '_';
            }
            return new string(arr);
        }

        private static string ResolveOutputPath(string output, string prefix)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrWhiteSpace(output))
                return Path.IsPathRooted(output) ? output : Path.GetFullPath(Path.Combine(projectRoot, output));
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            return Path.Combine(projectRoot, "Temp", "pipeline-screenshots", prefix + "_" + stamp + ".png");
        }
    }
}
#endif

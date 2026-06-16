using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace AxuFrameKit.Common.Utility.Misc
{
    /// <summary>
    /// 该脚本强制Unity游戏窗口保持设置的宽高比。这意味着虽然您可以调整窗口大小，
    /// 但它将始终保持您设置的宽高比。
    /// 
    /// 这是通过拦截窗口调整大小事件（WindowProc回调）并相应地修改它们来实现的。
    /// 您还可以设置窗口的最小/最大宽度和高度（以像素为单位）。
    /// 宽高比和最小/最大分辨率都指的是游戏区域，因此，正如预期的那样，窗口
    /// 标题栏和边框不包括在内。
    ///
    /// 此脚本还将在全屏模式下强制执行宽高比。当您切换到全屏时，
    /// 应用程序将自动设置为当前显示器上最大可能的分辨率，同时保持
    /// 宽高比。如果显示器的宽高比不同，黑色条将被添加到左/右或上/下。
    ///
    /// 确保在播放器设置中激活“Resizable Window”（可调整大小的窗口），否则您的窗口将无法调整大小。
    /// 您可能还想在播放器设置的“Supported Aspect Ratios”（支持的宽高比）下取消选中所有不受支持的宽高比。
    /// 
    /// 注意：这使用了WinAPI，所以它只能在Windows上运行。在Windows 10上进行了测试，也应该适用于所有较新的版本。
    /// </summary>
    public class AspectRatioController : MonoBehaviour
    {
        /// <summary>
        /// 每当窗口分辨率更改或用户切换全屏时，都会触发此事件。
        /// 参数是新的宽度、高度和全屏状态（true表示全屏）。
        /// </summary>
        public ResolutionChangedEvent resolutionChangedEvent;
        [Serializable]
        public class ResolutionChangedEvent : UnityEvent<int, int, bool> { }

        // 如果为false，则阻止切换到全屏。
        [SerializeField]
        private bool allowFullscreen = true;

        // 宽高比的宽度和高度。
        [SerializeField]
        private float aspectRatioWidth = 16;
        [SerializeField]
        private float aspectRatioHeight = 9;

        // 窗口宽度/高度的最小和最大值（以像素为单位）。
        [SerializeField]
        private int minWidthPixel = 512;
        [SerializeField]
        private int minHeightPixel = 512;
        [SerializeField]
        private int maxWidthPixel = 2048;
        [SerializeField]
        private int maxHeightPixel = 2048;

        // 当前锁定的宽高比。
        private float _aspect;

        // 游戏区域的宽度和高度。这不包括边框和窗口标题栏。
        // 一旦用户调整窗口大小，就会设置这些值。
        private int _setWidth = -1;
        private int _setHeight = -1;

        // 上一帧的全屏状态。
        private bool _wasFullscreenLastFrame;

        // AspectRatioController是否已初始化？
        // 一旦注册了WindowProc回调，就会设置为true。
        private bool _started;

        // 活动显示器的宽度和高度。这是窗口当前所在的显示器。
        private int _pixelHeightOfCurrentScreen;
        private int _pixelWidthOfCurrentScreen;

        // 一旦用户请求终止应用程序，就会设置为true。
        private bool _quitStarted;

        // WinAPI相关定义。
        #region WINAPI

        // 当窗口调整大小时，WM_SIZING消息通过WindowProc回调发送到窗口。
        private const int WmSizing = 0x214;

        // WM_SIZING消息的参数。
        private const int WmszLeft = 1;
        private const int WmszRight = 2;
        private const int WmszTop = 3;
        private const int WmszBottom = 6;

        // 检索指向WindowProc函数的指针。
        private const int GwlpWndproc = -4;

        // 委托设置为新的WindowProc回调函数。
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate _wndProcDelegate;

        // 检索调用线程的线程标识符。
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        // 检索指定窗口所属的类的名称。
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // 枚举与线程关联的所有非子窗口，方法是将每个窗口的句柄依次传递给应用程序定义的回调函数。
        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // 将消息信息传递给指定的窗口过程。
        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // 检索指定窗口的边界矩形的尺寸。
        // 尺寸以相对于屏幕左上角的屏幕坐标给出。
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, ref Rect lpRect);

        // 检索窗口客户区的坐标。客户端坐标指定客户区
        // 的左上角和右下角。因为客户端坐标是相对于窗口客户区的左上角
        // 的，所以左上角的坐标是(0,0)。
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, ref Rect lpRect);

        // 更改指定窗口的属性。该函数还在额外的窗口内存中的指定偏移量处
        // 设置32位（long）值。
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // 更改指定窗口的属性。该函数还在额外的窗口内存中的指定偏移量处
        // 设置一个值。 
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // 用于查找窗口句柄的Unity窗口类的名称。
        private const string UnityWndClassname = "UnityWndClass";

        // Unity窗口的窗口句柄。
        private IntPtr _unityHWnd;

        // 指向旧WindowProc回调函数的指针。
        private IntPtr _oldWndProcPtr;

        // 指向我们自己的WindowProc回调函数的指针。
        private IntPtr _newWndProcPtr;

        /// <summary>
        /// WinAPI RECT 定义。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        /// <summary>
        /// 由Unity调用。
        /// </summary>
        void Start()
        {
            // 不要在Unity编辑器中注册WindowProc回调，因为它会引用
            // Unity编辑器窗口，而不是实际的游戏窗口。
            if (Application.isEditor)
            {
                return;
            }

            // 注册当应用程序想要退出时的回调。
            Application.wantsToQuit += ApplicationWantsToQuit;

            // 查找主Unity窗口的窗口句柄。
            EnumThreadWindows(GetCurrentThreadId(), (hWnd, lParam) =>
            {
                var classText = new StringBuilder(UnityWndClassname.Length + 1);
                GetClassName(hWnd, classText, classText.Capacity);

                if (classText.ToString() == UnityWndClassname)
                {
                    _unityHWnd = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            // 将宽高比应用于当前分辨率。
            SetAspectRatio(aspectRatioWidth, aspectRatioHeight, true);

            // 保存当前全屏状态。
            _wasFullscreenLastFrame = Screen.fullScreen;

            // 注册（替换）WindowProc回调。每当触发窗口事件时都会调用此回调，
            // 例如调整大小或移动窗口。
            // 另外保存旧的WindowProc回调，因为我们必须从我们自己的新回调中调用它。
            _wndProcDelegate = WndProc;
            _newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _oldWndProcPtr = SetWindowLong(_unityHWnd, GwlpWndproc, _newWndProcPtr);

            // 初始化完成。
            _started = true;
        }

        /// <summary>
        /// 将目标宽高比设置为给定的宽高比。
        /// </summary>
        /// <param name="newAspectWidth">宽高比的新宽度。</param>
        /// <param name="newAspectHeight">宽高比的新高度。</param>
        /// <param name="apply">如果为true，则当前窗口分辨率会立即调整以匹配新的
        /// 宽高比。如果为false，则仅在下次手动调整窗口大小时执行此操作。</param>
        public void SetAspectRatio(float newAspectWidth, float newAspectHeight, bool apply)
        {
            // 计算新的宽高比。
            aspectRatioWidth = newAspectWidth;
            aspectRatioHeight = newAspectHeight;
            _aspect = aspectRatioWidth / aspectRatioHeight;

            // 如果请求，调整分辨率以匹配宽高比（这将触发WindowProc回调）。
            if (apply)
            {
                Screen.SetResolution(Screen.width, Mathf.RoundToInt(Screen.width / _aspect), Screen.fullScreen);
            }
        }

        /// <summary>
        /// WindowProc回调。处理发送到窗口的消息的应用程序定义函数。
        /// </summary>
        /// <param name="hWnd">窗口的句柄。</param>
        /// <param name="msg">用于标识事件的消息。</param>
        /// <param name="wParam">附加消息信息。此参数的内容
        /// 取决于uMsg参数的值。</param>
        /// <param name="lParam">附加消息信息。此参数的内容
        /// 取决于uMsg参数的值。</param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // 检查消息类型。
            // 我们只对调整大小事件感兴趣，所以忽略其他所有内容。
            if (msg == WmSizing)
            {
                // 获取窗口大小结构体。
                Rect rc = (Rect)Marshal.PtrToStructure(lParam, typeof(Rect));

                // 计算窗口边框宽度和高度。
                Rect windowRect = new Rect();
                GetWindowRect(_unityHWnd, ref windowRect);

                Rect clientRect = new Rect();
                GetClientRect(_unityHWnd, ref clientRect);

                int borderWidth = windowRect.Right - windowRect.Left - (clientRect.Right - clientRect.Left);
                int borderHeight = windowRect.Bottom - windowRect.Top - (clientRect.Bottom - clientRect.Top);

                // 在应用宽高比之前移除边框（包括窗口标题栏）。
                rc.Right -= borderWidth;
                rc.Bottom -= borderHeight;

                // 限制窗口大小。
                int newWidth = Mathf.Clamp(rc.Right - rc.Left, minWidthPixel, maxWidthPixel);
                int newHeight = Mathf.Clamp(rc.Bottom - rc.Top, minHeightPixel, maxHeightPixel);

                // 根据宽高比和调整大小的方向进行调整。
                switch (wParam.ToInt32())
                {
                    case WmszLeft:
                        rc.Left = rc.Right - newWidth;
                        rc.Bottom = rc.Top + Mathf.RoundToInt(newWidth / _aspect);
                        break;
                    case WmszRight:
                        rc.Right = rc.Left + newWidth;
                        rc.Bottom = rc.Top + Mathf.RoundToInt(newWidth / _aspect);
                        break;
                    case WmszTop:
                        rc.Top = rc.Bottom - newHeight;
                        rc.Right = rc.Left + Mathf.RoundToInt(newHeight * _aspect);
                        break;
                    case WmszBottom:
                        rc.Bottom = rc.Top + newHeight;
                        rc.Right = rc.Left + Mathf.RoundToInt(newHeight * _aspect);
                        break;
                    case WmszRight + WmszBottom:
                        rc.Right = rc.Left + newWidth;
                        rc.Bottom = rc.Top + Mathf.RoundToInt(newWidth / _aspect);
                        break;
                    case WmszRight + WmszTop:
                        rc.Right = rc.Left + newWidth;
                        rc.Top = rc.Bottom - Mathf.RoundToInt(newWidth / _aspect);
                        break;
                    case WmszLeft + WmszBottom:
                        rc.Left = rc.Right - newWidth;
                        rc.Bottom = rc.Top + Mathf.RoundToInt(newWidth / _aspect);
                        break;
                    case WmszLeft + WmszTop:
                        rc.Left = rc.Right - newWidth;
                        rc.Top = rc.Bottom - Mathf.RoundToInt(newWidth / _aspect);
                        break;
                }

                // 保存实际的Unity游戏区域分辨率。
                // 这不包括边框。
                _setWidth = rc.Right - rc.Left;
                _setHeight = rc.Bottom - rc.Top;

                // 加回边框。
                rc.Right += borderWidth;
                rc.Bottom += borderHeight;

                // 触发分辨率更改事件。
                resolutionChangedEvent.Invoke(_setWidth, _setHeight, Screen.fullScreen);

                // 写回更改后的窗口参数。
                Marshal.StructureToPtr(rc, lParam, true);
            }

            // 调用原始WindowProc函数。
            return CallWindowProc(_oldWndProcPtr, hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        void Update()
        {
            // 如果不允许全屏，则阻止切换到全屏。
            if (!allowFullscreen && Screen.fullScreen)
            {
                Screen.fullScreen = false;
            }

            if (Screen.fullScreen && !_wasFullscreenLastFrame)
            {
                // 检测到切换到全屏。设置为最大屏幕分辨率，同时保持宽高比。
                int height;
                int width;

                // 检查需要在哪里添加黑边。根据当前的宽高比和显示器的实际宽高比，
                // 黑边将出现在左/右或上/下。
                // 当然，如果宽高比完全匹配，也可能根本没有黑边。
                bool blackBarsLeftRight = _aspect < (float)_pixelWidthOfCurrentScreen / _pixelHeightOfCurrentScreen;

                if (blackBarsLeftRight)
                {
                    height = _pixelHeightOfCurrentScreen;
                    width = Mathf.RoundToInt(_pixelHeightOfCurrentScreen * _aspect);
                }
                else
                {
                    width = _pixelWidthOfCurrentScreen;
                    height = Mathf.RoundToInt(_pixelWidthOfCurrentScreen / _aspect);
                }

                Screen.SetResolution(width, height, true);
                resolutionChangedEvent.Invoke(width, height, true);
            }
            else if (!Screen.fullScreen && _wasFullscreenLastFrame)
            {
                // 检测到从全屏切换到窗口。设置以前的窗口分辨率。
                Screen.SetResolution(_setWidth, _setHeight, false);
                resolutionChangedEvent.Invoke(_setWidth, _setHeight, false);
            }
            else if (!Screen.fullScreen && _setWidth != -1 && _setHeight != -1 && (Screen.width != _setWidth || Screen.height != _setHeight))
            {
                // 检测到Aero Snap。根据高度设置宽度。
                // 必须这样做，因为Aero Snap不会触发WM_SIZING。
                _setHeight = Screen.height;
                _setWidth = Mathf.RoundToInt(Screen.height * _aspect);

                // Aero Snap也就是“贴靠窗口”在调整大小时总是会被触发。它会导致闪烁、卡顿、调整大小时居中，并且在调整大小过程中不平滑。
                // 为了解决这个问题，我们需要在宽高比偏离目标宽高比时更改分辨率。
                // 这将如丝般顺滑地调整大小，并禁用Aero Snap即“贴靠窗口”。
                if (!IsAspectRatioValid(Screen.width, Screen.height))
                {
                    Screen.SetResolution(_setWidth, _setHeight, Screen.fullScreen);
                    resolutionChangedEvent.Invoke(_setWidth, _setHeight, Screen.fullScreen);
                }

                bool IsAspectRatioValid(int screenWidth, int screenHeight)
                {
                    float currentAspectRatio = (float)screenWidth / screenHeight;
                    float targetAspectRatio = (aspectRatioWidth / aspectRatioHeight);
                    float tolerance = 0.01f;

                    if (Mathf.Abs(currentAspectRatio - targetAspectRatio) > tolerance) // 由于浮点精度原因，请使用小容差进行检查
                    {
                        // 当前分辨率偏离目标宽高比。
                        return false;
                    }
                    else
                    {
                        // 当前分辨率符合目标宽高比。
                        return true;
                    }
                }
            }
            else if (!Screen.fullScreen)
            {
                // 保存当前屏幕的分辨率。
                // 下次切换到全屏时，此分辨率将被设置为窗口分辨率。
                // 如果需要，仅保留高度，宽度将根据高度和宽高比进行设置，以确保在全屏模式下
                // 也能保持宽高比。
                _pixelHeightOfCurrentScreen = Screen.currentResolution.height;
                _pixelWidthOfCurrentScreen = Screen.currentResolution.width;
            }

            // 保存全屏状态以供下一帧使用。
            _wasFullscreenLastFrame = Screen.fullScreen;

            // 当游戏窗口调整大小时，在编辑器中触发分辨率更改事件。
#if UNITY_EDITOR
            if (Screen.width != _setWidth || Screen.height != _setHeight)
            {
                _setWidth = Screen.width;
                _setHeight = Screen.height;
                resolutionChangedEvent.Invoke(_setWidth, _setHeight, Screen.fullScreen);
            }
#endif
        }

        /// <summary>
        /// 调用SetWindowLong32或SetWindowLongPtr64，具体取决于可执行文件是32位还是64位。
        /// 这样，我们就可以构建32位和64位可执行文件而不会遇到问题。
        /// </summary>
        /// <param name="hWnd">窗口处理句柄。</param>
        /// <param name="nIndex">要设置的值的从零开始的偏移量。</param>
        /// <param name="dwNewLong">替换值。</param>
        /// <returns>如果函数成功，返回值为指定偏移量的上一个值。否则为零。</returns>
        private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            // 检查IntPtr有多长。4字节意味着我们在32位上，所以相应地调用函数。
            if (IntPtr.Size == 4)
            {
                return SetWindowLong32(hWnd, nIndex, dwNewLong);
            }
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }

        /// <summary>
        /// 一旦应用程序想要退出，由Unity调用。
        /// 返回false将中止并保持应用程序处于活动状态。True将允许它退出。
        /// </summary>
        private bool ApplicationWantsToQuit()
        {
            // 仅在应用程序初始化后允许退出。
            if (!_started)
                return false;

            // 延迟退出，以便我们可以进行清理。
            if (!_quitStarted)
            {
                StartCoroutine("DelayedQuit");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 恢复旧的WindowProc回调，然后退出。
        /// </summary>
        IEnumerator DelayedQuit()
        {
            // 重新设置旧的WindowProc回调。通常，这将在检测到WM_CLOSE后
            // 在新回调本身中完成。这在64位上似乎工作正常，但是当我构建32位
            // 可执行文件时，这会导致应用程序在退出时崩溃。
            // 这真的不应该发生，我不确定为什么会这样。
            // 但是，这个解决方案在32位和64位上似乎都工作正常。
            SetWindowLong(_unityHWnd, GwlpWndproc, _oldWndProcPtr);

            // 等待帧结束（我们的回调现在已取消注册），然后允许应用程序退出。
            yield return new WaitForEndOfFrame();

            _quitStarted = true;
            Application.Quit();
        }
    }
}
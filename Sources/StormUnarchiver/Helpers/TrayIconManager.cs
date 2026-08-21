using System.Runtime.InteropServices;

namespace StormUnarchiver.Helpers;

public enum TrayState { Idle, Active, Error }

public class TrayIconManager : IDisposable
{
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 1;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_COMMAND = 0x0111;
    private const int WM_DESTROY = 0x0002;

    private const int NIF_MESSAGE = 0x01;
    private const int NIF_ICON = 0x02;
    private const int NIF_TIP = 0x04;
    private const int NIF_INFO = 0x10;
    private const int NIM_ADD = 0x00;
    private const int NIM_MODIFY = 0x01;
    private const int NIM_DELETE = 0x02;

    private const int NIIF_INFO = 0x01;

    private const int MF_STRING = 0x00;
    private const int MF_SEPARATOR = 0x0800;
    private const int TPM_BOTTOMALIGN = 0x0020;
    private const int TPM_LEFTALIGN = 0x0000;

    private const int IDM_SHOW = 1001;
    private const int IDM_EXIT = 1002;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    private IntPtr _hWnd;
    private IntPtr _hIcon;
    private IntPtr _hMenu;
    private readonly Action _onShowWindow;
    private readonly Action _onExitApp;
    private WndProcDelegate? _wndProcDelegate;
    private bool _iconAdded;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public TrayIconManager(Action onShowWindow, Action onExitApp)
    {
        _onShowWindow = onShowWindow;
        _onExitApp = onExitApp;
    }

    public void Initialize()
    {
        _wndProcDelegate = WndProc;
        _hIcon = CreateStormIcon();
        _hMenu = CreatePopupMenu();
        AppendMenuW(_hMenu, MF_STRING, IDM_SHOW, "Показать окно");
        AppendMenuW(_hMenu, MF_SEPARATOR, 0, null);
        AppendMenuW(_hMenu, MF_STRING, IDM_EXIT, "Выход");

        // Register window class
        var className = "StormUnarchiverTrayClass";
        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = GetModuleHandleW(null),
            lpszClassName = className
        };
        RegisterClassExW(ref wc);

        // Create message-only window
        _hWnd = CreateWindowExW(0, className, "StormTray", 0,
            0, 0, 0, 0, new IntPtr(-3) /* HWND_MESSAGE */, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

        // Add tray icon
        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _hIcon,
            szTip = "STORM UNARCHIVER"
        };
        Shell_NotifyIconW(NIM_ADD, ref nid);
        _iconAdded = true;
    }

    public void ShowBalloon(string title, string message)
    {
        if (!_iconAdded) return;

        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = 1,
            uFlags = NIF_INFO,
            szInfoTitle = title,
            szInfo = message,
            dwInfoFlags = NIIF_INFO
        };
        Shell_NotifyIconW(NIM_MODIFY, ref nid);
    }

    /// <summary>
    /// #12 — Update tray icon to reflect monitoring state (green=active, gray=idle, red=error).
    /// </summary>
    public void SetMonitoringState(TrayState state)
    {
        if (!_iconAdded) return;

        var newIcon = CreateStormIcon(state);
        var tip = state switch
        {
            TrayState.Active => "STORM UNARCHIVER — Мониторинг...",
            TrayState.Error => "STORM UNARCHIVER — Ошибка!",
            _ => "STORM UNARCHIVER — Ожидание"
        };

        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = 1,
            uFlags = NIF_ICON | NIF_TIP,
            hIcon = newIcon,
            szTip = tip
        };
        Shell_NotifyIconW(NIM_MODIFY, ref nid);

        // Free old icon, keep new
        if (_hIcon != IntPtr.Zero) DestroyIcon(_hIcon);
        _hIcon = newIcon;
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_TRAYICON:
                var mouseMsg = (int)(lParam.ToInt64() & 0xFFFF);
                if (mouseMsg == WM_LBUTTONDBLCLK)
                {
                    _onShowWindow();
                }
                else if (mouseMsg == WM_RBUTTONUP)
                {
                    GetCursorPos(out var pt);
                    SetForegroundWindow(_hWnd);
                    TrackPopupMenu(_hMenu, TPM_LEFTALIGN | TPM_BOTTOMALIGN,
                        pt.X, pt.Y, 0, _hWnd, IntPtr.Zero);
                }
                return IntPtr.Zero;

            case WM_COMMAND:
                var cmdId = (int)(wParam.ToInt64() & 0xFFFF);
                if (cmdId == IDM_SHOW)
                    _onShowWindow();
                else if (cmdId == IDM_EXIT)
                    _onExitApp();
                return IntPtr.Zero;
        }
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private static IntPtr CreateStormIcon(TrayState state = TrayState.Idle)
    {
        string iconFileName = state switch
        {
            TrayState.Active => "tray_active.ico",
            TrayState.Error => "tray_error.ico",
            _ => "tray_idle.ico"
        };

        try
        {
            var assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", iconFileName);
            if (File.Exists(assetPath))
            {
                IntPtr hIcon = LoadImageW(IntPtr.Zero, assetPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
                if (hIcon != IntPtr.Zero) return hIcon;
            }

            var appIcoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(appIcoPath))
            {
                IntPtr hIcon = LoadImageW(IntPtr.Zero, appIcoPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
                if (hIcon != IntPtr.Zero) return hIcon;
            }
        }
        catch { }

        // Fallback procedural icon
        byte r, g, b;
        switch (state)
        {
            case TrayState.Active:
                r = 74; g = 222; b = 128; // Green
                break;
            case TrayState.Error:
                r = 248; g = 113; b = 113; // Red
                break;
            default:
                r = 76; g = 201; b = 240; // Cyan (idle/default)
                break;
        }

        int size = 16;
        var pixels = new byte[size * size * 4];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = (y * size + x) * 4;
                bool isLightning = IsLightningPixel(x, y, size);

                if (isLightning)
                {
                    pixels[idx + 0] = b;
                    pixels[idx + 1] = g;
                    pixels[idx + 2] = r;
                    pixels[idx + 3] = 255;
                }
                else
                {
                    int cx = size / 2, cy = size / 2;
                    double dist = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= 7)
                    {
                        pixels[idx + 0] = 46;
                        pixels[idx + 1] = 27;
                        pixels[idx + 2] = 26;
                        pixels[idx + 3] = 255;
                    }
                }
            }
        }

        var hBitmap = CreateBitmap(size, size, 1, 32, pixels);
        var hMask = CreateBitmap(size, size, 1, 1, null);

        var iconInfo = new ICONINFO
        {
            fIcon = true,
            hbmMask = hMask,
            hbmColor = hBitmap
        };
        return CreateIconIndirect(ref iconInfo);
    }

    private static bool IsLightningPixel(int x, int y, int size)
    {
        // Simple lightning bolt shape
        if (y >= 1 && y <= 3 && x >= 6 && x <= 10) return true;   // top
        if (y >= 4 && y <= 6 && x >= 5 && x <= 9) return true;    // upper-mid
        if (y >= 7 && y <= 8 && x >= 4 && x <= 11) return true;   // wide middle bar
        if (y >= 9 && y <= 11 && x >= 6 && x <= 10) return true;  // lower-mid
        if (y >= 12 && y <= 14 && x >= 7 && x <= 9) return true;  // bottom
        return false;
    }

    public void Dispose()
    {
        if (_iconAdded)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1
            };
            Shell_NotifyIconW(NIM_DELETE, ref nid);
            _iconAdded = false;
        }

        if (_hMenu != IntPtr.Zero)
        {
            DestroyMenu(_hMenu);
            _hMenu = IntPtr.Zero;
        }

        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }

        if (_hWnd != IntPtr.Zero)
        {
            DestroyWindow(_hWnd);
            _hWnd = IntPtr.Zero;
        }
    }

    // ===== P/Invoke Declarations =====

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIconW(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEX lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
        IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, int uFlags, int uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y,
        int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImageW(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint nPlanes, uint nBitCount, byte[]? lpBits);
}

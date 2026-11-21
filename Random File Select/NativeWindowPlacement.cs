using System.Runtime.InteropServices;

namespace Random_File_Select;

public static class NativeWindowPlacement
{
    private const int SwShowNormal = 1;
    private const int SwShowMinimized = 2;
    private const int SwShowMaximized = 3;

    public static WindowPlacementSettings? Get(IntPtr hwnd)
    {
        var placement = new WINDOWPLACEMENT
        {
            length = Marshal.SizeOf<WINDOWPLACEMENT>()
        };

        if (!GetWindowPlacement(hwnd, ref placement))
            return null;

        RECT rc = placement.rcNormalPosition;
        return new WindowPlacementSettings
        {
            NormalLeft = rc.Left,
            NormalTop = rc.Top,
            NormalRight = rc.Right,
            NormalBottom = rc.Bottom,
            ShowCmd = placement.showCmd
        };
    }

    public static void Set(IntPtr hwnd, WindowPlacementSettings saved)
    {
        if (saved.NormalRight <= saved.NormalLeft || saved.NormalBottom <= saved.NormalTop)
            return;

        int showCmd = MapShowCmdForRestore(saved.ShowCmd);

        var placement = new WINDOWPLACEMENT
        {
            length = Marshal.SizeOf<WINDOWPLACEMENT>(),
            flags = 0,
            showCmd = showCmd,
            ptMinPosition = default,
            ptMaxPosition = default,
            rcNormalPosition = new RECT
            {
                Left = saved.NormalLeft,
                Top = saved.NormalTop,
                Right = saved.NormalRight,
                Bottom = saved.NormalBottom
            }
        };

        SetWindowPlacement(hwnd, ref placement);
    }

    private static int MapShowCmdForRestore(int showCmd) =>
        showCmd == SwShowMaximized ? SwShowMaximized : SwShowNormal;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }
}
using System;
using System.Runtime.InteropServices;

namespace CC98.Services.Helpers;

/// <summary>
///     提供 Win32 方法的封装。该类型为静态类型。
///     引入新API后，此类将被废弃。
/// </summary>
public static partial class NativeMethods
{
    public const int GwlStyle = -16;
    public const int WsThickframe = 0x00040000;
    public const uint SwpNomove = 0x0002;
    public const uint SwpNozorder = 0x0004;
    public const uint SwpFramechanged = 0x0020;
    public static readonly IntPtr HwndTop = new(0);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int GetDpiForWindow(IntPtr hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);
}
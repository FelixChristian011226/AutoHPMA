using System;
using System.Runtime.InteropServices;

public static class NativeMethodsService
{
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    public const int SW_SHOW = 5;
    public const int SW_RESTORE = 9;

}
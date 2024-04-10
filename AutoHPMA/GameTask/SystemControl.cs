using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace AutoHPMA.GameTask;

public class SystemControl
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


    public static nint FindHandleByProcessName(params string[] names)
    {
        foreach (var name in names)
        {
            var pros = Process.GetProcessesByName(name);
            if (pros.Any())
            {
                return pros[0].MainWindowHandle;
            }
        }

        return 0;
    }

    public static nint FindMumuSimulatorHandle()
    {
        return FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
    }

    public static IntPtr FindChildWindowByTitle(IntPtr parentHandle, string targetTitle)
    {
        IntPtr result = IntPtr.Zero;
        EnumChildWindows(parentHandle, (hWnd, lParam) =>
        {
            StringBuilder windowText = new StringBuilder(255);
            GetWindowText(hWnd, windowText, 255);
            if (windowText.ToString() == targetTitle)
            {
                result = hWnd;
                return false; // 返回 false 以停止窗口枚举
            }
            return true; // 返回 true 继续枚举
        }, IntPtr.Zero);

        return result;
    }



}

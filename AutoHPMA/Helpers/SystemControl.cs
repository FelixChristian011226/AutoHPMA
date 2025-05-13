using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace AutoHPMA.Helpers;

public class SystemControl
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(nint hwndParent, EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);


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

    public static nint FindChildWindowByTitle(nint parentHandle, string targetTitle)
    {
        nint result = nint.Zero;
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
        }, nint.Zero);

        return result;
    }



}

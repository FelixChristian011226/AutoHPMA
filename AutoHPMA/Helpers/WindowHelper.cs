using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AutoHPMA.Models;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace AutoHPMA.Helpers;

public class WindowHelper
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(nint hwndParent, EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);


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

    public static List<WindowInfo> GetAllVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        
        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                var windowText = new StringBuilder(255);
                GetWindowText(hWnd, windowText, 255);
                var title = windowText.ToString();
                
                // 跳过没有标题的窗口（通常是系统窗口）
                if (string.IsNullOrEmpty(title))
                    return true;
                
                GetWindowThreadProcessId(hWnd, out uint processId);
                
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    var windowInfo = new WindowInfo
                    {
                        Handle = hWnd,
                        Title = title,
                        ProcessName = process.ProcessName,
                        ProcessId = (int)processId
                    };
                    
                    windows.Add(windowInfo);
                }
                catch (ArgumentException)
                {
                    // 进程可能已经退出，忽略此窗口
                }
            }
            
            return true;
        }, nint.Zero);
        
        return windows.OrderBy(w => w.Title).ToList();
    }
}

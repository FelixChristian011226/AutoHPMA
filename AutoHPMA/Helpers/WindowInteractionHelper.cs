using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AutoHPMA.Helpers;

/// <summary>
/// 窗口交互辅助类，提供鼠标、键盘等输入模拟功能
/// </summary>
public static class WindowInteractionHelper
{
    #region 常量定义

    // 鼠标消息
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_PARENTNOTIFY = 0x0210;

    // 键盘消息
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    // 虚拟键码
    private const int VK_RETURN = 0x0D;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_SPACE = 0x20;

    // MapVirtualKey 映射类型
    private const uint MAPVK_VK_TO_VSC = 0x00;

    #endregion

    #region P/Invoke 声明

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 将 X、Y 坐标打包为 lParam
    /// </summary>
    private static IntPtr MakeLParam(uint x, uint y)
    {
        return (IntPtr)((y << 16) | (x & 0xFFFF));
    }

    private static readonly Random _random = new();

    #endregion

    #region 鼠标操作

    /// <summary>
    /// 异步发送鼠标点击
    /// </summary>
    /// <param name="hWnd">目标窗口句柄</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="cancellationToken">取消令牌（可选）</param>
    public static async Task SendMouseClickAsync(IntPtr hWnd, uint x, uint y, CancellationToken cancellationToken = default)
    {
        var lParam = MakeLParam(x, y);
        SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
        
        try
        {
            await Task.Delay(100, cancellationToken);
        }
        finally
        {
            SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }
    }

    /// <summary>
    /// 异步发送鼠标长按操作
    /// </summary>
    /// <param name="hWnd">目标窗口句柄</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="duration">长按持续时间（毫秒）</param>
    /// <param name="cancellationToken">取消令牌（可选）</param>
    public static async Task SendMouseLongPressAsync(IntPtr hWnd, uint x, uint y, int duration = 1000, CancellationToken cancellationToken = default)
    {
        var lParam = MakeLParam(x, y);
        SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
        
        try
        {
            await Task.Delay(duration, cancellationToken);
        }
        finally
        {
            SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }
    }

    /// <summary>
    /// 异步发送带噪声的鼠标拖拽操作
    /// </summary>
    /// <param name="hWnd">目标窗口句柄</param>
    /// <param name="startX">起点 X 坐标</param>
    /// <param name="startY">起点 Y 坐标</param>
    /// <param name="endX">终点 X 坐标</param>
    /// <param name="endY">终点 Y 坐标</param>
    /// <param name="duration">拖拽持续时间（毫秒）</param>
    /// <param name="cancellationToken">取消令牌（可选）</param>
    public static async Task SendMouseDragWithNoiseAsync(
        IntPtr hWnd, 
        uint startX, uint startY, 
        uint endX, uint endY, 
        int duration = 500, 
        CancellationToken cancellationToken = default)
    {
        var startLParam = MakeLParam(startX, startY);
        var endLParam = MakeLParam(endX, endY);
        IntPtr currentLParam = startLParam;

        PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)1, startLParam);
        
        try
        {
            await Task.Delay(50, cancellationToken);

            // 计算移动步数和每步延迟
            const int steps = 30;
            int stepDelay = duration / steps;
            double stepX = (endX - startX) / (double)steps;
            double stepY = (endY - startY) / (double)steps;

            // 逐步移动鼠标，添加微小随机偏移
            for (int i = 1; i <= steps; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int offsetX = _random.Next(-2, 3);
                int offsetY = _random.Next(-2, 3);
                
                uint currentX = (uint)(startX + (stepX * i) + offsetX);
                uint currentY = (uint)(startY + (stepY * i) + offsetY);
                currentLParam = MakeLParam(currentX, currentY);
                
                PostMessage(hWnd, WM_MOUSEMOVE, (IntPtr)1, currentLParam);
                await Task.Delay(stepDelay, cancellationToken);
            }

            // 在终点位置进行多次确认
            for (int i = 0; i < 3; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int offsetX = _random.Next(-1, 2);
                int offsetY = _random.Next(-1, 2);

                uint finalX = (uint)(endX + offsetX);
                uint finalY = (uint)(endY + offsetY);
                currentLParam = MakeLParam(finalX, finalY);

                PostMessage(hWnd, WM_MOUSEMOVE, (IntPtr)1, currentLParam);
                await Task.Delay(50, cancellationToken);
            }

            PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, endLParam);
            await Task.Delay(50, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, currentLParam);
            throw;
        }
    }

    /// <summary>
    /// 异步发送带 ParentNotify 的鼠标点击
    /// </summary>
    public static async Task SendMouseClickWithParentNotifyAsync(IntPtr hWnd, uint x, uint y, CancellationToken cancellationToken = default)
    {
        IntPtr lParam = MakeLParam(x, y);

        uint wParamForParentNotify = WM_LBUTTONDOWN | ((y & 0xFFFF) << 16) | (x & 0xFFFF);
        PostMessage(hWnd, WM_PARENTNOTIFY, new IntPtr(wParamForParentNotify), lParam);
        await Task.Delay(50, cancellationToken);

        PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
        
        try
        {
            await Task.Delay(100, cancellationToken);
        }
        finally
        {
            PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        }
    }

    #endregion

    #region 键盘操作

    /// <summary>
    /// 异步发送按键
    /// </summary>
    /// <param name="hWnd">目标窗口句柄</param>
    /// <param name="virtualKey">虚拟键码</param>
    /// <param name="cancellationToken">取消令牌（可选）</param>
    public static async Task SendKeyAsync(IntPtr hWnd, int virtualKey, CancellationToken cancellationToken = default)
    {
        uint scanCode = MapVirtualKey((uint)virtualKey, MAPVK_VK_TO_VSC);
        IntPtr lParamDown = (IntPtr)(1 | (scanCode << 16));
        IntPtr lParamUp = (IntPtr)(0xC0000001 | (scanCode << 16));

        PostMessage(hWnd, WM_KEYDOWN, (IntPtr)virtualKey, lParamDown);
        
        try
        {
            await Task.Delay(50, cancellationToken);
        }
        finally
        {
            PostMessage(hWnd, WM_KEYUP, (IntPtr)virtualKey, lParamUp);
        }
    }

    /// <summary>
    /// 异步发送回车键
    /// </summary>
    public static Task SendEnterAsync(IntPtr hWnd, CancellationToken cancellationToken = default)
        => SendKeyAsync(hWnd, VK_RETURN, cancellationToken);

    /// <summary>
    /// 异步发送 ESC 键
    /// </summary>
    public static Task SendESCAsync(IntPtr hWnd, CancellationToken cancellationToken = default)
        => SendKeyAsync(hWnd, VK_ESCAPE, cancellationToken);

    /// <summary>
    /// 异步发送空格键
    /// </summary>
    public static Task SendSpaceAsync(IntPtr hWnd, CancellationToken cancellationToken = default)
        => SendKeyAsync(hWnd, VK_SPACE, cancellationToken);

    #endregion

    #region 窗口操作

    /// <summary>
    /// 获取窗口位置和大小
    /// </summary>
    public static void GetWindowPositionAndSize(IntPtr hWnd, out int left, out int top, out int width, out int height)
    {
        if (GetWindowRect(hWnd, out RECT rect))
        {
            left = rect.Left;
            top = rect.Top;
            width = rect.Right - rect.Left;
            height = rect.Bottom - rect.Top;
        }
        else
        {
            int error = Marshal.GetLastWin32Error();
            throw new System.ComponentModel.Win32Exception(error);
        }
    }

    #endregion
}


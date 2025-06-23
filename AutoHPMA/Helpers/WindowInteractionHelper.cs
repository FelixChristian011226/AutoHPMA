using AutoHPMA.GameTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace AutoHPMA.Helpers;

/// <summary>
/// 窗口交互类
/// </summary>
public class WindowInteractionHelper
{
    // 定义模拟鼠标点击所需的消息常量
    private const uint WM_PARENTNOTIFY = 0x0210;
    private const uint WM_LBUTTONDOWN = 0x201;
    private const uint WM_LBUTTONUP = 0x202;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_CHAR = 0x0102;
    private const uint WM_CLOSE = 0x010;

    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    private const int VK_RETURN = 0x0D;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_SPACE = 0x20;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public MouseInputUnion mi;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct MouseInputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // 计算lParam所需的坐标点
    private static IntPtr MakeLParam(uint x, uint y)
    {
        return (IntPtr)((y << 16) | (x & 0xFFFF));
    }

    public static void SendMouseClick(IntPtr hWnd, uint x, uint y)
    {
        var lParam = MakeLParam(x, y);

        SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
        Thread.Sleep(100); // 延时100毫秒
        SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
    }
    public static void SendMouseClickWithParentNotify(IntPtr hWnd, uint x, uint y)
    {
        IntPtr lParam = MakeLParam(x, y);

        // 模拟鼠标点击的完整序列，包括 WM_PARENTNOTIFY
        uint wParamForParentNotify = (uint)WM_LBUTTONDOWN | ((y & 0xFFFF) << 16) | (x & 0xFFFF);
        IntPtr wParam = new IntPtr(wParamForParentNotify);
        PostMessage(hWnd, WM_PARENTNOTIFY, wParam, lParam);
        Thread.Sleep(50); // 延时

        PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
        Thread.Sleep(100); // 模拟点击持续时间

        PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);

    }

    public static void SendEnter(IntPtr hWnd)
    {
        SendKey(hWnd, VK_RETURN);
    }

    public static void SendESC(IntPtr hWnd)
    {
        SendKey(hWnd, VK_ESCAPE);
    }

    public static void SendSpace(IntPtr hWnd)
    {
        SendKey(hWnd, VK_SPACE);
    }

    public static void SendKey(IntPtr hWnd, int virtualKey)
    {
        const uint MAPVK_VK_TO_VSC = 0x00;
        uint scanCode = MapVirtualKey((uint)virtualKey, MAPVK_VK_TO_VSC);

        IntPtr lParamDown = (IntPtr)(1 | (scanCode << 16));
        IntPtr lParamUp = (IntPtr)(0xC0000001 | (scanCode << 16));

        //SetForegroundWindow(hWnd);
        //Thread.Sleep(20);

        PostMessage(hWnd, WM_KEYDOWN, (IntPtr)virtualKey, lParamDown);
        Thread.Sleep(50);
        PostMessage(hWnd, WM_KEYUP, (IntPtr)virtualKey, lParamUp);
    }

    // 添加MapVirtualKey的声明
    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static void GetWindowPositionAndSize(IntPtr hWnd, out int left, out int top, out int width, out int height)
    {
        RECT rect;
        if (GetWindowRect(hWnd, out rect))
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

    private static readonly Random _random = new Random();

    public static void SendMouseDragWithNoise(IntPtr hWnd, uint startX, uint startY, uint endX, uint endY, int duration = 500)
    {
        var startLParam = MakeLParam(startX, startY);
        var endLParam = MakeLParam(endX, endY);

        // 按下鼠标左键
        PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)1, startLParam);
        Thread.Sleep(50);

        // 计算移动的步数和每步的延迟
        int steps = 30;
        int stepDelay = duration / steps;
        double stepX = (endX - startX) / (double)steps;
        double stepY = (endY - startY) / (double)steps;

        // 逐步移动鼠标，添加微小随机偏移
        for (int i = 1; i <= steps; i++)
        {
            // 添加微小随机偏移（±2像素）
            int offsetX = _random.Next(-2, 3);
            int offsetY = _random.Next(-2, 3);
            
            uint currentX = (uint)(startX + (stepX * i) + offsetX);
            uint currentY = (uint)(startY + (stepY * i) + offsetY);
            var currentLParam = MakeLParam(currentX, currentY);
            
            PostMessage(hWnd, WM_MOUSEMOVE, (IntPtr)1, currentLParam);
            Thread.Sleep(stepDelay);
        }

        //在终点位置进行多次确认
        for (int i = 0; i < 3; i++)
        {
            // 在终点位置添加微小随机偏移
            int offsetX = _random.Next(-1, 2);
            int offsetY = _random.Next(-1, 2);

            uint finalX = (uint)(endX + offsetX);
            uint finalY = (uint)(endY + offsetY);
            var finalLParam = MakeLParam(finalX, finalY);

            PostMessage(hWnd, WM_MOUSEMOVE, (IntPtr)1, finalLParam);
            Thread.Sleep(50);
        }

        // 释放鼠标左键
        PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, endLParam);
        Thread.Sleep(50);

    }

    public static void SendMouseDrag(IntPtr hWnd, uint startX, uint startY, uint endX, uint endY, int duration = 500)
    {
        var startLParam = MakeLParam(startX, startY);
        var endLParam = MakeLParam(endX, endY);

        // 按下鼠标左键
        SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)1, startLParam);
        Thread.Sleep(50);

        // 计算移动的步数和每步的延迟
        int steps = 10;
        int stepDelay = duration / steps;
        double stepX = (endX - startX) / (double)steps;
        double stepY = (endY - startY) / (double)steps;

        // 逐步移动鼠标
        for (int i = 1; i <= steps; i++)
        {
            uint currentX = (uint)(startX + (stepX * i));
            uint currentY = (uint)(startY + (stepY * i));
            var currentLParam = MakeLParam(currentX, currentY);

            // 发送鼠标移动消息
            SendMessage(hWnd, WM_MOUSEMOVE, (IntPtr)1, currentLParam);
            Thread.Sleep(stepDelay);
        }

        // 释放鼠标左键
        SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, endLParam);
    }

    // 添加鼠标移动消息常量
    private const uint WM_MOUSEMOVE = 0x0200;

}

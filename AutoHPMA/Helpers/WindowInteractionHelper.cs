﻿using AutoHPMA.GameTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace AutoHPMA.Helpers;

/// <summary>
/// 窗口交互类
/// </summary>
public class WindowInteractionHelper
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

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


    // 定义模拟鼠标点击所需的消息常量
    private const uint WM_PARENTNOTIFY = 0x0210;
    private const uint WM_LBUTTONDOWN = 0x201;  
    private const uint WM_LBUTTONUP = 0x202;
    private const uint WM_CLOSE = 0x010;

    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;


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

}

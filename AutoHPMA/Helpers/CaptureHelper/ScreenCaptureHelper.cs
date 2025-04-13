using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AutoHPMA.Helpers.CaptureHelper
{
    /// <summary>
    /// 截屏
    /// </summary>
    public static class ScreenCaptureHelper
    {
        public const int PW_CLIENTONLY = 0x00000001;
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(nint hWnd, out Rectangle lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PrintWindow(nint hWnd, nint hdcBlt, int nFlags);

        [DllImport("gdi32.dll")]
        public static extern nint CreateCompatibleBitmap(nint hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern nint CreateCompatibleDC(nint hdc);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(nint hdc);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(nint hObject);

        [DllImport("user32.dll")]
        public static extern nint GetWindowDC(nint hWnd);

        [DllImport("gdi32.dll")]
        public static extern nint SelectObject(nint hdc, nint hObject);

        public static Bitmap CaptureWindow(nint hWnd)
        {
            // 获取目标窗口的设备上下文
            nint windowDC = GetWindowDC(hWnd);
            // 创建兼容的内存设备上下文
            nint memDC = CreateCompatibleDC(windowDC);

            GetWindowRect(hWnd, out Rectangle rect);
            int width = rect.Width - rect.X;
            int height = rect.Height - rect.Y;

            // 创建兼容的位图
            nint hBitmap = CreateCompatibleBitmap(windowDC, width, height);

            // 选择位图对象到内存DC中，并保留旧的位图
            nint oldBitmap = SelectObject(memDC, hBitmap);

            // 将目标窗口内容打印到内存DC中的位图上
            PrintWindow(hWnd, memDC, PW_CLIENTONLY);

            // 从内存DC中获取位图信息
            Bitmap bmp = Image.FromHbitmap(hBitmap);

            // 清理
            SelectObject(memDC, oldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memDC);
            DeleteObject(windowDC);

            return bmp;
        }

    }
}

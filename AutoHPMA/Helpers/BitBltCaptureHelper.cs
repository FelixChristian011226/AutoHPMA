﻿using System.Diagnostics;
using System.Drawing;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;
using AutoHPMA.Helpers;
using Vanara.PInvoke;

namespace AutoHPMA.Helpers
{
    public class BitBltCaptureHelper
    {

        public static Bitmap? Capture(IntPtr hWnd)
        {
            Windows.Win32.Foundation.HWND _hWnd = new Windows.Win32.Foundation.HWND(hWnd);
            if (_hWnd == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                GetWindowRect(_hWnd, out var windowRect);
                var width = windowRect.Width - windowRect.X;
                var height = windowRect.Height - windowRect.Y;

                var hdcSrc = GetWindowDC(_hWnd);
                var hdcDest = CreateCompatibleDC(hdcSrc);
                var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
                var hOld = SelectObject(hdcDest, hBitmap);
                Windows.Win32.PInvoke.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, ROP_CODE.SRCCOPY);
                SelectObject(hdcDest, hOld);
                DeleteDC(hdcDest);
                ReleaseDC(_hWnd, hdcSrc);

                var bitmap = Image.FromHbitmap(hBitmap);
                DeleteObject(hBitmap);
                return bitmap;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }
    }
}
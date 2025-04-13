using System.Diagnostics;
using System.Drawing;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;
using Vanara.PInvoke;

namespace AutoHPMA.Helpers.CaptureHelper.CaptureHelper
{
    /// <summary>
    /// BitBlt截图
    /// </summary>
    public class BitBltCaptureHelper
    {

        public static Bitmap? Capture(nint hWnd)
        {
            Windows.Win32.Foundation.HWND _hWnd = new Windows.Win32.Foundation.HWND(hWnd);
            if (_hWnd == nint.Zero)
            {
                return null;
            }

            try
            {
                GetWindowRect(_hWnd, out var windowRect);
                var width = windowRect.Width;
                var height = windowRect.Height;

                var hdcSrc = GetWindowDC(_hWnd);
                var hdcDest = CreateCompatibleDC(hdcSrc);
                var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
                var hOld = SelectObject(hdcDest, hBitmap);
                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, ROP_CODE.SRCCOPY);
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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AutoHPMA.Helpers
{
    public static class ScreenCaptureHelper
    {
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

        public static Bitmap CaptureWindow(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out Rectangle rect);

            Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }

        public static void SaveBitmapToFile(Bitmap bitmap, string folderPath, string fileName)
        {
            string fullPath = System.IO.Path.Combine(folderPath, fileName);
            bitmap.Save(fullPath, ImageFormat.Png);
        }
    }
}

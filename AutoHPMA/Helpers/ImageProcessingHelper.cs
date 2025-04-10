using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Extensions;
namespace AutoHPMA.Helpers
{
    /// <summary>
    /// 图像处理
    /// </summary>
    public class ImageProcessingHelper
    {
        // 截取 Bitmap 图像
        public static Bitmap CropBitmap(Bitmap bitmap, int x, int y, int width, int height)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            Bitmap croppedBitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(bitmap, new Rectangle(0, 0, width, height), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            }

            return croppedBitmap;
        }

        // 将 Bitmap 以不同格式保存到指定目录
        public static void SaveBitmapAs(Bitmap bitmap, string directoryPath, string fileName, ImageFormat format)
        {

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string fullPath = Path.Combine(directoryPath, fileName);

            bitmap.Save(fullPath, format);
        }

    }
}

using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
namespace AutoHPMA.Helpers
{
    public class ImageProcessingHelper
    {
        // 截取 Bitmap 图像
        public static Bitmap CropBitmap(Bitmap source, int x, int y, int width, int height)
        {
            Rectangle cropArea = new Rectangle(x, y, width, height);
            Bitmap croppedBitmap = source.Clone(cropArea, source.PixelFormat);
            return croppedBitmap;
        }
        //public static Bitmap CropBitmap(Bitmap bitmap, int x, int y, int width, int height)
        //{
        //    if (bitmap == null)
        //    {
        //        throw new ArgumentNullException("bitmap");
        //    }

        //    // 创建一个新的 Bitmap 对象来存储裁剪后的图像
        //    Bitmap croppedBitmap = new Bitmap(width, height);

        //    // 创建一个Graphics对象，并使用Graphics类的DrawImage方法裁剪图像
        //    using (Graphics g = Graphics.FromImage(croppedBitmap))
        //    {
        //        g.DrawImage(bitmap, new Rectangle(0, 0, width, height), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
        //    }

        //    return croppedBitmap;
        //}

        // 将 Bitmap 以不同格式保存到指定目录
        public static void SaveBitmapAs(Bitmap bitmap, string directoryPath, string fileName, ImageFormat format)
        {
            // 确保目录路径存在，如果不存在，则创建它
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 拼接完整的文件路径
            string fullPath = Path.Combine(directoryPath, fileName);

            // 保存文件
            bitmap.Save(fullPath, format);
        }

        // 比对两个 Bitmap 图像并返回相似度
        public static double CompareImages(string imagePath1, string imagePath2)
        {
            // 加载图像
            Mat image1 = CvInvoke.Imread(imagePath1);
            Mat image2 = CvInvoke.Imread(imagePath2);
            if (image1 == null || image2 == null)
            {
                throw new Exception("无法加载图像");
            }
            // 确保图像具有相同的大小和深度
            if (image1.Size != image2.Size || image1.Depth != image2.Depth)
            {
                throw new Exception("图像大小或深度不匹配");
            }
            // 计算结构相似性指数(SSIM)
            Mat image1Gray = new Mat();
            Mat image2Gray = new Mat();
            CvInvoke.CvtColor(image1, image1Gray, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(image2, image2Gray, ColorConversion.Bgr2Gray);
            Mat ssimImage = new Mat();
            CvInvoke.CvtColor(image2, ssimImage, ColorConversion.Bgr2Gray);
            double ssimValue = CvInvoke.CompareHist(image1Gray, image2Gray, HistogramCompMethod.Bhattacharyya);
            return ssimValue;
        }

    }
}

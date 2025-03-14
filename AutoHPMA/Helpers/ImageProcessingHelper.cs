using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Extensions;
namespace AutoHPMA.Helpers
{
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

        public static Scalar Compare_SSIM(string imgFile1, string imgFile2)
        {
            var image1 = Cv2.ImRead(imgFile1);
            var image2Tmp = Cv2.ImRead(imgFile2);
            // 将两个图片处理成同样大小，否则会有错误
            var image2 = new Mat();
            Cv2.Resize(image2Tmp, image2, new OpenCvSharp.Size(image1.Size().Width, image1.Size().Height));
            double C1 = 6.5025, C2 = 58.5225;
            var validImage1 = new Mat();
            var validImage2 = new Mat();
            image1.ConvertTo(validImage1, MatType.CV_32F);
            image2.ConvertTo(validImage2, MatType.CV_32F);


            Mat image1_1 = validImage1.Mul(validImage1);
            Mat image2_2 = validImage2.Mul(validImage2);
            Mat image1_2 = validImage1.Mul(validImage2);

            Mat gausBlur1 = new Mat(), gausBlur2 = new Mat(), gausBlur12 = new Mat();
            Cv2.GaussianBlur(validImage1, gausBlur1, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(validImage2, gausBlur2, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(image1_2, gausBlur12, new OpenCvSharp.Size(11, 11), 1.5);

            Mat imageAvgProduct = gausBlur1.Mul(gausBlur2);
            Mat u1Squre = gausBlur1.Mul(gausBlur1);
            Mat u2Squre = gausBlur2.Mul(gausBlur2);

            Mat imageConvariance = new Mat(), imageVariance1 = new Mat(), imageVariance2 = new Mat();
            Mat squreAvg1 = new Mat(), squreAvg2 = new Mat();
            Cv2.GaussianBlur(image1_1, squreAvg1, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(image2_2, squreAvg2, new OpenCvSharp.Size(11, 11), 1.5);

            imageConvariance = gausBlur12 - gausBlur1.Mul(gausBlur2);
            imageVariance1 = squreAvg1 - gausBlur1.Mul(gausBlur1);
            imageVariance2 = squreAvg2 - gausBlur2.Mul(gausBlur2);

            var member = ((2 * gausBlur1.Mul(gausBlur2) + C1).Mul(2 * imageConvariance + C2));
            var denominator = ((u1Squre + u2Squre + C1).Mul(imageVariance1 + imageVariance2 + C2));

            Mat ssim = new Mat();
            Cv2.Divide(member, denominator, ssim);

            var sclar = Cv2.Mean(ssim);

            return sclar;

        }

        public static Scalar Compare_SSIM(Bitmap img1, Bitmap img2)
        {
            var image1 = BitmapConverter.ToMat(img1);
            var image2Tmp = BitmapConverter.ToMat(img2);
            // 将两个图片处理成同样大小，否则会有错误
            var image2 = new Mat();
            Cv2.Resize(image2Tmp, image2, new OpenCvSharp.Size(image1.Size().Width, image1.Size().Height));
            double C1 = 6.5025, C2 = 58.5225;
            var validImage1 = new Mat();
            var validImage2 = new Mat();
            image1.ConvertTo(validImage1, MatType.CV_32F);
            image2.ConvertTo(validImage2, MatType.CV_32F);


            Mat image1_1 = validImage1.Mul(validImage1);
            Mat image2_2 = validImage2.Mul(validImage2);
            Mat image1_2 = validImage1.Mul(validImage2);

            Mat gausBlur1 = new Mat(), gausBlur2 = new Mat(), gausBlur12 = new Mat();
            Cv2.GaussianBlur(validImage1, gausBlur1, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(validImage2, gausBlur2, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(image1_2, gausBlur12, new OpenCvSharp.Size(11, 11), 1.5);

            Mat imageAvgProduct = gausBlur1.Mul(gausBlur2);
            Mat u1Squre = gausBlur1.Mul(gausBlur1);
            Mat u2Squre = gausBlur2.Mul(gausBlur2);

            Mat imageConvariance = new Mat(), imageVariance1 = new Mat(), imageVariance2 = new Mat();
            Mat squreAvg1 = new Mat(), squreAvg2 = new Mat();
            Cv2.GaussianBlur(image1_1, squreAvg1, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(image2_2, squreAvg2, new OpenCvSharp.Size(11, 11), 1.5);

            imageConvariance = gausBlur12 - gausBlur1.Mul(gausBlur2);
            imageVariance1 = squreAvg1 - gausBlur1.Mul(gausBlur1);
            imageVariance2 = squreAvg2 - gausBlur2.Mul(gausBlur2);

            var member = ((2 * gausBlur1.Mul(gausBlur2) + C1).Mul(2 * imageConvariance + C2));
            var denominator = ((u1Squre + u2Squre + C1).Mul(imageVariance1 + imageVariance2 + C2));

            Mat ssim = new Mat();
            Cv2.Divide(member, denominator, ssim);

            var sclar = Cv2.Mean(ssim);

            return sclar;

        }

        public static double AverageScalarValue(OpenCvSharp.Scalar scalar)
        {
            // 计算Scalar中所有值的平均
            // 通常情况下，Scalar包含通道的值，对于SSIM可能只有一个通道有意义，但为彩色图处理时可能包含RGB。
            // 而在这种情况下，简单地计算所有通道的平均值。

            double sum = scalar.Val0 + scalar.Val1 + scalar.Val2;
            double average = sum / 3.0;
            return average;
        }

    }
}

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
        //public static Bitmap CropBitmap(Bitmap source, int x, int y, int width, int height)
        //{
        //    Rectangle cropArea = new Rectangle(x, y, width, height);
        //    Bitmap croppedBitmap = source.Clone(cropArea, source.PixelFormat);
        //    return croppedBitmap;
        //}
        public static Bitmap CropBitmap(Bitmap bitmap, int x, int y, int width, int height)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            // 创建一个新的 Bitmap 对象来存储裁剪后的图像
            Bitmap croppedBitmap = new Bitmap(width, height);

            // 创建一个Graphics对象，并使用Graphics类的DrawImage方法裁剪图像
            using (Graphics g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(bitmap, new Rectangle(0, 0, width, height), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            }

            return croppedBitmap;
        }

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
        //public static double CompareImages(string imagePath1, string imagePath2)
        //{
        //    // 加载图像
        //    Mat image1 = CvInvoke.Imread(imagePath1);
        //    Mat image2 = CvInvoke.Imread(imagePath2);
        //    if (image1 == null || image2 == null)
        //    {
        //        throw new Exception("无法加载图像");
        //    }
        //    // 确保图像具有相同的大小和深度
        //    if (image1.Size != image2.Size || image1.Depth != image2.Depth)
        //    {
        //        throw new Exception("图像大小或深度不匹配");
        //    }
        //    // 计算结构相似性指数(SSIM)
        //    Mat image1Gray = new Mat();
        //    Mat image2Gray = new Mat();
        //    CvInvoke.CvtColor(image1, image1Gray, ColorConversion.Bgr2Gray);
        //    CvInvoke.CvtColor(image2, image2Gray, ColorConversion.Bgr2Gray);
        //    Mat ssimImage = new Mat();
        //    CvInvoke.CvtColor(image2, ssimImage, ColorConversion.Bgr2Gray);
        //    double ssimValue = CvInvoke.CompareHist(image1Gray, image2Gray, HistogramCompMethod.Bhattacharyya);
        //    return ssimValue;
        //}

        public static Scalar Compare_SSIM(string imgFile1, string imgFile2)
        {
            var image1 = Cv2.ImRead(imgFile1);
            var image2Tmp = Cv2.ImRead(imgFile2);
            // 将两个图片处理成同样大小，否则会有错误： The operation is neither 'array op array' (where arrays have the same size and the same number of channels), nor 'array op scalar', nor 'scalar op array'
            var image2 = new Mat();
            Cv2.Resize(image2Tmp, image2, new OpenCvSharp.Size(image1.Size().Width, image1.Size().Height));
            double C1 = 6.5025, C2 = 58.5225;
            var validImage1 = new Mat();
            var validImage2 = new Mat();
            image1.ConvertTo(validImage1, MatType.CV_32F); //数据类型转换为 float,防止后续计算出现错误
            image2.ConvertTo(validImage2, MatType.CV_32F);


            Mat image1_1 = validImage1.Mul(validImage1); //图像乘积
            Mat image2_2 = validImage2.Mul(validImage2);
            Mat image1_2 = validImage1.Mul(validImage2);

            Mat gausBlur1 = new Mat(), gausBlur2 = new Mat(), gausBlur12 = new Mat();
            Cv2.GaussianBlur(validImage1, gausBlur1, new OpenCvSharp.Size(11, 11), 1.5); //高斯卷积核计算图像均值
            Cv2.GaussianBlur(validImage2, gausBlur2, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(image1_2, gausBlur12, new OpenCvSharp.Size(11, 11), 1.5);

            Mat imageAvgProduct = gausBlur1.Mul(gausBlur2); //均值乘积
            Mat u1Squre = gausBlur1.Mul(gausBlur1); //各自均值的平方
            Mat u2Squre = gausBlur2.Mul(gausBlur2);

            Mat imageConvariance = new Mat(), imageVariance1 = new Mat(), imageVariance2 = new Mat();
            Mat squreAvg1 = new Mat(), squreAvg2 = new Mat();
            Cv2.GaussianBlur(image1_1, squreAvg1, new OpenCvSharp.Size(11, 11), 1.5); //图像平方的均值
            Cv2.GaussianBlur(image2_2, squreAvg2, new OpenCvSharp.Size(11, 11), 1.5);

            imageConvariance = gausBlur12 - gausBlur1.Mul(gausBlur2);// 计算协方差
            imageVariance1 = squreAvg1 - gausBlur1.Mul(gausBlur1); //计算方差
            imageVariance2 = squreAvg2 - gausBlur2.Mul(gausBlur2);

            var member = ((2 * gausBlur1.Mul(gausBlur2) + C1).Mul(2 * imageConvariance + C2));
            var denominator = ((u1Squre + u2Squre + C1).Mul(imageVariance1 + imageVariance2 + C2));

            Mat ssim = new Mat();
            Cv2.Divide(member, denominator, ssim);

            var sclar = Cv2.Mean(ssim);

            return sclar;  // 变化率，即差异

        }

        public static Scalar Compare_SSIM(Bitmap img1, Bitmap img2)
        {
            var image1 = BitmapConverter.ToMat(img1);
            var image2Tmp = BitmapConverter.ToMat(img2);
            // 将两个图片处理成同样大小，否则会有错误： The operation is neither 'array op array' (where arrays have the same size and the same number of channels), nor 'array op scalar', nor 'scalar op array'
            var image2 = new Mat();
            Cv2.Resize(image2Tmp, image2, new OpenCvSharp.Size(image1.Size().Width, image1.Size().Height));
            double C1 = 6.5025, C2 = 58.5225;
            var validImage1 = new Mat();
            var validImage2 = new Mat();
            image1.ConvertTo(validImage1, MatType.CV_32F); //数据类型转换为 float,防止后续计算出现错误
            image2.ConvertTo(validImage2, MatType.CV_32F);


            Mat image1_1 = validImage1.Mul(validImage1); //图像乘积
            Mat image2_2 = validImage2.Mul(validImage2);
            Mat image1_2 = validImage1.Mul(validImage2);

            Mat gausBlur1 = new Mat(), gausBlur2 = new Mat(), gausBlur12 = new Mat();
            Cv2.GaussianBlur(validImage1, gausBlur1, new OpenCvSharp.Size(11, 11), 1.5); //高斯卷积核计算图像均值
            Cv2.GaussianBlur(validImage2, gausBlur2, new OpenCvSharp.Size(11, 11), 1.5);
            Cv2.GaussianBlur(image1_2, gausBlur12, new OpenCvSharp.Size(11, 11), 1.5);

            Mat imageAvgProduct = gausBlur1.Mul(gausBlur2); //均值乘积
            Mat u1Squre = gausBlur1.Mul(gausBlur1); //各自均值的平方
            Mat u2Squre = gausBlur2.Mul(gausBlur2);

            Mat imageConvariance = new Mat(), imageVariance1 = new Mat(), imageVariance2 = new Mat();
            Mat squreAvg1 = new Mat(), squreAvg2 = new Mat();
            Cv2.GaussianBlur(image1_1, squreAvg1, new OpenCvSharp.Size(11, 11), 1.5); //图像平方的均值
            Cv2.GaussianBlur(image2_2, squreAvg2, new OpenCvSharp.Size(11, 11), 1.5);

            imageConvariance = gausBlur12 - gausBlur1.Mul(gausBlur2);// 计算协方差
            imageVariance1 = squreAvg1 - gausBlur1.Mul(gausBlur1); //计算方差
            imageVariance2 = squreAvg2 - gausBlur2.Mul(gausBlur2);

            var member = ((2 * gausBlur1.Mul(gausBlur2) + C1).Mul(2 * imageConvariance + C2));
            var denominator = ((u1Squre + u2Squre + C1).Mul(imageVariance1 + imageVariance2 + C2));

            Mat ssim = new Mat();
            Cv2.Divide(member, denominator, ssim);

            var sclar = Cv2.Mean(ssim);

            return sclar;  // 变化率，即差异

        }

        public static double AverageScalarValue(OpenCvSharp.Scalar scalar)
        {
            // 计算Scalar中所有值的平均
            // 通常情况下，Scalar包含通道的值，对于SSIM可能只有一个通道有意义，但为彩色图处理时可能包含RGB。
            // 而在这种情况下，我们可以简单地计算所有通道的平均值。
            // 注意: 对于某些应用，可能只关心第一个值

            double sum = scalar.Val0 + scalar.Val1 + scalar.Val2;
            double average = sum / 3.0;
            return average;
        }

    }
}

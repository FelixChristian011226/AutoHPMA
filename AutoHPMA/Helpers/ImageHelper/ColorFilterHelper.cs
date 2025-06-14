using OpenCvSharp;
using System;
using System.Windows.Media;

namespace AutoHPMA.Helpers.ImageHelper
{
    public class ColorFilterHelper
    {
        public const int KEY_MASK_WHITE_PIXELS = 1;
        public const int KEY_FILTERED_PIXELS = 2;

        /// <summary>
        /// 从十六进制颜色字符串获取Color对象
        /// </summary>
        /// <param name="hex">十六进制颜色字符串（如"ffffff"）</param>
        /// <returns>Color对象</returns>
        public static Color GetColorFromHex(string hex)
        {
            try
            {
                // 确保hex是6位
                hex = hex.PadRight(6, '0');
                // 添加FF作为Alpha通道
                hex = "FF" + hex;
                return (Color)ColorConverter.ConvertFromString("#" + hex);
            }
            catch
            {
                // 如果转换失败，返回红色
                return Colors.Red;
            }
        }

        /// <summary>
        /// 对图像进行颜色过滤
        /// </summary>
        /// <param name="sourceMat">源图像</param>
        /// <param name="maskMat">遮罩图像（可选）</param>
        /// <param name="targetColorHex">目标颜色的十六进制值</param>
        /// <param name="colorThreshold">颜色阈值</param>
        /// <returns>处理后的图像</returns>
        public static Mat FilterColor(Mat sourceMat, Mat? maskMat, string targetColorHex, int colorThreshold)
        {
            if (sourceMat == null || sourceMat.Empty())
                throw new ArgumentException("源图像不能为空");

            Mat resultMat = sourceMat.Clone();
            int maskWhitePixels = 0;
            int filteredPixels = 0;

            // 如果有遮罩，应用遮罩
            if (maskMat != null && !maskMat.Empty())
            {
                Mat grayMask = new Mat();
                Cv2.CvtColor(maskMat, grayMask, ColorConversionCodes.BGR2GRAY);
                // 将遮罩二值化
                Cv2.Threshold(grayMask, grayMask, 127, 255, ThresholdTypes.Binary);
                // 统计遮罩中的白色像素数量
                maskWhitePixels = Cv2.CountNonZero(grayMask);
                
                // 创建反遮罩
                Mat maskInverse = new Mat();
                Cv2.BitwiseNot(grayMask, maskInverse);
                
                // 创建黑色背景
                Mat blackBg = new Mat(sourceMat.Size(), sourceMat.Type(), Scalar.Black);
                
                // 将遮罩区域设为黑色
                Cv2.BitwiseAnd(blackBg, blackBg, resultMat, maskInverse);
                
                // 将非遮罩区域保留原图
                Cv2.BitwiseAnd(sourceMat, sourceMat, resultMat, grayMask);
            }

            // 获取选中的颜色（目标颜色）
            var targetColor = GetColorFromHex(targetColorHex);

            // 创建1x1目标颜色图像
            Mat targetBGR = new Mat(1, 1, MatType.CV_8UC3, new Scalar(targetColor.B, targetColor.G, targetColor.R));
            Mat targetHSV = new Mat();
            Cv2.CvtColor(targetBGR, targetHSV, ColorConversionCodes.BGR2HSV);
            var targetHSVValue = targetHSV.Get<Vec3b>(0, 0); // H, S, V

            // 转换源图像为HSV
            Mat hsvMat = new Mat();
            Cv2.CvtColor(resultMat, hsvMat, ColorConversionCodes.BGR2HSV);

            // 构造HSV范围（Hue ±阈值, S/V较宽容）
            Scalar lowerBound = new Scalar(
                Math.Max(0, targetHSVValue.Item0 - colorThreshold),   // H
                Math.Max(50, targetHSVValue.Item1 - 50),              // S
                Math.Max(50, targetHSVValue.Item2 - 50));             // V

            Scalar upperBound = new Scalar(
                Math.Min(180, targetHSVValue.Item0 + colorThreshold),
                255,
                255);

            // 创建掩码
            Mat mask = new Mat();
            Cv2.InRange(hsvMat, lowerBound, upperBound, mask);

            // 统计过滤后的像素数量
            filteredPixels = Cv2.CountNonZero(mask);

            // 创建黑色背景图像
            Mat blackBackground = new Mat(resultMat.Size(), resultMat.Type(), Scalar.Black);

            // 创建结果图像
            Mat filteredImage = new Mat();
            Cv2.BitwiseAnd(resultMat, resultMat, filteredImage, mask);

            // 创建反掩码
            Mat inverseMask = new Mat();
            Cv2.BitwiseNot(mask, inverseMask);

            // 将非目标颜色区域设为黑色
            Mat blackAreas = new Mat();
            Cv2.BitwiseAnd(blackBackground, blackBackground, blackAreas, inverseMask);

            // 合并结果
            Cv2.Add(filteredImage, blackAreas, resultMat);

            // 将统计结果存储在结果图像的属性中
            resultMat.Set(KEY_MASK_WHITE_PIXELS, maskWhitePixels);
            resultMat.Set(KEY_FILTERED_PIXELS, filteredPixels);

            return resultMat;
        }

        /// <summary>
        /// 对图像进行颜色过滤（从文件路径）
        /// </summary>
        /// <param name="sourcePath">源图像路径</param>
        /// <param name="maskPath">遮罩图像路径（可选）</param>
        /// <param name="targetColorHex">目标颜色的十六进制值</param>
        /// <param name="colorThreshold">颜色阈值</param>
        /// <returns>处理后的图像</returns>
        public static Mat FilterColorFromFile(string sourcePath, string? maskPath, string targetColorHex, int colorThreshold)
        {
            if (string.IsNullOrEmpty(sourcePath))
                throw new ArgumentException("源图像路径不能为空");

            // 读取源图像
            Mat sourceMat = Cv2.ImRead(sourcePath);
            if (sourceMat.Empty())
                throw new ArgumentException("无法读取源图像");

            // 读取遮罩图像（如果有）
            Mat? maskMat = null;
            if (!string.IsNullOrEmpty(maskPath))
            {
                maskMat = Cv2.ImRead(maskPath);
                if (maskMat.Empty())
                    throw new ArgumentException("无法读取遮罩图像");
            }

            return FilterColor(sourceMat, maskMat, targetColorHex, colorThreshold);
        }

        /// <summary>
        /// 计算指定颜色在遮罩区域内的匹配百分比
        /// </summary>
        /// <param name="sourceMat">源图像</param>
        /// <param name="maskMat">遮罩图像</param>
        /// <param name="targetColorHex">目标颜色的十六进制值</param>
        /// <param name="colorThreshold">颜色阈值</param>
        /// <returns>匹配百分比（0-100）</returns>
        public static double CalculateColorMatchPercentage(Mat sourceMat, Mat maskMat, string targetColorHex, int colorThreshold)
        {
            if (sourceMat == null || sourceMat.Empty())
                throw new ArgumentException("源图像不能为空");
            if (maskMat == null || maskMat.Empty())
                throw new ArgumentException("遮罩图像不能为空");

            // 将遮罩转换为灰度图并二值化
            Mat grayMask = new Mat();
            Cv2.CvtColor(maskMat, grayMask, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(grayMask, grayMask, 127, 255, ThresholdTypes.Binary);
            
            // 统计遮罩中的白色像素数量
            int maskWhitePixels = Cv2.CountNonZero(grayMask);
            if (maskWhitePixels == 0)
                return 0;

            // 获取目标颜色
            var targetColor = GetColorFromHex(targetColorHex);

            // 创建1x1目标颜色图像
            Mat targetBGR = new Mat(1, 1, MatType.CV_8UC3, new Scalar(targetColor.B, targetColor.G, targetColor.R));
            Mat targetHSV = new Mat();
            Cv2.CvtColor(targetBGR, targetHSV, ColorConversionCodes.BGR2HSV);
            var targetHSVValue = targetHSV.Get<Vec3b>(0, 0);

            // 转换源图像为HSV
            Mat hsvMat = new Mat();
            Cv2.CvtColor(sourceMat, hsvMat, ColorConversionCodes.BGR2HSV);

            // 构造HSV范围
            Scalar lowerBound = new Scalar(
                Math.Max(0, targetHSVValue.Item0 - colorThreshold),
                Math.Max(50, targetHSVValue.Item1 - 50),
                Math.Max(50, targetHSVValue.Item2 - 50));

            Scalar upperBound = new Scalar(
                Math.Min(180, targetHSVValue.Item0 + colorThreshold),
                255,
                255);

            // 创建掩码
            Mat colorMask = new Mat();
            Cv2.InRange(hsvMat, lowerBound, upperBound, colorMask);

            // 将颜色掩码与遮罩进行与运算，只保留遮罩区域内的匹配结果
            Mat finalMask = new Mat();
            Cv2.BitwiseAnd(colorMask, grayMask, finalMask);

            // 统计匹配的像素数量
            int matchedPixels = Cv2.CountNonZero(finalMask);

            // 计算百分比
            return (double)matchedPixels / maskWhitePixels * 100;
        }
    }
} 
using OpenCvSharp;
using System;
using System.Windows.Media;

namespace AutoHPMA.Helpers.ImageHelper
{
    public class ColorFilterHelper
    {
        /// <summary>
        /// 参与过滤的总像素数（无遮罩时为原图像素总数，有遮罩时为遮罩白色区域像素数）
        /// </summary>
        public const int KEY_TOTAL_FILTER_PIXELS = 1;
        /// <summary>
        /// 匹配目标颜色的像素数
        /// </summary>
        public const int KEY_MATCHED_PIXELS = 2;

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
        /// <param name="hueThreshold">色相阈值 (H, 0-90)</param>
        /// <param name="saturationTolerance">饱和度/a*容差 (0-255)</param>
        /// <param name="valueTolerance">明度/b*容差 (0-255)</param>
        /// <param name="colorSpace">颜色空间: "HSV" 或 "LAB"</param>
        /// <returns>处理后的图像</returns>
        public static Mat FilterColor(Mat sourceMat, Mat? maskMat, string targetColorHex, 
            int hueThreshold, int saturationTolerance, int valueTolerance, string colorSpace = "LAB")
        {
            if (sourceMat == null || sourceMat.Empty())
                throw new ArgumentException("源图像不能为空");

            Mat resultMat = sourceMat.Clone();
            int totalFilterPixels = 0;
            int matchedPixels = 0;

            // 如果有遮罩，应用遮罩；否则使用原图全部像素
            if (maskMat != null && !maskMat.Empty())
            {
                using Mat grayMask = new Mat();
                Cv2.CvtColor(maskMat, grayMask, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(grayMask, grayMask, 127, 255, ThresholdTypes.Binary);
                totalFilterPixels = Cv2.CountNonZero(grayMask);
                
                using Mat maskInverse = new Mat();
                Cv2.BitwiseNot(grayMask, maskInverse);
                
                using Mat blackBg = new Mat(sourceMat.Size(), sourceMat.Type(), Scalar.Black);
                Cv2.BitwiseAnd(blackBg, blackBg, resultMat, maskInverse);
                Cv2.BitwiseAnd(sourceMat, sourceMat, resultMat, grayMask);
            }
            else
            {
                totalFilterPixels = sourceMat.Rows * sourceMat.Cols;
            }

            var targetColor = GetColorFromHex(targetColorHex);
            using Mat mask = new Mat();

            if (colorSpace == "LAB")
            {
                // LAB 颜色空间匹配 - 更接近人眼感知
                using Mat targetBGR = new Mat(1, 1, MatType.CV_8UC3, new Scalar(targetColor.B, targetColor.G, targetColor.R));
                using Mat targetLAB = new Mat();
                Cv2.CvtColor(targetBGR, targetLAB, ColorConversionCodes.BGR2Lab);
                var targetLabValue = targetLAB.Get<Vec3b>(0, 0); // L, a, b

                using Mat labMat = new Mat();
                Cv2.CvtColor(resultMat, labMat, ColorConversionCodes.BGR2Lab);

                // L: 明度 (0-255), a: 红绿 (0-255, 128为中性), b: 黄蓝 (0-255, 128为中性)
                // tolerance 直接作为各通道的容差
                int lLow = Math.Max(0, targetLabValue.Item0 - valueTolerance);
                int lHigh = Math.Min(255, targetLabValue.Item0 + valueTolerance);
                int aLow = Math.Max(0, targetLabValue.Item1 - saturationTolerance);
                int aHigh = Math.Min(255, targetLabValue.Item1 + saturationTolerance);
                int bLow = Math.Max(0, targetLabValue.Item2 - hueThreshold * 3); // 色相影响 b 通道较多
                int bHigh = Math.Min(255, targetLabValue.Item2 + hueThreshold * 3);

                Cv2.InRange(labMat,
                    new Scalar(lLow, aLow, bLow),
                    new Scalar(lHigh, aHigh, bHigh),
                    mask);
            }
            else // HSV
            {
                using Mat targetBGR = new Mat(1, 1, MatType.CV_8UC3, new Scalar(targetColor.B, targetColor.G, targetColor.R));
                using Mat targetHSV = new Mat();
                Cv2.CvtColor(targetBGR, targetHSV, ColorConversionCodes.BGR2HSV);
                var targetHSVValue = targetHSV.Get<Vec3b>(0, 0);

                using Mat hsvMat = new Mat();
                Cv2.CvtColor(resultMat, hsvMat, ColorConversionCodes.BGR2HSV);

                int hLow = targetHSVValue.Item0 - hueThreshold;
                int hHigh = targetHSVValue.Item0 + hueThreshold;
                int sLow = Math.Max(0, targetHSVValue.Item1 - saturationTolerance);
                int vLow = Math.Max(0, targetHSVValue.Item2 - valueTolerance);

                // 处理色相环绕
                if (hLow < 0)
                {
                    using Mat mask1 = new Mat();
                    using Mat mask2 = new Mat();
                    Cv2.InRange(hsvMat, new Scalar(0, sLow, vLow), new Scalar(hHigh, 255, 255), mask1);
                    Cv2.InRange(hsvMat, new Scalar(180 + hLow, sLow, vLow), new Scalar(180, 255, 255), mask2);
                    Cv2.BitwiseOr(mask1, mask2, mask);
                }
                else if (hHigh > 180)
                {
                    using Mat mask1 = new Mat();
                    using Mat mask2 = new Mat();
                    Cv2.InRange(hsvMat, new Scalar(hLow, sLow, vLow), new Scalar(180, 255, 255), mask1);
                    Cv2.InRange(hsvMat, new Scalar(0, sLow, vLow), new Scalar(hHigh - 180, 255, 255), mask2);
                    Cv2.BitwiseOr(mask1, mask2, mask);
                }
                else
                {
                    Cv2.InRange(hsvMat, new Scalar(hLow, sLow, vLow), new Scalar(hHigh, 255, 255), mask);
                }
            }

            matchedPixels = Cv2.CountNonZero(mask);

            using Mat blackBackground = new Mat(resultMat.Size(), resultMat.Type(), Scalar.Black);
            using Mat filteredImage = new Mat();
            Cv2.BitwiseAnd(resultMat, resultMat, filteredImage, mask);

            using Mat inverseMask = new Mat();
            Cv2.BitwiseNot(mask, inverseMask);

            using Mat blackAreas = new Mat();
            Cv2.BitwiseAnd(blackBackground, blackBackground, blackAreas, inverseMask);

            Cv2.Add(filteredImage, blackAreas, resultMat);

            resultMat.Set(KEY_TOTAL_FILTER_PIXELS, totalFilterPixels);
            resultMat.Set(KEY_MATCHED_PIXELS, matchedPixels);

            return resultMat;
        }

        /// <summary>
        /// 对图像进行颜色过滤（简化版本，使用默认参数）
        /// </summary>
        public static Mat FilterColor(Mat sourceMat, Mat? maskMat, string targetColorHex, int colorThreshold)
        {
            // 向后兼容：使用默认的 LAB 颜色空间和自动计算的容差
            return FilterColor(sourceMat, maskMat, targetColorHex, 
                colorThreshold, 
                colorThreshold * 3,  // 饱和度容差
                colorThreshold * 3,  // 明度容差
                "LAB");
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
            using Mat sourceMat = Cv2.ImRead(sourcePath);
            if (sourceMat.Empty())
                throw new ArgumentException("无法读取源图像");

            // 读取遮罩图像（如果有）
            Mat? maskMat = null;
            if (!string.IsNullOrEmpty(maskPath))
            {
                maskMat = Cv2.ImRead(maskPath);
                if (maskMat.Empty())
                {
                    maskMat?.Dispose();
                    throw new ArgumentException("无法读取遮罩图像");
                }
            }

            try
            {
                return FilterColor(sourceMat, maskMat, targetColorHex, colorThreshold);
            }
            finally
            {
                maskMat?.Dispose();
            }
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
            using Mat grayMask = new Mat();
            Cv2.CvtColor(maskMat, grayMask, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(grayMask, grayMask, 127, 255, ThresholdTypes.Binary);
            
            // 统计遮罩中的白色像素数量
            int maskWhitePixels = Cv2.CountNonZero(grayMask);
            if (maskWhitePixels == 0)
                return 0;

            // 获取目标颜色
            var targetColor = GetColorFromHex(targetColorHex);

            // 创建1x1目标颜色图像并转换为HSV
            using Mat targetBGR = new Mat(1, 1, MatType.CV_8UC3, new Scalar(targetColor.B, targetColor.G, targetColor.R));
            using Mat targetHSV = new Mat();
            Cv2.CvtColor(targetBGR, targetHSV, ColorConversionCodes.BGR2HSV);
            var targetHSVValue = targetHSV.Get<Vec3b>(0, 0);

            // 转换源图像为HSV
            using Mat hsvMat = new Mat();
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
            using Mat colorMask = new Mat();
            Cv2.InRange(hsvMat, lowerBound, upperBound, colorMask);

            // 将颜色掩码与遮罩进行与运算，只保留遮罩区域内的匹配结果
            using Mat finalMask = new Mat();
            Cv2.BitwiseAnd(colorMask, grayMask, finalMask);

            // 统计匹配的像素数量
            int matchedPixels = Cv2.CountNonZero(finalMask);

            // 计算百分比
            return (double)matchedPixels / maskWhitePixels * 100;
        }
    }
} 
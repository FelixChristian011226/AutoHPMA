using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace AutoHPMA.Helpers.ImageHelper
{
    /// <summary>
    /// 模板匹配
    /// </summary>
    public class MatchTemplateHelper
    {
        public static Point MatchTemplate(Mat srcMat, Mat dstMat, TemplateMatchModes matchMode, Mat? maskMat = null, double threshold = 0.8)
        {
            try
            {
                using var result = new Mat();
                Cv2.MatchTemplate(srcMat, dstMat, result, matchMode, maskMat!);

                if (matchMode is TemplateMatchModes.SqDiff or TemplateMatchModes.CCoeff or TemplateMatchModes.CCorr)
                {
                    Cv2.Normalize(result, result, 0, 1, NormTypes.MinMax);
                }

                Cv2.MinMaxLoc(result, out var minValue, out var maxValue, out var minLoc, out var maxLoc);

                if (matchMode is TemplateMatchModes.SqDiff or TemplateMatchModes.SqDiffNormed)
                {
                    if (minValue <= 1 - threshold)
                    {
                        return minLoc;
                    }
                }
                else
                {
                    if (maxValue >= threshold)
                    {
                        return maxLoc;
                    }
                }

                return default;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex.Message);
                //_logger.LogDebug(ex, ex.Message);
                return default;
            }
        }

        public static List<Point> MatchTemplateMulti(Mat srcMat, Mat dstMat, Mat? maskMat = null, double threshold = 0.8, int maxCount = 8)
        {
            var points = new List<Point>();
            try
            {
                using var result = new Mat();
                Cv2.MatchTemplate(srcMat, dstMat, result, TemplateMatchModes.CCoeffNormed, maskMat!);

                using var mask = new Mat(result.Height, result.Width, MatType.CV_8UC1, Scalar.White);
                using var maskSub = new Mat(result.Height, result.Width, MatType.CV_8UC1, Scalar.Black);
                while (points.Count < maxCount)
                {
                    Cv2.MinMaxLoc(result, out _, out var maxValue, out _, out var maxLoc, mask);
                    if (maxValue < threshold)
                        break;

                    points.Add(maxLoc);

                    // 使用矩形遮罩排除已匹配区域
                    var maskRect = new Rect(maxLoc.X, maxLoc.Y, dstMat.Width, dstMat.Height);
                    maskSub.Rectangle(maskRect, Scalar.White, -1);
                    Cv2.Subtract(mask, maskSub, mask);
                    maskSub.Rectangle(maskRect, Scalar.Black, -1); // 重置 maskSub
                }

                return points;
            }
            catch (Exception)
            {
                // 静默处理异常，返回已找到的结果
                return points;
            }
        }

        public static List<Rect> MatchOnePicForOnePic(Mat srcMat, Mat dstMat, TemplateMatchModes matchMode, Mat? maskMat, double threshold, int maxCount = -1)
        {
            List<Rect> list = [];

            if (maxCount < 0)
            {
                maxCount = srcMat.Width * srcMat.Height / dstMat.Width / dstMat.Height;
            }

            // 克隆源图像，避免修改原图
            using var workMat = srcMat.Clone();

            for (int i = 0; i < maxCount; i++)
            {
                var point = MatchTemplate(workMat, dstMat, matchMode, maskMat, threshold);
                if (point != new Point())
                {
                    // 在工作副本上遮罩已匹配区域
                    Cv2.Rectangle(workMat, point, new Point(point.X + dstMat.Width, point.Y + dstMat.Height), Scalar.Black, -1);
                    list.Add(new Rect(point.X, point.Y, dstMat.Width, dstMat.Height));
                }
                else
                {
                    break;
                }
            }

            return list;
        }

        /// <summary>
        /// 从输入图像生成Mask，透明区域为黑色，非透明区域为白色
        /// </summary>
        /// <param name="inputMat">输入图像（需要带 Alpha 通道，即 BGRA 格式）</param>
        /// <returns>生成的 Mask（调用方负责释放）</returns>
        public static Mat GenerateMask(Mat inputMat)
        {
            try
            {
                // 确保输入图像是 BGRA 格式
                Mat bgraMat;
                bool needDispose = false;

                if (inputMat.Channels() == 4)
                {
                    bgraMat = inputMat;
                }
                else
                {
                    bgraMat = new Mat();
                    Cv2.CvtColor(inputMat, bgraMat, ColorConversionCodes.BGR2BGRA);
                    needDispose = true;
                }

                try
                {
                    // 使用向量化操作提取 Alpha 通道并生成 Mask
                    // Split 分离 BGRA 通道: [0]=B, [1]=G, [2]=R, [3]=A
                    Cv2.Split(bgraMat, out Mat[] channels);

                    // 释放不需要的通道
                    channels[0].Dispose();
                    channels[1].Dispose();
                    channels[2].Dispose();

                    // 使用 Alpha 通道，阈值处理生成二值 Mask
                    using var alphaChannel = channels[3];
                    var mask = new Mat();
                    Cv2.Threshold(alphaChannel, mask, 0, 255, ThresholdTypes.Binary);

                    return mask;
                }
                finally
                {
                    if (needDispose)
                    {
                        bgraMat.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                // 发生异常时返回空 Mat
                return new Mat();
            }
        }

    }
}

using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace AutoHPMA.Helpers
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

                var mask = new Mat(result.Height, result.Width, MatType.CV_8UC1, Scalar.White);
                var maskSub = new Mat(result.Height, result.Width, MatType.CV_8UC1, Scalar.Black);
                while (true)
                {
                    Cv2.MinMaxLoc(result, out _, out var maxValue, out _, out var maxLoc, mask);
                    var maskRect = new Rect(maxLoc.X, maxLoc.Y, dstMat.Width, dstMat.Height);
                    maskSub.Rectangle(maskRect, Scalar.White, -1);
                    mask -= maskSub;
                    if (maxValue >= threshold)
                        points.Add(maxLoc);
                    else
                        break;
                }

                return points;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex.Message);
                //_logger.LogDebug(ex, ex.Message);
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

            for (int i = 0; i < maxCount; i++)
            {
                var point = MatchTemplate(srcMat, dstMat, matchMode, maskMat, threshold);
                if (point != new Point())
                {
                    // 把结果给遮掩掉，避免重复识别
                    Cv2.Rectangle(srcMat, point, new Point(point.X + dstMat.Width, point.Y + dstMat.Height), Scalar.Black, -1);
                    list.Add(new Rect(point.X, point.Y, dstMat.Width, dstMat.Height));
                }
                else
                {
                    break;
                }
            }

            return list;
        }


    }
}

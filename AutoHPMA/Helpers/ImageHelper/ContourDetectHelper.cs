using OpenCvSharp;
using System;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace AutoHPMA.Helpers.ImageHelper
{
    public class ContourDetectHelper
    {
        /// <summary>
        /// 对彩色图像进行二值化处理。
        /// </summary>
        /// <param name="src">输入的彩色图像（BGR 格式）。</param>
        /// <param name="threshold">二值化阈值（0~255）。像素值大于该阈值的置为白色，否则置为黑色。</param>
        /// <returns>二值化后的黑白图像（单通道，CV_8UC1）。</returns>
        public static Mat Binarize(Mat src, double threshold = 127)
        {
            // 1. 将彩色图像转换为灰度图
            Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            // 2. 创建目标 Mat，单通道 8-bit
            Mat binary = new Mat();

            // 3. 应用固定阈值二值化
            //    THRESH_BINARY: 大于 threshold 的像素设为 maxValue（255），否则设为 0
            Cv2.Threshold(gray, binary, threshold, 255, ThresholdTypes.Binary);

            return binary;
        }

        /// <summary>
        /// 对二值图像进行轮廓检测，返回一个近似矩形。
        /// </summary>
        /// <param name="binaryImage">二值化后的单通道图像 (CV_8UC1)。</param>
        public static Rect DetectApproxRectangle(Mat binaryImage)
        {
            if (binaryImage.Channels() != 1)
                throw new ArgumentException("Input image must be binary (1-channel).");

            // 1. 轮廓检测
            Cv2.FindContours(binaryImage.Clone(), out Point[][] contours, out HierarchyIndex[] hierarchy,
                             RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            Rect bestRect = default;
            double maxArea = 0;

            foreach (var contour in contours)
            {
                // 2. 去除太小的轮廓（比如文字、噪声）
                double area = Cv2.ContourArea(contour);
                if (area < 1000) continue;  // 可根据图像大小调整这个阈值

                // 3. 多边形逼近
                var approx = Cv2.ApproxPolyDP(contour, 5, true);

                // 4. 可选：也可检测是否接近矩形（4边）但这里我们只要包围矩形
                Rect boundingRect = Cv2.BoundingRect(approx);

                // 5. 选择最大区域的矩形
                if (area > maxArea)
                {
                    maxArea = area;
                    bestRect = boundingRect;
                }
            }

            return bestRect;
        }
    }
}

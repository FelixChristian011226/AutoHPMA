using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace AutoHPMA.Helpers
{
    public class RectangleDetectHelper
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
        /// 用形态学梯度找出黑白交界处的边缘，再检测水平/垂直直线并标注。
        /// </summary>
        /// <param name="binary">输入二值化后的单通道图像 (CV_8UC1)。</param>
        /// <param name="morphSize">形态学操作的核大小。</param>
        /// <param name="gradThresh">二值化梯度图的阈值。</param>
        /// <param name="rho">霍夫变换中距离精度（像素）。</param>
        /// <param name="theta">霍夫变换中角度精度（弧度）。</param>
        /// <param name="houghThresh">霍夫变换累加器阈值。</param>
        /// <param name="minLen">直线最小长度。</param>
        /// <param name="maxGap">直线最大间隙。</param>
        /// <param name="angleThresh">角度阈值 (度) 用于分类水平/垂直。</param>
        /// <returns>在原图上标注后的彩色图像。</returns>
        public static Mat DetectBlackWhiteBordersWithMorph(
            Mat binary,
            int morphSize = 3,
            byte gradThresh = 50,
            double rho = 1, double theta = Math.PI / 180, int houghThresh = 50,
            double minLen = 60, double maxGap = 10, double angleThresh = 1)
        {
            // 1. 形态学梯度：得到二值图中白黑边界的轮廓
            Mat grad = new Mat();
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(morphSize, morphSize));
            Cv2.MorphologyEx(binary, grad, MorphTypes.Gradient, kernel);

            // 2. 二值化梯度图（去掉轻微噪声）
            Mat edges = new Mat();
            Cv2.Threshold(grad, edges, gradThresh, 255, ThresholdTypes.Binary);

            // 3. 霍夫直线检测
            var lines = Cv2.HoughLinesP(edges, rho, theta, houghThresh, minLen, maxGap);

            // 4. 将原二值图转换为 BGR 以便画彩色线
            Mat output = new Mat();
            Cv2.CvtColor(binary, output, ColorConversionCodes.GRAY2BGR);

            // 5. 标注水平（红）和垂直（蓝）直线
            foreach (var l in lines)
            {
                Point p1 = l.P1, p2 = l.P2;
                double ang = Math.Abs(Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180.0 / Math.PI);
                if (ang < angleThresh || ang > 180 - angleThresh)
                    Cv2.Line(output, p1, p2, Scalar.Red, 2);
                else if (Math.Abs(ang - 90) < angleThresh)
                    Cv2.Line(output, p1, p2, Scalar.Blue, 2);
            }

            return output;
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

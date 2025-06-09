using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = OpenCvSharp.Point;

namespace AutoHPMA.Helpers
{
    public class ProgressCircleDetector
    {
        /// <summary>
        /// 检测圆形进度条并计算进度
        /// </summary>
        /// <param name="inputImage">输入图像</param>
        /// <param name="minRadius">最小圆半径</param>
        /// <param name="maxRadius">最大圆半径</param>
        /// <returns>包含检测结果的元组：(进度百分比, 标注后的图像)</returns>
        public static (double progress, Mat resultImage) DetectProgressCircle(Mat inputImage, int minRadius = 20, int maxRadius = 100)
        {
            // 克隆输入图像用于绘制结果
            Mat resultImage = inputImage.Clone();
            
            // 转换为HSV颜色空间，便于颜色分割
            Mat hsv = new Mat();
            Cv2.CvtColor(inputImage, hsv, ColorConversionCodes.BGR2HSV);
            
            // 定义金黄色的HSV范围（可以根据实际情况调整）
            Scalar lowerGold = new Scalar(20, 100, 100);  // 金黄色的HSV下限
            Scalar upperGold = new Scalar(30, 255, 255);  // 金黄色的HSV上限
            
            // 创建掩码
            Mat goldMask = new Mat();
            Cv2.InRange(hsv, lowerGold, upperGold, goldMask);
            
            // 使用霍夫圆变换检测圆形
            var circles = Cv2.HoughCircles(
                goldMask,
                HoughModes.Gradient,
                dp: 1,
                minDist: inputImage.Rows / 8,
                param1: 100,
                param2: 30,
                minRadius: minRadius,
                maxRadius: maxRadius
            );
            
            if (circles == null || circles.Length == 0)
            {
                return (0, resultImage);
            }
            
            // 获取最大的圆（假设只有一个进度条）
            var circle = circles.OrderByDescending(c => c.Radius).First();
            
            // 创建圆形掩码
            Mat circleMask = new Mat(inputImage.Size(), MatType.CV_8UC1, Scalar.Black);
            Cv2.Circle(circleMask, (int)circle.Center.X, (int)circle.Center.Y, (int)circle.Radius, Scalar.White, -1);
            
            // 计算进度
            Mat progressMask = new Mat();
            Cv2.BitwiseAnd(goldMask, circleMask, progressMask);
            
            int totalPixels = Cv2.CountNonZero(circleMask);
            int progressPixels = Cv2.CountNonZero(progressMask);
            double progress = (double)progressPixels / totalPixels * 100;
            
            // 在结果图像上绘制检测到的圆和进度
            Cv2.Circle(resultImage, (int)circle.Center.X, (int)circle.Center.Y, (int)circle.Radius, Scalar.Green, 2);
            Cv2.Circle(resultImage, (int)circle.Center.X, (int)circle.Center.Y, 2, Scalar.Red, 3);
            
            // 添加进度文本
            string progressText = $"{progress:F1}%";
            Cv2.PutText(resultImage, progressText, 
                new Point((int)circle.Center.X - 30, (int)circle.Center.Y), 
                HersheyFonts.HersheySimplex, 0.8, Scalar.White, 2);
            
            return (progress, resultImage);
        }
    }
} 
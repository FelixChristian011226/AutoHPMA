using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;

namespace AutoHPMA.Helpers.RecognizeHelper;

public class TesseractOCRHelper
{
    public static string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

    public static Bitmap PreprocessImage(Bitmap inputBitmap)
    {
        // 将 Bitmap 转换为 Mat 对象
        Mat src = OpenCvSharp.Extensions.BitmapConverter.ToMat(inputBitmap);

        // 转换为灰度图
        Mat gray = new Mat();
        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

        // 二值化处理（阈值可以根据实际情况调整）
        Mat binary = new Mat();
        Cv2.Threshold(gray, binary, 128, 255, ThresholdTypes.Binary);

        // 可选：进行降噪处理
        Mat denoised = new Mat();
        Cv2.MedianBlur(binary, denoised, 3);

        // 转换回 Bitmap 返回
        return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(denoised);
    }


    /// <summary>
    /// 将 Bitmap 转换为 Pix 对象
    /// </summary>
    /// <param name="bmp">输入的 Bitmap</param>
    /// <returns>转换后的 Pix 对象</returns>
    public static Pix BitmapToPix(Bitmap bmp)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            // 指定使用 System.Drawing.Imaging.ImageFormat.Png 避免命名冲突
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return Pix.LoadFromMemory(ms.ToArray());
        }
    }

    public static string TesseractTextRecognition(Bitmap bmp)
    {
        string result = "";
        try
        {
            using (var engine = new TesseractEngine(tessDataPath, "chi_sim", EngineMode.Default))
            {
                using (var pix = BitmapToPix(bmp))
                {
                    using (var page = engine.Process(pix))
                    {
                        result = page.GetText();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OCR Error: {ex.Message}");
        }
        return result.Trim();
    }
}

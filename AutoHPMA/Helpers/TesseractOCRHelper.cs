using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;

namespace AutoHPMA.Helpers;

public class TesseractOCRHelper
{
    public static string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using PaddleOCRSharp;

namespace AutoHPMA.Helpers;

public class PaddleOCRHelper
{
    private static readonly OCRModelConfig config = null;
    private static readonly OCRParameter oCRParameter = new OCRParameter
    {
        use_angle_cls = false,
        use_gpu = true
    };
    private static readonly PaddleOCREngine engine = new PaddleOCREngine(config, oCRParameter);

    public static string TextRecognition(Bitmap bmp)
    {
        string result = "";

        if (bmp == null)
            return result;

        OCRResult ocrResult = engine.DetectText(bmp);

        foreach (var item in ocrResult.TextBlocks)
        {
            result += item.Text;
        }

        return result;
    }
}


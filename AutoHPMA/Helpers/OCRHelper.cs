using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using PaddleOCRSharp;

namespace AutoHPMA.Helpers;

public class OCRHelper
{
    public static string WordRecognition(Bitmap bmp)
    {
        string result="";

        OCRModelConfig config = null;
        OCRParameter oCRParameter = new OCRParameter();
        OCRResult ocrResult = new OCRResult();


        PaddleOCREngine engine = new PaddleOCREngine(config, oCRParameter);
        {
            ocrResult = engine.DetectText(bmp);
        }

        foreach (var item in ocrResult.TextBlocks)
        {
            result += item.Text + "\n";
        }

        return result;

    }
}

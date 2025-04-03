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
    public static string TextRecognition(Bitmap bmp)
    {
        string result="";

        OCRModelConfig config = null;
        OCRParameter oCRParameter = new OCRParameter {
            use_angle_cls = false,
            use_gpu = true
        };
        OCRResult ocrResult = new OCRResult();


        PaddleOCREngine engine = new PaddleOCREngine(config, oCRParameter);
        {
            ocrResult = engine.DetectText(bmp);
        }

        foreach (var item in ocrResult.TextBlocks)
        {
            //result += item.Text + "\n";
            result += item.Text;
        }

        return result;

    }

}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using System.IO;
using Sdcb.PaddleOCR.Models.Local;

namespace AutoHPMA.Helpers.RecognizeHelper;

public class PaddleOCRHelper
{
    private readonly PaddleOcrAll paddleOcrAll;
    private readonly FullOcrModel model;

    public PaddleOCRHelper()
    {
        model = LocalFullModels.ChineseV4;

        paddleOcrAll = new PaddleOcrAll(model, PaddleDevice.Onnx())
        {
            AllowRotateDetection = false,
            Enable180Classification = false
        };
    }

    public string Ocr(Mat mat)
    {
        return OcrResult(mat).Text;
    }

    public PaddleOcrResult OcrResult(Mat mat)
    {
        var result = paddleOcrAll.Run(mat);
        return result;
    }

}


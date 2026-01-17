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

/// <summary>
/// PaddleOCR 辅助类（单例模式，避免重复加载模型）
/// </summary>
public class PaddleOCRHelper : IDisposable
{
    private static readonly Lazy<PaddleOCRHelper> _instance = new(() => new PaddleOCRHelper());
    
    /// <summary>
    /// 获取 PaddleOCRHelper 单例实例
    /// </summary>
    public static PaddleOCRHelper Instance => _instance.Value;

    private readonly PaddleOcrAll _paddleOcrAll;
    private readonly FullOcrModel _model;
    private bool _isDisposed;

    private PaddleOCRHelper()
    {
        _model = LocalFullModels.ChineseV4;
        _paddleOcrAll = new PaddleOcrAll(_model, PaddleDevice.Onnx())
        {
            AllowRotateDetection = false,
            Enable180Classification = false
        };
    }

    public string Ocr(Mat mat)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return OcrResult(mat).Text;
    }

    public PaddleOcrResult OcrResult(Mat mat)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _paddleOcrAll.Run(mat);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _paddleOcrAll?.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}

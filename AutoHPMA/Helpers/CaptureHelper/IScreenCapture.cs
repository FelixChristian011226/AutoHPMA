using OpenCvSharp;

namespace AutoHPMA.Helpers.CaptureHelper;

/// <summary>
/// 统一的屏幕捕获接口
/// </summary>
public interface IScreenCapture : IDisposable
{
    /// <summary>
    /// 是否正在捕获
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// 启动捕获
    /// </summary>
    /// <param name="hWnd">目标窗口句柄</param>
    void Start(nint hWnd);

    /// <summary>
    /// 停止捕获
    /// </summary>
    void Stop();

    /// <summary>
    /// 捕获一帧
    /// </summary>
    /// <returns>捕获的图像 (BGRA格式), 如果失败返回null</returns>
    Mat? Capture();
}

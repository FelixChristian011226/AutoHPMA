namespace AutoHPMA.Models
{
    /// <summary>
    /// 截屏方式枚举
    /// </summary>
    public enum CaptureMethod
    {
        /// <summary>
        /// Windows Graphics Capture API (推荐，支持硬件加速)
        /// </summary>
        WGC,
        
        /// <summary>
        /// BitBlt GDI API (兼容性好)
        /// </summary>
        BitBlt,
        
        /// <summary>
        /// PrintWindow API (可捕获非前台窗口)
        /// </summary>
        PrintWindow
    }
}
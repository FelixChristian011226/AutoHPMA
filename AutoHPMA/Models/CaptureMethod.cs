namespace AutoHPMA.Models
{
    /// <summary>
    /// 截屏方式枚举
    /// </summary>
    public enum CaptureMethod
    {
        /// <summary>
        /// Windows Graphics Capture API
        /// </summary>
        WGC,
        
        /// <summary>
        /// BitBlt API
        /// </summary>
        BitBlt
    }
}
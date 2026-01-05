using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.ImageHelper;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace AutoHPMA.GameTask
{
    #region 匹配相关类

    /// <summary>
    /// 模板匹配选项
    /// </summary>
    public class MatchOptions
    {
        /// <summary>自定义遮罩（可选）</summary>
        public Mat? Mask { get; set; }

        /// <summary>是否使用模板的 Alpha 通道生成遮罩</summary>
        public bool UseAlphaMask { get; set; } = false;

        /// <summary>是否查找多个匹配</summary>
        public bool FindMultiple { get; set; } = false;

        /// <summary>匹配阈值（默认 0.9）</summary>
        public double Threshold { get; set; } = 0.9;

        /// <summary>匹配模式</summary>
        public TemplateMatchModes MatchMode { get; set; } = TemplateMatchModes.CCoeffNormed;
    }

    /// <summary>
    /// 模板匹配结果
    /// </summary>
    public class MatchResult
    {
        /// <summary>是否匹配成功</summary>
        public bool Success { get; set; }

        /// <summary>匹配位置（单个匹配时的左上角坐标，未缩放）</summary>
        public Point Location { get; set; }

        /// <summary>匹配区域列表（已缩放，用于显示）</summary>
        public List<Rect> Rects { get; set; } = new();

        /// <summary>匹配区域列表（未缩放，用于多重匹配点击）</summary>
        public List<Rect> RectsUnscaled { get; set; } = new();

        /// <summary>模板尺寸（未缩放）</summary>
        public Size TemplateSize { get; set; }

        /// <summary>静态失败结果</summary>
        public static MatchResult Failed => new() { Success = false };
    }

    #endregion

    public abstract class BaseGameTask : IGameTask
    {
        protected static LogWindow _logWindow => AppContextService.Instance.LogWindow;
        protected static MaskWindow _maskWindow => AppContextService.Instance.MaskWindow;
        protected static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;

        protected readonly ILogger _logger;
        protected nint _displayHwnd, _gameHwnd;
        protected int offsetX, offsetY;
        protected double scale;
        protected CancellationTokenSource _cts;

        public event EventHandler? TaskCompleted;

        public BaseGameTask(ILogger logger, nint displayHwnd, nint gameHwnd)
        {
            _logger = logger;
            _displayHwnd = displayHwnd;
            _gameHwnd = gameHwnd;
            _cts = new CancellationTokenSource();
            CalOffset();
        }

        #region 工具方法

        /// <summary>
        /// 缩放矩形区域
        /// </summary>
        protected Rect ScaleRect(Rect rect, double scale)
        {
            return new Rect(
                (int)(rect.X * scale),
                (int)(rect.Y * scale),
                (int)(rect.Width * scale),
                (int)(rect.Height * scale)
            );
        }

        /// <summary>
        /// 计算游戏窗口偏移和缩放比例
        /// </summary>
        protected void CalOffset()
        {
            int left, top, width, height;
            int leftMumu, topMumu;
            WindowInteractionHelper.GetWindowPositionAndSize(_displayHwnd, out leftMumu, out topMumu, out width, out height);
            WindowInteractionHelper.GetWindowPositionAndSize(_gameHwnd, out left, out top, out width, out height);
            offsetX = left - leftMumu;
            offsetY = top - topMumu;
            scale = width / 1280.0;
        }

        #endregion

        #region 截屏与预处理

        /// <summary>
        /// 捕获屏幕并进行预处理（缩放和颜色转换）
        /// </summary>
        /// <returns>预处理后的 Mat 对象（BGR 格式，已缩放至 1280 基准）</returns>
        protected Mat CaptureAndPreprocess()
        {
            _cts.Token.ThrowIfCancellationRequested();
            var captureMat = _capture.Capture();
            Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
            Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
            return captureMat;
        }

        #endregion

        #region 模板匹配（Find）

        /// <summary>
        /// 在屏幕上查找模板
        /// </summary>
        /// <param name="template">模板图像（支持 BGR 或 BGRA）</param>
        /// <param name="options">匹配选项（可选）</param>
        /// <returns>匹配结果</returns>
        protected MatchResult Find(Mat template, MatchOptions? options = null)
        {
            options ??= new MatchOptions();
            var captureMat = CaptureAndPreprocess();

            // 处理模板和遮罩
            Mat templateBGR;
            Mat? mask = options.Mask;

            if (template.Channels() == 4)
            {
                // 4通道模板（带透明通道）
                if (options.UseAlphaMask && mask == null)
                {
                    mask = MatchTemplateHelper.GenerateMask(template);
                }
                templateBGR = new Mat();
                Cv2.CvtColor(template, templateBGR, ColorConversionCodes.BGRA2BGR);
            }
            else
            {
                templateBGR = template;
            }

            // 执行匹配
            if (options.FindMultiple)
            {
                // 多重匹配
                var rects = MatchTemplateHelper.MatchOnePicForOnePic(
                    captureMat, templateBGR, options.MatchMode, mask, options.Threshold);

                if (rects.Count == 0) return MatchResult.Failed;

                return new MatchResult
                {
                    Success = true,
                    Location = new Point(rects[0].X, rects[0].Y),
                    Rects = rects.Select(r => ScaleRect(r, scale)).ToList(),
                    RectsUnscaled = rects,
                    TemplateSize = new Size(template.Width, template.Height)
                };
            }
            else
            {
                // 单个匹配
                var matchPoint = MatchTemplateHelper.MatchTemplate(
                    captureMat, templateBGR, options.MatchMode, mask, options.Threshold);

                if (matchPoint == default) return MatchResult.Failed;

                var unscaledRect = new Rect(matchPoint.X, matchPoint.Y, template.Width, template.Height);

                return new MatchResult
                {
                    Success = true,
                    Location = matchPoint,
                    Rects = new List<Rect> { ScaleRect(unscaledRect, scale) },
                    RectsUnscaled = new List<Rect> { unscaledRect },
                    TemplateSize = new Size(template.Width, template.Height)
                };
            }
        }

        #endregion

        #region 点击操作（Click）

        /// <summary>
        /// 点击指定位置（未缩放坐标）
        /// </summary>
        /// <param name="location">未缩放的坐标位置</param>
        protected void Click(Point location)
        {
            _cts.Token.ThrowIfCancellationRequested();
            WindowInteractionHelper.SendMouseClick(
                _gameHwnd,
                (uint)(location.X * scale - offsetX),
                (uint)(location.Y * scale - offsetY)
            );
        }

        /// <summary>
        /// 根据匹配结果点击模板中心位置
        /// </summary>
        /// <param name="result">匹配结果</param>
        protected void ClickMatchCenter(MatchResult result)
        {
            if (!result.Success) return;
            var centerX = result.Location.X + result.TemplateSize.Width / 2.0;
            var centerY = result.Location.Y + result.TemplateSize.Height / 2.0;
            Click(new Point((int)centerX, (int)centerY));
        }

        /// <summary>
        /// 点击多个匹配结果的中心位置
        /// </summary>
        /// <param name="result">匹配结果</param>
        /// <param name="delayMs">每次点击之间的延迟（毫秒）</param>
        protected async Task ClickMultiMatchCentersAsync(MatchResult result, int delayMs = 1000)
        {
            if (!result.Success) return;

            foreach (var rect in result.RectsUnscaled)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var centerX = rect.X + result.TemplateSize.Width / 2.0;
                var centerY = rect.Y + result.TemplateSize.Height / 2.0;
                Click(new Point((int)centerX, (int)centerY));
                await Task.Delay(delayMs, _cts.Token);
            }
        }

        /// <summary>
        /// 尝试查找并点击模板
        /// </summary>
        /// <param name="template">模板图像</param>
        /// <param name="threshold">匹配阈值</param>
        /// <returns>是否成功找到并点击</returns>
        protected bool TryClickTemplate(Mat template, double threshold = 0.9)
        {
            var result = Find(template, new MatchOptions { Threshold = threshold });
            if (result.Success)
            {
                ClickMatchCenter(result);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试查找并点击带 Alpha 通道的模板
        /// </summary>
        /// <param name="template">带 Alpha 通道的模板图像</param>
        /// <param name="threshold">匹配阈值</param>
        /// <param name="showMatch">是否显示匹配结果</param>
        /// <returns>是否成功找到并点击</returns>
        protected bool TryClickTemplateWithAlpha(Mat template, double threshold = 0.9, bool showMatch = false)
        {
            var result = Find(template, new MatchOptions { UseAlphaMask = true, Threshold = threshold });
            if (result.Success)
            {
                if (showMatch)
                    ShowMatchRects(result, "Click");
                ClickMatchCenter(result);
                return true;
            }
            return false;
        }

        #endregion

        #region 拖拽操作

        /// <summary>
        /// 拖拽移动（未缩放坐标）
        /// </summary>
        protected bool DragMove(ref Point start, ref Point end, int duration = 500)
        {
            _cts.Token.ThrowIfCancellationRequested();
            WindowInteractionHelper.SendMouseDragWithNoise(
                _gameHwnd,
                (uint)(start.X * scale - offsetX),
                (uint)(start.Y * scale - offsetY),
                (uint)(end.X * scale - offsetX),
                (uint)(end.Y * scale - offsetY),
                duration
            );
            return true;
        }

        #endregion

        #region 显示匹配结果

        /// <summary>
        /// 在遮罩窗口显示匹配结果
        /// </summary>
        /// <param name="result">匹配结果</param>
        /// <param name="layerName">图层名称</param>
        protected void ShowMatchRects(MatchResult result, string layerName = "Match")
        {
            if (result.Success)
            {
                _maskWindow?.SetLayerRects(layerName, result.Rects);
            }
        }

        /// <summary>
        /// 清除指定图层的显示
        /// </summary>
        /// <param name="layerName">图层名称</param>
        protected void ClearMatchRects(string layerName = "Match")
        {
            _maskWindow?.ClearLayer(layerName);
        }

        #endregion

        #region 任务控制

        public virtual void Stop()
        {
            _cts.Cancel();
            TaskCompleted?.Invoke(this, EventArgs.Empty);
        }

        // 子类必须实现
        public abstract void Start();
        public abstract bool SetParameters(Dictionary<string, object> parameters);

        #endregion
    }
}
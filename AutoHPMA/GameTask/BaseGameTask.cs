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
    public abstract class BaseGameTask : IGameTask
    {
        protected static LogWindow _logWindow => AppContextService.Instance.LogWindow;
        protected static MaskWindow _maskWindow => AppContextService.Instance.MaskWindow;
        protected static WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;

        protected readonly ILogger _logger;
        protected nint _displayHwnd, _gameHwnd;
        protected int offsetX, offsetY;
        protected double scale;
        protected List<Rect> detect_rects = new();
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

        protected Rect ScaleRect(Rect rect, double scale)
        {
            return new Rect(
                (int)(rect.X * scale),
                (int)(rect.Y * scale),
                (int)(rect.Width * scale),
                (int)(rect.Height * scale)
            );
        }

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

        protected bool FindMatch(ref Mat mat, double threshold = 0.9)
        {
            _cts.Token.ThrowIfCancellationRequested();
            var captureMat = _capture.Capture();
            Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
            Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
            if (matchpoint == default) return false;
            detect_rects.Clear();
            detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
            _maskWindow?.SetLayerRects("Match", detect_rects);
            return true;
        }

        protected bool FindAndClick(ref Mat mat, double threshold = 0.9)
        {
            _cts.Token.ThrowIfCancellationRequested();
            var captureMat = _capture.Capture();
            Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
            Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
            if (matchpoint == default) return false;
            detect_rects.Clear();
            detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
            _maskWindow?.SetLayerRects("Match", detect_rects);
            WindowInteractionHelper.SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + mat.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + mat.Height / 2.0 * scale));
            return true;
        }

        protected bool FindAndClickWithMask(ref Mat mat, ref Mat mask, double threshold = 0.9)
        {
            _cts.Token.ThrowIfCancellationRequested();
            var captureMat = _capture.Capture();
            Cv2.Resize(captureMat, captureMat, new Size(mask.Width, mask.Height));
            Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
            Cv2.BitwiseAnd(captureMat, mask, captureMat);
            var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
            if (matchpoint == default) return false;
            detect_rects.Clear();
            detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
            _maskWindow?.SetLayerRects("Match", detect_rects);
            WindowInteractionHelper.SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + mat.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + mat.Height / 2.0 * scale));
            return true;
        }

        protected bool FindAndClickWithAlpha(ref Mat mat, double threshold = 0.9)
        {
            _cts.Token.ThrowIfCancellationRequested();
            var captureMat = _capture.Capture();
            var maskMat = MatchTemplateHelper.GenerateMask(mat);
            Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
            Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);
            using(var matBGR = mat.Clone())
            {
                Cv2.CvtColor(matBGR, matBGR, ColorConversionCodes.BGRA2BGR);
                var matchpoint = MatchTemplateHelper.MatchTemplate(captureMat, matBGR, TemplateMatchModes.CCoeffNormed, maskMat, threshold);
                if (matchpoint == default) return false;
                detect_rects.Clear();
                detect_rects.Add(new Rect((int)(matchpoint.X * scale), (int)(matchpoint.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
                _maskWindow?.SetLayerRects("Match", detect_rects);
                WindowInteractionHelper.SendMouseClick(_gameHwnd, (uint)(matchpoint.X * scale - offsetX + mat.Width / 2.0 * scale), (uint)(matchpoint.Y * scale - offsetY + mat.Height / 2.0 * scale));
                return true;
            }      
        }

        protected async Task<bool> FindAndClickMultiAsync(Mat mat, double threshold = 0.9)
        {
            _cts.Token.ThrowIfCancellationRequested();
            var captureMat = _capture.Capture();
            Cv2.Resize(captureMat, captureMat, new Size(captureMat.Width / scale, captureMat.Height / scale));
            Cv2.CvtColor(captureMat, captureMat, ColorConversionCodes.BGRA2BGR);

            var matchrects = MatchTemplateHelper.MatchOnePicForOnePic(captureMat, mat, TemplateMatchModes.CCoeffNormed, null, threshold);
            if (matchrects.Count == 0)
            {
                return false;
            }
            detect_rects.Clear();
            foreach (var rect in matchrects)
            {
                detect_rects.Add(new Rect((int)(rect.X * scale), (int)(rect.Y * scale), (int)(mat.Width * scale), (int)(mat.Height * scale)));
            }
            _maskWindow?.SetLayerRects("MultiClick", detect_rects);
            foreach (var rect in matchrects)
            {
                WindowInteractionHelper.SendMouseClick(_gameHwnd,
                    (uint)(rect.X * scale - offsetX + mat.Width / 2.0 * scale),
                    (uint)(rect.Y * scale - offsetY + mat.Height / 2.0 * scale));
                //_logWindow?.AddLogMessage("DBG", "第" + ++index + "次点赞：(" + rect.X + "," + rect.Y + ")");
                await Task.Delay(1000);
            }
            _maskWindow?.ClearLayer("MultiClick");
            return true;
        }

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

        public virtual void Stop()
        {
            _cts.Cancel();
            TaskCompleted?.Invoke(this, EventArgs.Empty);
        }

        // 子类必须实现
        public abstract void Start();
        public abstract bool SetParameters(Dictionary<string, object> parameters);
    }
} 
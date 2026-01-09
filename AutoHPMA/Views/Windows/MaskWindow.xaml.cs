using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using OpenCvSharp;
using Rect = OpenCvSharp.Rect;

namespace AutoHPMA.Views.Windows
{
    public partial class MaskWindow : System.Windows.Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        /// <summary>
        /// 检测框数据结构
        /// </summary>
        private class RectData
        {
            public Rect Rect { get; set; }
            public string? Text { get; set; }
        }

        /// <summary>
        /// 临时检测框（带过期时间）
        /// </summary>
        private class TemporaryRect : RectData
        {
            public DateTime ExpireTime { get; set; }
        }

        // 临时检测框列表（自动过期，如点击操作）
        private readonly List<TemporaryRect> _temporaryRects = new();

        // 状态标识框（由 FindStateByRules 自动设置，表示当前所处的状态）
        private readonly List<RectData> _stateIndicatorRects = new();

        // 任务状态框（由任务代码手动设置，如选项框、厨具状态等）
        private readonly List<RectData> _taskStateRects = new();

        // 清理定时器
        private DispatcherTimer? _cleanupTimer;

        // 用于线程安全的锁
        private readonly object _lock = new();

        public MaskWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置窗口为透明并穿透鼠标事件
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            // 启动清理定时器
            StartCleanupTimer();
        }

        #region 定时器管理

        /// <summary>
        /// 启动定期清理临时检测框的定时器
        /// </summary>
        private void StartCleanupTimer()
        {
            _cleanupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _cleanupTimer.Tick += (s, e) => CleanupExpiredRects();
            _cleanupTimer.Start();
        }

        /// <summary>
        /// 清理过期的临时检测框
        /// </summary>
        private void CleanupExpiredRects()
        {
            bool needsRedraw = false;
            lock (_lock)
            {
                var now = DateTime.Now;
                int removed = _temporaryRects.RemoveAll(r => r.ExpireTime <= now);
                needsRedraw = removed > 0;
            }

            if (needsRedraw)
            {
                Redraw();
            }
        }

        #endregion

        #region 临时检测框方法

        /// <summary>
        /// 添加单个临时检测框
        /// </summary>
        /// <param name="rect">检测框区域</param>
        /// <param name="text">可选的文字内容</param>
        /// <param name="durationMs">显示时长（毫秒），默认 1500ms</param>
        public void AddTemporaryRect(Rect rect, string? text = null, int durationMs = 500)
        {
            lock (_lock)
            {
                _temporaryRects.Add(new TemporaryRect
                {
                    Rect = rect,
                    Text = text,
                    ExpireTime = DateTime.Now.AddMilliseconds(durationMs)
                });
            }
            Redraw();
        }

        /// <summary>
        /// 添加多个临时检测框
        /// </summary>
        /// <param name="rects">检测框区域列表</param>
        /// <param name="textContents">可选的文字内容字典</param>
        /// <param name="durationMs">显示时长（毫秒），默认 1500ms</param>
        public void AddTemporaryRects(List<Rect> rects, Dictionary<Rect, string>? textContents = null, int durationMs = 500)
        {
            var expireTime = DateTime.Now.AddMilliseconds(durationMs);
            lock (_lock)
            {
                foreach (var rect in rects)
                {
                    string? text = null;
                    textContents?.TryGetValue(rect, out text);
                    _temporaryRects.Add(new TemporaryRect
                    {
                        Rect = rect,
                        Text = text,
                        ExpireTime = expireTime
                    });
                }
            }
            Redraw();
        }

        #endregion

        #region 状态标识框方法（FindStateByRules 自动设置）

        /// <summary>
        /// 设置状态标识框（由 FindStateByRules 自动调用）
        /// </summary>
        /// <param name="rects">检测框区域列表</param>
        public void SetStateIndicatorRects(List<Rect> rects)
        {
            lock (_lock)
            {
                _stateIndicatorRects.Clear();
                foreach (var rect in rects)
                {
                    _stateIndicatorRects.Add(new RectData { Rect = rect });
                }
            }
            Redraw();
        }

        /// <summary>
        /// 清除状态标识框
        /// </summary>
        public void ClearStateIndicatorRects()
        {
            lock (_lock)
            {
                _stateIndicatorRects.Clear();
            }
            Redraw();
        }

        #endregion

        #region 任务状态框方法（任务代码手动设置）

        /// <summary>
        /// 设置任务状态框（覆盖式更新）
        /// </summary>
        /// <param name="rects">检测框区域列表</param>
        /// <param name="textContents">可选的文字内容字典</param>
        public void SetTaskStateRects(List<Rect> rects, Dictionary<Rect, string>? textContents = null)
        {
            lock (_lock)
            {
                _taskStateRects.Clear();
                foreach (var rect in rects)
                {
                    string? text = null;
                    textContents?.TryGetValue(rect, out text);
                    _taskStateRects.Add(new RectData
                    {
                        Rect = rect,
                        Text = text
                    });
                }
            }
            Redraw();
        }

        /// <summary>
        /// 清除任务状态框
        /// </summary>
        public void ClearTaskStateRects()
        {
            lock (_lock)
            {
                _taskStateRects.Clear();
            }
            Redraw();
        }

        #endregion

        #region 兼容旧 API

        /// <summary>
        /// 设置状态检测框（兼容旧 API，等同于 SetTaskStateRects）
        /// </summary>
        public void SetStateRects(List<Rect> rects, Dictionary<Rect, string>? textContents = null)
        {
            SetTaskStateRects(rects, textContents);
        }

        /// <summary>
        /// 清除状态检测框（兼容旧 API，等同于 ClearTaskStateRects）
        /// </summary>
        public void ClearStateRects()
        {
            ClearTaskStateRects();
        }

        #endregion

        #region 清除所有

        /// <summary>
        /// 清除所有检测框
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _temporaryRects.Clear();
                _stateIndicatorRects.Clear();
                _taskStateRects.Clear();
            }
            Redraw();
        }

        #endregion

        #region 绘制逻辑

        /// <summary>
        /// 重绘所有检测框
        /// </summary>
        private void Redraw()
        {
            // 确保在 UI 线程执行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(Redraw));
                return;
            }

            RootGrid.Children.Clear();
            var canvas = new Canvas { Background = Brushes.Transparent };

            // 处理 DPI 缩放
            PresentationSource source = PresentationSource.FromVisual(this);
            double dpiX = 1.0, dpiY = 1.0;
            if (source?.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformFromDevice.M11;
                dpiY = source.CompositionTarget.TransformFromDevice.M22;
            }

            lock (_lock)
            {
                // 绘制状态标识框（绿色，表示当前状态）
                foreach (var stateRect in _stateIndicatorRects)
                {
                    DrawRect(canvas, stateRect.Rect, stateRect.Text, Brushes.LimeGreen, dpiX, dpiY);
                }

                // 绘制任务状态框（蓝色）
                foreach (var taskRect in _taskStateRects)
                {
                    DrawRect(canvas, taskRect.Rect, taskRect.Text, Brushes.DeepSkyBlue, dpiX, dpiY);
                }

                // 绘制临时检测框（红色）
                foreach (var tempRect in _temporaryRects)
                {
                    DrawRect(canvas, tempRect.Rect, tempRect.Text, Brushes.Red, dpiX, dpiY);
                }
            }

            RootGrid.Children.Add(canvas);
        }

        /// <summary>
        /// 绘制单个检测框
        /// </summary>
        private void DrawRect(Canvas canvas, Rect rect, string? text, Brush stroke, double dpiX, double dpiY)
        {
            var shape = new Rectangle
            {
                Stroke = stroke,
                StrokeThickness = 2,
                Width = rect.Width * dpiX,
                Height = rect.Height * dpiY
            };
            Canvas.SetLeft(shape, rect.X * dpiX);
            Canvas.SetTop(shape, rect.Y * dpiY);
            canvas.Children.Add(shape);

            // 如果有文字内容，添加 TextBlock
            if (!string.IsNullOrEmpty(text))
            {
                var textBlock = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(textBlock, rect.X * dpiX + 5);
                Canvas.SetTop(textBlock, rect.Y * dpiY + 5);
                canvas.Children.Add(textBlock);
            }
        }

        #endregion

        #region 窗口位置同步

        /// <summary>
        /// 同步遮罩窗口位置与大小
        /// </summary>
        public void RefreshPosition(IntPtr targetHwnd, int offsetX = 0, int offsetY = 0)
        {
            if (GetWindowRect(targetHwnd, out RECT winRect))
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                double dpiX = 1.0, dpiY = 1.0;
                if (source?.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformFromDevice.M11;
                    dpiY = source.CompositionTarget.TransformFromDevice.M22;
                }

                Left = winRect.Left * dpiX + offsetX;
                Top = winRect.Top * dpiY + offsetY;
                Width = (winRect.Right - winRect.Left) * dpiX;
                Height = (winRect.Bottom - winRect.Top) * dpiY;
            }
        }

        #endregion

        #region Win32 API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }
        #endregion
    }
}

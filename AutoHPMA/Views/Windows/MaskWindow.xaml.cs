using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using OpenCvSharp;
using Rect = OpenCvSharp.Rect;    // 新增：引用 OpenCvSharp 命名空间

namespace AutoHPMA.Views.Windows
{
    public partial class MaskWindow : System.Windows.Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        // 每个图层都包含一个 Canvas 和对应的 Rect 列表
        private class Layer
        {
            public Canvas Canvas { get; }
            public List<Rect> Rects { get; } = new List<Rect>();
            public Layer(string name)
            {
                Canvas = new Canvas { Name = name, Background = Brushes.Transparent };
            }
        }

        // 管理所有图层，Key 可以是任意字符串标识
        private readonly Dictionary<string, Layer> _layers = new Dictionary<string, Layer>();

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
        }

        #region 图层管理方法

        /// <summary>
        /// 添加一个新图层（若已存在，则返回已有图层）
        /// </summary>
        public void AddLayer(string layerName)
        {
            if (_layers.ContainsKey(layerName)) return;
            var layer = new Layer(layerName);
            _layers[layerName] = layer;
            RootGrid.Children.Add(layer.Canvas);
        }

        /// <summary>
        /// 设置指定图层的矩形列表（覆盖式更新）
        /// </summary>
        public void SetLayerRects(string layerName, List<Rect> rects)
        {
            EnsureLayer(layerName);
            var layer = _layers[layerName];
            layer.Rects.Clear();
            layer.Rects.AddRange(rects);
            RedrawLayer(layer);
        }

        /// <summary>
        /// 清除指定图层的所有矩形
        /// </summary>
        public void ClearLayer(string layerName)
        {
            if (!_layers.TryGetValue(layerName, out var layer)) return;
            layer.Rects.Clear();
            layer.Canvas.Children.Clear();
        }

        /// <summary>
        /// 显示指定图层
        /// </summary>
        public void ShowLayer(string layerName)
        {
            if (_layers.TryGetValue(layerName, out var layer))
                layer.Canvas.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 隐藏指定图层
        /// </summary>
        public void HideLayer(string layerName)
        {
            if (_layers.TryGetValue(layerName, out var layer))
                layer.Canvas.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 清除所有图层及其内容
        /// </summary>
        public void ClearAllLayers()
        {
            foreach (var layer in _layers.Values)
            {
                layer.Rects.Clear();
                layer.Canvas.Children.Clear();
            }
            RootGrid.Children.Clear();
            _layers.Clear();
        }

        private void EnsureLayer(string layerName)
        {
            if (!_layers.ContainsKey(layerName))
                AddLayer(layerName);
        }


        #endregion

        #region 绘制逻辑

        /// <summary>
        /// 针对单个图层重绘其 Canvas
        /// </summary>
        private void RedrawLayer(Layer layer)
        {
            var canvas = layer.Canvas;
            canvas.Children.Clear();

            // 处理 DPI 缩放
            PresentationSource source = PresentationSource.FromVisual(this);
            double dpiX = 1.0, dpiY = 1.0;
            if (source?.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformFromDevice.M11;
                dpiY = source.CompositionTarget.TransformFromDevice.M22;
            }

            foreach (var rect in layer.Rects)
            {
                var shape = new Rectangle
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Width = rect.Width * dpiX,
                    Height = rect.Height * dpiY
                };
                Canvas.SetLeft(shape, rect.X * dpiX);
                Canvas.SetTop(shape, rect.Y * dpiY);
                canvas.Children.Add(shape);
            }
        }

        #endregion

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

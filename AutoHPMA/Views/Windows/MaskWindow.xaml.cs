using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using OpenCvSharp;    // 新增：引用 OpenCvSharp 命名空间

namespace AutoHPMA.Views.Windows
{
    public partial class MaskWindow : System.Windows.Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        public MaskWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 使窗口透明且点击穿透
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }

        /// <summary>
        /// 使用 OpenCvSharp.Rect 列表来绘制红色矩形
        /// </summary>
        public void UpdateRects(List<OpenCvSharp.Rect> cvRects)
        {
            OverlayCanvas.Children.Clear();

            // 获取当前 DPI 缩放比例
            PresentationSource source = PresentationSource.FromVisual(this);
            double dpiX = 1.0, dpiY = 1.0;
            if (source?.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformFromDevice.M11;
                dpiY = source.CompositionTarget.TransformFromDevice.M22;
            }

            foreach (var cvR in cvRects)
            {
                double left = cvR.X * dpiX;
                double top = cvR.Y * dpiY;
                double width = cvR.Width * dpiX;
                double height = cvR.Height * dpiY;

                var rect = new Rectangle
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Width = width,
                    Height = height
                };

                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);
                OverlayCanvas.Children.Add(rect);
            }
        }


        /// <summary>
        /// 同步遮罩窗口位置与大小，使其覆盖在目标窗口之上
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
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        #endregion
    }
}

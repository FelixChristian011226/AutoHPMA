using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.ImageHelper;
using AutoHPMA.Helpers.RecognizeHelper;
using AutoHPMA.Messages;
using AutoHPMA.Models;
using AutoHPMA.Views.Windows;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Vanara.PInvoke;
using Wpf.Ui.Controls;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class TestViewModel : ObservableObject
    {
        #region Observable Properties
        // 截屏测试
        [ObservableProperty]
        private CaptureMethod _selectedCaptureMethod = CaptureMethod.WGC;
        
        public ObservableCollection<CaptureMethod> CaptureMethods { get; }
            = new ObservableCollection<CaptureMethod>(
                  (CaptureMethod[])Enum.GetValues(typeof(CaptureMethod))
              );
        
        // 窗口选择
        [ObservableProperty]
        private WindowInfo? _selectedWindow;
        
        public ObservableCollection<WindowInfo> AvailableWindows { get; } = new ObservableCollection<WindowInfo>();

        // 鼠标模拟
        [ObservableProperty]
        private WindowInfo? _selectedClickWindow;
        
        public ObservableCollection<ClickActionModel> ClickActions { get; } = new ObservableCollection<ClickActionModel>();
        
        public ObservableCollection<DragActionModel> DragActions { get; } = new ObservableCollection<DragActionModel>();
        

        // 文字识别
        [ObservableProperty]
        private string _ocrResult = string.Empty;
        
        [ObservableProperty]
        private string _selectedOCR = "PaddleOCR";
        
        public ObservableCollection<string> OCRs { get; } = new ObservableCollection<string> { "PaddleOCR", "Tesseract" };

        //模板匹配
        [ObservableProperty]
        private string? _sourceImagePath;
        [ObservableProperty]
        private string? _templateImagePath;
        [ObservableProperty]
        private string? _maskImagePath;
        [ObservableProperty]
        private double _threshold = 0.8;

        public ObservableCollection<TemplateMatchModes> MatchModes { get; }
            = new ObservableCollection<TemplateMatchModes>(
                  (TemplateMatchModes[])Enum.GetValues(typeof(TemplateMatchModes))
              );

        [ObservableProperty]
        private TemplateMatchModes _selectedMatchMode = TemplateMatchModes.CCoeffNormed;

        [ObservableProperty]
        private System.Windows.Media.ImageSource? _resultImage;

        [ObservableProperty]
        private string _matchRectInfo = string.Empty;

        [ObservableProperty]
        private List<Rect> _matchRects = new();

        //轮廓检测
        [ObservableProperty] private int _minLen = 60;
        [ObservableProperty] private int _maxGap = 10;
        [ObservableProperty] private int _angleThresh = 1;

        [ObservableProperty] private int _minRadius = 0;
        [ObservableProperty] private int _maxRadius = 0;

        [ObservableProperty] private string? _detectImagePath;
        [ObservableProperty] private System.Windows.Media.ImageSource? _detectResultImage;

        // 色彩过滤
        [ObservableProperty] private string? _colorFilterSourcePath;
        [ObservableProperty] private string? _colorFilterMaskPath;
        [ObservableProperty] private string _targetColorHex = "ffffff";
        [ObservableProperty] private int _colorThreshold = 30;
        [ObservableProperty] private System.Windows.Media.ImageSource? _colorFilterResultImage;
        [ObservableProperty] private string _colorFilterStats = string.Empty;

        private System.Windows.Media.Color GetColorFromHex(string hex)
        {
            try
            {
                // 确保hex是6位
                hex = hex.PadRight(6, '0');
                // 添加FF作为Alpha通道
                hex = "FF" + hex;
                return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#" + hex);
            }
            catch
            {
                // 如果转换失败，返回红色
                return System.Windows.Media.Colors.Red;
            }
        }
        
        public TestViewModel()
        {
            RefreshWindowList();
            InitializeClickActions();
            InitializeDragActions();
        }
        
        private void InitializeClickActions()
        {
            ClickActions.Add(new ClickActionModel(200, 200, 500, 1, "默认点击"));
        }
        
        private void InitializeDragActions()
        {
            DragActions.Add(new DragActionModel(200, 200, 400, 400, 500, "默认拖拽"));
        }
        
        [RelayCommand]
        private void RefreshWindowList()
        {
            AvailableWindows.Clear();
            var windows = WindowHelper.GetAllVisibleWindows();
            foreach (var window in windows)
            {
                AvailableWindows.Add(window);
            }
        }

        #endregion

        #region 截屏测试
        [RelayCommand]
        public void OnScreenshotTest(object sender)
        {
            if (SelectedWindow == null)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "请先选择要截屏的窗口"
                };
                uiMessageBox.ShowDialogAsync();
                return;
            }

            var _gameHwnd = SelectedWindow.Handle;
            if (_gameHwnd != IntPtr.Zero)
            {
                try
                {
                    // 创建并显示实时预览窗口
                    var previewWindow = new ScreenshotPreviewWindow(_selectedCaptureMethod, _gameHwnd);
                    previewWindow.Show();
                }
                catch (Exception ex)
                {
                    var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "❎ 错误",
                        Content = "打开预览窗口失败：" + ex.Message,
                    };
                    _ = uiMessageBox.ShowDialogAsync();
                }
            }
            else
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "❎ 错误",
                    Content = "未找到目标窗口",
                };
                _ = uiMessageBox.ShowDialogAsync();
            }
        }
        #endregion

        #region 鼠标模拟
        [RelayCommand]
        public async void OnClickTest(object sender)
        {
            if (SelectedClickWindow == null)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "请先选择要点击的窗口"
                };
                await uiMessageBox.ShowDialogAsync();
                return;
            }

            var targetHwnd = SelectedClickWindow.Handle;
            if (targetHwnd == IntPtr.Zero)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "目标窗口句柄无效"
                };
                await uiMessageBox.ShowDialogAsync();
                return;
            }

            try
            {
                // 窗口置顶
                WindowInteractionHelper.SetForegroundWindow(targetHwnd);
                
                // 等待3秒延迟
                await Task.Delay(3000);
                
                // 执行表格中的所有点击动作
                foreach (var clickAction in ClickActions)
                {
                    for (int i = 0; i < clickAction.Times; i++)
                    {
                        WindowInteractionHelper.SendMouseClick(targetHwnd, (uint)clickAction.X, (uint)clickAction.Y);
                        await Task.Delay(clickAction.Interval);
                    }
                }
            }
            catch (Exception ex)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = $"点击测试失败：{ex.Message}"
                };
                await uiMessageBox.ShowDialogAsync();
            }
        }

        [RelayCommand]
        public async void OnDragTest(object sender)
        {
            if (SelectedClickWindow == null)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "请先选择要拖拽的窗口"
                };
                await uiMessageBox.ShowDialogAsync();
                return;
            }

            var targetHwnd = SelectedClickWindow.Handle;
            if (targetHwnd == IntPtr.Zero)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "目标窗口句柄无效"
                };
                await uiMessageBox.ShowDialogAsync();
                return;
            }

            try
            {
                // 窗口置顶
                WindowInteractionHelper.SetForegroundWindow(targetHwnd);
                
                // 等待3秒延迟
                await Task.Delay(3000);
                
                // 执行表格中的所有拖拽动作
                foreach (var dragAction in DragActions)
                {
                    WindowInteractionHelper.SendMouseDrag(
                        targetHwnd,
                        (uint)dragAction.StartX,
                        (uint)dragAction.StartY,
                        (uint)dragAction.EndX,
                        (uint)dragAction.EndY,
                        dragAction.Duration
                    );
                    
                    // 拖拽动作之间的间隔
                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = $"拖拽测试失败：{ex.Message}"
                };
                await uiMessageBox.ShowDialogAsync();
            }
        }
        
        [RelayCommand]
        public void AddClickAction()
        {
            ClickActions.Add(new ClickActionModel(200, 200, 500, 1, $"点击动作{ClickActions.Count + 1}"));
        }
        
        [RelayCommand]
        public void RemoveClickAction(ClickActionModel action)
        {
            if (action != null)
            {
                ClickActions.Remove(action);
            }
        }
        
        [RelayCommand]
        public void AddDragAction()
        {
            DragActions.Add(new DragActionModel(200, 200, 400, 400, 500, $"拖拽动作{DragActions.Count + 1}"));
        }
        
        [RelayCommand]
        public void RemoveDragAction(DragActionModel action)
        {
            if (action != null)
            {
                DragActions.Remove(action);
            }
        }
        #endregion

        #region 文字识别
        [RelayCommand]
        public async void OnOCRTest(object sender)
        {
            // 弹出文件选择对话框
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filePath = dlg.FileName;
                try
                {
                    // 加载选中的图片
                    Mat mat = Cv2.ImRead(filePath);
                    
                    // 根据选择的OCR引擎进行识别
                    string text = await Task.Run(() => OcrText(mat));
                    OcrResult = text;
                }
                catch (Exception ex)
                {
                    OcrResult = "识别出错：" + ex.Message;
                }
            }
        }
        
        private string OcrText(Mat mat)
        {
            if (SelectedOCR == "PaddleOCR")
            {
                var paddleOCRHelper = new PaddleOCRHelper();
                return paddleOCRHelper.Ocr(mat);
            }
            else if (SelectedOCR == "Tesseract")
            {
                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
                return TesseractOCRHelper.TesseractTextRecognition(TesseractOCRHelper.PreprocessImage(bitmap));
            }
            return string.Empty;
        }
        #endregion

        #region 模板匹配
        [RelayCommand]
        private void OnSelectSourceImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dlg.ShowDialog() == true)
            {
                SourceImagePath = dlg.FileName;
            }
        }

        [RelayCommand]
        private void OnSelectTemplateImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dlg.ShowDialog() == true)
            {
                TemplateImagePath = dlg.FileName;
            }
        }

        [RelayCommand]
        private void OnSelectMaskImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dlg.ShowDialog() == true)
            {
                MaskImagePath = dlg.FileName;
            }
        }

        [RelayCommand]
        private void OnTemplateMatch()
        {
            if (string.IsNullOrEmpty(SourceImagePath) || string.IsNullOrEmpty(TemplateImagePath))
                return;

            Mat originalMat = Cv2.ImRead(SourceImagePath);
            Mat detectMat = originalMat.Clone();
            Mat templateMat = Cv2.ImRead(TemplateImagePath, ImreadModes.Unchanged);
            Mat maskMat;
            if(!string.IsNullOrEmpty(MaskImagePath))
            {
                maskMat = Cv2.ImRead(MaskImagePath);
                Cv2.CvtColor(maskMat, maskMat, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                if (templateMat.Channels() == 4)
                {
                    maskMat = MatchTemplateHelper.GenerateMask(templateMat);
                    // 保存mask
                    string maskPath = Path.ChangeExtension(TemplateImagePath, null) + "_mask.png";
                    maskMat.SaveImage(maskPath);
                }
                else
                {
                    maskMat = null;
                }
            }
            templateMat = Cv2.ImRead(TemplateImagePath);

            var matches = MatchTemplateHelper.MatchOnePicForOnePic(
                detectMat,
                templateMat,
                SelectedMatchMode,
                maskMat,
                threshold: Threshold
            );

            MatchRects = matches;
            MatchRectInfo = string.Join("\n", matches.Select(r => $"X: {r.X}, Y: {r.Y}, Width: {r.Width}, Height: {r.Height}"));

            foreach (var rect in matches)
            {
                Cv2.Rectangle(
                    originalMat,
                    new Point(rect.X, rect.Y),
                    new Point(rect.X + rect.Width, rect.Y + rect.Height),
                    Scalar.Red,
                    thickness: 2
                );
            }

            var bitmapSource = BitmapSourceConverter.ToBitmapSource(originalMat);
            bitmapSource.Freeze();
            ResultImage = bitmapSource;
        }

        [RelayCommand]
        private void OnCropImage()
        {
            if (string.IsNullOrEmpty(SourceImagePath) || MatchRects.Count == 0)
                return;

            try
            {
                Mat originalMat = Cv2.ImRead(SourceImagePath);
                string directory = Path.GetDirectoryName(SourceImagePath)!;
                string fileName = Path.GetFileNameWithoutExtension(SourceImagePath);
                string extension = Path.GetExtension(SourceImagePath);

                for (int i = 0; i < MatchRects.Count; i++)
                {
                    var rect = MatchRects[i];
                    var croppedMat = new Mat(originalMat, rect);
                    string outputPath = Path.Combine(directory, $"{fileName}_cropped_{i + 1}{extension}");
                    croppedMat.SaveImage(outputPath);
                }
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "✅ 成功",
                    Content = "已成功裁切 " + MatchRects.Count + "个区域",
                };
                _ = uiMessageBox.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "❎ 错误",
                    Content = "裁切失败："+ex.Message,
                };
                _ = uiMessageBox.ShowDialogAsync();
            }
            return;
        }
        #endregion

        #region 轮廓检测
        [RelayCommand]
        private void SelectDetectImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg"
            };
            if (dlg.ShowDialog() == true)
                DetectImagePath = dlg.FileName;
        }

        [RelayCommand]
        private void DetectLines()
        {
            if (string.IsNullOrEmpty(DetectImagePath))
                return;
            Mat src = Cv2.ImRead(DetectImagePath, ImreadModes.Color);
            Mat src_binary = ContourDetectHelper.Binarize(src);
            Mat src_line  = ContourDetectHelper.DetectBlackWhiteBordersWithMorph(src_binary, minLen: MinLen, maxGap: MaxGap, angleThresh:AngleThresh);

            var bmp = BitmapSourceConverter.ToBitmapSource(src_line);
            bmp.Freeze();
            DetectResultImage = bmp;
        }

        [RelayCommand]
        private void DetectRectangle()
        {
            if (string.IsNullOrEmpty(DetectImagePath))
                return;

            Mat src = Cv2.ImRead(DetectImagePath, ImreadModes.Color);

            Mat src_binary = ContourDetectHelper.Binarize(src, 200);

            var rect = ContourDetectHelper.DetectApproxRectangle(src_binary);

            Cv2.Rectangle(src, rect, Scalar.Red, 2);

            //string resultPath = Path.Combine(
            //    Path.GetDirectoryName(DetectImagePath),
            //    "test_result.png"
            //);
            //Cv2.ImWrite(resultPath, output);


            var bmp = BitmapSourceConverter.ToBitmapSource(src);
            bmp.Freeze();
            DetectResultImage = bmp;
        }

        [RelayCommand]
        private void DetectCircles()
        {
            if (string.IsNullOrEmpty(DetectImagePath))
                return;

            Mat src = Cv2.ImRead(DetectImagePath, ImreadModes.Color);
            var circles = ContourDetectHelper.DetectCircles(src, MinRadius, MaxRadius);

            // 在原图上绘制检测到的圆
            foreach (var circle in circles)
            {
                Cv2.Circle(src, (int)circle.Center.X, (int)circle.Center.Y, (int)circle.Radius, Scalar.Green, 1);
                // 绘制圆心
                Cv2.Circle(src, (int)circle.Center.X, (int)circle.Center.Y, 2, Scalar.Red, 2);
            }

            var bmp = BitmapSourceConverter.ToBitmapSource(src);
            bmp.Freeze();
            DetectResultImage = bmp;
        }
        #endregion

        #region 色彩过滤
        [RelayCommand]
        private void SelectColorFilterSource()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dlg.ShowDialog() == true)
            {
                ColorFilterSourcePath = dlg.FileName;
            }
        }

        [RelayCommand]
        private void SelectColorFilterMask()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dlg.ShowDialog() == true)
            {
                ColorFilterMaskPath = dlg.FileName;
            }
        }

        [RelayCommand]
        private void StartColorFilter()
        {
            if (string.IsNullOrEmpty(ColorFilterSourcePath))
                return;

            try
            {
                // 读取源图像
                Mat sourceMat = Cv2.ImRead(ColorFilterSourcePath);
                if (sourceMat.Empty())
                    throw new Exception("无法读取源图像");

                // 读取遮罩图像（如果有）
                Mat? maskMat = null;
                if (!string.IsNullOrEmpty(ColorFilterMaskPath))
                {
                    maskMat = Cv2.ImRead(ColorFilterMaskPath);
                    if (maskMat.Empty())
                        throw new Exception("无法读取遮罩图像");
                }

                // 使用ColorFilterHelper进行颜色过滤
                var resultMat = ColorFilterHelper.FilterColor(
                    sourceMat,
                    maskMat,
                    TargetColorHex,
                    ColorThreshold
                );

                // 显示结果图像
                var bitmapSource = BitmapSourceConverter.ToBitmapSource(resultMat);
                bitmapSource.Freeze();
                ColorFilterResultImage = bitmapSource;

                // 获取统计结果
                int maskWhitePixels = resultMat.Get<int>(ColorFilterHelper.KEY_MASK_WHITE_PIXELS);
                int filteredPixels = resultMat.Get<int>(ColorFilterHelper.KEY_FILTERED_PIXELS);
                double percentage = maskWhitePixels > 0 ? (double)filteredPixels / maskWhitePixels * 100 : 0;

                // 更新统计信息
                ColorFilterStats = $"遮罩白色像素数: {maskWhitePixels}\n" +
                                 $"过滤后像素数: {filteredPixels}\n" +
                                 $"占比: {percentage:F2}%";

                // 显示调试信息
                var targetColor = ColorFilterHelper.GetColorFromHex(TargetColorHex);
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "调试信息",
                    Content = $"目标颜色: {TargetColorHex}\n" +
                             $"RGB值: R={targetColor.R}, G={targetColor.G}, B={targetColor.B}\n" +
                             $"阈值: ±{ColorThreshold}\n\n" +
                             ColorFilterStats,
                };
                _ = uiMessageBox.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "❎ 错误",
                    Content = "色彩过滤失败：" + ex.Message,
                };
                _ = uiMessageBox.ShowDialogAsync();
            }
        }
        #endregion
    }
}

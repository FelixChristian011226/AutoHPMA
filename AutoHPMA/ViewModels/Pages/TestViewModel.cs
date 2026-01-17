using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.ImageHelper;
using AutoHPMA.Helpers.RecognizeHelper;
using AutoHPMA.Models;
using AutoHPMA.Views.Windows;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public ObservableCollection<CaptureMethod> CaptureMethods { get; } =
            new((CaptureMethod[])Enum.GetValues(typeof(CaptureMethod)));

        // 窗口选择
        [ObservableProperty]
        private WindowInfo? _selectedWindow;

        public ObservableCollection<WindowInfo> AvailableWindows { get; } = new();

        // 鼠标模拟
        [ObservableProperty]
        private WindowInfo? _selectedClickWindow;

        [ObservableProperty]
        private WindowInfo? _selectedClickChildWindow;

        public ObservableCollection<WindowInfo> AvailableChildWindows { get; } = new();

        [ObservableProperty]
        private bool _hasChildWindows = false;

        public ObservableCollection<ClickActionModel> ClickActions { get; } = new();
        public ObservableCollection<DragActionModel> DragActions { get; } = new();
        public ObservableCollection<LongPressActionModel> LongPressActions { get; } = new();

        // 文字识别
        [ObservableProperty]
        private string _ocrResult = string.Empty;

        [ObservableProperty]
        private string _selectedOCR = "PaddleOCR";

        public ObservableCollection<string> OCRs { get; } = new() { "PaddleOCR", "Tesseract" };

        // 模板匹配
        [ObservableProperty]
        private string? _sourceImagePath;

        [ObservableProperty]
        private string? _templateImagePath;

        [ObservableProperty]
        private string? _maskImagePath;

        [ObservableProperty]
        private double _threshold = 0.8;

        public ObservableCollection<TemplateMatchModes> MatchModes { get; } =
            new((TemplateMatchModes[])Enum.GetValues(typeof(TemplateMatchModes)));

        [ObservableProperty]
        private TemplateMatchModes _selectedMatchMode = TemplateMatchModes.CCoeffNormed;

        [ObservableProperty]
        private System.Windows.Media.ImageSource? _resultImage;

        [ObservableProperty]
        private string _matchRectInfo = string.Empty;

        [ObservableProperty]
        private List<Rect> _matchRects = new();

        // 轮廓检测
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

        #endregion

        #region 构造函数

        public TestViewModel()
        {
            RefreshWindowList();
            ClickActions.Add(new ClickActionModel(200, 200, 500, 1, "默认点击"));
            DragActions.Add(new DragActionModel(200, 200, 400, 400, 500, "默认拖拽"));
            LongPressActions.Add(new LongPressActionModel(200, 200, 1000, 500, 1, "默认长按"));
        }

        #endregion

        #region 通用辅助方法

        /// <summary>
        /// 显示错误消息框
        /// </summary>
        private static void ShowError(string content)
        {
            var box = new Wpf.Ui.Controls.MessageBox { Title = "❎ 错误", Content = content };
            _ = box.ShowDialogAsync();
        }

        /// <summary>
        /// 显示成功消息框
        /// </summary>
        private static void ShowSuccess(string content)
        {
            var box = new Wpf.Ui.Controls.MessageBox { Title = "✅ 成功", Content = content };
            _ = box.ShowDialogAsync();
        }

        /// <summary>
        /// 打开文件选择对话框
        /// </summary>
        private static string? SelectImageFile(string filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg")
        {
            var dlg = new OpenFileDialog { Filter = filter };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        /// <summary>
        /// 验证窗口是否已选择
        /// </summary>
        private bool ValidateWindow(WindowInfo? window, string action)
        {
            if (window == null)
            {
                ShowError($"请先选择要{action}的窗口");
                return false;
            }
            if (window.Handle == IntPtr.Zero)
            {
                ShowError("目标窗口句柄无效");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 将 Mat 转换为 ImageSource
        /// </summary>
        private static System.Windows.Media.ImageSource ToImageSource(Mat mat)
        {
            var bmp = BitmapSourceConverter.ToBitmapSource(mat);
            bmp.Freeze();
            return bmp;
        }

        #endregion

        #region 窗口管理

        [RelayCommand]
        private void RefreshWindowList()
        {
            AvailableWindows.Clear();
            AvailableChildWindows.Clear();
            SelectedClickWindow = null;
            SelectedClickChildWindow = null;
            HasChildWindows = false;

            // 只获取父窗口列表
            var windows = WindowHelper.GetAllVisibleWindows();
            foreach (var window in windows)
                AvailableWindows.Add(window);
        }

        /// <summary>
        /// 刷新子窗口列表（基于选中的父窗口）
        /// </summary>
        private void RefreshChildWindowList()
        {
            AvailableChildWindows.Clear();
            SelectedClickChildWindow = null;
            HasChildWindows = false;

            if (SelectedClickWindow == null)
                return;

            var childWindows = WindowHelper.GetChildWindows(
                SelectedClickWindow.Handle,
                SelectedClickWindow.ProcessName,
                SelectedClickWindow.ProcessId);

            if (childWindows.Count > 0)
            {
                HasChildWindows = true;
                foreach (var child in childWindows)
                    AvailableChildWindows.Add(child);
            }
        }

        partial void OnSelectedClickWindowChanged(WindowInfo? value) => RefreshChildWindowList();

        /// <summary>
        /// 获取实际用于交互的窗口句柄（优先使用子窗口）
        /// </summary>
        private IntPtr GetEffectiveClickWindowHandle()
        {
            if (SelectedClickChildWindow != null)
                return SelectedClickChildWindow.Handle;
            return SelectedClickWindow?.Handle ?? IntPtr.Zero;
        }

        #endregion

        #region 截屏测试

        [RelayCommand]
        public void OnScreenshotTest(object sender)
        {
            if (!ValidateWindow(SelectedWindow, "截屏")) return;

            try
            {
                var previewWindow = new ScreenshotPreviewWindow(_selectedCaptureMethod, SelectedWindow!.Handle);
                previewWindow.Show();
            }
            catch (Exception ex)
            {
                ShowError("打开预览窗口失败：" + ex.Message);
            }
        }

        #endregion

        #region 鼠标模拟

        /// <summary>
        /// 通用动作测试方法
        /// </summary>
        private async Task ExecuteActionTest<T>(
            IEnumerable<T> actions,
            string actionName,
            Func<T, IntPtr, Task> executeAction)
        {
            if (!ValidateWindow(SelectedClickWindow, actionName)) return;

            // 使用实际交互窗口（优先子窗口）
            var targetHwnd = GetEffectiveClickWindowHandle();
            if (targetHwnd == IntPtr.Zero)
            {
                ShowError("目标窗口句柄无效");
                return;
            }

            try
            {
                // 先将父窗口置于前台
                WindowInteractionHelper.SetForegroundWindow(SelectedClickWindow!.Handle);
                await Task.Delay(3000);

                foreach (var action in actions)
                {
                    await executeAction(action, targetHwnd);
                }
            }
            catch (Exception ex)
            {
                ShowError($"{actionName}测试失败：{ex.Message}");
            }
        }

        [RelayCommand]
        public async void OnClickTest(object sender)
        {
            await ExecuteActionTest(ClickActions, "点击", async (action, hwnd) =>
            {
                for (int i = 0; i < action.Times; i++)
                {
                    await WindowInteractionHelper.SendMouseClickAsync(hwnd, (uint)action.X, (uint)action.Y);
                    await Task.Delay(action.Interval);
                }
            });
        }

        [RelayCommand]
        public async void OnDragTest(object sender)
        {
            await ExecuteActionTest(DragActions, "拖拽", async (action, hwnd) =>
            {
                await WindowInteractionHelper.SendMouseDragWithNoiseAsync(
                    hwnd,
                    (uint)action.StartX, (uint)action.StartY,
                    (uint)action.EndX, (uint)action.EndY,
                    action.Duration);
                await Task.Delay(500);
            });
        }

        [RelayCommand]
        public async void OnLongPressTest(object sender)
        {
            await ExecuteActionTest(LongPressActions, "长按", async (action, hwnd) =>
            {
                for (int i = 0; i < action.Times; i++)
                {
                    await WindowInteractionHelper.SendMouseLongPressAsync(hwnd, (uint)action.X, (uint)action.Y, action.Duration);
                    await Task.Delay(action.Interval);
                }
            });
        }

        [RelayCommand]
        public void AddClickAction() =>
            ClickActions.Add(new ClickActionModel(200, 200, 500, 1, $"点击动作{ClickActions.Count + 1}"));

        [RelayCommand]
        public void RemoveClickAction(ClickActionModel action) => ClickActions.Remove(action);

        [RelayCommand]
        public void AddDragAction() =>
            DragActions.Add(new DragActionModel(200, 200, 400, 400, 500, $"拖拽动作{DragActions.Count + 1}"));

        [RelayCommand]
        public void RemoveDragAction(DragActionModel action) => DragActions.Remove(action);

        [RelayCommand]
        public void AddLongPressAction() =>
            LongPressActions.Add(new LongPressActionModel(200, 200, 1000, 500, 1, $"长按动作{LongPressActions.Count + 1}"));

        [RelayCommand]
        public void RemoveLongPressAction(LongPressActionModel action) => LongPressActions.Remove(action);

        #endregion

        #region 文字识别

        [RelayCommand]
        public async void OnOCRTest(object sender)
        {
            var filePath = SelectImageFile();
            if (filePath == null) return;

            try
            {
                Mat mat = Cv2.ImRead(filePath);
                OcrResult = await Task.Run(() => OcrText(mat));
            }
            catch (Exception ex)
            {
                OcrResult = "识别出错：" + ex.Message;
            }
        }

        private string OcrText(Mat mat)
        {
            return SelectedOCR switch
            {
                "PaddleOCR" => new PaddleOCRHelper().Ocr(mat),
                "Tesseract" => TesseractOCRHelper.TesseractTextRecognition(
                    TesseractOCRHelper.PreprocessImage(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat))),
                _ => string.Empty
            };
        }

        #endregion

        #region 模板匹配

        [RelayCommand]
        private void OnSelectSourceImage() => SourceImagePath = SelectImageFile() ?? SourceImagePath;

        [RelayCommand]
        private void OnSelectTemplateImage() => TemplateImagePath = SelectImageFile() ?? TemplateImagePath;

        [RelayCommand]
        private void OnSelectMaskImage() => MaskImagePath = SelectImageFile() ?? MaskImagePath;

        [RelayCommand]
        private void OnTemplateMatch()
        {
            if (string.IsNullOrEmpty(SourceImagePath) || string.IsNullOrEmpty(TemplateImagePath))
                return;

            Mat originalMat = Cv2.ImRead(SourceImagePath);
            Mat detectMat = originalMat.Clone();
            Mat templateMat = Cv2.ImRead(TemplateImagePath, ImreadModes.Unchanged);

            Mat? maskMat = GetMaskMat(templateMat);
            templateMat = Cv2.ImRead(TemplateImagePath);

            var matches = MatchTemplateHelper.MatchOnePicForOnePic(
                detectMat, templateMat, SelectedMatchMode, maskMat, threshold: Threshold);

            MatchRects = matches;
            MatchRectInfo = string.Join("\n", matches.Select(r => $"X: {r.X}, Y: {r.Y}, Width: {r.Width}, Height: {r.Height}"));

            foreach (var rect in matches)
            {
                Cv2.Rectangle(originalMat, new Point(rect.X, rect.Y),
                    new Point(rect.X + rect.Width, rect.Y + rect.Height), Scalar.Red, thickness: 2);
            }

            ResultImage = ToImageSource(originalMat);
        }

        private Mat? GetMaskMat(Mat templateMat)
        {
            if (!string.IsNullOrEmpty(MaskImagePath))
            {
                var mask = Cv2.ImRead(MaskImagePath);
                Cv2.CvtColor(mask, mask, ColorConversionCodes.BGR2GRAY);
                return mask;
            }

            if (templateMat.Channels() == 4)
            {
                var mask = MatchTemplateHelper.GenerateMask(templateMat);
                string maskPath = Path.ChangeExtension(TemplateImagePath, null) + "_mask.png";
                mask.SaveImage(maskPath);
                return mask;
            }

            return null;
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
                    var croppedMat = new Mat(originalMat, MatchRects[i]);
                    string outputPath = Path.Combine(directory, $"{fileName}_cropped_{i + 1}{extension}");
                    croppedMat.SaveImage(outputPath);
                }

                ShowSuccess($"已成功裁切 {MatchRects.Count} 个区域");
            }
            catch (Exception ex)
            {
                ShowError("裁切失败：" + ex.Message);
            }
        }

        #endregion

        #region 轮廓检测

        [RelayCommand]
        private void SelectDetectImage() => DetectImagePath = SelectImageFile("Image Files (*.png;*.jpg)|*.png;*.jpg") ?? DetectImagePath;

        [RelayCommand]
        private void DetectLines()
        {
            if (string.IsNullOrEmpty(DetectImagePath)) return;

            Mat src = Cv2.ImRead(DetectImagePath, ImreadModes.Color);
            Mat src_binary = ContourDetectHelper.Binarize(src);
            Mat src_line = ContourDetectHelper.DetectBlackWhiteBordersWithMorph(src_binary, minLen: MinLen, maxGap: MaxGap, angleThresh: AngleThresh);
            DetectResultImage = ToImageSource(src_line);
        }

        [RelayCommand]
        private void DetectRectangle()
        {
            if (string.IsNullOrEmpty(DetectImagePath)) return;

            Mat src = Cv2.ImRead(DetectImagePath, ImreadModes.Color);
            Mat src_binary = ContourDetectHelper.Binarize(src, 200);
            var rect = ContourDetectHelper.DetectApproxRectangle(src_binary);
            Cv2.Rectangle(src, rect, Scalar.Red, 2);
            DetectResultImage = ToImageSource(src);
        }

        [RelayCommand]
        private void DetectCircles()
        {
            if (string.IsNullOrEmpty(DetectImagePath)) return;

            Mat src = Cv2.ImRead(DetectImagePath, ImreadModes.Color);
            var circles = ContourDetectHelper.DetectCircles(src, MinRadius, MaxRadius);

            foreach (var circle in circles)
            {
                Cv2.Circle(src, (int)circle.Center.X, (int)circle.Center.Y, (int)circle.Radius, Scalar.Green, 1);
                Cv2.Circle(src, (int)circle.Center.X, (int)circle.Center.Y, 2, Scalar.Red, 2);
            }

            DetectResultImage = ToImageSource(src);
        }

        #endregion

        #region 色彩过滤

        [RelayCommand]
        private void SelectColorFilterSource() => ColorFilterSourcePath = SelectImageFile() ?? ColorFilterSourcePath;

        [RelayCommand]
        private void SelectColorFilterMask() => ColorFilterMaskPath = SelectImageFile() ?? ColorFilterMaskPath;

        [RelayCommand]
        private void StartColorFilter()
        {
            if (string.IsNullOrEmpty(ColorFilterSourcePath)) return;

            try
            {
                Mat sourceMat = Cv2.ImRead(ColorFilterSourcePath);
                if (sourceMat.Empty())
                    throw new Exception("无法读取源图像");

                Mat? maskMat = null;
                if (!string.IsNullOrEmpty(ColorFilterMaskPath))
                {
                    maskMat = Cv2.ImRead(ColorFilterMaskPath);
                    if (maskMat.Empty())
                        throw new Exception("无法读取遮罩图像");
                }

                var resultMat = ColorFilterHelper.FilterColor(sourceMat, maskMat, TargetColorHex, ColorThreshold);
                ColorFilterResultImage = ToImageSource(resultMat);

                int maskWhitePixels = resultMat.Get<int>(ColorFilterHelper.KEY_MASK_WHITE_PIXELS);
                int filteredPixels = resultMat.Get<int>(ColorFilterHelper.KEY_FILTERED_PIXELS);
                double percentage = maskWhitePixels > 0 ? (double)filteredPixels / maskWhitePixels * 100 : 0;

                ColorFilterStats = $"遮罩白色像素数: {maskWhitePixels}\n过滤后像素数: {filteredPixels}\n占比: {percentage:F2}%";

                var targetColor = ColorFilterHelper.GetColorFromHex(TargetColorHex);
                var box = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "调试信息",
                    Content = $"目标颜色: {TargetColorHex}\nRGB值: R={targetColor.R}, G={targetColor.G}, B={targetColor.B}\n阈值: ±{ColorThreshold}\n\n{ColorFilterStats}",
                };
                _ = box.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                ShowError("色彩过滤失败：" + ex.Message);
            }
        }

        #endregion
    }
}

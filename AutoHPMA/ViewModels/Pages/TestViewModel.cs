using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.RecognizeHelper;
using AutoHPMA.Views.Windows;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
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
using OpenCvSharp.WpfExtensions;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class TestViewModel : ObservableObject
    {

        #region Observable Properties
        // 截屏测试
        [ObservableProperty]
        private int _screenshotLeft = 0;
        [ObservableProperty]
        private int _screenshotTop = 0;
        [ObservableProperty]
        private int _screenshotWidth = 0;
        [ObservableProperty]
        private int _screenshotHeight = 0;
        [ObservableProperty]
        private string _screenshotFilename = "CaptureTest";

        // 模拟点击
        [ObservableProperty]
        private int _clickLeft = 200;
        [ObservableProperty]
        private int _clickTop = 200;
        [ObservableProperty]
        private int _clickInterval = 500;
        [ObservableProperty]
        private int _clickTimes = 10;

        [ObservableProperty]
        private int _dragStartX = 200;
        [ObservableProperty]
        private int _dragStartY = 200;
        [ObservableProperty]
        private int _dragEndX = 400;
        [ObservableProperty]
        private int _dragEndY = 400;
        [ObservableProperty]
        private int _dragDuration = 500;

        // 文字识别
        [ObservableProperty]
        private string _ocrResult = string.Empty;

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

        //轮廓检测
        [ObservableProperty] private int _minLen = 60;
        [ObservableProperty] private int _maxGap = 10;
        [ObservableProperty] private int _angleThresh = 1;

        [ObservableProperty] private int _minRadius = 10;
        [ObservableProperty] private int _maxRadius = 50;

        [ObservableProperty] private string? _detectImagePath;
        [ObservableProperty] private System.Windows.Media.ImageSource? _detectResultImage;

        #endregion

        [RelayCommand]
        public async void OnScreenshotTest(object sender)
        {
            var _gameHwnd = WindowHelper.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
            if (_gameHwnd != IntPtr.Zero)
            {
                // 对子窗口截图会被屏蔽，只能截取父窗口
                //_gameHwnd = SystemControl.FindChildWindowByTitle(_gameHwnd, "MuMuPlayer");
            }
            else
            {
                _gameHwnd = WindowHelper.FindHandleByProcessName("哈利波特：魔法觉醒", "Harry Potter Magic Awakened");
            }

            if (_gameHwnd != IntPtr.Zero)
            {
                //Bitmap bmp = ScreenCaptureHelper.CaptureWindow(_gameHwnd);
                //Bitmap bmp = BitBltCaptureHelper.Capture(_gameHwnd);

                var capture = new WindowsGraphicsCapture();
                capture.Start(_gameHwnd);
                await Task.Delay(100);
                Mat frame = capture.Capture();
                capture.Stop();
                Bitmap bmp = frame.ToBitmap();

                string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                Directory.CreateDirectory(folderPath);
                Bitmap croppedBmp;
                if (_screenshotWidth == 0 || _screenshotHeight == 0)
                    croppedBmp = bmp;
                else
                    croppedBmp = ImageProcessingHelper.CropBitmap(bmp, _screenshotLeft, _screenshotTop, _screenshotWidth, _screenshotHeight);
                ImageProcessingHelper.SaveBitmapAs(croppedBmp, folderPath, _screenshotFilename + ".png", ImageFormat.Png);
            }
        }



        [RelayCommand]
        public async void OnClickTest(object sender)
        {
            IntPtr hWnd = WindowHelper.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
            IntPtr hWndChild = WindowHelper.FindChildWindowByTitle(hWnd, "MuMuPlayer");
            //IntPtr hWndChild = SystemControl.FindHandleByProcessName("哈利波特：魔法觉醒", "Harry Potter Magic Awakened");
            //IntPtr hWndChild = new IntPtr(Convert.ToInt32("004D078E", 16));
            //WindowInteractionHelper.SetForegroundWindow(hWndChild);

            if (hWndChild != IntPtr.Zero)
            {
                for (int i = 0; i < _clickTimes; i++)
                {
                    WindowInteractionHelper.SendMouseClick(hWndChild, (uint)_clickLeft, (uint)_clickTop);
                    Thread.Sleep(_clickInterval);
                }
            }
        }

        [RelayCommand]
        public async void OnDragTest(object sender)
        {
            IntPtr hWnd = WindowHelper.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
            IntPtr hWndChild = WindowHelper.FindChildWindowByTitle(hWnd, "MuMuPlayer");

            if (hWndChild != IntPtr.Zero)
            {
                WindowInteractionHelper.SendMouseDrag(
                    hWndChild,
                    (uint)_dragStartX,
                    (uint)_dragStartY,
                    (uint)_dragEndX,
                    (uint)_dragEndY,
                    _dragDuration
                );
            }
        }

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
                    //Bitmap bitmap = new Bitmap(filePath);
                    // 可选择在后台线程中执行识别，防止阻塞 UI
                    //string text = await Task.Run(() => PaddleOCRHelper.TextRecognition(bitmap));
                    //string text = await Task.Run(() => TesseractOCRHelper.TesseractTextRecognition(TesseractOCRHelper.PreprocessImage(bitmap)));
                    //OcrResult = text;

                    // 加载选中的图片
                    Mat mat = Cv2.ImRead(filePath);
                    // 进行 OCR 识别
                    PaddleOCRHelper paddleOCRHelper = new PaddleOCRHelper();
                    string text = await Task.Run(() => paddleOCRHelper.Ocr(mat));
                    OcrResult = text;

                }
                catch (Exception ex)
                {
                    OcrResult = "识别出错：" + ex.Message;
                }
            }
        }

        // 模板匹配
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

        //轮廓检测

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
                Cv2.Circle(src, (int)circle.Center.X, (int)circle.Center.Y, (int)circle.Radius, Scalar.Green, 2);
                // 绘制圆心
                Cv2.Circle(src, (int)circle.Center.X, (int)circle.Center.Y, 2, Scalar.Red, 3);
            }

            var bmp = BitmapSourceConverter.ToBitmapSource(src);
            bmp.Freeze();
            DetectResultImage = bmp;
        }
 
    }
}

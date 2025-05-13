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
        #endregion

        [RelayCommand]
        public async void OnScreenshotTest(object sender)
        {
            var _gameHwnd = SystemControl.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
            if (_gameHwnd != IntPtr.Zero)
            {
                // 对子窗口截图会被屏蔽，只能截取父窗口
                //_gameHwnd = SystemControl.FindChildWindowByTitle(_gameHwnd, "MuMuPlayer");
            }
            else
            {
                _gameHwnd = SystemControl.FindHandleByProcessName("哈利波特：魔法觉醒", "Harry Potter Magic Awakened");
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
            IntPtr hWnd = SystemControl.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
            IntPtr hWndChild = SystemControl.FindChildWindowByTitle(hWnd, "MuMuPlayer");
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
            Cv2.CvtColor(detectMat, detectMat, ColorConversionCodes.BGR2GRAY);
            Mat templateMat = Cv2.ImRead(TemplateImagePath);
            Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGR2GRAY);
            Mat maskMat;
            if(!string.IsNullOrEmpty(MaskImagePath))
            {
                maskMat = Cv2.ImRead(MaskImagePath);
                Cv2.CvtColor(maskMat, maskMat, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                maskMat = null;
            }

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


    }
}

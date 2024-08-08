using AutoHPMA.GameTask;
using AutoHPMA.Helpers;
using AutoHPMA.Views.Windows;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class TestViewModel : ObservableObject
    {
        // 截屏测试
        [ObservableProperty]
        private int _screenshotLeft = 0;
        [ObservableProperty]
        private int _screenshotTop = 0;
        [ObservableProperty]
        private int _screenshotWidth = 2582;
        [ObservableProperty]
        private int _screenshotHeight = 1550;
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


        [RelayCommand]
        public async void OnScreenshotTest(object sender)
        {
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle(); // 获取Mumu模拟器窗口句柄
            if (mumuHwnd != IntPtr.Zero)
            {
                // 截取窗口图像
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                // 确保目标文件夹存在
                string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                Directory.CreateDirectory(folderPath);
                // 截取指定区域图像
                Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, _screenshotLeft, _screenshotTop, _screenshotWidth, _screenshotHeight);
                // 保存图像文件
                ImageProcessingHelper.SaveBitmapAs(croppedBmp, folderPath, _screenshotFilename + ".png", ImageFormat.Png);

                //Scalar similarity = ImageProcessingHelper.Compare_SSIM("D:\\Learning\\VisualStudio\\source\\repo\\AutoHPMA\\AutoHPMA\\bin\\Debug\\net8.0-windows\\Captures\\CaptureTest1.bmp", "D:\\Learning\\VisualStudio\\source\\repo\\AutoHPMA\\AutoHPMA\\bin\\Debug\\net8.0-windows\\Captures\\CaptureTest2.bmp");
                //MessageBox.Show($"相似度：{similarity}");


            }
        }

        [RelayCommand]
        public async void OnCaptureGather(object sender)
        {
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle();
            if (mumuHwnd != IntPtr.Zero)
            {
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                Directory.CreateDirectory(folderPath);
                Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1505, 1388, 1598 - 1505, 1474 - 1388);
                ImageProcessingHelper.SaveBitmapAs(croppedBmp, folderPath, "[Test]gather" + ".png", ImageFormat.Png);
            }
        }

        [RelayCommand]
        public async void OnCaptureTip(object sender)
        {
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle();
            if (mumuHwnd != IntPtr.Zero)
            {
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                Directory.CreateDirectory(folderPath);
                Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1951, 373, 2042 - 1951, 442 - 373);
                ImageProcessingHelper.SaveBitmapAs(croppedBmp, folderPath, "[Test]tip" + ".png", ImageFormat.Png);
            }
        }

        [RelayCommand]
        public async void OnCaptureTime(object sender)
        {
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle();
            if (mumuHwnd != IntPtr.Zero)
            {
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                Directory.CreateDirectory(folderPath);
                Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1227, 125, 1328 - 1227, 187 - 125);
                ImageProcessingHelper.SaveBitmapAs(croppedBmp, folderPath, "[Test]time" + ".png", ImageFormat.Png);
            }
        }

        [RelayCommand]
        public async void OnCompareGather(object sender)
        {
            Bitmap gather = new Bitmap("Assets/Captures/gather.png");
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle();
            if (mumuHwnd != IntPtr.Zero)
            {
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1505, 1388, 1598 - 1505, 1474 - 1388);

                double similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(gather, croppedBmp));
                LogWindow _logWindow = LogWindow.GetInstance();
                _logWindow.AddLogMessage("INF","相似度：" + similarity);
            }
        }

        [RelayCommand]
        public async void OnCompareTip(object sender)
        {
            Bitmap tip = new Bitmap("Assets/Captures/tip.png");
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle();
            if (mumuHwnd != IntPtr.Zero)
            {
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1951, 373, 2042 - 1951, 442 - 373);

                double similarity = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(tip, croppedBmp));
                LogWindow _logWindow = LogWindow.GetInstance();
                _logWindow.AddLogMessage("INF", "相似度：" + similarity);
            }
        }

        [RelayCommand]
        public async void OnCompareTime(object sender)
        {
            Bitmap Time6 = new Bitmap("Assets/Captures/Time/6.png");
            Bitmap Time10 = new Bitmap("Assets/Captures/Time/10.png");
            Bitmap Time15 = new Bitmap("Assets/Captures/Time/15.png");
            Bitmap Time16 = new Bitmap("Assets/Captures/Time/16.png");
            Bitmap Time17 = new Bitmap("Assets/Captures/Time/17.png");
            Bitmap Time18 = new Bitmap("Assets/Captures/Time/18.png");
            Bitmap Time20 = new Bitmap("Assets/Captures/Time/20.png");
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle();
            if (mumuHwnd != IntPtr.Zero)
            {
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                Bitmap croppedBmp = ImageProcessingHelper.CropBitmap(bmp, 1227, 125, 1328 - 1227, 187 - 125);

                double[] similarity = new double[7];
                similarity[0]  = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(Time6, croppedBmp));
                similarity[1] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(Time10, croppedBmp));
                similarity[2] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(Time15, croppedBmp));
                similarity[3] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(Time16, croppedBmp));
                similarity[4] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(Time17, croppedBmp));
                similarity[5] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(Time18, croppedBmp));
                similarity[6] = ImageProcessingHelper.AverageScalarValue(ImageProcessingHelper.Compare_SSIM(Time20, croppedBmp));
                String[] strings = new String[7] { "6", "10", "15", "16", "17", "18", "20" };

                LogWindow _logWindow = LogWindow.GetInstance();

                for(int i=0; i<7; i++)
                {
                    if (similarity[i] > 0.9)
                    {
                        _logWindow.AddLogMessage("INF", "相似度：" + similarity[i].ToString("N4") + "  时间：" + strings[i]);
                    }
                }
                //_logWindow.AddLogMessage("INF", "相似度：" + similarity[0]);
            }
        }

        [RelayCommand]
        public async void OnClickTest(object sender)
        {
            IntPtr hWnd = SystemControl.FindMumuSimulatorHandle();
            //IntPtr hWndChild = SystemControl.FindChildWindowByTitle(hWnd, "MuMuPlayer");
            IntPtr hWndChild = SystemControl.FindHandleByProcessName("哈利波特：魔法觉醒", "Harry Potter Magic Awakened");
            //IntPtr hWndChild = new IntPtr(Convert.ToInt32("004D078E", 16));
            WindowInteractionHelper.SetForegroundWindow(hWndChild);

            if (hWndChild != IntPtr.Zero)
            {
                for (int i = 0; i < _clickTimes; i++)
                {
                    WindowInteractionHelper.SendMouseClick(hWndChild, (uint)_clickLeft, (uint)_clickTop);
                    Thread.Sleep(_clickInterval);
                }
            }
        }


    }
}

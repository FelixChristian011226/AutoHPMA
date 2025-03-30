using AutoHPMA.GameTask;
using AutoHPMA.Helpers;
using AutoHPMA.Views.Windows;
using Fischless.GameCapture.Graphics;
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


        [RelayCommand]
        public async void OnScreenshotTest(object sender)
        {
            var _gameHwnd = SystemControl.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
            if (_gameHwnd != IntPtr.Zero)
            {
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

                var capture = new GraphicsCapture();
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


    }
}

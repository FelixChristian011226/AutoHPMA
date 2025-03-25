using AutoHPMA.GameTask;
using AutoHPMA.Helpers;
using AutoHPMA.Views.Windows;
using OpenCvSharp;
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

namespace AutoHPMA.ViewModels.Pages
{
    public partial class TestViewModel : ObservableObject
    {
        // 启动方式
        public ObservableCollection<StartupOptionItem> StartupOptions { get; } = new ObservableCollection<StartupOptionItem>
        {
            new StartupOptionItem { Option = StartupOption.MumuSimulator, DisplayName = "Mumu模拟器" },
            new StartupOptionItem { Option = StartupOption.OfficialLauncher, DisplayName = "官方启动器" }
        };

        [ObservableProperty]
        private StartupOptionItem _selectedStartupOption;

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
            IntPtr _targetHwnd = IntPtr.Zero;
            if (SelectedStartupOption.Option == StartupOption.MumuSimulator)
                _targetHwnd = SystemControl.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
            else
                _targetHwnd = SystemControl.FindHandleByProcessName("哈利波特：魔法觉醒", "Harry Potter Magic Awakened");

            if (_targetHwnd != IntPtr.Zero)
            {
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(_targetHwnd);
                //Bitmap bmp = BitBltCaptureHelper.Capture(_targetHwnd);
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
            IntPtr hWnd = SystemControl.FindMumuSimulatorHandle();
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

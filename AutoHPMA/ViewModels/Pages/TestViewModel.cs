using AutoHPMA.GameTask;
using AutoHPMA.Helpers;
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
        [ObservableProperty]
        private int _screenshotLeft = 0;
        [ObservableProperty]
        private int _screenshotTop = 0;
        [ObservableProperty]
        private int _screenshotWidth = 500;
        [ObservableProperty]
        private int _screenshotHeight = 500;
        [ObservableProperty]
        private string _screenshotFilename = "CaptureTest";

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

    }
}

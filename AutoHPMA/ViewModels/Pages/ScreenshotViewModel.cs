using AutoHPMA.Models;
using AutoHPMA.Views.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Abstractions.Controls;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Services;
using OpenCvSharp.Extensions;

namespace AutoHPMA.ViewModels.Pages
{
    public class ScreenshotViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        private ImageSource _imageSource;
        public ImageSource ImageSource
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        public ScreenshotViewModel()
        {
            
        }

        public static void TakeScreenshot()
        {
            var gameHwnd = AppContextService.Instance.GameHwnd;
            if (gameHwnd == IntPtr.Zero) return;

            var capture = new WindowsGraphicsCapture();
            capture.Start(gameHwnd);
            Task.Delay(100).Wait();
            var frame = capture.Capture();
            capture.Stop();
            var bmp = frame.ToBitmap();

            string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
            Directory.CreateDirectory(folderPath);
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"screenshot_{timestamp}.png";
            string fullPath = Path.Combine(folderPath, filename);
            
            bmp.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
            bmp.Dispose();
        }

        private void BitmapUpdated(Bitmap bmp)
        {
            using (var memory = new MemoryStream())
            {
                bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                // 必须在UI线程上更新
                Application.Current.Dispatcher.Invoke(() => ImageSource = bitmapImage);
            }
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();
            DashboardViewModel.ScreenshotUpdated += BitmapUpdated;

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() 
        {
            DashboardViewModel.ScreenshotUpdated -= BitmapUpdated;

            return Task.CompletedTask;
        }

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }
    }
}

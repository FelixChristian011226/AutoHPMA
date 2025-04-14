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

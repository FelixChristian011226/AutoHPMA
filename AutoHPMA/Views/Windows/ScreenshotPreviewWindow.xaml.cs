using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AutoHPMA.Helpers;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Helpers.CaptureHelper.CaptureHelper;
using AutoHPMA.Models;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace AutoHPMA.Views.Windows
{
    public partial class ScreenshotPreviewWindow : FluentWindow
    {
        private readonly CaptureMethod _captureMethod;
        private readonly IntPtr _targetWindow;
        private bool _isCapturing = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private WindowsGraphicsCapture? _wgcCapture;
        private Mat? _currentFrame;

        public ScreenshotPreviewWindow(CaptureMethod captureMethod, IntPtr targetWindow)
        {
            InitializeComponent();
            _captureMethod = captureMethod;
            _targetWindow = targetWindow;
            
            Title = $"截屏实时预览 - {captureMethod}";
            StatusText.Text = $"截屏方式: {captureMethod}";
        }

        private async void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCapturing)
            {
                await StartCapture();
            }
            else
            {
                StopCapture();
            }
        }

        private async Task StartCapture()
        {
            try
            {
                _isCapturing = true;
                StartStopButton.Content = "停止预览";
                SaveButton.IsEnabled = false;
                StatusText.Text = "正在启动截屏...";

                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                if (_captureMethod == CaptureMethod.WGC)
                {
                    _wgcCapture = new WindowsGraphicsCapture();
                    _wgcCapture.Start(_targetWindow);
                    await Task.Delay(100, token); // 等待WGC初始化
                }

                StatusText.Text = "截屏中...";
                SaveButton.IsEnabled = true;

                // 开始截屏循环
                await Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            Mat? frame = null;

                            if (_captureMethod == CaptureMethod.WGC && _wgcCapture != null)
                            {
                                frame = _wgcCapture.Capture();
                            }
                            else if (_captureMethod == CaptureMethod.BitBlt)
                            {
                                var bitmap = BitBltCaptureHelper.Capture(_targetWindow);
                                if (bitmap != null)
                                {
                                    frame = bitmap.ToMat();
                                    bitmap.Dispose();
                                }
                            }

                            if (frame != null && !frame.Empty())
                            {
                                // 更新当前帧
                                _currentFrame?.Dispose();
                                _currentFrame = frame.Clone();

                                // 在UI线程更新图像
                                Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        var bitmapSource = frame.ToBitmapSource();
                                        bitmapSource.Freeze();
                                        PreviewImage.Source = bitmapSource;
                                    }
                                    catch (Exception ex)
                                    {
                                        StatusText.Text = $"显示错误: {ex.Message}";
                                    }
                                });

                                frame.Dispose();
                            }

                            // 控制帧率，大约30fps
                            await Task.Delay(33, token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                StatusText.Text = $"截屏错误: {ex.Message}";
                            });
                            await Task.Delay(1000, token);
                        }
                    }
                }, token);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"启动失败: {ex.Message}";
                StopCapture();
            }
        }

        private void StopCapture()
        {
            _isCapturing = false;
            StartStopButton.Content = "开始预览";
            SaveButton.IsEnabled = false;
            StatusText.Text = "已停止";

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _wgcCapture?.Stop();
            _wgcCapture?.Dispose();
            _wgcCapture = null;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFrame == null || _currentFrame.Empty())
            {
                var errorBox = new MessageBox
                {
                    Title = "错误",
                    Content = "没有可保存的截图"
                };
                _ = errorBox.ShowDialogAsync();
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG图片|*.png|JPEG图片|*.jpg|所有文件|*.*",
                    DefaultExt = "png",
                    FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var bitmap = _currentFrame.ToBitmap();
                    var format = Path.GetExtension(saveDialog.FileName).ToLower() switch
                    {
                        ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                        _ => ImageFormat.Png
                    };
                    
                    bitmap.Save(saveDialog.FileName, format);
                    bitmap.Dispose();

                    var successBox = new MessageBox
                    {
                        Title = "成功",
                        Content = $"截图已保存到: {saveDialog.FileName}"
                    };
                    _ = successBox.ShowDialogAsync();
                }
            }
            catch (Exception ex)
            {
                var errorBox = new MessageBox
                {
                    Title = "保存失败",
                    Content = ex.Message
                };
                _ = errorBox.ShowDialogAsync();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            StopCapture();
            _currentFrame?.Dispose();
            base.OnClosed(e);
        }
    }
}
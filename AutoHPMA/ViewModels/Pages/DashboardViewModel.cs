// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.Views;
using AutoHPMA.GameTask;
using AutoHPMA.Views.Windows;
using AutoHPMA.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Diagnostics;
using Wpf.Ui.Controls;
using Microsoft.Extensions.Logging;
using System.Windows.Interop;
using System.Windows.Media;
using System.Net.WebSockets;
using System.Windows.Threading;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {

        private bool _taskDispatcherEnabled = false;
        private DispatcherTimer _syncWindowTimer;
        private DispatcherTimer _captureTimer;

        [ObservableProperty]
        private bool _logWindowEnabled = true;
        [ObservableProperty]
        private int _logWindowLeft = 150;
        [ObservableProperty]
        private int _logWindowTop = 100;

        [ObservableProperty]
        private int _captureInterval = 500;

        [ObservableProperty] private Visibility _startButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _stopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartTriggerCommand))]
        private bool _startButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StopTriggerCommand))]
        private bool _stopButtonEnabled = true;

        private LogWindow? _logWindow; 

        private TaskFlow _taskFlow;

        public static event Action<Bitmap> ScreenshotUpdated;

        private static void OnScreenshotUpdated(Bitmap bmp)
        {
            ScreenshotUpdated?.Invoke(bmp);
        }

        public DashboardViewModel()
        {
            InitializeCaptureTimer();
            InitializeSyncWindowTimer();
            _taskFlow = TaskFlow.Instance();
            //_taskFlow.Init(_logWindow);
        }
        private void InitializeCaptureTimer()
        {
            _captureTimer = new DispatcherTimer();
            _captureTimer.Interval = TimeSpan.FromMilliseconds(_captureInterval);
        }
        private void UpdateCaptureTimer()
        {
            _captureTimer.Interval = TimeSpan.FromMilliseconds(_captureInterval);
        }
        public void InitializeSyncWindowTimer()
        {
            _syncWindowTimer = new DispatcherTimer();
            _syncWindowTimer.Interval = TimeSpan.FromMilliseconds(50);
        }
        private void CaptureTimer_Tick(object? sender, EventArgs e)
        {
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle();
            var mumuChildHwnd = SystemControl.FindChildWindowByTitle(mumuHwnd, "MuMuPlayer");
            if (mumuHwnd != IntPtr.Zero)
            {
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                OnScreenshotUpdated(bmp); // 发布截图更新事件
                string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                Directory.CreateDirectory(folderPath);
                ImageProcessingHelper.SaveBitmapAs(bmp, folderPath,"capture.png", ImageFormat.Png);

                _taskFlow.WorkAsync(mumuChildHwnd, bmp);
            }
        }
        private void SyncWindowTimer_Tick(object? sender, EventArgs e)
        {
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle(); // 获取Mumu模拟器的句柄
            if (mumuHwnd != IntPtr.Zero)
            {
                if (NativeMethodsService.IsIconic(mumuHwnd)) // 如果Mumu模拟器最小化
                {
                    //LogWindow.GetInstance().HideLogWindow();
                    _logWindow?.HideLogWindow();
                }
                else if (NativeMethodsService.GetForegroundWindow()!=mumuHwnd) // 如果Mumu模拟器不在顶层
                {
                    //LogWindow.GetInstance().HideLogWindow();
                    _logWindow?.HideLogWindow();
                }
                else
                {
                    //LogWindow.GetInstance().ShowLogWindow();
                    _logWindow?.ShowLogWindow();
                }
            }
        }

        private bool CanStartTrigger() => StartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStartTrigger))]
        private async Task OnStartTriggerAsync()
        {
            var hWnd = SystemControl.FindMumuSimulatorHandle();
            if (hWnd == IntPtr.Zero)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "未找到Mumu模拟器窗口。\n请先启动Mumu模拟器！",
                };
                var result = await uiMessageBox.ShowDialogAsync();
                return;
            }
            if (!_taskDispatcherEnabled)
            {
                _taskDispatcherEnabled = true;
                StartButtonVisibility = Visibility.Collapsed;
                StopButtonVisibility = Visibility.Visible;

                //int showWindowCommand;
                //if (NativeMethodsService.IsZoomed(hWnd))
                //{
                //    // 如果窗口已经是最大化状态，则只需要将其置于前端
                //    showWindowCommand = NativeMethodsService.SW_SHOW;
                //}
                //else if (NativeMethodsService.IsIconic(hWnd))
                //{
                //    // 如果窗口是最小化的，则恢复
                //    showWindowCommand = NativeMethodsService.SW_RESTORE;
                //}
                //else
                //{
                //    // 如果窗口既不是最小化也不是最大化，则不做状态改变，只尝试置于前端
                //    showWindowCommand = NativeMethodsService.SW_SHOW;
                //}
                //// 从最小化状态恢复窗口并试图将其置于前端。
                //NativeMethodsService.ShowWindow(hWnd, showWindowCommand);
                //NativeMethodsService.SetForegroundWindow(hWnd);

                // 隐藏WPF应用的主窗口。基于你的项目设置，可能需要调整获取主窗口的方式。
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.MainWindow.Hide();
                });

                UpdateCaptureTimer();
                _captureTimer.Tick += CaptureTimer_Tick;
                _captureTimer.Start();
                
                _taskDispatcherEnabled = true;
                if (_logWindowEnabled)
                {
                    _syncWindowTimer.Tick += SyncWindowTimer_Tick;
                    _syncWindowTimer.Start();
                    _logWindow = LogWindow.Instance();
                    _taskFlow.Init(_logWindow);
                    _logWindow.ShowInTaskbar = false;
                    _logWindow.Owner = GetMumuSimulatorWindow(); // 将Mumu模拟器窗口设置为LogWindow的Owner
                    _logWindow.RefreshPosition(hWnd, _logWindowLeft, _logWindowTop);
                    _logWindow.AddLogMessage("INF","---日志窗口已启动---"); // 添加日志消息
                    //for (int i = 0; i < 100; i++) { _logWindow.AddLogMessage("INF", "消息" + i); }
                }

            }

        }

        private bool CanStopTrigger() => StopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStopTrigger))]
        private void OnStopTrigger()
        {
            if (_taskDispatcherEnabled)
            {
                _taskDispatcherEnabled = false;
                StartButtonVisibility = Visibility.Visible;
                StopButtonVisibility = Visibility.Collapsed;

                _logWindow?.Close();

                _captureTimer.Stop();
                _captureTimer.Tick -= CaptureTimer_Tick;

                _syncWindowTimer.Stop();
                _syncWindowTimer.Tick -= SyncWindowTimer_Tick;

                _taskFlow.Reset();
            }
        }

        [RelayCommand]
        public void OnGoToWikiUrl()
        {
            Process.Start(new ProcessStartInfo("https://github.com/FelixChristian011226/AutoHPMA") { UseShellExecute = true });
        }

        [RelayCommand]
        public async void OnGoToScreenshotPage(object sender)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "提示",
                Content = "请进入截屏页面",
            };
            var result = await uiMessageBox.ShowDialogAsync();
        }

        public void OnNavigatedFrom()
        {
            //throw new NotImplementedException();
        }

        public void OnNavigatedTo()
        {
            //throw new NotImplementedException();
        }

        private Window GetMumuSimulatorWindow()
        {
            var hWnd = SystemControl.FindMumuSimulatorHandle();
            if (hWnd != IntPtr.Zero)
            {
                var hwndSource = HwndSource.FromHwnd(hWnd);
                if (hwndSource != null)
                {
                    var rootVisual = hwndSource.RootVisual;
                    if (rootVisual != null)
                    {
                        var parent = VisualTreeHelper.GetParent(rootVisual);
                        if (parent is Window window)
                        {
                            return window;
                        }
                    }
                }
            }
            return null;
        }

    }
}

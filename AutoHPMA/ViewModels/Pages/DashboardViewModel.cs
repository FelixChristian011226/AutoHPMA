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
using System.Collections.ObjectModel;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutoHPMA.ViewModels.Pages
{

    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {

        private DispatcherTimer _syncWindowTimer;
        private DispatcherTimer _captureTimer;

        [ObservableProperty]
        private bool _realTimeScreenshotEnabled = true;

        [ObservableProperty]
        private bool _logWindowEnabled = true;

        [ObservableProperty]
        private bool _debugLogEnabled = true;

        [ObservableProperty]
        private int _captureInterval = 500;

        [ObservableProperty] private Visibility _startButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _stopButtonVisibility = Visibility.Collapsed;
        [ObservableProperty] private Visibility _autoClubQuizStartButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _autoClubQuizStopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartTriggerCommand))]
        private bool _startButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StopTriggerCommand))]
        private bool _stopButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoClubQuizStartTriggerCommand))]
        private bool _autoClubQuizStartButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoClubQuizStopTriggerCommand))]
        private bool _autoClubQuizStopButtonEnabled = true;

        private AutoClubQuiz? _autoClubQuiz;

        private LogWindow? _logWindow;
        private int _logWindowLeft = 0;
        private int _logWindowTop = 0;

        private GraphicsCapture capture;

        private IntPtr _displayHwnd,_gameHwnd;

        private enum StartupOption
        {
            OfficialLauncher,
            MumuSimulator,
            None
        }

        private StartupOption _startupOption = StartupOption.None;

        public static event Action<Bitmap> ScreenshotUpdated;

        private static void OnScreenshotUpdated(Bitmap bmp)
        {
            ScreenshotUpdated?.Invoke(bmp);
        }

        public DashboardViewModel()
        {
            InitializeCaptureTimer();
            InitializeSyncWindowTimer();
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
            _syncWindowTimer.Interval = TimeSpan.FromMilliseconds(100);
        }
        private void CaptureTimer_Tick(object? sender, EventArgs e)
        {
            if (_gameHwnd != IntPtr.Zero)
            {
                if(_realTimeScreenshotEnabled)
                {
                    //Bitmap bmp = ScreenCaptureHelper.CaptureWindow(_targetHwnd);
                    Mat? frame = capture?.Capture();
                    Bitmap? bmp = frame?.ToBitmap();

                    //string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                    //Directory.CreateDirectory(folderPath);
                    //ImageProcessingHelper.SaveBitmapAs(bmp, folderPath, "TEST" + ".png", ImageFormat.Png);

                    OnScreenshotUpdated(bmp); // 发布截图更新事件
                }
            }
        }
        private void SyncWindowTimer_Tick(object? sender, EventArgs e)
        {
            if (_gameHwnd != IntPtr.Zero)
            {
                if (NativeMethodsService.IsIconic(_gameHwnd)) // 最小化
                {
                    _logWindow?.HideLogWindow();
                }
                else if (NativeMethodsService.GetForegroundWindow()!= _displayHwnd) // 由于Mumu模拟器有两个句柄，真正的游戏窗口句柄是子句柄，而且不在顶层，所以需要父句柄
                {
                    _logWindow?.HideLogWindow();
                }
                else
                {
                    _logWindow?.ShowLogWindow();
                }
                _logWindow?.RefreshPosition(_gameHwnd, _logWindowLeft, _logWindowTop);
            }
        }

        private bool CanStartTrigger() => StartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStartTrigger))]
        private void OnStartTrigger()
        {
            _displayHwnd = SystemControl.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");
            if (_displayHwnd != IntPtr.Zero)
            {
                _startupOption = StartupOption.MumuSimulator;
                _gameHwnd = SystemControl.FindChildWindowByTitle(_displayHwnd, "MuMuPlayer");
            }
            else
            {
                _gameHwnd = SystemControl.FindHandleByProcessName("哈利波特：魔法觉醒", "Harry Potter Magic Awakened");
                if (_gameHwnd != IntPtr.Zero)
                {
                    _startupOption = StartupOption.OfficialLauncher;
                }
            }

            if (_gameHwnd == IntPtr.Zero)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "未找到游戏窗口。\n请先启动游戏！",
                };
                var result = uiMessageBox.ShowDialogAsync();
                return;
            }

            StartButtonVisibility = Visibility.Collapsed;
            StopButtonVisibility = Visibility.Visible;

            // 当官方启动器时，将游戏窗口置于前端
            if(_startupOption == StartupOption.OfficialLauncher)
            {
                NativeMethodsService.ShowWindow(_gameHwnd, NativeMethodsService.SW_SHOW);
                NativeMethodsService.SetForegroundWindow(_gameHwnd);
            }

            // 隐藏WPF应用的主窗口。
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.MainWindow.Hide();
            });

            UpdateCaptureTimer();
            _captureTimer.Tick += CaptureTimer_Tick;
            _captureTimer.Start();

            // 启动日志窗口
            if (_logWindowEnabled)
            {
                _syncWindowTimer.Tick += SyncWindowTimer_Tick;
                _syncWindowTimer.Start();
                _logWindow = LogWindow.Instance();

                _logWindow.ShowInTaskbar = false;   //在ALT+TAB中不显示
                _logWindow.Owner = GetGameWindow(); // 将游戏窗口设置为LogWindow的Owner
                _logWindow.RefreshPosition(_gameHwnd, _logWindowLeft, _logWindowTop);
                _logWindow.ShowDebugLogs = DebugLogEnabled;
                _logWindow.AddLogMessage("INF",_startupOption+": 已启动");
                ShowGameWindowInfo();
            }

            capture = new GraphicsCapture();
            capture.Start(_displayHwnd);

        }

        private bool CanStopTrigger() => StopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStopTrigger))]
        private void OnStopTrigger()
        {
            StartButtonVisibility = Visibility.Visible;
            StopButtonVisibility = Visibility.Collapsed;

            _logWindow?.Close();

            _captureTimer.Tick -= CaptureTimer_Tick;
            _captureTimer.Stop();

            _syncWindowTimer.Tick -= SyncWindowTimer_Tick;
            _syncWindowTimer.Stop();

            capture.Stop();
            capture = null;
            GC.Collect();

        }


        private bool CanAutoClubQuizStartTrigger() => AutoClubQuizStartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStartTrigger))]
        private void OnAutoClubQuizStartTrigger()
        {
            AutoClubQuizStartButtonVisibility = Visibility.Collapsed;
            AutoClubQuizStopButtonVisibility = Visibility.Visible;
            _logWindow?.AddLogMessage("INF", "社团答题任务已启动");

            _autoClubQuiz = new AutoClubQuiz(_displayHwnd, _gameHwnd);
            _autoClubQuiz.Start();

        }

        private bool CanAutoClubQuizStopTrigger() => AutoClubQuizStopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStopTrigger))]
        private void OnAutoClubQuizStopTrigger()
        {
            AutoClubQuizStartButtonVisibility = Visibility.Visible;
            AutoClubQuizStopButtonVisibility = Visibility.Collapsed;
            _logWindow?.AddLogMessage("INF", "社团答题任务已终止");

            _autoClubQuiz?.Stop();
            _autoClubQuiz = null;
        }


        [RelayCommand]
        public void OnGoToWikiUrl()
        {
            Process.Start(new ProcessStartInfo("https://felixchristian011226.github.io/AutoHPMA-Web/") { UseShellExecute = true });
        }

        [RelayCommand]
        public async void OnRealTimeScreenshot(object sender)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "⚠ 提示",
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

        private System.Windows.Window GetGameWindow()
        {
            var hWnd = _gameHwnd;
            if (hWnd != IntPtr.Zero)
            {
                var hwndSource = HwndSource.FromHwnd(hWnd);
                if (hwndSource != null)
                {
                    var rootVisual = hwndSource.RootVisual;
                    if (rootVisual != null)
                    {
                        var parent = VisualTreeHelper.GetParent(rootVisual);
                        if (parent is System.Windows.Window window)
                        {
                            return window;
                        }
                    }
                }
            }
            return null;
        }

        private void ShowGameWindowInfo()
        {
            try
            {
                int left, top, width, height;
                int leftMumu, topMumu;
                WindowInteractionHelper.GetWindowPositionAndSize(_gameHwnd, out left, out top, out width, out height);
                _logWindow.AddLogMessage("INF", "检测到游戏窗口分辨率"+width+"*"+height);
                _logWindow.AddLogMessage("DBG", "游戏窗口位置：左上角(" + left + "," + top + ")");
                WindowInteractionHelper.GetWindowPositionAndSize(_displayHwnd, out leftMumu, out topMumu, out width, out height);
                _logWindow.AddLogMessage("DBG", "检测到模拟器窗口分辨率" + width + "*" + height);
                _logWindow.AddLogMessage("DBG", "模拟器窗口位置：左上角(" + leftMumu + "," + topMumu + ")");

                _logWindow.AddLogMessage("DBG", "坐标偏移量: (" + (left - leftMumu) + "," + (top - topMumu) + ")");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取窗口信息失败：{ex.Message}");
            }
        }

    }
}

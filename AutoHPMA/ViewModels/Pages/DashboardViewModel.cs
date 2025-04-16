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
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Services;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using AutoHPMA.Messages;

namespace AutoHPMA.ViewModels.Pages
{

    public partial class DashboardViewModel : ObservableObject
    {

        private DispatcherTimer _syncWindowTimer;
        private DispatcherTimer _captureTimer;

        private Bitmap? bmp;

        # region Observable Properties
        [ObservableProperty]
        private bool _realTimeScreenshotEnabled = true;

        [ObservableProperty]
        private bool _logWindowEnabled = true;

        [ObservableProperty]
        private bool _debugLogEnabled = false;

        [ObservableProperty]
        private int _captureInterval = 500;

        [ObservableProperty] 
        private Visibility _startButtonVisibility = Visibility.Visible;
        [ObservableProperty] 
        private Visibility _stopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartTriggerCommand))]
        private bool _startButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StopTriggerCommand))]
        private bool _stopButtonEnabled = true;
        #endregion

        # region argument from AppContextService
        private LogWindow? _logWindow
        {
            get => AppContextService.Instance.LogWindow;
            set
            {
                AppContextService.Instance.LogWindow = value;
                OnPropertyChanged(nameof(_logWindow));
            }
        }
        private int _logWindowLeft = 0;
        private int _logWindowTop = 0;

        private WindowsGraphicsCapture _capture
        {
            get => AppContextService.Instance.Capture;
            set
            {
                AppContextService.Instance.Capture = value;
                OnPropertyChanged(nameof(_capture));
            }
        }

        private IntPtr _displayHwnd
        {
            get => AppContextService.Instance.DisplayHwnd;
            set
            {
                AppContextService.Instance.DisplayHwnd = value;
                OnPropertyChanged(nameof(_displayHwnd));
            }
        }

        private IntPtr _gameHwnd
        {
            get => AppContextService.Instance.GameHwnd;
            set
            {
                AppContextService.Instance.GameHwnd = value;
                OnPropertyChanged(nameof(_gameHwnd));
            }
        }
        #endregion

        private enum StartupOption
        {
            OfficialLauncher,
            MumuSimulator,
            None
        }
        private StartupOption _startupOption = StartupOption.None;

        public static event Action<Bitmap> ScreenshotUpdated;

        private static void OnScreenshotUpdated(ref Bitmap bmp)
        {
            ScreenshotUpdated?.Invoke(bmp);
        }

        public DashboardViewModel()
        {
        }

        private void InitializeCaptureTimer()
        {
            _captureTimer = new DispatcherTimer();
            _captureTimer.Interval = TimeSpan.FromMilliseconds(_captureInterval);
        }
        public void InitializeSyncWindowTimer()
        {
            _syncWindowTimer = new DispatcherTimer();
            _syncWindowTimer.Interval = TimeSpan.FromMilliseconds(50);
        }
        private void CaptureTimer_Tick(object? sender, EventArgs e)
        {
            if (_gameHwnd != IntPtr.Zero && _realTimeScreenshotEnabled)
            {
                bmp?.Dispose();
                bmp = null;

                using (var frame = _capture?.Capture())
                {
                    if (frame != null)
                    {
                        bmp = frame.ToBitmap();
                        OnScreenshotUpdated(ref bmp);
                    }
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
            GetGameHwnd();

            if (_gameHwnd == IntPtr.Zero)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "⚠️ 错误",
                    Content = "未找到游戏窗口。请先启动游戏！",
                    
                };
                _ = uiMessageBox.ShowDialogAsync();
                return;
            } 
            else
            {
                var snackbarInfo = new SnackbarInfo
                {
                    Title = "启动成功",
                    Message = "截图器已启动，可启动其他任务。",
                    Appearance = ControlAppearance.Success,
                    Icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24, 36),
                    Duration = TimeSpan.FromSeconds(3),
                };
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage(snackbarInfo));
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

            InitializeCaptureTimer();
            InitializeSyncWindowTimer();

            _captureTimer.Tick += CaptureTimer_Tick;
            _captureTimer.Start();

            // 启动日志窗口
            if (_logWindowEnabled)
            {
                _syncWindowTimer.Tick += SyncWindowTimer_Tick;
                _syncWindowTimer.Start();

                _logWindow = new LogWindow();
                _logWindow.Show();
                _logWindow.ShowInTaskbar = false;   //在ALT+TAB中不显示
                //_logWindow.Owner = GetWindow(_gameHwnd); // 将游戏窗口设置为LogWindow的Owner
                _logWindow.RefreshPosition(_gameHwnd, _logWindowLeft, _logWindowTop);
                _logWindow.ShowDebugLogs = DebugLogEnabled;
                _logWindow.AddLogMessage("INF", "检测到[Yellow]" + _startupOption+ "[/Yellow]已启动");
                ShowGameWindowInfo();
            }

            _capture = new WindowsGraphicsCapture();
            _capture.Start(_displayHwnd);

        }

        private bool CanStopTrigger() => StopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStopTrigger))]
        private void OnStopTrigger()
        {
            StartButtonVisibility = Visibility.Visible;
            StopButtonVisibility = Visibility.Collapsed;

            _logWindow?.Close();
            _logWindow = null;

            _captureTimer.Tick -= CaptureTimer_Tick;
            _captureTimer.Stop();

            _syncWindowTimer.Tick -= SyncWindowTimer_Tick;
            _syncWindowTimer.Stop();

            _capture.Stop();
            _capture = null;
            GC.Collect();

        }


        [RelayCommand]
        public void OnGoToWikiUrl()
        {
            Process.Start(new ProcessStartInfo("https://www.felixchristian.top/2025/04/04/16-AutoHPMA/") { UseShellExecute = true });
        }

        public void OnNavigatedFrom()
        {
            //throw new NotImplementedException();
        }

        public void OnNavigatedTo()
        {
            //throw new NotImplementedException();
        }

        /**
         * @brief 获取游戏窗口
         * @param hWnd 窗口句柄
         * @return System.Windows.Window 窗口对象
         */
        private System.Windows.Window GetWindow(IntPtr hWnd)
        {
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
            throw new InvalidOperationException("无法获取窗口对象");
        }

        /**
         * @brief 获取游戏窗口句柄（Mumu模拟器或官方启动器）
         * @param void
         * @return void
         * @note 直接对成员变量赋值
         * _displayHwnd：模拟器窗口句柄
         * _gameHwnd：游戏窗口句柄（若为Mumu模拟器，则为子句柄）
         * _startupOption：启动选项（Mumu模拟器或官方启动器）
         */
        private void GetGameHwnd()
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
        }

        /**
         * @brief 获取游戏窗口信息
         * @param void
         * @return void
         * @note 输出方式为日志窗口
         * 输出信息包括：
         * 游戏窗口分辨率和坐标
         * Mumu模拟器窗口分辨率和坐标
         */
        private void ShowGameWindowInfo()
        {
            try
            {
                int left, top, width, height;
                int leftMumu, topMumu;
                WindowInteractionHelper.GetWindowPositionAndSize(_gameHwnd, out left, out top, out width, out height);
                _logWindow.AddLogMessage("INF", "检测到游戏窗口分辨率 [Yellow]" + width+"*"+height+ "[/Yellow]");
                _logWindow.AddLogMessage("DBG", "游戏窗口位置：左上角(" + left + "," + top + ")");
                WindowInteractionHelper.GetWindowPositionAndSize(_displayHwnd, out leftMumu, out topMumu, out width, out height);
                _logWindow.AddLogMessage("DBG", "检测到模拟器窗口分辨率" + width + "*" + height);
                _logWindow.AddLogMessage("DBG", "模拟器窗口位置：左上角(" + leftMumu + "," + topMumu + ")");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取窗口信息失败：{ex.Message}");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }




    }
}

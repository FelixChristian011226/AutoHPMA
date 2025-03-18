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

namespace AutoHPMA.ViewModels.Pages
{
    public enum StartupOption
    {
        MumuSimulator,
        MumuSimulator1080,
        OfficialLauncher
    }

    public class StartupOptionItem
    {
        public StartupOption Option { get; set; }
        public string DisplayName { get; set; }
    }

    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        public ObservableCollection<StartupOptionItem> StartupOptions { get; } = new ObservableCollection<StartupOptionItem>
        {
            new StartupOptionItem { Option = StartupOption.MumuSimulator, DisplayName = "Mumu模拟器" },
            new StartupOptionItem { Option = StartupOption.MumuSimulator1080, DisplayName = "Mumu1920*1080" },
            new StartupOptionItem { Option = StartupOption.OfficialLauncher, DisplayName = "官方启动器" }
        };

        [ObservableProperty]
        private StartupOptionItem _selectedStartupOption;

        private bool _taskDispatcherEnabled = false;
        private DispatcherTimer _syncWindowTimer;
        private DispatcherTimer _captureTimer;

        [ObservableProperty]
        private bool _realTimeScreenshotEnabled = true;

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

        private IntPtr _targetHwnd;

        public static event Action<Bitmap> ScreenshotUpdated;

        private static void OnScreenshotUpdated(Bitmap bmp)
        {
            ScreenshotUpdated?.Invoke(bmp);
        }

        public DashboardViewModel()
        {
            _selectedStartupOption = StartupOptions[0];
            InitializeCaptureTimer();
            InitializeSyncWindowTimer();
            _taskFlow = TaskFlow.Instance();
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
            IntPtr _taskHwnd = IntPtr.Zero;
            if (SelectedStartupOption.Option == StartupOption.OfficialLauncher)
                _taskHwnd = _targetHwnd;
            else
                _taskHwnd = SystemControl.FindChildWindowByTitle(_targetHwnd, "MuMuPlayer");
            if (_targetHwnd != IntPtr.Zero)
            {
                if(_realTimeScreenshotEnabled)
                {
                    //Bitmap bmp = ScreenCaptureHelper.CaptureWindow(_targetHwnd);
                    Bitmap bmp = BitBltCaptureHelper.Capture(_targetHwnd);
                    OnScreenshotUpdated(bmp); // 发布截图更新事件
                }

                _taskFlow.WorkAsync(_taskHwnd, _targetHwnd);
            }
        }
        private void SyncWindowTimer_Tick(object? sender, EventArgs e)
        {
            if (_targetHwnd != IntPtr.Zero)
            {
                if (NativeMethodsService.IsIconic(_targetHwnd)) // 最小化
                {
                    //LogWindow.GetInstance().HideLogWindow();
                    _logWindow?.HideLogWindow();
                }
                else if (NativeMethodsService.GetForegroundWindow()!= _targetHwnd) // 不在顶层
                {
                    //LogWindow.GetInstance().HideLogWindow();
                    _logWindow?.HideLogWindow();
                }
                else
                {
                    //LogWindow.GetInstance().ShowLogWindow();
                    _logWindow?.ShowLogWindow();
                }
                _logWindow?.RefreshPosition(_targetHwnd, _logWindowLeft, _logWindowTop);
            }
        }

        private bool CanStartTrigger() => StartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStartTrigger))]
        private async Task OnStartTriggerAsync()
        {
            if (SelectedStartupOption.Option == StartupOption.OfficialLauncher)
                _targetHwnd = SystemControl.FindHandleByProcessName("哈利波特：魔法觉醒", "Harry Potter Magic Awakened");
            else
                _targetHwnd = SystemControl.FindHandleByProcessName("Mumu模拟器", "MuMuPlayer");

            if (_targetHwnd == IntPtr.Zero)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "未找到游戏窗口。\n请先启动游戏！",
                };
                var result = await uiMessageBox.ShowDialogAsync();
                return;
            }
            if (!_taskDispatcherEnabled)
            {
                _taskDispatcherEnabled = true;
                StartButtonVisibility = Visibility.Collapsed;
                StopButtonVisibility = Visibility.Visible;

                // 当官方启动器时，将游戏窗口置于前端
                if(SelectedStartupOption.Option == StartupOption.OfficialLauncher)
                {
                    NativeMethodsService.ShowWindow(_targetHwnd, NativeMethodsService.SW_SHOW);
                    NativeMethodsService.SetForegroundWindow(_targetHwnd);
                }
                //int showWindowCommand;
                //if (NativeMethodsService.IsZoomed(_targetHwnd))
                //{
                //    // 如果窗口已经是最大化状态，则只需要将其置于前端
                //    showWindowCommand = NativeMethodsService.SW_SHOW;
                //}
                //else if (NativeMethodsService.IsIconic(_targetHwnd))
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
                //NativeMethodsService.ShowWindow(_targetHwnd, showWindowCommand);
                //NativeMethodsService.SetForegroundWindow(_targetHwnd);

                // 隐藏WPF应用的主窗口。
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
                    int op = 0;
                    if (SelectedStartupOption.Option == StartupOption.OfficialLauncher)
                        op = 0;
                    else if (SelectedStartupOption.Option == StartupOption.MumuSimulator)
                        op = 1;
                    else if (SelectedStartupOption.Option == StartupOption.MumuSimulator1080)
                        op = 2;
                    _taskFlow.Init(_logWindow, op);
                    _taskFlow.Reset();

                    _logWindow.ShowInTaskbar = false;
                    //_logWindow.Owner = GetMumuSimulatorWindow(); // 将Mumu模拟器窗口设置为LogWindow的Owner
                    _logWindow.Owner = GetGameWindow(); // 将Mumu模拟器窗口设置为LogWindow的Owner
                    _logWindow.RefreshPosition(_targetHwnd, _logWindowLeft, _logWindowTop);
                    _logWindow.AddLogMessage("INF","---日志窗口已启动---");
                    //if (SelectedStartupOption.Option == StartupOption.MumuSimulator)
                    //    _logWindow.AddLogMessage("INF", "MumuSimulator");
                    //else
                    //    _logWindow.AddLogMessage("INF", "Official Launcher");
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

                _captureTimer.Tick -= CaptureTimer_Tick;
                _captureTimer.Stop();

                _syncWindowTimer.Tick -= SyncWindowTimer_Tick;
                _syncWindowTimer.Stop();

                _taskFlow.Stop();
            }
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

        private Window GetGameWindow()
        {
            //var hWnd = SystemControl.FindMumuSimulatorHandle();
            var hWnd = _targetHwnd;
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

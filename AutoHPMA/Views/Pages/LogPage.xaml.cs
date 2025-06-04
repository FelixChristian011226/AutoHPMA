using AutoHPMA.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AutoHPMA.Views.Pages
{
    public partial class LogPage : INavigableView<LogViewModel>
    {
        public LogViewModel ViewModel { get; }
        private bool _isInitialFocus = true;
        private DispatcherTimer _focusTimer;

        public LogPage(LogViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            
            // 添加页面加载事件
            Loaded += LogPage_Loaded;
            
            // 添加全局焦点变化事件
            FocusManager.AddGotFocusHandler(this, OnGotFocus);

            // 初始化焦点恢复定时器
            _focusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // 500毫秒后恢复焦点
            };
            _focusTimer.Tick += (s, e) =>
            {
                _focusTimer.Stop();
                if (IsLoaded) // 确保页面仍然加载
                {
                    LogTextBox.Focus();
                }
            };
        }

        private void LogPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 页面加载时设置焦点
            LogTextBox.Focus();
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            // 如果是初始焦点设置，则跳过
            if (_isInitialFocus)
            {
                _isInitialFocus = false;
                return;
            }

            // 如果获得焦点的不是LogTextBox，则启动定时器
            if (e.Source != LogTextBox)
            {
                _focusTimer.Stop(); // 停止之前的定时器（如果有）
                _focusTimer.Start(); // 启动新的定时器
            }
        }
    }
} 
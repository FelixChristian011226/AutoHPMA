using AutoHPMA.GameTask;
using AutoHPMA.Helpers.CaptureHelper;
using AutoHPMA.Messages;
using AutoHPMA.Services;
using AutoHPMA.Views.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace AutoHPMA.ViewModels.Pages
{

    public partial class TaskViewModel : ObservableObject, INavigationAware
    {

        #region Observable Properties
        [ObservableProperty] 
        private Visibility _autoClubQuizStartButtonVisibility = Visibility.Visible;
        [ObservableProperty] 
        private Visibility _autoClubQuizStopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoClubQuizStartTriggerCommand))]
        private bool _autoClubQuizStartButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoClubQuizStopTriggerCommand))]
        private bool _autoClubQuizStopButtonEnabled = true;

        [ObservableProperty]
        private int _answerDelay = 0;

        #endregion

        private IntPtr _displayHwnd => AppContextService.Instance.DisplayHwnd;
        private IntPtr _gameHwnd => AppContextService.Instance.GameHwnd;
        private LogWindow? _logWindow => AppContextService.Instance.LogWindow;
        private WindowsGraphicsCapture _capture => AppContextService.Instance.Capture;


        private AutoClubQuiz? _autoClubQuiz;
        private AppContextService appContextService;

        public TaskViewModel()
        {
            // 获取单例实例
            appContextService = AppContextService.Instance;
            // 订阅属性变化通知
            appContextService.PropertyChanged += AppContextService_PropertyChanged;

        }

        private void AppContextService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppContextService.LogWindow) ||
                e.PropertyName == nameof(AppContextService.DisplayHwnd) ||
                e.PropertyName == nameof(AppContextService.GameHwnd) ||
                e.PropertyName == nameof(AppContextService.Capture))
            {
                // 当共享数据有更新时执行相应操作
                // CheckRequiredParameters();
            }
        }

        private bool CanAutoClubQuizStartTrigger() => AutoClubQuizStartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStartTrigger))]
        private void OnAutoClubQuizStartTrigger()
        {

            if (_gameHwnd == IntPtr.Zero || _displayHwnd == IntPtr.Zero || _capture == null || _logWindow == null)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "⚠️ 错误",
                    Content = "任务启动失败。请先启动截图器!",
                };
                var result = uiMessageBox.ShowDialogAsync();
                return;
            }
            else
            {
                var snackbarInfo = new SnackbarInfo
                {
                    Title = "启动成功",
                    Message = "自动社团答题已启动。",
                    Appearance = ControlAppearance.Success,
                    Icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                    Duration = TimeSpan.FromSeconds(3)
                };
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage(snackbarInfo));
            }

            AutoClubQuizStartButtonVisibility = Visibility.Collapsed;
            AutoClubQuizStopButtonVisibility = Visibility.Visible;
            _logWindow?.AddLogMessage("INF", "[Aquamarine]---社团答题任务已启动---[/Aquamarine]");

            //_autoClubQuiz = new AutoClubQuiz(_displayHwnd, _gameHwnd);
            //_autoClubQuiz.SetAnswerDelay(_answerDelay);
            //_autoClubQuiz.Start();

        }

        private bool CanAutoClubQuizStopTrigger() => AutoClubQuizStopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStopTrigger))]
        private void OnAutoClubQuizStopTrigger()
        {
            AutoClubQuizStartButtonVisibility = Visibility.Visible;
            AutoClubQuizStopButtonVisibility = Visibility.Collapsed;
            _logWindow?.AddLogMessage("INF", "[Aquamarine]---社团答题任务已终止---[/Aquamarine]");

            _autoClubQuiz?.Stop();
            _autoClubQuiz = null;
            GC.Collect();
        }


        public Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
    }
}

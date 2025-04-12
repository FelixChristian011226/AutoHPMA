using AutoHPMA.GameTask;
using AutoHPMA.Helpers;
using AutoHPMA.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Abstractions.Controls;

namespace AutoHPMA.ViewModels.Pages
{

    public partial class TaskViewModel : ObservableObject, INavigationAware
    {

        [ObservableProperty] private Visibility _autoClubQuizStartButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _autoClubQuizStopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoClubQuizStartTriggerCommand))]
        private bool _autoClubQuizStartButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AutoClubQuizStopTriggerCommand))]
        private bool _autoClubQuizStopButtonEnabled = true;


        private LogWindow? _logWindow;
        private GraphicsCapture capture;
        private AutoClubQuiz? _autoClubQuiz;
        private IntPtr _displayHwnd, _gameHwnd;


        private bool CanAutoClubQuizStartTrigger() => AutoClubQuizStartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanAutoClubQuizStartTrigger))]
        private void OnAutoClubQuizStartTrigger()
        {

            _displayHwnd = App._displayHwnd;
            _gameHwnd = App._gameHwnd;
            _logWindow = LogWindow.GetInstance();

            if (_gameHwnd == IntPtr.Zero || _displayHwnd == IntPtr.Zero)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "错误",
                    Content = "任务启动失败，请先在启动页面启动截图器",
                };
                var result = uiMessageBox.ShowDialogAsync();
                return;
            }

            AutoClubQuizStartButtonVisibility = Visibility.Collapsed;
            AutoClubQuizStopButtonVisibility = Visibility.Visible;
            _logWindow?.AddLogMessage("INF", "[Aquamarine]---社团答题任务已启动---[/Aquamarine]");

            _autoClubQuiz = new AutoClubQuiz(_displayHwnd, _gameHwnd);
            _autoClubQuiz.Start();

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
